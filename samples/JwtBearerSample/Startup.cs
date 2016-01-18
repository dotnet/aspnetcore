using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace JwtBearerSample
{
    public class Startup
    {
        public Startup()
        {
            Configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddUserSecrets()
                .Build();
        }

        public IConfiguration Configuration { get; set; }

        // Shared between users in memory
        public IList<Todo> Todos { get; } = new List<Todo>();

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            // Simple error page to avoid a repo dependency.
            app.Use(async (context, next) =>
            {
                try
                {
                    await next();
                }
                catch (Exception ex)
                {
                    if (context.Response.HasStarted)
                    {
                        throw;
                    }
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync(ex.ToString());
                }
            });

            app.UseIISPlatformHandler();

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseJwtBearerAuthentication(new JwtBearerOptions
            {
                AutomaticAuthenticate = true,
                AutomaticChallenge = true,
                // You also need to update /wwwroot/app/scripts/app.js
                Authority = Configuration["jwt:authority"],
                Audience = Configuration["jwt:audience"]
            });

            // [Authorize] would usually handle this
            app.Use(async (context, next) =>
            {
                // Use this if options.AutomaticAuthenticate = false
                // var user = await context.Authentication.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);

                var user = context.User; // We can do this because of  options.AutomaticAuthenticate = true; above.
                if (user?.Identity?.IsAuthenticated ?? false)
                {
                    await next();
                }
                else
                {
                    // We can do this because of options.AutomaticChallenge = true; above
                    await context.Authentication.ChallengeAsync();
                }
            });

            // MVC would usually handle this:
            app.Map("/api/TodoList", todoApp =>
            {
                todoApp.Run(async context =>
                {
                    var response = context.Response;
                    if (context.Request.Method.Equals("POST", System.StringComparison.OrdinalIgnoreCase))
                    {
                        var reader = new StreamReader(context.Request.Body);
                        var body = await reader.ReadToEndAsync();
                        var obj = JObject.Parse(body);
                        var todo = new Todo() { Description = obj["Description"].Value<string>(), Owner = context.User.Identity.Name };
                        Todos.Add(todo);
                    }
                    else
                    {
                        response.ContentType = "application/json";
                        response.Headers[HeaderNames.CacheControl] = "no-cache";
                        var json = JToken.FromObject(Todos);
                        await response.WriteAsync(json.ToString());
                    }
                });
            });
        }

        // Entry point for the application.
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseDefaultConfiguration(args)
                .UseIISPlatformHandlerUrl()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
