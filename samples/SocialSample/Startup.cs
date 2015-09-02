using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNet.Authentication;
using Microsoft.AspNet.Authentication.Cookies;
using Microsoft.AspNet.Authentication.Google;
using Microsoft.AspNet.Authentication.MicrosoftAccount;
using Microsoft.AspNet.Authentication.OAuth;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Newtonsoft.Json.Linq;

namespace CookieSample
{
    /* Note all servers must use the same address and port because these are pre-registered with the various providers. */
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication();
            services.Configure<SharedAuthenticationOptions>(options =>
            {
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            });
            services.AddClaimsTransformation(p =>
            {
                var id = new ClaimsIdentity("xform");
                id.AddClaim(new Claim("ClaimsTransformation", "TransformAddedClaim"));
                p.AddIdentity(id);
                return p;
            });
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerfactory)
        {
            loggerfactory.AddConsole(LogLevel.Information);

            app.UseCookieAuthentication(options =>
            {
                options.AutomaticAuthentication = true;
                options.LoginPath = new PathString("/login");
            });

            // https://developers.facebook.com/apps/
            app.UseFacebookAuthentication(options =>
            {
                options.AppId = "569522623154478";
                options.AppSecret = "a124463c4719c94b4228d9a240e5dc1a";
            });

            app.UseOAuthAuthentication("Google-AccessToken", options =>
            {
                options.ClientId = "560027070069-37ldt4kfuohhu3m495hk2j4pjp92d382.apps.googleusercontent.com";
                options.ClientSecret = "n2Q-GEw9RQjzcRbU3qhfTj8f";
                options.CallbackPath = new PathString("/signin-google-token");
                options.AuthorizationEndpoint = GoogleAuthenticationDefaults.AuthorizationEndpoint;
                options.TokenEndpoint = GoogleAuthenticationDefaults.TokenEndpoint;
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
            });

            // https://console.developers.google.com/project
            app.UseGoogleAuthentication(options =>
            {
                options.ClientId = "560027070069-37ldt4kfuohhu3m495hk2j4pjp92d382.apps.googleusercontent.com";
                options.ClientSecret = "n2Q-GEw9RQjzcRbU3qhfTj8f";
            });

            // https://apps.twitter.com/
            app.UseTwitterAuthentication(options =>
            {
                options.ConsumerKey = "6XaCTaLbMqfj6ww3zvZ5g";
                options.ConsumerSecret = "Il2eFzGIrYhz6BWjYhVXBPQSfZuS4xoHpSSyD9PI";
            });

            /* https://account.live.com/developers/applications
            The MicrosoftAccount service has restrictions that prevent the use of http://localhost:54540/ for test applications.
            As such, here is how to change this sample to uses http://mssecsample.localhost.this:54540/ instead.

            Edit the Project.json file and replace http://localhost:54540/ with http://mssecsample.localhost.this:54540/.

            From an admin command console first enter:
             notepad C:\Windows\System32\drivers\etc\hosts
            and add this to the file, save, and exit (and reboot?):
             127.0.0.1 MsSecSample.localhost.this

            Then you can choose to run the app as admin (see below) or add the following ACL as admin:
             netsh http add urlacl url=http://mssecsample.localhost.this:54540/ user=[domain\user]

            The sample app can then be run via:
             dnx . web
            */
            app.UseOAuthAuthentication("Microsoft-AccessToken", options =>
            {
                options.Caption = "MicrosoftAccount-AccessToken - Requires project changes";
                options.ClientId = "00000000480FF62E";
                options.ClientSecret = "bLw2JIvf8Y1TaToipPEqxTVlOeJwCUsr";
                options.CallbackPath = new PathString("/signin-microsoft-token");
                options.AuthorizationEndpoint = MicrosoftAccountAuthenticationDefaults.AuthorizationEndpoint;
                options.TokenEndpoint = MicrosoftAccountAuthenticationDefaults.TokenEndpoint;
                options.Scope.Add("wl.basic");
            });

            app.UseMicrosoftAccountAuthentication(options =>
            {
                options.Caption = "MicrosoftAccount - Requires project changes";
                options.ClientId = "00000000480FF62E";
                options.ClientSecret = "bLw2JIvf8Y1TaToipPEqxTVlOeJwCUsr";
                options.Scope.Add("wl.emails");
            });

            // https://github.com/settings/applications/
            app.UseOAuthAuthentication("GitHub-AccessToken", options =>
            {
                options.ClientId = "8c0c5a572abe8fe89588";
                options.ClientSecret = "e1d95eaf03461d27acd6f49d4fc7bf19d6ac8cda";
                options.CallbackPath = new PathString("/signin-github-token");
                options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
                options.TokenEndpoint = "https://github.com/login/oauth/access_token";
            });

            app.UseOAuthAuthentication("GitHub", options =>
            {
                options.ClientId = "49e302895d8b09ea5656";
                options.ClientSecret = "98f1bf028608901e9df91d64ee61536fe562064b";
                options.CallbackPath = new PathString("/signin-github");
                options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
                options.TokenEndpoint = "https://github.com/login/oauth/access_token";
                options.UserInformationEndpoint = "https://api.github.com/user";
                options.ClaimsIssuer = "OAuth2-Github";
                options.SaveTokensAsClaims = false;
                // Retrieving user information is unique to each provider.
                options.Notifications = new OAuthAuthenticationNotifications
                {
                    OnAuthenticated = async notification =>
                    {
                        // Get the GitHub user
                        var request = new HttpRequestMessage(HttpMethod.Get, notification.Options.UserInformationEndpoint);
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", notification.AccessToken);
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                        var response = await notification.Backchannel.SendAsync(request, notification.HttpContext.RequestAborted);
                        response.EnsureSuccessStatusCode();

                        var user = JObject.Parse(await response.Content.ReadAsStringAsync());
                        
                        var identifier = user.Value<string>("id");
                        if (!string.IsNullOrEmpty(identifier))
                        {
                            notification.Identity.AddClaim(new Claim(
                                ClaimTypes.NameIdentifier, identifier,
                                ClaimValueTypes.String, notification.Options.ClaimsIssuer));
                        }

                        var userName = user.Value<string>("login");
                        if (!string.IsNullOrEmpty(userName))
                        {
                            notification.Identity.AddClaim(new Claim(
                                ClaimsIdentity.DefaultNameClaimType, userName,
                                ClaimValueTypes.String, notification.Options.ClaimsIssuer));
                        }

                        var name = user.Value<string>("name");
                        if (!string.IsNullOrEmpty(name))
                        {
                            notification.Identity.AddClaim(new Claim(
                                "urn:github:name", name,
                                ClaimValueTypes.String, notification.Options.ClaimsIssuer));
                        }

                        var link = user.Value<string>("url");
                        if (!string.IsNullOrEmpty(link))
                        {
                            notification.Identity.AddClaim(new Claim(
                                "urn:github:url", link,
                                ClaimValueTypes.String, notification.Options.ClaimsIssuer));
                        }
                    },
                };
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
                        await context.Response.WriteAsync("<a href=\"?authscheme=" + type.AuthenticationScheme + "\">" + (type.Caption ?? "(suppressed)") + "</a><br>");
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
                await context.Response.WriteAsync("<a href=\"/logout\">Logout</a>");
                await context.Response.WriteAsync("</body></html>");
            });
        }
    }
}
