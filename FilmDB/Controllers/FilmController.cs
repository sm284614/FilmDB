using FilmDB.Data;
using FilmDB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace FilmDB.Controllers
{
    public class FilmController : Controller
    {
        private readonly ApplicationDbContext _db;
        public FilmController(ApplicationDbContext db)
        {
            _db = db;
        }
        public IActionResult Film(int page = 1, int pageSize = 100)
        {
            List<Film> filmList = _db.Film
                .OrderBy(f => f.Year)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            return View(filmList);
        }
        public IActionResult GenreYearDetail(string genre, int year)
        {
            // Fetch data for the specific genre and year
            var films = _db.Film
                .Where(f => f.Year == year)  // Filter films by the year
                .Join(_db.Film_Genre, f => f.FilmId, fg => fg.FilmId, (f, fg) => new { f, fg })  // Join Film and FilmGenre tables
                .Join(_db.Genre, joined => joined.fg.GenreId, g => g.GenreId, (joined, g) => new { joined.f, g })  // Join with Genre table
                .Where(joined => joined.g.Name == genre)  // Filter by genre name
                .OrderByDescending(joined => joined.f.Year)  // Order by year
                .Select(joined => joined.f)  // Select only Film objects
                .Distinct() // Ensure no duplicate films
                .ToList();
            ViewBag.FilterDescription = $"{genre} films from {year}";
            // Pass data to the view
            return View("Film", films);
        }
        public IActionResult FilmDetail(string id)
        {
            // Query the Film from the database based on the filmId
            var film = _db.Film.FirstOrDefault(f => f.FilmId.ToString() == id);

            if (film == null)
            {
                // Handle case where film is not found
                return NotFound();
            }

            // Query the genres related to the film
            var genres = _db.Genre
                .Join(_db.Film_Genre, g => g.GenreId, fg => fg.GenreId, (g, fg) => new { g, fg }) // Join Genre and Film_Genre
                .Where(joined => joined.fg.FilmId.ToString() == id) // Filter by the FilmId (id)
                .Select(joined => joined.g) // Select the Genre object from the joined result
                .ToList(); // Execute the query and get the list

            // Query the people involved in the film and their jobs
            var personJobDetail = _db.Film_Person
                 .Where(fp => fp.FilmId.ToString() == id)
                 .Join(_db.Person, fp => fp.PersonId, p => p.PersonId, (fp, p) => new { fp, p }) // Join FilmPerson with Person
                 .Join(_db.Job, joined => joined.fp.JobId, j => j.JobId, (joined, j) => new PersonJob
                 {
                     PersonId = joined.p.PersonId, // Person's ID
                     PersonName = joined.p.Name,     // Person's Name
                     JobTitle = j.Title        // Job Title
                 })
                 .OrderBy(pjd => pjd.JobTitle)
                 .ToList();  // Execute the LINQ query and convert to list

            // Create the FilmDetail object
            var filmDetail = new FilmDetail
            {
                Film = film,
                Genres = genres,
                PersonJobDetail = personJobDetail
            };

            return View(filmDetail);
        }
        public IActionResult FilmYear(int year)
        {
            // Fetch data for the specific genre and year
            var films = _db.Film
                .Where(f => f.Year == year)  // Filter films by the year
                .OrderByDescending(fy => fy.Year)  // Order by year
                .ToList();
            ViewBag.FilterDescription = $"Films from {year}";
            // Pass data to the view
            return View("Film", films);
        }
    }
}
