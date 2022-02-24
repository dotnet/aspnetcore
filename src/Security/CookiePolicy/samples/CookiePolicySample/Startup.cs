// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
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
        response.ContentType = "text/html";
        await response.WriteAsync("<html><body>\r\n");

        await response.WriteAsync($"<a href=\"{context.Request.PathBase}/\">Home</a><br>\r\n");
        await response.WriteAsync($"<a href=\"{context.Request.PathBase}/Login\">Login</a><br>\r\n");
        await response.WriteAsync($"<a href=\"{context.Request.PathBase}/Logout\">Logout</a><br>\r\n");
        await response.WriteAsync($"<a href=\"{context.Request.PathBase}/CreateTempCookie\">Create Temp Cookie</a><br>\r\n");
        await response.WriteAsync($"<a href=\"{context.Request.PathBase}/RemoveTempCookie\">Remove Temp Cookie</a><br>\r\n");
        await response.WriteAsync($"<a href=\"{context.Request.PathBase}/CreateEssentialCookie\">Create Essential Cookie</a><br>\r\n");
        await response.WriteAsync($"<a href=\"{context.Request.PathBase}/RemoveEssentialCookie\">Remove Essential Cookie</a><br>\r\n");
        await response.WriteAsync($"<a href=\"{context.Request.PathBase}/GrantConsent\">Grant Consent</a><br>\r\n");
        await response.WriteAsync($"<a href=\"{context.Request.PathBase}/WithdrawConsent\">Withdraw Consent</a><br>\r\n");
        await response.WriteAsync("<br>\r\n");
        await response.WriteAsync($"<a href=\"/NeedsConsent{context.Request.Path}\">Needs Consent</a><br>\r\n");
        await response.WriteAsync($"<a href=\"/NeedsNoConsent{context.Request.Path}\">Needs No Consent</a><br>\r\n");
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
            await response.WriteAsync($" - {cookie.Key} = {cookie.Value} <br>\r\n");
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
}
