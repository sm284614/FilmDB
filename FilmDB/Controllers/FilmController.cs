using FilmDB.Data;
using FilmDB.Models;
using FilmDB.Models.Database;
using Microsoft.AspNetCore.Mvc;

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
            List<Genre> genreList = _db.Genre.ToList();
            ViewBag.GenreList = genreList;
            return View(filmList);
        }
        public IActionResult FilterFilmsByGenre(List<short> genreIds)
        {
            if (genreIds.Count > 0)
            {
                var films = _db.Film
                    .Where(f => genreIds.All(gid =>
                        _db.Film_Genre.Any(fg => fg.FilmId == f.FilmId && fg.GenreId == gid)
                    ))
                    .OrderByDescending(f => f.Year)
                    .ToList();
                var genreNames = _db.Genre
                    .Where(g => genreIds.Contains(g.GenreId))
                    .OrderBy(g => g.Name)
                    .Select(g => g.Name) // Select only the names
                    .ToList();
                ViewBag.FilterDescription = string.Join(@"/", genreNames) + " films";
                ViewBag.FilmCount = films.Count;
                return PartialView("_FilmTable", films);
            }
            else
            {
                ViewBag.FilterDescription = "No genres selected";
                ViewBag.FilmCount = 0;
                return PartialView("_FilmTable", new List<Film>());
            }
        }
        public IActionResult FilmSearch(string query)
        {
            var films = _db.Film
            .Where(f => f.Title.Contains(query))
            .OrderByDescending(f => f.Year)
            .ThenBy(f => f.Title) // Then order by title (ascending)
            .ToList();
            ViewBag.FilterDescription = $"Titles matching '{query}'";
            ViewBag.FilmCount = films.Count;
            return PartialView("_FilmTable", films);
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
            ViewBag.FilmCount = films.Count;
            // Pass data to the view
            return View("Film", films);
        }
        public IActionResult GenreYearRangeDetail(string genre, int startYear, int endYear)
        {
            // Fetch data for the specific genre and year
            var films = _db.Film
                .Where(f => f.Year >= startYear && f.Year <= endYear)  // Filter films by the year
                .Join(_db.Film_Genre, f => f.FilmId, fg => fg.FilmId, (f, fg) => new { f, fg })  // Join Film and FilmGenre tables
                .Join(_db.Genre, joined => joined.fg.GenreId, g => g.GenreId, (joined, g) => new { joined.f, g })  // Join with Genre table
                .Where(joined => joined.g.Name == genre)  // Filter by genre name
                .OrderByDescending(joined => joined.f.Year)  // Order by year
                .Select(joined => joined.f)  // Select only Film objects
                .Distinct() // Ensure no duplicate films
                .ToList();
            ViewBag.FilterDescription = $"{genre} films between {startYear} and {endYear}";
            ViewBag.FilmCount = films.Count;
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

            // Query actors (JobTitle = "Actor" or "Self") and set JobTitle to the Character Name
            var cast = _db.Film_Person
                .Where(fp => fp.FilmId.ToString() == id)
                .Join(_db.Person, fp => fp.PersonId, p => p.PersonId, (fp, p) => new { fp, p }) // Join FilmPerson with Person
                .Join(_db.Job, joined => joined.fp.JobId, j => j.JobId, (joined, j) => new { joined.fp, joined.p, j }) // Join with Job
                .Where(joined => joined.j.Title == "Actor" || joined.j.Title == "Self") // Only actors
                .GroupJoin(_db.Film_Person_Character,
                           joined => new { joined.fp.FilmId, joined.fp.PersonId },
                           fpc => new { fpc.FilmId, fpc.PersonId },
                           (joined, fpcs) => new { joined.fp, joined.p, joined.j, fpcs }) // Left join to characters
                .SelectMany(joined => joined.fpcs.DefaultIfEmpty(), (joined, fpc) => new { joined.fp, joined.p, joined.j, fpc })
                .GroupJoin(_db.Character,
                           j => j.fpc.CharacterId,
                           c => c.CharacterId,
                           (j, characters) => new { j.fp, j.p, j.j, CharacterName = characters.Select(c => c.Name).FirstOrDefault() })
                .Select(j => new PersonJob
                {
                    PersonId = j.p.PersonId,  // Person's ID
                    PersonName = j.p.Name,    // Person's Name
                    JobTitle = j.CharacterName ?? "" // Use CharacterName or empty string
                })
                .OrderBy(pjd => pjd.PersonName) // Sort alphabetically by actor name
                .ToList();  // Execute the LINQ query and convert to list


            // Query the crew involved in the film and their jobs
            var crew = _db.Film_Person
                 .Where(fp => fp.FilmId.ToString() == id)
                 .Join(_db.Person, fp => fp.PersonId, p => p.PersonId, (fp, p) => new { fp, p }) // Join FilmPerson with Person
                 .Join(_db.Job, joined => joined.fp.JobId, j => j.JobId, (joined, j) => new PersonJob
                 {
                     PersonId = joined.p.PersonId, // Person's ID
                     PersonName = joined.p.Name,     // Person's Name
                     JobTitle = j.Title,        // Job Title
                 })
                 .Where(pjd => pjd.JobTitle != "Actor" && pjd.JobTitle != "Self") // Exclude "Actor" and "Self"
                 .OrderBy(pjd => pjd.JobTitle)
                 .ToList();  // Execute the LINQ query and convert to list

            // Create the FilmDetail object
            var filmDetail = new FilmDetail
            {
                Film = film,
                Genres = genres,
                Cast = cast,
                Crew = crew
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
            ViewBag.FilmCount = films.Count();
            // Pass data to the view
            return View("Film", films);
        }
    }
}
