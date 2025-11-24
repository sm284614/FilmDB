using FilmDB.Data;
using FilmDB.Models;
using FilmDB.Models.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace FilmDB.Controllers
{
    public class PersonController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        public PersonController(ApplicationDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }
        public IActionResult Person()
        {
            var personList = _db.Person
                .Select(p => new PersonFilm
                {
                    Person = p,
                    Film = _db.Film.FirstOrDefault(f => f.FilmId == p.FirstFilmId) ?? new Film(),
                    Count = _db.Film_Person.Count(fp => fp.PersonId == p.PersonId) // Count total films for the person
                })                
                .Take(100) // Limit to some results
                .ToList();

            return View(personList);
        }
        public IActionResult PersonSearch(string query)
        {
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
                .Take(200) // Limit to 100 results for pagination
                .ToList();

            // Set the result data in the ViewBag
            ViewBag.ResultData = $"Showing {personFilms.Count} results for '{query}'";

            // Return the view with the result data
            return View("Person", personFilms);
        }
        public IActionResult PersonFilmography(string id)
        {
            var person = _db.Person.FirstOrDefault(p => p.PersonId == id);

            if (person == null)
            {
                return NotFound();
            }

            var filmJobs = (from f in _db.Film
                            join fp in _db.Film_Person on f.FilmId equals fp.FilmId
                            join j in _db.Job on fp.JobId equals j.JobId
                            where fp.PersonId == id  // Replace with the person ID parameter
                            orderby f.Year descending, j.Title ascending
                            select new PersonFilmJobDetail
                            {
                                FilmId = f.FilmId,
                                FilmTitle = f.Title,
                                FilmYear = f.Year,
                                JobTitle = j.Title
                            }).ToList();

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
            List<Job> jobList = _db.Job.ToList();
            return View("Job", jobList);
        }
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client)]
        public IActionResult JobCount(int id)
        {
            // Retrieve the job details
            var job = _db.Job.FirstOrDefault(j => j.JobId == id);
            if (job == null)
            {
                return NotFound(); // Return 404 if job not found
            }
            var cacheKey = $"JobCount_{id}";
            var personJobCounts = _cache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6);
                return _db.Film_Person
                    .Where(fp => fp.JobId == id)
                    .GroupBy(fp => fp.PersonId)
                    .Select(group => new PersonJobSummary
                    {
                        Person = _db.Person.FirstOrDefault(p => p.PersonId == group.Key),
                        JobCount = group.Count(),
                        EarliestYear = _db.Film_Person
                            .Where(fp => fp.PersonId == group.Key)
                            .Join(_db.Film, fp => fp.FilmId, f => f.FilmId, (fp, f) => f.Year)
                            .Min(),
                        LatestYear = _db.Film_Person
                            .Where(fp => fp.PersonId == group.Key)
                            .Join(_db.Film, fp => fp.FilmId, f => f.FilmId, (fp, f) => f.Year)
                            .Max()
                    })
                    .OrderByDescending(pjc => pjc.JobCount)
                    .Take(100)
                    .ToList();
            });

            // Create the JobPersonJobCount model
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
            var collaborations = _db.Film_Person
                .Where(fp1 => fp1.PersonId == id) // Get all films the given person worked on
                .Join(_db.Film_Person, fp1 => fp1.FilmId, fp2 => fp2.FilmId, (fp1, fp2) => new { fp1, fp2 }) // Find others who worked on those films
                .Where(joined => joined.fp2.PersonId != id) // Exclude the given person
                .Join(_db.Person, joined => joined.fp2.PersonId, p => p.PersonId, (joined, p) => new { joined, p }) // Get collaborator details
                .DefaultIfEmpty()
                .Join(_db.Job, joined => joined!.joined.fp2.JobId, j => j.JobId, (joined, j) => new //'joined' on this line is possible null
                {
                    Collaborator = joined!.p, //'joined' on this line is possible null
                    Job = j,
                    FilmId = joined.joined.fp2.FilmId, // Include FilmId to count distinct films
                    Year = _db.Film.Where(f => f.FilmId == joined.joined.fp2.FilmId).Select(f => f.Year).FirstOrDefault()
                })
                .DefaultIfEmpty()
                .AsEnumerable()
                .GroupBy(x => x!.Collaborator.PersonId) // Group by collaborator (not job, since we're aggregating jobs)
                .Select(group => new PersonJobCount
                {
                    Person = group.First()!.Collaborator, //'group' on this line is possible null
                    Job = new Job { Title = string.Join(", ", group.Select(x => x!.Job.Title).Distinct()) }, // Combine all jobs into a single string //'x' on this (and subsequent) line is possible null
                    JobCount = group.Select(x => x!.FilmId).Distinct().Count(), // Count unique films instead of job entries
                    EarliestYear = group.Min(x => x!.Year),
                    LatestYear = group.Max(x => x!.Year)
                })
                .OrderByDescending(pjc => pjc.JobCount)
                .ToList();


            // Create a collaboration model
            var collaborationModel = new Collaboration
            {
                Person = _db.Person.FirstOrDefault(p => p.PersonId == id) ?? new Person(), // Get the main person's details
                CollaborationList = collaborations
            };

            return View(collaborationModel);
        }
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client)]
        public IActionResult TwoPersonCollaboration(string id1, string id2)
        {
            var sharedFilms = (from fp1 in _db.Film_Person
                               join fp2 in _db.Film_Person on fp1.FilmId equals fp2.FilmId
                               join f in _db.Film on fp1.FilmId equals f.FilmId
                               join p1 in _db.Person on fp1.PersonId equals p1.PersonId
                               join p2 in _db.Person on fp2.PersonId equals p2.PersonId
                               join j1 in _db.Job on fp1.JobId equals j1.JobId
                               join j2 in _db.Job on fp2.JobId equals j2.JobId
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
