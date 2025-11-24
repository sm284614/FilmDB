using Microsoft.EntityFrameworkCore;
using FilmDB.Models.Database;
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
        public DbSet<Character> Character { get; set; }
        public DbSet<FilmPersonCharacter> Film_Person_Character { get; set; }
        public DbSet<PersonJobSummary> Person_Job_Summary { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FilmPerson>().HasNoKey();
            modelBuilder.Entity<FilmPersonCharacter>().HasNoKey();

            modelBuilder.Entity<Person>().ToTable("Person");
            modelBuilder.Entity<Person>().HasKey(p => p.PersonId);

            // Film indexes (for year filtering and title search)
            modelBuilder.Entity<Film>().HasIndex(f => f.Year);
            modelBuilder.Entity<Film>().HasIndex(f => f.Title);
            modelBuilder.Entity<Film>().HasIndex(f => f.GenreBitField);

            // FilmGenre indexes (for genre filtering joins)
            modelBuilder.Entity<FilmGenre>().HasIndex(fg => fg.FilmId);
            modelBuilder.Entity<FilmGenre>().HasIndex(fg => fg.GenreId);

            // FilmPerson indexes (for cast & crew lookups)
            modelBuilder.Entity<FilmPerson>().HasIndex(fp => fp.FilmId);
            modelBuilder.Entity<FilmPerson>().HasIndex(fp => fp.PersonId);
            modelBuilder.Entity<FilmPerson>().HasIndex(fp => fp.JobId);

            // FilmPersonCharacter composite index - for character lookups
            modelBuilder.Entity<FilmPersonCharacter>().HasIndex(fpc => new { fpc.FilmId, fpc.PersonId });
            modelBuilder.Entity<FilmPersonCharacter>().HasIndex(fpc => fpc.CharacterId);

            // Person index (for person name searches)
            modelBuilder.Entity<Person>().HasIndex(p => p.Name);

            // Character index (for character name searches)
            modelBuilder.Entity<Character>().HasIndex(c => c.Name);
        }
    }
}