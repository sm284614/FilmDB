using FilmDB.Models.Database;

namespace FilmDB.Models
{
    public class TwoPersonCollaborationDetail
    {
        public Film Film { get; set; }
        public Person Person1 { get; set; }
        public Job Job1 { get; set; }
        public Person Person2 { get; set; }
        public Job Job2 { get; set; }
    }
}
