using System;
using System.Linq;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace OpenIdConnect.AzureAdSample
{
    public class Startup
    {
        private const string GraphResourceID = "https://graph.windows.net";

        public Startup()
        {
            Configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddUserSecrets()
                .Build();
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

            var clientId = Configuration["oidc:clientid"];
            var clientSecret = Configuration["oidc:clientsecret"];
            var authority = Configuration["oidc:authority"];
            var resource = "https://graph.windows.net";
            app.UseOpenIdConnectAuthentication(new OpenIdConnectOptions
            {
                ClientId = clientId,
                ClientSecret = clientSecret, // for code flow
                Authority = authority,
                ResponseType = OpenIdConnectResponseTypes.CodeIdToken,
                // GetClaimsFromUserInfoEndpoint = true,
                Events = new OpenIdConnectEvents()
                {
                    OnAuthorizationCodeReceived = async context =>
                    {
                        var request = context.HttpContext.Request;
                        var currentUri = UriHelper.Encode(request.Scheme, request.Host, request.PathBase, request.Path);
                        var credential = new ClientCredential(clientId, clientSecret);                                 
                        var authContext = new AuthenticationContext(authority, new AuthPropertiesTokenCache(context.Properties));

                        var result = await authContext.AcquireTokenByAuthorizationCodeAsync(
                            context.ProtocolMessage.Code, new Uri(currentUri), credential, resource);

                        context.HandleCodeRedemption(result.AccessToken, result.IdToken);
                    }
                }
            });

            app.Run(async context =>
            {
                if (context.Request.Path.Equals("/signout"))
                {
                    await context.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync($"<html><body>Signing out {context.User.Identity.Name}<br>{Environment.NewLine}");
                    await context.Response.WriteAsync("<a href=\"/\">Sign In</a>");
                    await context.Response.WriteAsync($"</body></html>");
                    return;
                }

                if (!context.User.Identities.Any(identity => identity.IsAuthenticated))
                {
                    await context.Authentication.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties { RedirectUri = "/" });
                    return;
                }

                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync($"<html><body>Hello Authenticated User {context.User.Identity.Name}<br>{Environment.NewLine}");
                await context.Response.WriteAsync("Claims:<br>" + Environment.NewLine);
                foreach (var claim in context.User.Claims)
                {
                    await context.Response.WriteAsync($"{claim.Type}: {claim.Value}<br>{Environment.NewLine}");
                }

                await context.Response.WriteAsync("Tokens:<br>" + Environment.NewLine);
                try
                {
                    // Retrieve the auth session with the cached tokens
                    var authenticateContext = new AuthenticateContext(CookieAuthenticationDefaults.AuthenticationScheme);
                    await context.Authentication.AuthenticateAsync(authenticateContext);
                    var authProperties = new AuthenticationProperties(authenticateContext.Properties);
                    var tokenCache = new AuthPropertiesTokenCache(authProperties);

                    // Use ADAL to get the right token
                    var authContext = new AuthenticationContext(authority, tokenCache);
                    var credential = new ClientCredential(clientId, clientSecret);
                    string userObjectID = context.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
                    var result = authContext.AcquireTokenSilent(resource, credential, new UserIdentifier(userObjectID, UserIdentifierType.UniqueId));

                    // Update the cookie with the modified tokens
                    if (tokenCache.HasCacheChanged)
                    {
                        await context.Authentication.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, authenticateContext.Principal, authProperties);
                    }

                    await context.Response.WriteAsync($"access_token: {result.AccessToken}<br>{Environment.NewLine}");
                }
                catch (Exception ex)
                {
                    await context.Response.WriteAsync($"AquireToken error: {ex.Message}<br>{Environment.NewLine}");
                }

                await context.Response.WriteAsync("<a href=\"/signout\">Sign Out</a>");
                await context.Response.WriteAsync($"</body></html>");
            });
        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseDefaultHostingConfiguration(args)
                .UseKestrel()
                .UseIISIntegration()()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}

