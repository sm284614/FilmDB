using FilmDB.Data;
using FilmDB.Models;
using FilmDB.Models.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FilmDB.Controllers
{
    public class GenreController : Controller
    {
        private readonly ApplicationDbContext _db;
        public GenreController(ApplicationDbContext db)
        {
            _db = db;
        }
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client)]
        public IActionResult Genre()
        {
            List<Genre> genreList = _db.Genre.ToList();
            return View(genreList);
        }
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client)]
        public IActionResult GenreGraph(int genre_id)
        {
            var genre = _db.Genre
                .Where(g => g.GenreId == genre_id)
                .FirstOrDefault();

            if (genre == null)
            {
                return NotFound();
            }
            int genreBitValue = genre_id;
            var genreData = new GenreFilmYearCount
            {
                Name = genre.Name,
                FilmYears = _db.Film
                    .Where(f => (f.GenreBitField & genreBitValue) == genreBitValue)
                    .GroupBy(f => f.Year)
                    .Select(group => new FilmYearCount
                    {
                        Year = group.Key,
                        FilmCount = group.Count()
                    })
                    .OrderBy(x => x.Year)
                    .ToList()
            };
            return PartialView("_GenreGraph", genreData);
        }

        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client)]
        public IActionResult GenreGraphData(int genre_id)
        {
            var genre = _db.Genre
                .Where(g => g.GenreId == genre_id)
                .FirstOrDefault();

            if (genre == null)
            {
                return NotFound();
            }

            // Use GenreBitField for faster queries
            int genreBitValue = genre_id;

            var genreData = new GenreFilmYearCount
            {
                Name = genre.Name,
                FilmYears = _db.Film
                    .Where(f => (f.GenreBitField & genreBitValue) == genreBitValue)
                    .GroupBy(f => f.Year)
                    .Select(group => new FilmYearCount
                    {
                        Year = group.Key,
                        FilmCount = group.Count()
                    })
                    .OrderBy(x => x.Year)
                    .ToList()
            };
            return Json(new
            {
                genreName = genre.Name,
                years = genreData.FilmYears.Select(y => y.Year).ToArray(),
                counts = genreData.FilmYears.Select(y => y.FilmCount).ToArray()
            });
        }
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client)]
        public IActionResult CombinedGenreGraphData(string genre_ids)
        {
            if (string.IsNullOrEmpty(genre_ids))
            {
                return BadRequest(new { error = "No genre IDs provided" });
            }

            // Parse genre IDs from comma-separated string
            var genreIdList = genre_ids.Split(',')
                .Select(id => int.TryParse(id.Trim(), out var result) ? result : 0)
                .Where(id => id > 0)
                .ToList();

            if (!genreIdList.Any())
            {
                return BadRequest(new { error = "Invalid genre IDs" });
            }

            // Get genre names for display
            var genres = _db.Genre
                .Where(g => genreIdList.Contains(g.GenreId))
                .ToList();

            if (!genres.Any())
            {
                return NotFound(new { error = "Genres not found" });
            }

            // FIXED: Calculate combined bit mask using bitwise OR
            // Since genre_id IS the bit value (1, 2, 4, 8, etc.), just OR them together
            // Example: Adventure (1) + Drama (2) + Fantasy (4) = 7
            int combinedBitMask = 0;
            foreach (var genreId in genreIdList)
            {
                combinedBitMask |= genreId;  // Direct OR, no bit shift needed
            }

            // Query films that have ALL selected genres
            // A film matches if: (GenreBitField & combinedBitMask) == combinedBitMask
            // This ensures ALL bits for selected genres are set
            var filmsByYear = _db.Film
                .Where(f => (f.GenreBitField & combinedBitMask) == combinedBitMask)
                .GroupBy(f => f.Year)
                .Select(group => new
                {
                    Year = group.Key,
                    Count = group.Count()
                })
                .OrderBy(x => x.Year)
                .ToList();

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

        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client)]
        public IActionResult GenreInfo(int genre_id)
        {
            // Use GenreBitField for faster queries
            int genreBitValue = genre_id;
            var genreData = _db.Genre
                .Where(g => g.GenreId == genre_id)
                .Select(g => new GenreFilmYearCount
                {
                    GenreId = g.GenreId,
                    Name = g.Name,
                    FilmYears = _db.Film
                        .Where(f => (f.GenreBitField & genreBitValue) == genreBitValue && f.Year < 2025)
                        .GroupBy(f => f.Year)
                        .Select(group => new FilmYearCount
                        {
                            Year = group.Key,
                            FilmCount = group.Count()
                        })
                        .OrderBy(fyc => fyc.Year)
                        .ToList()
                })
                .FirstOrDefault();

            if (genreData == null)
            {
                return NotFound();
            }

            return View(genreData);
        }

        // Handles both single genres (e.g., "Action") and combined genres (e.g., "Action + Sci-Fi")
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client)]
        public async Task<IActionResult> GenreYearDetail(string genre, int year)
        {
            if (string.IsNullOrEmpty(genre))
            {
                return NotFound();
            }
            // Check if it's a combined genre (contains " + ")
            if (genre.Contains(" + "))
            {
                // Parse genre names (e.g., "Action + Sci-Fi" -> ["Action", "Sci-Fi"])
                var genreNames = genre.Split('+')
                    .Select(g => g.Trim())
                    .ToList();

                // Get genre entities
                var genreEntities = await _db.Genre
                    .Where(g => genreNames.Contains(g.Name))
                    .ToListAsync();

                if (!genreEntities.Any())
                {
                    return NotFound();
                }
                // Calculate combined bit mask
                int combinedBitMask = 0;
                foreach (var genreEntity in genreEntities)
                {
                    combinedBitMask |= genreEntity.GenreId;  // Direct OR since GenreId IS the bit value
                }

                // Query films that have ALL selected genres in the specified year
                var combinedFilms = await _db.Film
                    .Where(f => f.Year == year && (f.GenreBitField & combinedBitMask) == combinedBitMask)
                    .OrderBy(f => f.Title)
                    .ToListAsync();
                ViewBag.Genre = genre;
                ViewBag.Year = year;
                return View(combinedFilms);
            }
            else
            {
                // Single genre - existing logic
                var genreEntity = await _db.Genre
                    .FirstOrDefaultAsync(g => g.Name == genre);
                if (genreEntity == null)
                {
                    return NotFound();
                }
                int genreBitValue = genreEntity.GenreId;
                var films = await _db.Film
                    .Where(f => f.Year == year && (f.GenreBitField & genreBitValue) == genreBitValue)
                    .OrderBy(f => f.Title)
                    .ToListAsync();
                ViewBag.Genre = genre;
                ViewBag.Year = year;
                return View(films);
            }
        }
    }
}