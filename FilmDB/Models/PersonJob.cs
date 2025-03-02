using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FilmDB.Models
{
    public class PersonJob
    {
        public string PersonId { get; set; } = "";
        public string PersonName { get; set; } = "";
        public string JobTitle { get; set; } = "";
    }
}
