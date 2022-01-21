// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.Cookies;
using RazorPagesWebSite.Conventions;

namespace RazorPagesWebSite;

public class StartupWithBasePath
{
    private readonly IWebHostEnvironment _hostingEnvironment;

    public StartupWithBasePath(IWebHostEnvironment hostingEnvironment)
    {
        _hostingEnvironment = hostingEnvironment;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options => options.LoginPath = "/Login");
        var builder = services.AddMvc()
            .AddCookieTempDataProvider()
            .AddRazorPagesOptions(options =>
            {
                options.Conventions.AuthorizePage("/Conventions/Auth");
                options.Conventions.AuthorizeFolder("/Conventions/AuthFolder");
                options.Conventions.AuthorizeAreaFolder("Accounts", "/RequiresAuth");
                options.Conventions.AllowAnonymousToAreaPage("Accounts", "/RequiresAuth/AllowAnonymous");
                options.Conventions.Add(new CustomModelTypeConvention());
            });
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute("areaRoute", "{area:exists}/{controller=Home}/{action=Index}");
            endpoints.MapRazorPages();
        });
    }
}
