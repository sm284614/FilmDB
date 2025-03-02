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

            var genreData = new GenreFilmYearCount
            {
                Name = genre.Name,
                FilmYears = _db.Film
                    .Join(_db.Film_Genre, f => f.FilmId, fg => fg.FilmId, (f, fg) => new { f, fg })
                    .Where(joined => joined.fg.GenreId == genre_id)
                    .GroupBy(joined => joined.f.Year)
                    .Select(group => new FilmYearCount
                    {
                        Year = group.Key,
                        FilmCount = group.Count()
                    })
                    .OrderBy(x => x.Year)
                    .ToList()
            };

            return PartialView("_GenreGraph", genreData);  // Return as a partial view
        }
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client)]
        public IActionResult GenreInfo(int genre_id)
        {
            var genreData = _db.Genre
                .Where(g => g.GenreId == genre_id)
                .Select(g => new GenreFilmYearCount
                {
                    GenreId = g.GenreId,
                    Name = g.Name,
                    FilmYears = _db.Film
                        .Join(_db.Film_Genre,
                              f => f.FilmId,
                              fg => fg.FilmId,
                              (f, fg) => new { f, fg })
                        .Where(joined => joined.fg.GenreId == genre_id && joined.f.Year < 2025)
                        .GroupBy(joined => joined.f.Year)
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



    }
}
