// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BlazorUnitedApp;
using BlazorUnitedApp.Data;
using BlazorUnitedApp.Localization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Validation;
using Microsoft.Extensions.Validation.Localization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<WeatherForecastService>();

// Validation pipeline + IStringLocalizer-backed localization configured against the
// shared resource scope identified by SharedValidationMessages. The validation-demo
// page invokes the pipeline manually on form submission and renders the localized
// validation errors.
builder.Services.AddValidation();
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddValidationLocalization<SharedValidationMessages>();

// Drive the active culture per request. The validation-demo page links to
// ?culture=en|fr|de which the QueryStringRequestCultureProvider picks up; the
// validation localizer reads CultureInfo.CurrentUICulture at message-resolution time.
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    string[] supportedCultures = ["en", "fr", "de"];
    options.SetDefaultCulture(supportedCultures[0])
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();
app.UseRequestLocalization();

// POST endpoint that sets the .AspNetCore.Culture cookie and redirects back to the
// referrer. Standard pattern for cookie-based culture switching: the
// CookieRequestCultureProvider (registered by default by AddLocalization) reads the
// cookie on subsequent requests and sets CultureInfo.CurrentUICulture accordingly,
// so subsequent IStringLocalizer lookups (and our IValidationLocalizer) see the new
// culture. The query-string-based switch alone is not sticky — it only affects the
// single request that carries it.
//
// Antiforgery is disabled on this endpoint because the culture switch is intentionally
// a same-site form post that does not mutate user state. For production scenarios that
// need full CSRF protection, attach an antiforgery token from the calling form and
// drop the DisableAntiforgery() call.
app.MapPost("/_culture", (
    HttpContext ctx,
    [Microsoft.AspNetCore.Mvc.FromForm] string culture,
    [Microsoft.AspNetCore.Mvc.FromForm] string redirectUri) =>
{
    ctx.Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
        new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });
    return Results.LocalRedirect(redirectUri);
}).DisableAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode();

app.Run();
