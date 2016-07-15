using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.DependencyInjection;

namespace RewriteSample
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        { 
            app.UseRewriter(new UrlRewriteOptions()
                .ImportFromModRewrite("Rewrite.txt"));
            app.Run(context => context.Response.WriteAsync(context.Request.Path));
        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
