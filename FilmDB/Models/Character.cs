using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace FilmDB.Models
{
    public class Character
    {
        [Key]
        [Column("character_id")]
        public byte CharacterId { get; set; }
        [Column("name")]
        public string Name { get; set; } = "";
    }
}
