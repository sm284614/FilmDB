namespace FilmDB.Models
{
    public class GenreFilmYearCount
    {
        public int GenreId { get; set; }
        public string Name { get; set; } = "";
        public List<FilmYearCount> FilmYears { get; set; } = new List<FilmYearCount>();
    }
}
