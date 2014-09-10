using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Security;
using Microsoft.AspNet.Security;
using Microsoft.AspNet.Security.Cookies;
using Microsoft.AspNet.Security.Facebook;
using Microsoft.AspNet.Security.Google;
using Microsoft.AspNet.Security.MicrosoftAccount;
using Microsoft.AspNet.Security.OAuth;
using Microsoft.AspNet.Security.Twitter;
using Newtonsoft.Json.Linq;

namespace CookieSample
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseErrorPage();

            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions()
            {
                LoginPath = new PathString("/login"),
            });

            app.UseFacebookAuthentication(new FacebookAuthenticationOptions()
            {
                AppId = "569522623154478",
                AppSecret = "a124463c4719c94b4228d9a240e5dc1a",
            });

            app.UseOAuthAuthentication(new OAuthAuthenticationOptions<IOAuthAuthenticationNotifications>("Google-AccessToken")
            {
                ClientId = "560027070069-37ldt4kfuohhu3m495hk2j4pjp92d382.apps.googleusercontent.com",
                ClientSecret = "n2Q-GEw9RQjzcRbU3qhfTj8f",
                CallbackPath = new PathString("/signin-google-token"),
                AuthorizationEndpoint = GoogleAuthenticationDefaults.AuthorizationEndpoint,
                TokenEndpoint = GoogleAuthenticationDefaults.TokenEndpoint,
                Scope = { "openid", "profile", "email" },
            });

            app.UseGoogleAuthentication(new GoogleAuthenticationOptions()
            {
                ClientId = "560027070069-37ldt4kfuohhu3m495hk2j4pjp92d382.apps.googleusercontent.com",
                ClientSecret = "n2Q-GEw9RQjzcRbU3qhfTj8f",
            });

            app.UseTwitterAuthentication(new TwitterAuthenticationOptions()
            {
                ConsumerKey = "6XaCTaLbMqfj6ww3zvZ5g",
                ConsumerSecret = "Il2eFzGIrYhz6BWjYhVXBPQSfZuS4xoHpSSyD9PI",
            });

            /*
            The MicrosoftAccount service has restrictions that prevent the use of http://localhost:12345/ for test applications.
            As such, here is how to change this sample to uses http://mssecsample.localhost.this:12345/ instead.

            Edit the Project.json file and replace http://localhost:12345/ with http://mssecsample.localhost.this:12345/.

            From an admin command console first enter:
             notepad C:\Windows\System32\drivers\etc\hosts
            and add this to the file, save, and exit (and reboot?):
             127.0.0.1 MsSecSample.localhost.this

            Then you can choose to run the app as admin (see below) or add the following ACL as admin:
             netsh http add urlacl url=http://mssecsample.localhost.this:12345/ user=[domain\user]

            The sample app can then be run via:
             k web
            */
            app.UseOAuthAuthentication(new OAuthAuthenticationOptions<IOAuthAuthenticationNotifications>("Microsoft-AccessToken")
            {
                Caption = "MicrosoftAccount-AccessToken - Requires project changes",
                ClientId = "00000000480FF62E",
                ClientSecret = "bLw2JIvf8Y1TaToipPEqxTVlOeJwCUsr",
                CallbackPath = new PathString("/signin-microsoft-token"),
                AuthorizationEndpoint = MicrosoftAccountAuthenticationDefaults.AuthorizationEndpoint,
                TokenEndpoint = MicrosoftAccountAuthenticationDefaults.TokenEndpoint,
                Scope = { "wl.basic" },
            });

            app.UseMicrosoftAccountAuthentication(new MicrosoftAccountAuthenticationOptions()
            {
                Caption = "MicrosoftAccount - Requires project changes",
                ClientId = "00000000480FF62E",
                ClientSecret = "bLw2JIvf8Y1TaToipPEqxTVlOeJwCUsr",
            });

            app.UseOAuthAuthentication(new OAuthAuthenticationOptions<IOAuthAuthenticationNotifications>("GitHub-AccessToken")
            {
                ClientId = "8c0c5a572abe8fe89588",
                ClientSecret = "e1d95eaf03461d27acd6f49d4fc7bf19d6ac8cda",
                CallbackPath = new PathString("/signin-github-token"),
                AuthorizationEndpoint = "https://github.com/login/oauth/authorize",
                TokenEndpoint = "https://github.com/login/oauth/access_token",
            });

            app.UseOAuthAuthentication(new OAuthAuthenticationOptions<IOAuthAuthenticationNotifications>("GitHub")
            {
                ClientId = "49e302895d8b09ea5656",
                ClientSecret = "98f1bf028608901e9df91d64ee61536fe562064b",
                CallbackPath = new PathString("/signin-github"),
                AuthorizationEndpoint = "https://github.com/login/oauth/authorize",
                TokenEndpoint = "https://github.com/login/oauth/access_token",
                UserInformationEndpoint = "https://api.github.com/user",
                // Retrieving user information is unique to each provider.
                Notifications = new OAuthAuthenticationNotifications()
                {
                    OnGetUserInformationAsync = async (context) =>
                    {
                        // Get the GitHub user
                        HttpRequestMessage userRequest = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                        userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                        userRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        userRequest.Headers.UserAgent.ParseAdd("Microsoft ASP.NET OAuth middleware for GitHub");
                        HttpResponseMessage userResponse = await context.Backchannel.SendAsync(userRequest, context.HttpContext.RequestAborted);
                        userResponse.EnsureSuccessStatusCode();
                        var text = await userResponse.Content.ReadAsStringAsync();
                        JObject user = JObject.Parse(text);

                        var identity = new ClaimsIdentity(
                            context.Options.AuthenticationType,
                            ClaimsIdentity.DefaultNameClaimType,
                            ClaimsIdentity.DefaultRoleClaimType);

                        JToken value;
                        var id = user.TryGetValue("id", out value) ? value.ToString() : null;
                        if (!string.IsNullOrEmpty(id))
                        {
                            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, id, ClaimValueTypes.String, context.Options.AuthenticationType));
                        }
                        var userName = user.TryGetValue("login", out value) ? value.ToString() : null;
                        if (!string.IsNullOrEmpty(userName))
                        {
                            identity.AddClaim(new Claim(ClaimsIdentity.DefaultNameClaimType, userName, ClaimValueTypes.String, context.Options.AuthenticationType));
                        }
                        var name = user.TryGetValue("name", out value) ? value.ToString() : null;
                        if (!string.IsNullOrEmpty(name))
                        {
                            identity.AddClaim(new Claim("urn:github:name", name, ClaimValueTypes.String, context.Options.AuthenticationType));
                        }
                        var link = user.TryGetValue("url", out value) ? value.ToString() : null;
                        if (!string.IsNullOrEmpty(link))
                        {
                            identity.AddClaim(new Claim("urn:github:url", link, ClaimValueTypes.String, context.Options.AuthenticationType));
                        }

                        context.Identity = identity;
                    },
                },
            });

            // Choose an authentication type
            app.Map("/login", signoutApp =>
            {
                signoutApp.Run(async context =>
                {
                    string authType = context.Request.Query["authtype"];
                    if (!string.IsNullOrEmpty(authType))
                    {
                        // By default the client will be redirect back to the URL that issued the challenge (/login?authtype=foo),
                        // send them to the home page instead (/).
                        context.Response.Challenge(new AuthenticationProperties() { RedirectUri = "/" }, authType);
                        return;
                    }

                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync("<html><body>");
                    await context.Response.WriteAsync("Choose an authentication type: <br>");
                    foreach (var type in context.GetAuthenticationTypes())
                    {
                        await context.Response.WriteAsync("<a href=\"?authtype=" + type.AuthenticationType + "\">" + (type.Caption ?? "(suppressed)") + "</a><br>");
                    }
                    await context.Response.WriteAsync("</body></html>");
                });
            });

            // Sign-out to remove the user cookie.
            app.Map("/logout", signoutApp =>
            {
                signoutApp.Run(async context =>
                {
                    context.Response.SignOut(CookieAuthenticationDefaults.AuthenticationType);
                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync("<html><body>");
                    await context.Response.WriteAsync("You have been logged out. Goodbye " + context.User.Identity.Name + "<br>");
                    await context.Response.WriteAsync("<a href=\"/\">Home</a>");
                    await context.Response.WriteAsync("</body></html>");
                });
            });

            // Deny anonymous request beyond this point.
            app.Use(async (context, next) =>
            {
                if (!context.User.Identity.IsAuthenticated)
                {
                    // The cookie middleware will intercept this 401 and redirect to /login
                    context.Response.Challenge();
                    return;
                }
                await next();
            });

            // Display user information
            app.Run(async context =>
            {
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync("<html><body>");
                await context.Response.WriteAsync("Hello " + context.User.Identity.Name + "<br>");
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