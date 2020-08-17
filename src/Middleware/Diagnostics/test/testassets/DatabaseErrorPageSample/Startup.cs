using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DatabaseErrorPageSample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<MyContext>(
                options => options.UseSqlite($"Data Source = DatabaseErrorPageSample.db"));
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();
#pragma warning disable CS0618 // Type or member is obsolete
            app.UseDatabaseErrorPage();
#pragma warning restore CS0618 // Type or member is obsolete
            app.Run(context =>
            {
                context.RequestServices.GetService<MyContext>().Blog.FirstOrDefault();
                return Task.FromResult(0);
            });
        }

        public static Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                    .UseKestrel()
                    .UseIISIntegration()
                    .UseStartup<Startup>();
                })
                .Build();

            return host.RunAsync();
        }
    }

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

