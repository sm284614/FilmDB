namespace FilmDB.Models
{
    public class Collaboration
    {
        public Person Person { get; set; }
        public List<PersonJobCount> CollaborationList { get; set; }
    }
}
