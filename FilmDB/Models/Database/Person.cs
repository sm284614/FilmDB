using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmDB.Models.Database
{
    public class Person
    {
        [Key]
        [Column("person_id")]
        public string PersonId { get; set; } = "";
        [Column("name")]
        public string Name { get; set; } = "";
        [Column("birth_year")]
        public short? BirthYear { get; set; }
        [Column("death_year")]
        public short? DeathYear { get; set; }
        [Column("first_film_id")]
        public string FirstFilmId { get; set; } = "";
    }
}
