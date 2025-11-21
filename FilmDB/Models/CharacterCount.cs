namespace FilmDB.Models
{
    public class CharacterCount
    {
        public Database.Character Character { get; set; }
        public int Count { get; set; }
        public CharacterCount()
        {
            Character = new Database.Character();
        }
    }
}
