using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmDB.Models.Database
{
    public class Job
    {
        [Key]
        [Column("job_id")]
        public byte JobId { get; set; }
        [Column("title")]
        public string Title { get; set; } = "";
        [Column("is_cast")]
        public bool IsCast { get; set; }
    }
}
