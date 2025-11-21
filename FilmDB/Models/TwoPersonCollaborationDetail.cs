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
        public TwoPersonCollaborationDetail()
        {
            Film = new Film();
            Person1 = new Person();
            Job1 = new Job();
            Person2 = new Person();
            Job2 = new Job();
        }
    }
}
