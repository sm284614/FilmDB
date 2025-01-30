namespace FilmDB.Models
{
    public class PersonJobCount
    {
        public Person Person { get; set; }
        public Job Job { get; set; }
        public int JobCount { get; set; }
        public short EarliestYear { get; set; }
        public short LatestYear { get; set; }
    }
}
