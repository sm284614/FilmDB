using FilmDB.Models.Database;

namespace FilmDB.Models
{
    public class PersonFilm
    {
        public Person Person { get; set; } = null!;
        public Film Film { get; set; } = null!;
        public int Count { get; set; } = 0;
    }
}
