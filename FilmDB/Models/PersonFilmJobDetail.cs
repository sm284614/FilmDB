namespace FilmDB.Models
{
    public class PersonFilmJobDetail
    {
        public string FilmId { get; set; } = "-1";
        public string FilmTitle { get; set; } = "unknown";
        public short FilmYear { get; set; } = 1900;
        public string JobTitle { get; set; } = "unknown";
    }
}
