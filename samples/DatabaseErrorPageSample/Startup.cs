using System;
using Microsoft.AspNet.Builder;
using Microsoft.Data.Entity;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace DatabaseErrorPageSample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddEntityFramework()
                .AddSqlServer()
                .AddDbContext<MyContext>(options => options.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=DatabaseErrorPageSample;Trusted_Connection=True;"));
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();
            app.UseDatabaseErrorPage();
            app.Run(context =>
            {
                context.ApplicationServices.GetService<MyContext>().Blog.FirstOrDefault();
                return Task.FromResult(0);
            });
        }
    }

    public class MyContext : DbContext
    {
        public DbSet<Blog> Blog { get; set; }
    }

    public class Blog
    {
        public int BlogId { get; set; }
        public string Url { get; set; }
    }
}
