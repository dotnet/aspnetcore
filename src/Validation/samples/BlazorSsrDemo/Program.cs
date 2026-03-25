// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using BlazorSsrDemo;
using BlazorSsrDemo.Components;
using BlazorSsrDemo.Resources;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents();

// DEMO: Add validation services and localization
builder.Services.AddValidation();
builder.Services.AddValidationLocalization<ValidationMessages>();

// DEMO: Add client-side validation support (uncomment to enable)
// builder.Services.AddClientSideValidation();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { new CultureInfo("en"), new CultureInfo("es") };
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRequestLocalization();
app.UseAntiforgery();

app.MapGet("/Culture/Set", (string culture, string redirectUri, HttpContext context) =>
{
    context.Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
        new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });

    return Results.LocalRedirect(redirectUri);
});

app.MapStaticAssets();
app.MapRazorComponents<App>();

app.Run();
