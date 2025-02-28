using FilmDB.Data;
using FilmDB.Models;
using Microsoft.AspNetCore.Mvc;

namespace FilmDB.Controllers
{
    public class CharacterController : Controller
    {
        private readonly ApplicationDbContext _db;
        public CharacterController(ApplicationDbContext db)
        {
            _db = db;
        }
        public IActionResult Character()
        {
            return View();
        }
        public IActionResult CharacterSearch(string query)
        {
            var characters = _db.Character
                .Where(c => c.Name.Contains(query))
                .OrderBy(c => c.Name)
                .ToList();

            return PartialView("_CharacterTable", characters);
        }
        public IActionResult CharacterDetail(int characterId)
        {
            var character = _db.Character
                .Where(c => c.CharacterId == characterId)
                .FirstOrDefault();
            if (character == null)
            {
                return NotFound();
            }

            var personFilms = _db.Film_Person_Character
                .Where(fpc => fpc.CharacterId == characterId)
                .Join(_db.Film, fpc => fpc.FilmId, f => f.FilmId, (fpc, film) => new { fpc, film })
                .Join(_db.Person, f => f.fpc.PersonId, p => p.PersonId, (f, person) => new PersonFilm
                {
                    Film = f.film,
                    Person = person
                })
                .OrderByDescending(fp => fp.Film.Year)
                .ToList();
            ViewBag.CharacterName = character.Name;
            return View(personFilms);
        }
    }
}
