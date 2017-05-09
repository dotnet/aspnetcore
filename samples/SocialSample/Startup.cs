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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            if (string.IsNullOrEmpty(Configuration["facebook:appid"]))
            {
                // User-Secrets: https://docs.asp.net/en/latest/security/app-secrets.html
                // See below for registration instructions for each provider.
                throw new InvalidOperationException("User secrets must be configured for each authentication provider.");
            }

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            });

            services.AddCookieAuthentication(o => o.LoginPath = new PathString("/login"));

            // You must first create an app with Facebook and add its ID and Secret to your user-secrets.
            // https://developers.facebook.com/apps/
            services.AddFacebookAuthentication(o =>
            {
                o.AppId = Configuration["facebook:appid"];
                o.AppSecret = Configuration["facebook:appsecret"];
                o.Scope.Add("email");
                o.Fields.Add("name");
                o.Fields.Add("email");
                o.SaveTokens = true;
            });

            // You must first create an app with Google and add its ID and Secret to your user-secrets.
            // https://console.developers.google.com/project
            services.AddOAuthAuthentication("Google-AccessToken", o =>
            {
                o.DisplayName = "Google-AccessToken";
                o.ClientId = Configuration["google:clientid"];
                o.ClientSecret = Configuration["google:clientsecret"];
                o.CallbackPath = new PathString("/signin-google-token");
                o.AuthorizationEndpoint = GoogleDefaults.AuthorizationEndpoint;
                o.TokenEndpoint = GoogleDefaults.TokenEndpoint;
                o.Scope.Add("openid");
                o.Scope.Add("profile");
                o.Scope.Add("email");
                o.SaveTokens = true;
            });

            // You must first create an app with Google and add its ID and Secret to your user-secrets.
            // https://console.developers.google.com/project
            services.AddGoogleAuthentication(o =>
            {
                o.ClientId = Configuration["google:clientid"];
                o.ClientSecret = Configuration["google:clientsecret"];
                o.SaveTokens = true;
                o.Events = new OAuthEvents()
                {
                    OnRemoteFailure = ctx =>
                    {
                        ctx.Response.Redirect("/error?FailureMessage=" + UrlEncoder.Default.Encode(ctx.Failure.Message));
                        ctx.HandleResponse();
                        return Task.FromResult(0);
                    }
                };
                o.ClaimActions.MapJsonSubKey("urn:google:image", "image", "url");
                o.ClaimActions.Remove(ClaimTypes.GivenName);
            });

            // You must first create an app with Twitter and add its key and Secret to your user-secrets.
            // https://apps.twitter.com/
            services.AddTwitterAuthentication(o =>
            {
                o.ConsumerKey = Configuration["twitter:consumerkey"];
                o.ConsumerSecret = Configuration["twitter:consumersecret"];
                // http://stackoverflow.com/questions/22627083/can-we-get-email-id-from-twitter-oauth-api/32852370#32852370
                // http://stackoverflow.com/questions/36330675/get-users-email-from-twitter-api-for-external-login-authentication-asp-net-mvc?lq=1
                o.RetrieveUserDetails = true;
                o.SaveTokens = true;
                o.ClaimActions.MapJsonKey("urn:twitter:profilepicture", "profile_image_url", ClaimTypes.Uri);
                o.Events = new TwitterEvents()
                {
                    OnRemoteFailure = ctx =>
                    {
                        ctx.Response.Redirect("/error?FailureMessage=" + UrlEncoder.Default.Encode(ctx.Failure.Message));
                        ctx.HandleResponse();
                        return Task.FromResult(0);
                    }
                };
            });

            /* Azure AD app model v2 has restrictions that prevent the use of plain HTTP for redirect URLs.
               Therefore, to authenticate through microsoft accounts, tryout the sample using the following URL:
               https://localhost:44318/
            */
            // You must first create an app with Microsoft Account and add its ID and Secret to your user-secrets.
            // https://apps.dev.microsoft.com/
            services.AddOAuthAuthentication("Microsoft-AccessToken", o =>
            {
                o.DisplayName = "MicrosoftAccount-AccessToken";
                o.ClientId = Configuration["microsoftaccount:clientid"];
                o.ClientSecret = Configuration["microsoftaccount:clientsecret"];
                o.CallbackPath = new PathString("/signin-microsoft-token");
                o.AuthorizationEndpoint = MicrosoftAccountDefaults.AuthorizationEndpoint;
                o.TokenEndpoint = MicrosoftAccountDefaults.TokenEndpoint;
                o.Scope.Add("https://graph.microsoft.com/user.read");
                o.SaveTokens = true;
            });

            // You must first create an app with Microsoft Account and add its ID and Secret to your user-secrets.
            // https://azure.microsoft.com/en-us/documentation/articles/active-directory-v2-app-registration/
            services.AddMicrosoftAccountAuthentication(o =>
            {
                o.ClientId = Configuration["microsoftaccount:clientid"];
                o.ClientSecret = Configuration["microsoftaccount:clientsecret"];
                o.SaveTokens = true;
            });

            // You must first create an app with GitHub and add its ID and Secret to your user-secrets.
            // https://github.com/settings/applications/
            services.AddOAuthAuthentication("GitHub-AccessToken", o =>
            {
                o.DisplayName = "Github-AccessToken";
                o.ClientId = Configuration["github-token:clientid"];
                o.ClientSecret = Configuration["github-token:clientsecret"];
                o.CallbackPath = new PathString("/signin-github-token");
                o.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
                o.TokenEndpoint = "https://github.com/login/oauth/access_token";
                o.SaveTokens = true;
                o.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
                o.ClaimActions.MapJsonKey(ClaimTypes.Name, "login");
                o.ClaimActions.MapJsonKey("urn:github:name", "name");
                o.ClaimActions.MapJsonKey(ClaimTypes.Email, "email", ClaimValueTypes.Email);
                o.ClaimActions.MapJsonKey("urn:github:url", "url");
            });

            // You must first create an app with GitHub and add its ID and Secret to your user-secrets.
            // https://github.com/settings/applications/
            services.AddOAuthAuthentication("GitHub", o =>
            {
                o.DisplayName = "Github";
                o.ClientId = Configuration["github:clientid"];
                o.ClientSecret = Configuration["github:clientsecret"];
                o.CallbackPath = new PathString("/signin-github");
                o.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
                o.TokenEndpoint = "https://github.com/login/oauth/access_token";
                o.UserInformationEndpoint = "https://api.github.com/user";
                o.ClaimsIssuer = "OAuth2-Github";
                o.SaveTokens = true;
                // Retrieving user information is unique to each provider.
                o.Events = new OAuthEvents
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
                };
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();

            app.UseAuthentication();

            // Choose an authentication type
            app.Map("/login", signinApp =>
            {
                signinApp.Run(async context =>
                {
                    var authType = context.Request.Query["authscheme"];
                    if (!string.IsNullOrEmpty(authType))
                    {
                        // By default the client will be redirect back to the URL that issued the challenge (/login?authtype=foo),
                        // send them to the home page instead (/).
                        await context.ChallengeAsync(authType, new AuthenticationProperties() { RedirectUri = "/" });
                        return;
                    }

                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync("<html><body>");
                    await context.Response.WriteAsync("Choose an authentication scheme: <br>");
                    var schemeProvider = context.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
                    foreach (var provider in await schemeProvider.GetAllSchemesAsync())
                    {
                        // REVIEW: we lost access to display name (which is buried in the handler options)
                        await context.Response.WriteAsync("<a href=\"?authscheme=" + provider.Name + "\">" + (provider.DisplayName ?? "(suppressed)") + "</a><br>");
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
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
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
                // Setting DefaultAuthenticateScheme causes User to be set
                var user = context.User;

                // This is what [Authorize] calls
                // var user = await context.AuthenticateAsync();

                // This is what [Authorize(ActiveAuthenticationSchemes = MicrosoftAccountDefaults.AuthenticationScheme)] calls
                // var user = await context.AuthenticateAsync(MicrosoftAccountDefaults.AuthenticationScheme);

                // Deny anonymous request beyond this point.
                if (user == null || !user.Identities.Any(identity => identity.IsAuthenticated))
                {
                    // This is what [Authorize] calls
                    // The cookie middleware will intercept this 401 and redirect to /login
                    await context.ChallengeAsync();

                    // This is what [Authorize(ActiveAuthenticationSchemes = MicrosoftAccountDefaults.AuthenticationScheme)] calls
                    // await context.ChallengeAsync(MicrosoftAccountDefaults.AuthenticationScheme);

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
                
                await context.Response.WriteAsync("Access Token: " + await context.GetTokenAsync("access_token") + "<br>");
                await context.Response.WriteAsync("Refresh Token: " + await context.GetTokenAsync("refresh_token") + "<br>");
                await context.Response.WriteAsync("Token Type: " + await context.GetTokenAsync("token_type") + "<br>");
                await context.Response.WriteAsync("expires_at: " + await context.GetTokenAsync("expires_at") + "<br>");
                await context.Response.WriteAsync("<a href=\"/logout\">Logout</a><br>");
                await context.Response.WriteAsync("</body></html>");
            });
        }
    }
}

