using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ComponentsWebSite
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app)
        {
            app.Use(async (ctx, next) =>
            {
                var responseText = "Hello world";
                ctx.Response.ContentType = "text/plain";
                ctx.Response.ContentLength = responseText.Length;
                await ctx.Response.WriteAsync(responseText);
                await next();
            });
        }
    }
}
