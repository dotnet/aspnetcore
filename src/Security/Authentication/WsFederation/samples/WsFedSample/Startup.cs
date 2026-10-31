// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.WsFederation;

namespace WsFedSample;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAuthentication(sharedOptions =>
        {
            sharedOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            sharedOptions.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            sharedOptions.DefaultChallengeScheme = WsFederationDefaults.AuthenticationScheme;
        })
        .AddWsFederation(options =>
        {
            options.Wtrealm = "https://Tratcheroutlook.onmicrosoft.com/WsFedSample";
            options.MetadataAddress = "https://login.windows.net/cdc690f9-b6b8-4023-813a-bae7143d1f87/FederationMetadata/2007-06/FederationMetadata.xml";
            // options.CallbackPath = "/";
            // options.SkipUnrecognizedRequests = true;
        })
        .AddCookie();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseDeveloperExceptionPage();
        app.UseAuthentication();

        app.Run(async context =>
        {
            if (context.Request.Path.Equals("/signedout"))
            {
                await WriteHtmlAsync(context.Response, async res =>
                {
                    await res.WriteAsync($"<h1>You have been signed out.</h1>");
                    await res.WriteAsync("<a class=\"btn btn-link\" href=\"/\">Sign In</a>");
                });
                return;
            }

            if (context.Request.Path.Equals("/signout"))
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await WriteHtmlAsync(context.Response, async res =>
                {
                    await context.Response.WriteAsync($"<h1>Signed out {HtmlEncode(context.User.Identity.Name)}</h1>");
                    await context.Response.WriteAsync("<a class=\"btn btn-link\" href=\"/\">Sign In</a>");
                });
                return;
            }

            if (context.Request.Path.Equals("/signout-remote"))
            {
                // Redirects
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await context.SignOutAsync(WsFederationDefaults.AuthenticationScheme, new AuthenticationProperties()
                {
                    RedirectUri = "/signedout"
                });
                return;
            }

            if (context.Request.Path.Equals("/Account/AccessDenied"))
            {
                await WriteHtmlAsync(context.Response, async res =>
                {
                    await context.Response.WriteAsync($"<h1>Access Denied for user {HtmlEncode(context.User.Identity.Name)} to resource '{HtmlEncode(context.Request.Query["ReturnUrl"])}'</h1>");
                    await context.Response.WriteAsync("<a class=\"btn btn-link\" href=\"/signout\">Sign Out</a>");
                });
                return;
            }

            // DefaultAuthenticateScheme causes User to be set
            var user = context.User;

            // This is what [Authorize] calls
            // var user = await context.AuthenticateAsync();

            // This is what [Authorize(ActiveAuthenticationSchemes = WsFederationDefaults.AuthenticationScheme)] calls
            // var user = await context.AuthenticateAsync(WsFederationDefaults.AuthenticationScheme);

            // Not authenticated
            if (user == null || !user.Identities.Any(identity => identity.IsAuthenticated))
            {
                // This is what [Authorize] calls
                await context.ChallengeAsync();

                // This is what [Authorize(ActiveAuthenticationSchemes = WsFederationDefaults.AuthenticationScheme)] calls
                // await context.ChallengeAsync(WsFederationDefaults.AuthenticationScheme);

                return;
            }

            // Authenticated, but not authorized
            if (context.Request.Path.Equals("/restricted") && !user.Identities.Any(identity => identity.HasClaim("special", "true")))
            {
                await context.ForbidAsync();
                return;
            }

            await WriteHtmlAsync(context.Response, async response =>
            {
                await response.WriteAsync($"<h1>Hello Authenticated User {HtmlEncode(user.Identity.Name)}</h1>");
                await response.WriteAsync("<a class=\"btn btn-default\" href=\"/restricted\">Restricted</a>");
                await response.WriteAsync("<a class=\"btn btn-default\" href=\"/signout\">Sign Out</a>");
                await response.WriteAsync("<a class=\"btn btn-default\" href=\"/signout-remote\">Sign Out Remote</a>");

                await response.WriteAsync("<h2>Claims:</h2>");
                await WriteTableHeader(response, new string[] { "Claim Type", "Value" }, context.User.Claims.Select(c => new string[] { c.Type, c.Value }));
            });
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
