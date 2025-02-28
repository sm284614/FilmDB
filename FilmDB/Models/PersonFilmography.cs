using FilmDB.Models.Database;

namespace FilmDB.Models
{
    public class PersonFilmography
    {
        public Person Person { get; set; } 
        public List<PersonFilmJobDetail> FilmJobs { get; set; }
    }
}
