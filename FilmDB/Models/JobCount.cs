namespace FilmDB.Models
{
    public class JobCount
    {
        public Job Job { get; set; }
        public List<PersonJobCount> PersonJobs { get; set; }
    }
}
