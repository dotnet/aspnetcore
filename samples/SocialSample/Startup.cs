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

namespace SocialSample
{
    /* Note all servers must use the same address and port because these are pre-registered with the various providers. */
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true);

            if (env.IsDevelopment())
            {
                // For more details on using the user secret store see http://go.microsoft.com/fwlink/?LinkID=532709
                builder.AddUserSecrets<Startup>();
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
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

            if (string.IsNullOrEmpty(Configuration["facebook:appid"]))
            {
                // User-Secrets: https://docs.asp.net/en/latest/security/app-secrets.html
                // See below for registration instructions for each provider.
                throw new InvalidOperationException("User secrets must be configured for each authentication provider.");
            }

            // You must first create an app with Facebook and add its ID and Secret to your user-secrets.
            // https://developers.facebook.com/apps/
            app.UseFacebookAuthentication(new FacebookOptions
            {
                AppId = Configuration["facebook:appid"],
                AppSecret = Configuration["facebook:appsecret"],
                Scope = { "email" },
                Fields = { "name", "email" },
                SaveTokens = true,
            });

            // You must first create an app with Google and add its ID and Secret to your user-secrets.
            // https://console.developers.google.com/project
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

            // You must first create an app with Google and add its ID and Secret to your user-secrets.
            // https://console.developers.google.com/project
            var googleOptions = new GoogleOptions
            {
                ClientId = Configuration["google:clientid"],
                ClientSecret = Configuration["google:clientsecret"],
                SaveTokens = true,
                Events = new OAuthEvents()
                {
                    OnRemoteFailure = ctx =>
                    {
                        ctx.Response.Redirect("/error?FailureMessage=" + UrlEncoder.Default.Encode(ctx.Failure.Message));
                        ctx.HandleResponse();
                        return Task.FromResult(0);
                    }
                }
            };
            googleOptions.ClaimActions.MapJsonSubKey("urn:google:image", "image", "url");
            googleOptions.ClaimActions.Remove(ClaimTypes.GivenName);
            app.UseGoogleAuthentication(googleOptions);

            // You must first create an app with Twitter and add its key and Secret to your user-secrets.
            // https://apps.twitter.com/
            var twitterOptions = new TwitterOptions
            {
                ConsumerKey = Configuration["twitter:consumerkey"],
                ConsumerSecret = Configuration["twitter:consumersecret"],
                // http://stackoverflow.com/questions/22627083/can-we-get-email-id-from-twitter-oauth-api/32852370#32852370
                // http://stackoverflow.com/questions/36330675/get-users-email-from-twitter-api-for-external-login-authentication-asp-net-mvc?lq=1
                RetrieveUserDetails = true,
                SaveTokens = true,
                Events = new TwitterEvents()
                {
                    OnRemoteFailure = ctx =>
                    {
                        ctx.Response.Redirect("/error?FailureMessage=" + UrlEncoder.Default.Encode(ctx.Failure.Message));
                        ctx.HandleResponse();
                        return Task.FromResult(0);
                    }
                }
            };
            twitterOptions.ClaimActions.MapJsonKey("urn:twitter:profilepicture", "profile_image_url", ClaimTypes.Uri);
            app.UseTwitterAuthentication(twitterOptions);

            /* Azure AD app model v2 has restrictions that prevent the use of plain HTTP for redirect URLs.
               Therefore, to authenticate through microsoft accounts, tryout the sample using the following URL:
               https://localhost:44318/
            */
            // You must first create an app with Microsoft Account and add its ID and Secret to your user-secrets.
            // https://apps.dev.microsoft.com/
            app.UseOAuthAuthentication(new OAuthOptions
            {
                AuthenticationScheme = "Microsoft-AccessToken",
                DisplayName = "MicrosoftAccount-AccessToken",
                ClientId = Configuration["microsoftaccount:clientid"],
                ClientSecret = Configuration["microsoftaccount:clientsecret"],
                CallbackPath = new PathString("/signin-microsoft-token"),
                AuthorizationEndpoint = MicrosoftAccountDefaults.AuthorizationEndpoint,
                TokenEndpoint = MicrosoftAccountDefaults.TokenEndpoint,
                Scope = { "https://graph.microsoft.com/user.read" },
                SaveTokens = true
            });

            // You must first create an app with Microsoft Account and add its ID and Secret to your user-secrets.
            // https://azure.microsoft.com/en-us/documentation/articles/active-directory-v2-app-registration/
            app.UseMicrosoftAccountAuthentication(new MicrosoftAccountOptions
            {
                DisplayName = "MicrosoftAccount",
                ClientId = Configuration["microsoftaccount:clientid"],
                ClientSecret = Configuration["microsoftaccount:clientsecret"],
                SaveTokens = true
            });

            // You must first create an app with GitHub and add its ID and Secret to your user-secrets.
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

            // You must first create an app with GitHub and add its ID and Secret to your user-secrets.
            // https://github.com/settings/applications/
            var githubOptions = new OAuthOptions
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
                SaveTokens = true,
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

                        context.RunClaimActions(user);
                    }
                }
            };
            githubOptions.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
            githubOptions.ClaimActions.MapJsonKey(ClaimTypes.Name, "login");
            githubOptions.ClaimActions.MapJsonKey("urn:github:name", "name");
            githubOptions.ClaimActions.MapJsonKey(ClaimTypes.Email, "email", ClaimValueTypes.Email);
            githubOptions.ClaimActions.MapJsonKey("urn:github:url", "url");
            app.UseOAuthAuthentication(githubOptions);

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


            app.Run(async context =>
            {
                // CookieAuthenticationOptions.AutomaticAuthenticate = true (default) causes User to be set
                var user = context.User;

                // This is what [Authorize] calls
                // var user = await context.Authentication.AuthenticateAsync(AuthenticationManager.AutomaticScheme);

                // This is what [Authorize(ActiveAuthenticationSchemes = MicrosoftAccountDefaults.AuthenticationScheme)] calls
                // var user = await context.Authentication.AuthenticateAsync(MicrosoftAccountDefaults.AuthenticationScheme);

                // Deny anonymous request beyond this point.
                if (user == null || !user.Identities.Any(identity => identity.IsAuthenticated))
                {
                    // This is what [Authorize] calls
                    // The cookie middleware will intercept this 401 and redirect to /login
                    await context.Authentication.ChallengeAsync();

                    // This is what [Authorize(ActiveAuthenticationSchemes = MicrosoftAccountDefaults.AuthenticationScheme)] calls
                    // await context.Authentication.ChallengeAsync(MicrosoftAccountDefaults.AuthenticationScheme);

                    return;
                }

                // Display user information
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
                await context.Response.WriteAsync("<a href=\"/logout\">Logout</a><br>");
                await context.Response.WriteAsync("</body></html>");
            });
        }
    }
}

