using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace JwtBearerSample
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            Environment = env;

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath);

            if (env.IsDevelopment())
            {
                // For more details on using the user secret store see http://go.microsoft.com/fwlink/?LinkID=532709
                builder.AddUserSecrets<Startup>();
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; set; }

        public IHostingEnvironment Environment { get; set; }

        // Shared between users in memory
        public IList<Todo> Todos { get; } = new List<Todo>();

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(o =>
                {
                    // You also need to update /wwwroot/app/scripts/app.js
                    o.Authority = Configuration["oidc:authority"];
                    o.Audience = Configuration["oidc:clientid"];
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseAuthentication();

            // [Authorize] would usually handle this
            app.Use(async (context, next) =>
            {
                // Use this if there are multiple authentication schemes
                var authResult = await context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
                if (authResult.Succeeded && authResult.Principal.Identity.IsAuthenticated)
                {
                    await next();
                }
                else if (authResult.Failure != null)
                {
                    // Rethrow, let the exception page handle it.
                    ExceptionDispatchInfo.Capture(authResult.Failure).Throw();
                }
                else
                {
                    await context.ChallengeAsync();
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
    }
}