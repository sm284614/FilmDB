using FilmDB.Models.Database;

namespace FilmDB.Models
{
    public class PersonFilm
    {
        public Person Person { get; set; }
        public Film Film { get; set; }
        public int Count { get; set; } = 0;
    }
}
