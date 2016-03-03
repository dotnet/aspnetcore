using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace CookieSample
{
    /* Note all servers must use the same address and port because these are pre-registered with the various providers. */
    public class Startup
    {
        public Startup()
        {
            Configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile("config.json")
                .AddUserSecrets()
                .Build();
        }

        public IConfiguration Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(options => options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerfactory)
        {
            loggerfactory.AddConsole(LogLevel.Information);

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

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AutomaticAuthenticate = true,
                AutomaticChallenge = true,
                LoginPath = new PathString("/login")
            });

            // You must first create an app with facebook and add it's ID and Secret to your config.json or user-secrets.
            // https://developers.facebook.com/apps/
            //app.UseFacebookAuthentication(new FacebookOptions
            //{
            //    AppId = Configuration["facebook:appid"],
            //    AppSecret = Configuration["facebook:appsecret"],
            //    Scope = { "email" },
            //    Fields = { "name", "email" }
            //});

            // See config.json
            app.UseOAuthAuthentication(new OAuthOptions
            {
                AuthenticationScheme = "Google-AccessToken",
                DisplayName = "Google-AccessToken",
                ClientId = Configuration["google:clientid"],
                ClientSecret = Configuration["google:clientsecret"],
                CallbackPath = new PathString("/signin-google-token"),
                AuthorizationEndpoint = GoogleDefaults.AuthorizationEndpoint,
                TokenEndpoint = GoogleDefaults.TokenEndpoint,
                Scope = { "openid", "profile", "email" },
                SaveTokens = true
            });

            // See config.json
            // https://console.developers.google.com/project
            app.UseGoogleAuthentication(new GoogleOptions
            {
                ClientId = Configuration["google:clientid"],
                ClientSecret = Configuration["google:clientsecret"],
                Events = new OAuthEvents()
                {
                    OnRemoteFailure = ctx =>

                    {
                        ctx.Response.Redirect("/error?FailureMessage=" + UrlEncoder.Default.Encode(ctx.Failure.Message));
                        ctx.HandleResponse();
                        return Task.FromResult(0);
                    }
                }
            });

            // See config.json
            // https://apps.twitter.com/
            app.UseTwitterAuthentication(new TwitterOptions
            {
                ConsumerKey = Configuration["twitter:consumerkey"],
                ConsumerSecret = Configuration["twitter:consumersecret"],
                Events = new TwitterEvents()
                {
                    OnRemoteFailure = ctx =>
                    {
                        ctx.Response.Redirect("/error?FailureMessage=" + UrlEncoder.Default.Encode(ctx.Failure.Message));
                        ctx.HandleResponse();
                        return Task.FromResult(0);
                    }
                }
            });

            // You must first create an app with live.com and add it's ID and Secret to your config.json or user-secrets.
            /* https://account.live.com/developers/applications
            The MicrosoftAccount service has restrictions that prevent the use of http://localhost:54540/ for test applications.
            As such, here is how to change this sample to uses http://mssecsample.localhost.this:54540/ instead.

            Edit the hosting.json file and add "server.urls": "http://mssecsample.localhost.this:54540/".

            From an admin command console first enter:
             notepad C:\Windows\System32\drivers\etc\hosts
            and add this to the file, save, and exit (and reboot?):
             127.0.0.1 MsSecSample.localhost.this

            [WebListener] Then you can choose to run the app as admin (see below) or add the following ACL as admin:
             netsh http add urlacl url=http://mssecsample.localhost.this:54540/ user=[domain\user]

            The sample app can then be run via:
             dnx web
            */
            //app.UseOAuthAuthentication(new OAuthOptions
            //{
            //    AuthenticationScheme = "Microsoft-AccessToken",
            //    DisplayName = "MicrosoftAccount-AccessToken - Requires project changes",
            //    ClientId = Configuration["msa:clientid"],
            //    ClientSecret = Configuration["msa:clientsecret"],
            //    CallbackPath = new PathString("/signin-microsoft-token"),
            //    AuthorizationEndpoint = MicrosoftAccountDefaults.AuthorizationEndpoint,
            //    TokenEndpoint = MicrosoftAccountDefaults.TokenEndpoint,
            //    Scope = { "wl.basic" },
            //    SaveTokens = true
            //});

            ////// You must first create an app with live.com and add it's ID and Secret to your config.json or user-secrets.
            //app.UseMicrosoftAccountAuthentication(new MicrosoftAccountOptions
            //{
            //    DisplayName = "MicrosoftAccount - Requires project changes",
            //    ClientId = Configuration["msa:clientid"],
            //    ClientSecret = Configuration["msa:clientsecret"],
            //    Scope = { "wl.emails" }
            //});

            // See config.json
            // https://github.com/settings/applications/
            app.UseOAuthAuthentication(new OAuthOptions
            {
                AuthenticationScheme = "GitHub-AccessToken",
                DisplayName = "Github-AccessToken",
                ClientId = Configuration["github-token:clientid"],
                ClientSecret = Configuration["github-token:clientsecret"],
                CallbackPath = new PathString("/signin-github-token"),
                AuthorizationEndpoint = "https://github.com/login/oauth/authorize",
                TokenEndpoint = "https://github.com/login/oauth/access_token",
                SaveTokens = true
            });

            // See config.json
            app.UseOAuthAuthentication(new OAuthOptions
            {
                AuthenticationScheme = "GitHub",
                DisplayName = "Github",
                ClientId = Configuration["github:clientid"],
                ClientSecret = Configuration["github:clientsecret"],
                CallbackPath = new PathString("/signin-github"),
                AuthorizationEndpoint = "https://github.com/login/oauth/authorize",
                TokenEndpoint = "https://github.com/login/oauth/access_token",
                UserInformationEndpoint = "https://api.github.com/user",
                ClaimsIssuer = "OAuth2-Github",
                // Retrieving user information is unique to each provider.
                Events = new OAuthEvents
                {
                    OnCreatingTicket = async context =>
                    {
                        // Get the GitHub user
                        var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                        var response = await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted);
                        response.EnsureSuccessStatusCode();

                        var user = JObject.Parse(await response.Content.ReadAsStringAsync());

                        var identifier = user.Value<string>("id");
                        if (!string.IsNullOrEmpty(identifier))
                        {
                            context.Identity.AddClaim(new Claim(
                                ClaimTypes.NameIdentifier, identifier,
                                ClaimValueTypes.String, context.Options.ClaimsIssuer));
                        }

                        var userName = user.Value<string>("login");
                        if (!string.IsNullOrEmpty(userName))
                        {
                            context.Identity.AddClaim(new Claim(
                                ClaimsIdentity.DefaultNameClaimType, userName,
                                ClaimValueTypes.String, context.Options.ClaimsIssuer));
                        }

                        var name = user.Value<string>("name");
                        if (!string.IsNullOrEmpty(name))
                        {
                            context.Identity.AddClaim(new Claim(
                                "urn:github:name", name,
                                ClaimValueTypes.String, context.Options.ClaimsIssuer));
                        }

                        var link = user.Value<string>("url");
                        if (!string.IsNullOrEmpty(link))
                        {
                            context.Identity.AddClaim(new Claim(
                                "urn:github:url", link,
                                ClaimValueTypes.String, context.Options.ClaimsIssuer));
                        }
                    }
                }
            });

            // Choose an authentication type
            app.Map("/login", signoutApp =>
            {
                signoutApp.Run(async context =>
                {
                    var authType = context.Request.Query["authscheme"];
                    if (!string.IsNullOrEmpty(authType))
                    {
                        // By default the client will be redirect back to the URL that issued the challenge (/login?authtype=foo),
                        // send them to the home page instead (/).
                        await context.Authentication.ChallengeAsync(authType, new AuthenticationProperties() { RedirectUri = "/" });
                        return;
                    }

                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync("<html><body>");
                    await context.Response.WriteAsync("Choose an authentication scheme: <br>");
                    foreach (var type in context.Authentication.GetAuthenticationSchemes())
                    {
                        await context.Response.WriteAsync("<a href=\"?authscheme=" + type.AuthenticationScheme + "\">" + (type.DisplayName ?? "(suppressed)") + "</a><br>");
                    }
                    await context.Response.WriteAsync("</body></html>");
                });
            });

            // Sign-out to remove the user cookie.
            app.Map("/logout", signoutApp =>
            {
                signoutApp.Run(async context =>
                {
                    context.Response.ContentType = "text/html";
                    await context.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    await context.Response.WriteAsync("<html><body>");
                    await context.Response.WriteAsync("You have been logged out. Goodbye " + context.User.Identity.Name + "<br>");
                    await context.Response.WriteAsync("<a href=\"/\">Home</a>");
                    await context.Response.WriteAsync("</body></html>");
                });
            });

            // Display the remote error
            app.Map("/error", errorApp =>
            {
                errorApp.Run(async context =>
                {
                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync("<html><body>");
                    await context.Response.WriteAsync("An remote failure has occurred: " + context.Request.Query["FailureMessage"] + "<br>");
                    await context.Response.WriteAsync("<a href=\"/\">Home</a>");
                    await context.Response.WriteAsync("</body></html>");
                });
            });

            // Deny anonymous request beyond this point.
            app.Use(async (context, next) =>
            {
                if (!context.User.Identities.Any(identity => identity.IsAuthenticated))
                {
                    // The cookie middleware will intercept this 401 and redirect to /login
                    await context.Authentication.ChallengeAsync();
                    return;
                }
                await next();
            });

            // Display user information
            app.Run(async context =>
            {
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync("<html><body>");
                await context.Response.WriteAsync("Hello " + (context.User.Identity.Name ?? "anonymous") + "<br>");
                foreach (var claim in context.User.Claims)
                {
                    await context.Response.WriteAsync(claim.Type + ": " + claim.Value + "<br>");
                }

                await context.Response.WriteAsync("Tokens:<br>");
                
                await context.Response.WriteAsync("Access Token: " + await context.Authentication.GetTokenAsync("access_token") + "<br>");
                await context.Response.WriteAsync("Refresh Token: " + await context.Authentication.GetTokenAsync("refresh_token") + "<br>");
                await context.Response.WriteAsync("Token Type: " + await context.Authentication.GetTokenAsync("token_type") + "<br>");
                await context.Response.WriteAsync("expires_at: " + await context.Authentication.GetTokenAsync("expires_at") + "<br>");
                await context.Response.WriteAsync("<a href=\"/logout\">Logout</a>");
                await context.Response.WriteAsync("</body></html>");
            });
        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseDefaultConfiguration(args)
                .UseServer("Microsoft.AspNetCore.Server.Kestrel")
                .UseIISPlatformHandlerUrl()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
