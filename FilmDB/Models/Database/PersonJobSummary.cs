using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmDB.Models.Database
{
    public class PersonJobSummary
    {
        [Key]
        [Column("person_id")]
        public Person? Person { get; set; }
        [Key]
        [Column("job_id")]
        public int JobId { get; set; }
        [Column("job_count")]
        public int JobCount { get; set; }
        [Column("earliest_year")]
        public short EarliestYear { get; set; }
        [Column("latest_year")]
        public short LatestYear { get; set; }
    }
}
