using FilmDB.Data;
using FilmDB.Models;
using FilmDB.Models.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace FilmDB.Controllers
{
    public class FilmController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        public FilmController(ApplicationDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)]
        public IActionResult Film(string? genreIds = null, int? startYear = null, int? endYear = null)
        {
            List<Film> filmList = [];
            List<int>? preSelectedGenres = null;
            int[]? preSelectedYearRange = null;
            // Check if we have filter parameters from graph click
            if (!string.IsNullOrEmpty(genreIds) && startYear.HasValue && endYear.HasValue)
            {
                // Parse genre IDs
                var genreIdList = genreIds.Split(',')
                    .Select(id => int.TryParse(id, out int gId) ? gId : 0)
                    .Where(id => id > 0)
                    .ToList();

                if (genreIdList.Any())
                {
                    // Calculate combined bit value
                    int combinedBitValue = 0;
                    foreach (var genreId in genreIdList)
                    {
                        combinedBitValue += genreId;
                    }
                    // Query filtered films
                    filmList = _db.Film
                        .AsNoTracking()
                        .Where(f => f.Year >= startYear && f.Year <= endYear)
                        .Where(f => (f.GenreBitField & combinedBitValue) == combinedBitValue)
                        .OrderByDescending(f => f.Year)
                        .ToList();
                    // Get genre names for display
                    var genreNames = _db.Genre
                        .AsNoTracking()
                        .Where(g => genreIdList.Contains(g.GenreId))
                        .Select(g => g.Name)
                        .ToList();
                    ViewBag.FilterDescription = $"{string.Join("/", genreNames)} films";
                    preSelectedGenres = genreIdList;
                    preSelectedYearRange = new[] { startYear.Value, endYear.Value };
                }
                else
                {
                    // Invalid genre IDs, fall back to featured
                    filmList = GetFeaturedFilms() ?? [];
                    ViewBag.FilterDescription = "Featured Films";
                }
            }
            else
            {
                // Default: Get featured films from cache
                filmList = GetFeaturedFilms() ?? [];
                ViewBag.FilterDescription = "Featured Films";
            }
            // Cache genre list (used on every page load)
            var genreList = _cache.GetOrCreate("GenreList", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
                return _db.Genre.ToList();
            });
            ViewBag.GenreList = genreList;
            ViewBag.FilmCount = filmList?.Count ?? 0;
            ViewBag.PreSelectedGenres = preSelectedGenres;
            ViewBag.PreSelectedYearRange = preSelectedYearRange;

            return View(filmList);
        }
        private List<Film>? GetFeaturedFilms()
        {
            return _cache.GetOrCreate("FeaturedFilms", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
                return _db.Film
                    .AsNoTracking()
                    .OrderBy(f => f.Year)
                    .ThenBy(f => f.Title)
                    .Take(20)
                    .ToList();
            });
        }
        public IActionResult FilterFilmsByGenreBitwise(List<int> genreIds)
        {
            if (genreIds.Count > 0)
            {
                //film.genre_bit_field encodes genres by their id (1=adventure,2=drama,4=fantasy,8=biography etc.)
                // Calculate the bitmask for the selected genres
                int bitField = genreIds.Sum();
                var films = _db.Film
                    .AsNoTracking()
                    .Where(f => (f.GenreBitField & bitField) == bitField)
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
        public IActionResult FilterFilmsByGenreBitwiseWithYearRange(List<int> genreIds, int startYear, int endYear)
        {
            if (genreIds != null && genreIds.Count > 0)
            {
                // Calculate the bitmask for the selected genres
                int bitField = 0;
                foreach (var genreId in genreIds)
                {
                    bitField += genreId;
                }
                var films = _db.Film
                    .AsNoTracking()
                    .Where(f => f.Year >= startYear && f.Year <= endYear)
                    .Where(f => (f.GenreBitField & bitField) == bitField)
                    .OrderByDescending(f => f.Year)
                    .ToList();
                var genreNames = _db.Genre
                    .AsNoTracking()
                    .Where(g => genreIds.Contains(g.GenreId))
                    .OrderBy(g => g.Name)
                    .Select(g => g.Name)
                    .ToList();
                ViewBag.FilterDescription = string.Join(@"/", genreNames) + $" films ({startYear}-{endYear})";
                ViewBag.FilmCount = films.Count;
                return PartialView("_FilmTable", films);
            }
            else
            {
                // No genres selected, just filter by year
                var films = _db.Film
                    .AsNoTracking()
                    .Where(f => f.Year >= startYear && f.Year <= endYear)
                    .OrderByDescending(f => f.Year)
                    .ToList();
                ViewBag.FilterDescription = $"Films from {startYear} to {endYear}";
                ViewBag.FilmCount = films.Count;
                return PartialView("_FilmTable", films);
            }
        }
        [ResponseCache(Duration = 360, Location = ResponseCacheLocation.Client)]
        public IActionResult FilmSearch(string query)
        {
            var films = _db.Film
                .AsNoTracking()
                .Where(f => f.Title.Contains(query))
                .OrderByDescending(f => f.Year)
                .ThenBy(f => f.Title) // Then order by title (ascending)
                .ToList();
            ViewBag.FilterDescription = $"Titles matching '{query}'";
            ViewBag.FilmCount = films.Count;
            return PartialView("_FilmTable", films);
        }
        [ResponseCache(Duration = 360, Location = ResponseCacheLocation.Client)]
        public IActionResult FilmDetail(string id)
        {
            var film = _db.Film
                .AsNoTracking()
                .FirstOrDefault(f => f.FilmId == id);
            if (film == null)
            {
                return NotFound();
            }
            // Single query for all film-person relationships
            var allFilmPeople = (from fp in _db.Film_Person.AsNoTracking()
                                 where fp.FilmId == id
                                 join p in _db.Person on fp.PersonId equals p.PersonId
                                 join j in _db.Job on fp.JobId equals j.JobId
                                 join fpc in _db.Film_Person_Character
                                     on new { fp.FilmId, fp.PersonId } equals new { fpc.FilmId, fpc.PersonId }
                                     into fpcGroup
                                 from fpc in fpcGroup.DefaultIfEmpty()
                                 join c in _db.Character
                                     on fpc != null ? fpc.CharacterId : (int?)null equals c.CharacterId
                                     into charGroup
                                 from c in charGroup.DefaultIfEmpty()
                                 select new
                                 {
                                     PersonId = p.PersonId,
                                     PersonName = p.Name,
                                     IsCast = j.IsCast,  // Use the flag instead of JobId
                                     JobTitle = j.Title,
                                     CharacterId = c != null ? c.CharacterId : 0,
                                     CharacterName = c != null ? c.Name : ""
                                 })
                                 .ToList();
            // Split into cast and crew in memory (already loaded)
            var cast = allFilmPeople
                .Where(x => x.IsCast)  // Simple boolean check
                .Select(x => new PersonJob
                {
                    PersonId = x.PersonId,
                    PersonName = x.PersonName,
                    CharacterId = x.CharacterId,
                    JobTitle = x.CharacterName
                })
                .OrderBy(pj => pj.JobTitle)
                .ToList();
            var crew = allFilmPeople
                .Where(x => !x.IsCast)  // Simple boolean check
                .Select(x => new PersonJob
                {
                    PersonId = x.PersonId,
                    PersonName = x.PersonName,
                    JobTitle = x.JobTitle
                })
                .OrderBy(pj => pj.JobTitle)
                .ToList();
            // Get genres
            var genres = _db.Film_Genre
                .AsNoTracking()
                .Where(fg => fg.FilmId == id)
                .Join(_db.Genre,
                      fg => fg.GenreId,
                      g => g.GenreId,
                      (fg, g) => g)
                .ToList();
            var filmDetail = new FilmDetail
            {
                Film = film,
                Genres = genres,
                Cast = cast,
                Crew = crew
            };
            return View(filmDetail);
        }
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)]
        public IActionResult FilmYear(int year)
        {
            // Fetch data for films from the specific year
            var films = _db.Film
                .AsNoTracking()
                .Where(f => f.Year == year)
                .OrderBy(f => f.Title)  
                .ToList();
            ViewBag.FilterDescription = $"Films from {year}";
            ViewBag.FilmCount = films.Count; 
            return View("FilmList", films);
        }
    }
}
