using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace IntegratedAuthSample
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.Use(Authenticate);

            app.Run(HandleRequest);
        }

        private ConcurrentDictionary<string, NtAuthWrapper> _states = new ConcurrentDictionary<string, NtAuthWrapper>();

        public async Task Authenticate(HttpContext context, Func<Task> next)
        {
            var connectionId = context.Connection.Id;
            var logger = context.RequestServices.GetRequiredService<ILogger<Startup>>();
            var authorization = context.Request.Headers[HeaderNames.Authorization].ToString();
            var auth = _states.GetOrAdd(connectionId, _ => new NtAuthWrapper());

            if (string.IsNullOrEmpty(authorization))
            {
                if (auth.IsCompleted)
                {
                    logger.LogInformation($"C:{connectionId}, No Authorization header, Cached User");
                    context.User = auth.GetPrincipal();
                    await next();
                    return;
                }

                logger.LogInformation($"C:{connectionId}, No Authorization header, 401 Negotiate");
                Challenge(context);
                return;
            }

            string token = null;
            if (authorization.StartsWith("Negotiate ", StringComparison.OrdinalIgnoreCase))
            {
                token = authorization.Substring("Negotiate ".Length).Trim();
            }

            // If no token found, no further work possible
            if (string.IsNullOrEmpty(token))
            {
                logger.LogInformation($"C:{connectionId}, Non-Negotiate Authorization header, 401 Negotiate; " + authorization);
                Challenge(context);
                return;
            }

            var outgoing = auth.GetOutgoingBlob(token);
            if (!auth.IsCompleted)
            {
                logger.LogInformation($"C:{connectionId}, Incomplete-Negotiate, 401 Negotiate " + outgoing);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers.Append(HeaderNames.WWWAuthenticate, "Negotiate " + outgoing);
                return;
            }

            // TODO SPN check

            logger.LogInformation($"C:{connectionId}, Completed-Negotiate, Negotiate " + outgoing);
            if (!string.IsNullOrEmpty(outgoing))
            {
                context.Response.Headers.Append(HeaderNames.WWWAuthenticate, "Negotiate " + outgoing);
            }

            context.User = auth.GetPrincipal();

            await next();
        }

        private void Challenge(HttpContext context)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.Append(HeaderNames.WWWAuthenticate, "Negotiate");
        }

        public async Task HandleRequest(HttpContext context)
        {
            var user = context.User.Identity;
            await context.Response.WriteAsync($"Authenticated? {user.IsAuthenticated}, Name: {user.Name}");
        }
    }
}
