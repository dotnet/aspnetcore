using System;
using System.Linq;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace OpenIdConnectSample
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath);

            if (env.IsDevelopment())
            {
                // For more details on using the user secret store see http://go.microsoft.com/fwlink/?LinkID=532709
                builder.AddUserSecrets();
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(sharedOptions =>
                sharedOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerfactory)
        {
            loggerfactory.AddConsole(LogLevel.Information);
            loggerfactory.AddDebug(LogLevel.Information);

            // Simple error page
            app.Use(async (context, next) =>
            {
                try
                {
                    await next();
                }
                catch (Exception ex)
                {
                    if (!context.Response.HasStarted)
                    {
                        context.Response.Clear();
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync(ex.ToString());
                    }
                    else
                    {
                        throw;
                    }
                }
            });

            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            app.UseOpenIdConnectAuthentication(new OpenIdConnectOptions
            {
                ClientId = Configuration["oidc:clientid"],
                ClientSecret = Configuration["oidc:clientsecret"], // for code flow
                Authority = Configuration["oidc:authority"],
                ResponseType = OpenIdConnectResponseTypes.Code,
                GetClaimsFromUserInfoEndpoint = true
            });

            app.Run(async context =>
            {
                if (context.Request.Path.Equals("/signedout"))
                {
                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync($"<html><body>You have been signed out.<br>{Environment.NewLine}");
                    await context.Response.WriteAsync("<a href=\"/\">Sign In</a>");
                    await context.Response.WriteAsync($"</body></html>");
                    return;
                }
                if (context.Request.Path.Equals("/signout"))
                {
                    await context.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync($"<html><body>Signed out {context.User.Identity.Name}<br>{Environment.NewLine}");
                    await context.Response.WriteAsync("<a href=\"/\">Sign In</a>");
                    await context.Response.WriteAsync($"</body></html>");
                    return;
                }
                if (context.Request.Path.Equals("/signout-remote"))
                {
                    // Redirects
                    await context.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    await context.Authentication.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties()
                    {
                        RedirectUri = "/signedout"
                    });
                    return;
                }
                if (context.Request.Path.Equals("/Account/AccessDenied"))
                {
                    await context.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync($"<html><body>Access Denied for user {context.User.Identity.Name} to resource '{context.Request.Query["ReturnUrl"]}'<br>{Environment.NewLine}");
                    await context.Response.WriteAsync("<a href=\"/signout\">Sign Out</a>");
                    await context.Response.WriteAsync($"</body></html>");
                    return;
                }

                // CookieAuthenticationOptions.AutomaticAuthenticate = true (default) causes User to be set
                var user = context.User;

                // This is what [Authorize] calls
                // var user = await context.Authentication.AuthenticateAsync(AuthenticationManager.AutomaticScheme);

                // This is what [Authorize(ActiveAuthenticationSchemes = OpenIdConnectDefaults.AuthenticationScheme)] calls
                // var user = await context.Authentication.AuthenticateAsync(OpenIdConnectDefaults.AuthenticationScheme);

                // Not authenticated
                if (user == null || !user.Identities.Any(identity => identity.IsAuthenticated))
                {
                    // This is what [Authorize] calls
                    // The cookie middleware will intercept this 401 and redirect to /login
                    await context.Authentication.ChallengeAsync();

                    // This is what [Authorize(ActiveAuthenticationSchemes = OpenIdConnectDefaults.AuthenticationScheme)] calls
                    // await context.Authentication.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme);

                    return;
                }

                // Authenticated, but not authorized
                if (context.Request.Path.Equals("/restricted") && !user.Identities.Any(identity => identity.HasClaim("special", "true")))
                {
                    await context.Authentication.ChallengeAsync();
                    return;
                }

                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync($"<html><body>Hello Authenticated User {user.Identity.Name}<br>{Environment.NewLine}");
                foreach (var claim in user.Claims)
                {
                    await context.Response.WriteAsync($"{claim.Type}: {claim.Value}<br>{Environment.NewLine}");
                }
                await context.Response.WriteAsync("<a href=\"/restricted\">Restricted</a><br>");
                await context.Response.WriteAsync("<a href=\"/signout\">Sign Out</a><br>");
                await context.Response.WriteAsync("<a href=\"/signout-remote\">Sign Out Remote</a><br>");
                await context.Response.WriteAsync($"</body></html>");
            });
        }
    }
}

