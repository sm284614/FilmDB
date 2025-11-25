using FilmDB.Data;
using FilmDB.Models;
using FilmDB.Models.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace FilmDB.Controllers
{
    public class GenreController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        public GenreController(ApplicationDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }
        /// <summary>
        /// List of film genres, ordered by internal id
        /// </summary>
        /// <returns></returns>
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)]
        public IActionResult Genre()
        {
            var genreList = _cache.GetOrCreate("GenreList", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
                return _db.Genre
                    .AsNoTracking()
                    .OrderBy(g =>g.GenreId)
                    .ToList();
            });
            return View(genreList);
        }
        /// <summary>
        /// Returns JSON data for films in a genre by year for graphing. (e.g. HORROR, 16, [(1968, 23), (1969, 45), (1970, 67)...])
        /// </summary>
        /// <param name="genre_id"></param>
        /// <returns></returns>
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client)]
        public IActionResult GenreGraphData(int genre_id)
        {
            var genreName = _db.Genre
                .AsNoTracking()
                .Where(g => g.GenreId == genre_id)
                .Select(g => g.Name)
                .FirstOrDefault();
            if (genreName == null)
            {
                return NotFound();
            }
            int genreBitValue = genre_id;

            var filmYears = _db.Film
                .AsNoTracking()
                .Where(f => (f.GenreBitField & genreBitValue) == genreBitValue)
                .GroupBy(f => f.Year)
                .Select(group => new FilmYearCount
                {
                    Year = group.Key,
                    FilmCount = group.Count()
                })
                .OrderBy(fy => fy.Year)
                .ToList();
            return Json(new
            {
                genreId = genre_id,
                genreName = genreName,
                years = filmYears.Select(fy => fy.Year).ToArray(),
                counts = filmYears.Select(fy => fy.FilmCount).ToArray()
            });
        }
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client)]
        public IActionResult CombinedGenreGraphData(string genre_ids)
        {
            // must be some input
            if (string.IsNullOrEmpty(genre_ids))
            {
                return NotFound(new { error = "No genre IDs provided" });
            }
            // Parse genre IDs from comma-separated string
            var genreIdList = genre_ids.Split(',')
                .Select(id => int.TryParse(id.Trim(), out var result) ? result : 0)
                .Where(id => id > 0)
                .ToList();
            // check parsed list
            if (!genreIdList.Any())
            {
                return BadRequest(new { error = "Invalid genre IDs" });
            }
            // Get genre names for display (use cached genre list)
            var allGenres = _cache.GetOrCreate("GenreList", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
                return _db.Genre
                    .AsNoTracking()
                    .OrderBy(g => g.GenreId)
                    .ToList();
            }) ?? new List<Genre>();
            var genres = allGenres
                .Where(g => genreIdList.Contains(g.GenreId))
                .ToList();
            // check found genres
            if (!genres.Any())
            {
                return NotFound(new { error = "Genres not found" });
            }
            // Calculate combined bit mask using bitwise OR
            int combinedBitMask = 0;
            foreach (var genreId in genreIdList)
            {
                combinedBitMask |= genreId;
            }
            // Query films that have ALL selected genres
            var filmsByYear = _db.Film
                .AsNoTracking()
                .Where(f => (f.GenreBitField & combinedBitMask) == combinedBitMask)
                .GroupBy(f => f.Year)
                .Select(group => new
                {
                    Year = group.Key,
                    Count = group.Count()
                })
                .OrderBy(x => x.Year)
                .ToList();
            // Prepare data for JSON response
            var years = filmsByYear.Select(x => x.Year).ToArray();
            var counts = filmsByYear.Select(x => x.Count).ToArray();
            // Create a combined genre name for display
            var genreNames = genres.Select(g => g.Name).OrderBy(n => n).ToList();
            var combinedName = string.Join(" + ", genreNames);
            return Json(new
            {
                genreName = combinedName,
                years = years,
                counts = counts
            });
        }
        public IActionResult GenreGraph(int genre_id)
        {
            var genreData = GetCachedGenreData(genre_id);
            if (genreData == null)
            {
                return NotFound();
            }
            return PartialView("_GenreGraph", genreData);
        }
        public IActionResult GenreInfo(int genre_id)
        {
            var genreData = GetCachedGenreData(genre_id);
            if (genreData == null)
            {
                return NotFound();
            }
            return View(genreData);
        }
        /// <summary>
        /// Returns a GenreFilmYearCount: a genre with a list of it's film-year counts. (e.g. HORROR, 16, [(1968, 23), (1969, 45), (1970, 67)...])
        /// </summary>
        private GenreFilmYearCount? GetCachedGenreData(int genre_id)
        {
            return _cache.GetOrCreate($"GenreData_{genre_id}", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                int genreBitValue = genre_id;
                var genre = _db.Genre
                    .AsNoTracking()
                    .Where(g => g.GenreId == genre_id)
                    .Select(g => new { g.GenreId, g.Name })
                    .FirstOrDefault();

                if (genre == null)
                {
                    return null;
                }
                var filmYears = _db.Film
                    .AsNoTracking()
                    .Where(f => (f.GenreBitField & genreBitValue) == genreBitValue)
                    .GroupBy(f => f.Year)
                    .Select(group => new FilmYearCount
                    {
                        Year = group.Key,
                        FilmCount = group.Count()
                    })
                    .OrderBy(fy => fy.Year)
                    .ToList();
                return new GenreFilmYearCount
                {
                    GenreId = genre.GenreId,
                    Name = genre.Name,
                    FilmYears = filmYears
                };
            });
        }
    }
}