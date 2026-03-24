// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using BlazorServerDemo.Localization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register JSON-based localizer
builder.Services.AddSingleton<IStringLocalizerFactory, JsonStringLocalizerFactory>();

// Configure request localization
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { new CultureInfo("en"), new CultureInfo("es") };
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

builder.Services.AddValidation();
builder.Services.AddValidationLocalization(options =>
{
    options.LocalizerProvider = (type, factory) => factory.Create("Validation", "BlazorServerDemo");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
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

app.MapStaticAssets();
app.MapRazorComponents<BlazorServerDemo.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
