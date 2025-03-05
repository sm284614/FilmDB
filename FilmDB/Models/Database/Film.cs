using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmDB.Models.Database
{
    public class Film
    {
        [Key]
        [Column("film_id")]
        public string FilmId { get; set; } = "";
        [Column("title")]
        public string Title { get; set; } = "";
        [Column("year")]
        public short Year { get; set; }
        [Column("run_time_minutes")]
        public short RunTimeMinutes { get; set; }
        [Column("genre_bit_field")]
        public int GenreBitField { get; set; }
    }
}
