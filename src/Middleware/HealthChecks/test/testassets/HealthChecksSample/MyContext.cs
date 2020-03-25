using Microsoft.EntityFrameworkCore;

namespace HealthChecksSample
{
    public class MyContext : DbContext
    {
        public MyContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Blog> Blog { get; set; }
    }

    public class Blog
    {
        public int BlogId { get; set; }
        public string Url { get; set; }
    }
}
