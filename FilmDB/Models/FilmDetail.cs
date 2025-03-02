using FilmDB.Models.Database;

namespace FilmDB.Models
{
    public class FilmDetail
    {
        public Film Film { get; set; }
        public List<Genre> Genres { get; set; }
        public List<PersonJob> Cast { get; set; }
        public List<PersonJob> Crew { get; set; }
    }
}
