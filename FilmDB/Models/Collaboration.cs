using FilmDB.Models.Database;

namespace FilmDB.Models
{
    public class Collaboration
    {
        public Person Person { get; set; }
        public List<PersonJobCount> CollaborationList { get; set; }
        public Collaboration()
        {
            Person = new Person();
            CollaborationList = new List<PersonJobCount>();
        }
    }
}
