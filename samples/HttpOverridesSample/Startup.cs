using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.HttpOverrides;

namespace HttpOverridesSample
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            app.UseIISPlatformHandler();
            app.UseOverrideHeaders(new OverrideHeaderOptions
            {
                ForwardedOptions = ForwardedHeaders.All
            });
            app.UseHttpMethodOverride();

            app.Run(async (context) =>
            {
                foreach (var header in context.Request.Headers)
                {
                    await context.Response.WriteAsync($"{header.Key}: {header.Value}\r\n");
                }
                await context.Response.WriteAsync($"Method: {context.Request.Method}\r\n");
                await context.Response.WriteAsync($"Scheme: {context.Request.Scheme}\r\n");
                await context.Response.WriteAsync($"RemoteIP: {context.Connection.RemoteIpAddress}\r\n");
                await context.Response.WriteAsync($"RemotePort: {context.Connection.RemotePort}\r\n");
            });
        }

        // Entry point for the application.
        public static void Main(string[] args)
        {
            var application = new WebApplicationBuilder()
                .UseConfiguration(WebApplicationConfiguration.GetDefault(args))
                .UseStartup<Startup>()
                .Build();

            application.Run();
        }
    }
}
