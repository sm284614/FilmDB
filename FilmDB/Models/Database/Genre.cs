using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmDB.Models.Database
{
    public class Genre
    {
        [Key]
        [Column("genre_id")]
        public byte GenreId { get; set; }
        [Column("name")]
        public string Name { get; set; } = "";
    }
}
