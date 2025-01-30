using FilmDB.Data;
using FilmDB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FilmDB.Controllers
{
    public class PersonController : Controller
    {
        private readonly ApplicationDbContext _db;
        public PersonController(ApplicationDbContext db)
        {
            _db = db;
        }
        public IActionResult Person()
        {
            List<Person> personList = _db.Person.Take(400).ToList();
            return View(personList);
        }
        public IActionResult PersonFilmography(string id)
        {
            var person = _db.Person
                            .FirstOrDefault(p => p.PersonId == id);

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
        public IActionResult Job()
        {
            List<Job> jobList = _db.Job.ToList();
            return View("Job", jobList);
        }
        public IActionResult JobCount(int id)
        {
            // Retrieve the job details
            var job = _db.Job.FirstOrDefault(j => j.JobId == id);
            if (job == null)
            {
                return NotFound(); // Return 404 if job not found
            }

            // Query to get a list of people with their job count for the given job ID
            var personJobCounts = _db.Film_Person
                .Where(fp => fp.JobId == id) // Filter by the job ID
                .GroupBy(fp => fp.PersonId) // Group by person
                .Select(group => new PersonJobCount
                {
                    Person = _db.Person.FirstOrDefault(p => p.PersonId == group.Key), // Get person details
                    JobCount = group.Count(), // Count occurrences of this job for the person
                    EarliestYear = _db.Film_Person
                    .Where(fp => fp.PersonId == group.Key)
                    .Join(_db.Film, fp => fp.FilmId, f => f.FilmId, (fp, f) => f.Year) // Join Film_Person and Film to get years
                    .Min(), // Get the earliest year this person worked on a film
                    LatestYear = _db.Film_Person
                    .Where(fp => fp.PersonId == group.Key)
                    .Join(_db.Film, fp => fp.FilmId, f => f.FilmId, (fp, f) => f.Year) // Join Film_Person and Film to get years
                    .Max() // Get the latest year this person worked on a film
                })
                .OrderByDescending(pjc => pjc.JobCount) // Order by job count (most frequent first)
                .Take(100)
                .ToList();

            // Create the JobPersonJobCount model
            var jobPersonJobCount = new JobCount
            {
                Job = job,
                PersonJobs = personJobCounts
            };

            return View(jobPersonJobCount);
        }
        public IActionResult Collaboration(string id)
        {
            // Get the list of collaborators for the given person
            var collaborations = _db.Film_Person
                .Where(fp1 => fp1.PersonId == id) // Get all films the given person worked on
                .Join(_db.Film_Person, fp1 => fp1.FilmId, fp2 => fp2.FilmId, (fp1, fp2) => new { fp1, fp2 }) // Find others who worked on those films
                .Where(joined => joined.fp2.PersonId != id) // Exclude the given person
                .Join(_db.Person, joined => joined.fp2.PersonId, p => p.PersonId, (joined, p) => new { joined, p }) // Get collaborator details
                .Join(_db.Job, joined => joined.joined.fp2.JobId, j => j.JobId, (joined, j) => new
                {
                    Collaborator = joined.p,
                    Job = j,
                    Year = _db.Film.Where(f => f.FilmId == joined.joined.fp2.FilmId).Select(f => f.Year).FirstOrDefault()
                })
                .GroupBy(x => new { x.Collaborator.PersonId, x.Job.JobId }) // Group by collaborator and job
                .Select(group => new PersonJobCount
                {
                    Person = group.First().Collaborator,
                    Job = group.First().Job,
                    JobCount = group.Count(),
                    EarliestYear = group.Min(x => x.Year),
                    LatestYear = group.Max(x => x.Year)
                })
                .OrderByDescending(pjc => pjc.JobCount)
                .ToList();

            // Create a collaboration model
            var collaborationModel = new Collaboration
            {
                Person = _db.Person.FirstOrDefault(p => p.PersonId == id), // Get the main person's details
                CollaborationList = collaborations
            };

            return View(collaborationModel);
        }

    }
}
