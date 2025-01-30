namespace FilmDB.Models
{
    public class FilmDetail
    {
        public Film Film { get; set; }
        public List<Genre> Genres { get; set; }
        public List<PersonJob> PersonJobDetail { get; set; }
    }
}
