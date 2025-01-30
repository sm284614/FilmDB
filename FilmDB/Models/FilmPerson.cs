using System.ComponentModel.DataAnnotations.Schema;

namespace FilmDB.Models
{
    public class FilmPerson
    {
        [Column("film_id")]
        public string FilmId { get; set; } = "";
        [Column("person_id")]
        public string PersonId { get; set; } = "";
        [Column("job_id")]
        public byte JobId { get; set; }
    }
}
