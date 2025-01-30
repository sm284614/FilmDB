﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmDB.Models
{
    public class Person
    {
        [Column("person_id")]
        public string PersonId { get; set; } = "";
        [Column("name")]
        public string Name { get; set; } = "";
        [Column("birth_year")]
        public short? BirthYear { get; set; }
        [Column("death_year")]
        public short? DeathYear { get; set; }
    }
}
