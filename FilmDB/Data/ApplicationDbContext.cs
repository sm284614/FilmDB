using Microsoft.EntityFrameworkCore;
using FilmDB.Models;

namespace FilmDB.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        public DbSet<Film> Film { get; set; }
        public DbSet<Job> Job { get; set; }
        public DbSet<Person> Person { get; set; }
        public DbSet<Genre> Genre { get; set; }
        public DbSet<FilmPerson> Film_Person { get; set; }
        public DbSet<FilmGenre> Film_Genre { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FilmPerson>().HasNoKey(); // Explicitly set no primary key
            modelBuilder.Entity<Person>().ToTable("Person");

            modelBuilder.Entity<Person>()
                .HasKey(p => p.PersonId);  // Explicitly set primary key
        }
    }
}
