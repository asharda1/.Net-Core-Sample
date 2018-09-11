using Microsoft.EntityFrameworkCore;

namespace Library.API.Entities
{
    public class LibraryContext : DbContext
    {
        /// <summary>
        /// Create/Updates DB for library project.
        /// </summary>
        /// <param name="options"></param>
        public LibraryContext(DbContextOptions<LibraryContext> options)
           : base(options)
        {
            Database.Migrate();
        }

        public DbSet<Author> Authors { get; set; }
        public DbSet<Book> Books { get; set; }

    }
}
