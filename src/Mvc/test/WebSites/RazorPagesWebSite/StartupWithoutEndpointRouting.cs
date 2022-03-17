// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using RazorPagesWebSite.Conventions;

namespace RazorPagesWebSite;

public class StartupWithoutEndpointRouting
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options => options.LoginPath = "/Login");
        services.AddMvc(options => options.EnableEndpointRouting = false)
            .AddMvcLocalization()
            .AddRazorPagesOptions(options =>
            {
                options.Conventions.AuthorizePage("/HelloWorldWithAuth");
                options.Conventions.AuthorizeFolder("/Pages/Admin");
                options.Conventions.AllowAnonymousToPage("/Pages/Admin/Login");
                options.Conventions.AddPageRoute("/HelloWorldWithRoute", "Different-Route/{text}");
                options.Conventions.AddPageRoute("/Pages/NotTheRoot", string.Empty);
                options.Conventions.Add(new CustomModelTypeConvention());
            })
            .WithRazorPagesAtContentRoot();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseAuthentication();

        app.UseStaticFiles();

        var supportedCultures = new[]
        {
                new CultureInfo("en-US"),
                new CultureInfo("fr-FR"),
            };

        app.UseRequestLocalization(new RequestLocalizationOptions
        {
            SupportedCultures = supportedCultures,
            SupportedUICultures = supportedCultures
        });

        app.UseMvc();
    }
}
