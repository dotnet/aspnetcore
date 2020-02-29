using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace OpenIdConnect.AzureAdSample
{
    public class Startup
    {
        public Startup(IConfiguration config)
        {
            Configuration = config;
        }

        public IConfiguration Configuration { get; set; }

        private string ClientId => Configuration["oidc:clientid"];
        private string ClientSecret => Configuration["oidc:clientsecret"];
        private string Authority => Configuration["oidc:authority"];
        private string Resource => "https://graph.windows.net";

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(sharedOptions =>
            {
                sharedOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                sharedOptions.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
                .AddCookie()
                .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, "AAD", o =>
            {
                o.ClientId = ClientId;
                o.ClientSecret = ClientSecret; // for code flow
                o.Authority = Authority;
                o.ResponseType = OpenIdConnectResponseType.CodeIdToken;
                o.SignedOutRedirectUri = "/signed-out";
                // GetClaimsFromUserInfoEndpoint = true,
                o.Events = new OpenIdConnectEvents()
                {
                    OnAuthorizationCodeReceived = async context =>
                    {
                        var request = context.HttpContext.Request;
                        var currentUri = UriHelper.BuildAbsolute(request.Scheme, request.Host, request.PathBase, request.Path);
                        var credential = new ClientCredential(ClientId, ClientSecret);
                        var authContext = new AuthenticationContext(Authority, AuthPropertiesTokenCache.ForCodeRedemption(context.Properties));

                        var result = await authContext.AcquireTokenByAuthorizationCodeAsync(
                            context.ProtocolMessage.Code, new Uri(currentUri), credential, Resource);

                        context.HandleCodeRedemption(result.AccessToken, result.IdToken);
                    }
                };
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();

            app.UseAuthentication();

            app.Run(async context =>
            {
                if (context.Request.Path.Equals("/signin"))
                {
                    if (context.User.Identities.Any(identity => identity.IsAuthenticated))
                    {
                        // User has already signed in
                        context.Response.Redirect("/");
                        return;
                    }

                    await context.ChallengeAsync(new AuthenticationProperties { RedirectUri = "/" });
                }
                else if (context.Request.Path.Equals("/signout"))
                {
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    await WriteHtmlAsync(context.Response,
                        async response =>
                        {
                            await response.WriteAsync($"<h1>Signed out locally: {HtmlEncode(context.User.Identity.Name)}</h1>");
                            await response.WriteAsync("<a class=\"btn btn-primary\" href=\"/\">Sign In</a>");
                        });
                }
                else if (context.Request.Path.Equals("/signout-remote"))
                {
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
                }
                else if (context.Request.Path.Equals("/signed-out"))
                {
                    await WriteHtmlAsync(context.Response,
                        async response =>
                        {
                            await response.WriteAsync($"<h1>You have been signed out.</h1>");
                            await response.WriteAsync("<a class=\"btn btn-primary\" href=\"/signin\">Sign In</a>");
                        });
                }
                else if (context.Request.Path.Equals("/remote-signedout"))
                {
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    await WriteHtmlAsync(context.Response,
                        async response =>
                        {
                            await response.WriteAsync($"<h1>Signed out remotely: {HtmlEncode(context.User.Identity.Name)}</h1>");
                            await response.WriteAsync("<a class=\"btn btn-primary\" href=\"/\">Sign In</a>");
                        });
                }
                else
                {
                    if (!context.User.Identities.Any(identity => identity.IsAuthenticated))
                    {
                        await context.ChallengeAsync(new AuthenticationProperties { RedirectUri = "/" });
                        return;
                    }

                    await WriteHtmlAsync(context.Response, async response =>
                    {
                        await response.WriteAsync($"<h1>Hello Authenticated User {HtmlEncode(context.User.Identity.Name)}</h1>");
                        await response.WriteAsync("<a class=\"btn btn-default\" href=\"/signout\">Sign Out Locally</a>");
                        await response.WriteAsync("<a class=\"btn btn-default\" href=\"/signout-remote\">Sign Out Remotely</a>");

                        await response.WriteAsync("<h2>Claims:</h2>");
                        await WriteTableHeader(response, new string[] { "Claim Type", "Value" }, context.User.Claims.Select(c => new string[] { c.Type, c.Value }));

                        await response.WriteAsync("<h2>Tokens:</h2>");
                        try
                        {
                            // Use ADAL to get the right token
                            var authContext = new AuthenticationContext(Authority, AuthPropertiesTokenCache.ForApiCalls(context, CookieAuthenticationDefaults.AuthenticationScheme));
                            var credential = new ClientCredential(ClientId, ClientSecret);
                            string userObjectID = context.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
                            var result = await authContext.AcquireTokenSilentAsync(Resource, credential, new UserIdentifier(userObjectID, UserIdentifierType.UniqueId));

                            await response.WriteAsync($"<h3>access_token</h3><code>{HtmlEncode(result.AccessToken)}</code><br>");
                        }
                        catch (Exception ex)
                        {
                            await response.WriteAsync($"AcquireToken error: {ex.Message}");
                        }
                    });
                }
            });
        }

        private static async Task WriteHtmlAsync(HttpResponse response, Func<HttpResponse, Task> writeContent)
        {
            var bootstrap = "<link rel=\"stylesheet\" href=\"https://stackpath.bootstrapcdn.com/bootstrap/3.4.1/css/bootstrap.min.css\" integrity=\"sha384-HSMxcRTRxnN+Bdg0JdbxYKrThecOKuH5zCYotlSAcp1+c8xmyTe9GYg1l9a69psu\" crossorigin=\"anonymous\">";

            response.ContentType = "text/html";
            await response.WriteAsync($"<html><head>{bootstrap}</head><body><div class=\"container\">");
            await writeContent(response);
            await response.WriteAsync("</div></body></html>");
        }

        private static async Task WriteTableHeader(HttpResponse response, IEnumerable<string> columns, IEnumerable<IEnumerable<string>> data)
        {
            await response.WriteAsync("<table class=\"table table-condensed\">");
            await response.WriteAsync("<tr>");
            foreach (var column in columns)
            {
                await response.WriteAsync($"<th>{HtmlEncode(column)}</th>");
            }
            await response.WriteAsync("</tr>");
            foreach (var row in data)
            {
                await response.WriteAsync("<tr>");
                foreach (var column in row)
                {
                    await response.WriteAsync($"<td>{HtmlEncode(column)}</td>");
                }
                await response.WriteAsync("</tr>");
            }
            await response.WriteAsync("</table>");
        }

        private static string HtmlEncode(string content) =>
            string.IsNullOrEmpty(content) ? string.Empty : HtmlEncoder.Default.Encode(content);
    }
}

