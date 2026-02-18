// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using BlazorValidationSample.Components;
using BlazorValidationSample.Resources;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddValidation();
builder.Services.AddValidationLocalization<ValidationMessages>();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { new CultureInfo("en"), new CultureInfo("es") };
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

var app = builder.Build();

app.UseStaticFiles();
app.UseRequestLocalization();
app.UseAntiforgery();

app.MapGet("/Culture/Set", (HttpContext context, string culture, string redirectUri) =>
{
    context.Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
        new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });

    return Results.LocalRedirect(redirectUri);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
