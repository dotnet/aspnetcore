// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;

namespace CookiePolicySample;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie();
        services.Configure<CookiePolicyOptions>(options =>
        {
            options.CheckConsentNeeded = context => context.Request.PathBase.Equals("/NeedsConsent");

            options.OnAppendCookie = context => { };
        });
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseCookiePolicy();
        app.UseAuthentication();

        app.Map("/NeedsConsent", NestedApp);
        app.Map("/NeedsNoConsent", NestedApp);
        NestedApp(app);
    }

    private void NestedApp(IApplicationBuilder app)
    {
        app.Run(async context =>
        {
            var path = context.Request.Path;
            switch (path)
            {
                case "/Login":
                    var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "bob") },
                        CookieAuthenticationDefaults.AuthenticationScheme));
                    await context.SignInAsync(user);
                    break;
                case "/Logout":
                    await context.SignOutAsync();
                    break;
                case "/CreateTempCookie":
                    context.Response.Cookies.Append("Temp", "1");
                    break;
                case "/RemoveTempCookie":
                    context.Response.Cookies.Delete("Temp");
                    break;
                case "/CreateEssentialCookie":
                    context.Response.Cookies.Append("EssentialCookie", "2",
                        new CookieOptions() { IsEssential = true });
                    break;
                case "/RemoveEssentialCookie":
                    context.Response.Cookies.Delete("EssentialCookie");
                    break;
                case "/GrantConsent":
                    context.Features.Get<ITrackingConsentFeature>().GrantConsent();
                    break;
                case "/WithdrawConsent":
                    context.Features.Get<ITrackingConsentFeature>().WithdrawConsent();
                    break;
            }

            // TODO: Debug log when cookie is suppressed

            await HomePage(context);
        });
    }

    private async Task HomePage(HttpContext context)
    {
        var response = context.Response;
        var cookies = context.Request.Cookies;
        var pathBase = context.Request.PathBase.ToUriComponent();
        var path = context.Request.Path.ToUriComponent();
        var encodedPathBase = HtmlEncode(pathBase);
        var encodedNeedsConsentPath = HtmlEncode($"/NeedsConsent{path}");
        var encodedNeedsNoConsentPath = HtmlEncode($"/NeedsNoConsent{path}");
        response.ContentType = "text/html";
        await response.WriteAsync("<html><body>\r\n");

        await response.WriteAsync($"<a href=\"{encodedPathBase}/\">Home</a><br>\r\n");
        await response.WriteAsync($"<a href=\"{encodedPathBase}/Login\">Login</a><br>\r\n");
        await response.WriteAsync($"<a href=\"{encodedPathBase}/Logout\">Logout</a><br>\r\n");
        await response.WriteAsync($"<a href=\"{encodedPathBase}/CreateTempCookie\">Create Temp Cookie</a><br>\r\n");
        await response.WriteAsync($"<a href=\"{encodedPathBase}/RemoveTempCookie\">Remove Temp Cookie</a><br>\r\n");
        await response.WriteAsync($"<a href=\"{encodedPathBase}/CreateEssentialCookie\">Create Essential Cookie</a><br>\r\n");
        await response.WriteAsync($"<a href=\"{encodedPathBase}/RemoveEssentialCookie\">Remove Essential Cookie</a><br>\r\n");
        await response.WriteAsync($"<a href=\"{encodedPathBase}/GrantConsent\">Grant Consent</a><br>\r\n");
        await response.WriteAsync($"<a href=\"{encodedPathBase}/WithdrawConsent\">Withdraw Consent</a><br>\r\n");
        await response.WriteAsync("<br>\r\n");
        await response.WriteAsync($"<a href=\"{encodedNeedsConsentPath}\">Needs Consent</a><br>\r\n");
        await response.WriteAsync($"<a href=\"{encodedNeedsNoConsentPath}\">Needs No Consent</a><br>\r\n");
        await response.WriteAsync("<br>\r\n");

        var feature = context.Features.Get<ITrackingConsentFeature>();
        await response.WriteAsync($"Consent: <br>\r\n");
        await response.WriteAsync($" - IsNeeded: {feature.IsConsentNeeded} <br>\r\n");
        await response.WriteAsync($" - Has: {feature.HasConsent} <br>\r\n");
        await response.WriteAsync($" - Can Track: {feature.CanTrack} <br>\r\n");
        await response.WriteAsync("<br>\r\n");

        await response.WriteAsync($"{cookies.Count} Request Cookies:<br>\r\n");
        foreach (var cookie in cookies)
        {
            await response.WriteAsync($" - {HtmlEncode(cookie.Key)} = {HtmlEncode(cookie.Value)} <br>\r\n");
        }
        await response.WriteAsync("<br>\r\n");

        var responseCookies = response.Headers.SetCookie;
        await response.WriteAsync($"{responseCookies.Count} Response Cookies:<br>\r\n");
        foreach (var cookie in responseCookies)
        {
            await response.WriteAsync($" - {cookie} <br>\r\n");
        }

        await response.WriteAsync("</body></html>");
    }

    private static string HtmlEncode(string content) =>
        string.IsNullOrEmpty(content) ? string.Empty : HtmlEncoder.Default.Encode(content);
}
