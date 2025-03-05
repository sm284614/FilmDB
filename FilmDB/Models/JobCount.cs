using FilmDB.Models.Database;

namespace FilmDB.Models
{
    public class JobCount
    {
        public Job Job { get; set; }
        public List<PersonJobSummary> PersonJobSummary { get; set; }
    }
}
