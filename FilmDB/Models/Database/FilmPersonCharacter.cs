using System.ComponentModel.DataAnnotations.Schema;

namespace FilmDB.Models.Database
{
    public class FilmPersonCharacter
    {
        [Column("film_id")]
        public string FilmId { get; set; } = "";
        [Column("person_id")]
        public string PersonId { get; set; } = "";
        [Column("character_id")]
        public int CharacterId { get; set; }
    }
}
