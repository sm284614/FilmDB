using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmDB.Models.Database
{
    public class FilmGenre
    {
        [Key]
        [Column("film_genre_id")]
        public int FilmGenreId { get; set; }
        [Column("film_id")]
        public string FilmId { get; set; } = "";
        [Column("genre_id")]
        public int GenreId { get; set; }
    }
}
