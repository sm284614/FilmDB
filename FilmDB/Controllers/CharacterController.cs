using FilmDB.Data;
using FilmDB.Models;
using FilmDB.Models.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Drawing;

namespace FilmDB.Controllers
{
    public class CharacterController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        public CharacterController(ApplicationDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }
        // Main character page - shows popular characters by appearance count (cached)
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
        public IActionResult Character()
        {
            var popularCharacters = CuratedCharacters();
            //var popularCharacters = FrequentCharacters(40);
            return View(popularCharacters);
        }
        public List<CharacterCount>? CuratedCharacters()
        {
            // Curated list of iconic character IDs
            // TODO: Add your actual character IDs here
            var popularCharacterIds = new[]
            {
                11981,  // Batman
                17190, // Superman
                102027,  // Spider man
                169943, //Harley Quinn
                17192, //Lois Lane

                23557, //Queen Victoria
                7259, //queen elizabeth
                21554, //President
                27832, //Joan of Arc
                3608, //Cleopatra
                8067, //Marie Antoinette
                6806, //Calamity Jane

                33764, // God
                14018, //dracula
                473, //Satan
                1431, //Hercules
                35320, //Zeus
                42739, //pandora
                17563, //king arthur
                22371, //queen Guinevere
                32892, //Mary Magdalene
                49699, //Baba Yaga

                4792, // Sherlock Holmes
                38759, //Hercule Poirot
                1202, //Peter Pan
                22534, //Wendy Darling
                7752, //Robin Hood
                31934, //Zorro
                7582, //Snow White
                78, //Cinderella
                137565, //Little Red Riding Hood
                16844, //Lady Macbeth
                36504, //Hamlet
                13231, //Jane Eyre
                9801, //Elizabeth Bennet
                6038, //Fantine
                33791,  // James Bond
                79862, //Jack Ryan
            };
            // Get iconic characters from cache (or DB if not cached)
            var popularCharacters = _cache.GetOrCreate("IconicCharacters", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
                // Get characters and their appearance counts
                var characters = _db.Character
                    .Where(c => popularCharacterIds.Contains(c.CharacterId))
                    .Select(c => new CharacterCount
                    {
                        Character = c,
                        Count = _db.Film_Person_Character.Count(fpc => fpc.CharacterId == c.CharacterId)
                    })
                    .ToList();
                // Sort by the order they appear in iconicCharacterIds array
                // This preserves your manual ordering
                return popularCharacterIds
                    .Select(id => characters.FirstOrDefault(c => c.Character.CharacterId == id))
                    .Where(c => c != null)
                    .ToList();
            });
            return popularCharacters;
        }
        public List<CharacterCount>? FrequentCharacters(int quantity)
        {
            var characters = _db.Character
                .Select(c => new CharacterCount
                {
                    Character = c,
                    Count = _db.Film_Person_Character.Count(fpc => fpc.CharacterId == c.CharacterId)
                })
                //.Where(cc => cc.Count < 50)
                .OrderByDescending(cc => cc.Count)
                .Take(quantity)
                .ToList();

            return characters;
        }
        public IActionResult CharacterSearch(string query)
        {
            var characters = _db.Character
                .Where(c => c.Name.Contains(query))
                .Select(c => new CharacterCount
                {
                    Character = c,
                    Count = _db.Film_Person_Character.Count(fpc => fpc.CharacterId == c.CharacterId)
                }).OrderByDescending(cc => cc.Count)
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
                .OrderByDescending(fp => fp.Film!.Year)
                .ToList();
            ViewBag.CharacterName = character.Name;
            return View(personFilms);
        }
    }
}
