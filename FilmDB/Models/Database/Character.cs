using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace FilmDB.Models.Database
{
    public class Character
    {
        [Key]
        [Column("character_id")]
        public int CharacterId { get; set; }
        [Column("name")]
        public string Name { get; set; } = "";
    }
}
