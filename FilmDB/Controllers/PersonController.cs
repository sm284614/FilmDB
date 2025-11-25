using FilmDB.Data;
using FilmDB.Models;
using FilmDB.Models.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace FilmDB.Controllers
{
    //these should all be optimised by now: using AsNoTracking and reduced to remove subqueries
    public class PersonController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly static int NumberOfPeople = 16;
        public PersonController(ApplicationDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)]
        public IActionResult Person()
        {
            var personList = _cache.GetOrCreate("PersonList", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
                // Get the first n (16) people ordered by name
                var people = _db.Person
                    .AsNoTracking()
                    .Take(NumberOfPeople)
                    .ToList();
                // Get all their first film IDs
                var firstFilmIds = people.Select(p => p.FirstFilmId).ToList();
                // Fetch all first films
                var firstFilms = _db.Film
                    .AsNoTracking()
                    .Where(f => firstFilmIds.Contains(f.FilmId))
                    .ToDictionary(f => f.FilmId);
                // Get film counts for all these people in one query
                var filmCounts = _db.Film_Person
                    .AsNoTracking()
                    .Where(fp => people.Select(p => p.PersonId).Contains(fp.PersonId))
                    .GroupBy(fp => fp.PersonId)
                    .Select(g => new { PersonId = g.Key, Count = g.Count() })
                    .ToDictionary(x => x.PersonId, x => x.Count);
                // Combine everything
                return people.Select(p => new PersonFilm
                {
                    Person = p,
                    Film = firstFilms.ContainsKey(p.FirstFilmId)
                        ? firstFilms[p.FirstFilmId]
                        : new Film(),
                    Count = filmCounts.ContainsKey(p.PersonId)
                        ? filmCounts[p.PersonId]
                        : 0
                }).ToList();
            }) ?? new List<PersonFilm>();
            return View(personList);
        }
        public IActionResult PersonSearch(string query)
        {
            // if we're doing this a lot, we can alter person model to include film count and first film name (as well as firstfilmid)
            // after adding those fields, we'd need to update them on film_person inserts (as we're not adding anything here though...)
            query = query.Trim();
            // Query to get people, their first film, and total film count
            var personFilms = _db.Person
                .Where(p => p.Name.Contains(query)) // Filter people based on the query
                .Select(p => new PersonFilm
                {
                    Person = p,
                    Film = _db.Film.FirstOrDefault(f => f.FilmId == p.FirstFilmId) ?? new Film(), // Get the first film
                    Count = _db.Film_Person.Count(fp => fp.PersonId == p.PersonId) // Count total films for the person
                })
                .OrderByDescending(pf => pf.Count) // Sort by person name
                .Take(100) // Limit to 100 results for pagination
                .ToList();

            // Set the result data in the ViewBag
            ViewBag.ResultData = $"Showing {personFilms.Count} results for '{query}'";

            // Return the view with the result data
            return View("Person", personFilms);
        }
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)]
        public IActionResult PersonFilmography(string id)
        {
            // Single query to get person
            var person = _db.Person
                .AsNoTracking()
                .FirstOrDefault(p => p.PersonId == id);
            if (person == null)
            {
                return NotFound();
            }
            // Query filmography: films featuring the given personid
            var filmJobs = (from f in _db.Film.AsNoTracking()
                            join fp in _db.Film_Person on f.FilmId equals fp.FilmId
                            join j in _db.Job on fp.JobId equals j.JobId
                            where fp.PersonId == id
                            orderby f.Year descending, j.Title ascending
                            select new PersonFilmJobDetail
                            {
                                FilmId = f.FilmId,
                                FilmTitle = f.Title,
                                FilmYear = f.Year,
                                JobTitle = j.Title
                            })
                            .ToList();
            var model = new PersonFilmography
            {
                Person = person,
                FilmJobs = filmJobs
            };
            return View(model);
        }
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)]
        public IActionResult Job()
        {
            var jobList = _cache.GetOrCreate("JobList", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
                return _db.Job
                    .AsNoTracking()
                    .OrderBy(j => j.Title)
                    .ToList();
            });
            return View("Job", jobList);
        }
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client)]
        public IActionResult JobCount(int id)
        {
            // Retrieve the job details
            var job = _db.Job
                .AsNoTracking()
                .FirstOrDefault(j => j.JobId == id);
            if (job == null)
            {
                return NotFound();
            }
            var cacheKey = $"JobCount_{id}";
            var personJobCounts = _cache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6);
                // Step 1: Get person stats (counts and year ranges)
                var personStats = (from fp in _db.Film_Person.AsNoTracking()
                                   where fp.JobId == id
                                   join f in _db.Film.AsNoTracking() on fp.FilmId equals f.FilmId
                                   group f by fp.PersonId into g
                                   select new
                                   {
                                       PersonId = g.Key,
                                       JobCount = g.Count(),
                                       EarliestYear = g.Min(f => f.Year),
                                       LatestYear = g.Max(f => f.Year)
                                   })
                                  .OrderByDescending(x => x.JobCount)
                                  .Take(100)
                                  .ToList();
                // Step 2: Get person details for the top 100
                var personIds = personStats.Select(x => x.PersonId).ToList();
                var people = _db.Person
                    .AsNoTracking()
                    .Where(p => personIds.Contains(p.PersonId))
                    .ToDictionary(p => p.PersonId);
                // Step 3: Combine the data
                return personStats
                    .Select(stat => new PersonJobSummary
                    {
                        Person = people.ContainsKey(stat.PersonId) ? people[stat.PersonId] : new Person(),
                        JobCount = stat.JobCount,
                        EarliestYear = stat.EarliestYear,
                        LatestYear = stat.LatestYear
                    })
                    .ToList();
            });

            var jobPersonJobCount = new JobCount
            {
                Job = job,
                PersonJobSummary = personJobCounts ?? new List<PersonJobSummary>()
            };

            return PartialView("_JobCount", jobPersonJobCount);
        }
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client)]
        public IActionResult Collaboration(string id)
        {
            // Get the main person
            //SELECT* FROM Person WHERE PersonId = @id
            var person = _db.Person
                .AsNoTracking()
                .FirstOrDefault(p => p.PersonId == id);
            if (person == null)
            {
                return NotFound();
            }
            // Get all collaborations: 
            //SELECT
            //    p.PersonId, p.Name, p.BirthYear, p.DeathYear,
            //    j.Title as JobTitle,
            //    fp2.FilmId,
            //    f.Year
            //FROM Film_Person fp1
            //JOIN Film_Person fp2 ON fp1.FilmId = fp2.FilmId
            //JOIN Person p ON fp2.PersonId = p.PersonId
            //JOIN Job j ON fp2.JobId = j.JobId
            //JOIN Film f ON fp2.FilmId = f.FilmId
            //WHERE fp1.PersonId = @id AND fp2.PersonId != @id
            var collaborations = (from fp1 in _db.Film_Person.AsNoTracking()
                                  where fp1.PersonId == id
                                  join fp2 in _db.Film_Person.AsNoTracking()
                                      on fp1.FilmId equals fp2.FilmId
                                  where fp2.PersonId != id
                                  join p in _db.Person.AsNoTracking()
                                      on fp2.PersonId equals p.PersonId
                                  join j in _db.Job.AsNoTracking()
                                      on fp2.JobId equals j.JobId
                                  join f in _db.Film.AsNoTracking()
                                      on fp2.FilmId equals f.FilmId
                                  select new
                                  {
                                      CollaboratorId = p.PersonId,
                                      Collaborator = p,
                                      JobTitle = j.Title,
                                      FilmId = fp2.FilmId,
                                      Year = f.Year
                                  })
                                  .ToList() // Execute query here
                                  .GroupBy(x => x.CollaboratorId)
                                  .Select(group => new PersonJobCount
                                  {
                                      Person = group.First().Collaborator,
                                      Job = new Job
                                      {
                                          Title = string.Join(", ", group.Select(x => x.JobTitle).Distinct().OrderBy(t => t))
                                      },
                                      JobCount = group.Select(x => x.FilmId).Distinct().Count(),
                                      EarliestYear = (short)group.Min(x => x.Year),
                                      LatestYear = (short)group.Max(x => x.Year)
                                  })
                                  .OrderByDescending(pjc => pjc.JobCount)
                                  .ToList();

            var collaborationModel = new Collaboration
            {
                Person = person,
                CollaborationList = collaborations
            };

            return View(collaborationModel);
        }
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client)]
        public IActionResult TwoPersonCollaboration(string id1, string id2)
        {
            if (id1 == id2)
            {
                return View(new List<TwoPersonCollaborationDetail>());
            }
            var sharedFilms = (from fp1 in _db.Film_Person.AsNoTracking()
                               join fp2 in _db.Film_Person.AsNoTracking() on fp1.FilmId equals fp2.FilmId
                               join f in _db.Film.AsNoTracking() on fp1.FilmId equals f.FilmId
                               join p1 in _db.Person.AsNoTracking() on fp1.PersonId equals p1.PersonId
                               join p2 in _db.Person.AsNoTracking() on fp2.PersonId equals p2.PersonId
                               join j1 in _db.Job.AsNoTracking() on fp1.JobId equals j1.JobId
                               join j2 in _db.Job.AsNoTracking() on fp2.JobId equals j2.JobId
                               where fp1.PersonId == id1
                               && fp2.PersonId == id2
                               && fp1.PersonId != fp2.PersonId  // Avoid self-matching
                               orderby f.Year descending
                               select new TwoPersonCollaborationDetail
                               {
                                   Film = f,
                                   Person1 = p1,
                                   Job1 = j1,
                                   Person2 = p2,
                                   Job2 = j2
                               }).ToList();
            return View(sharedFilms);
        }
    }
}
