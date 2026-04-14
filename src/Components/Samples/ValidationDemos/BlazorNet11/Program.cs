// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using BlazorNet11.Components;
using BlazorNet11.Data;
using BlazorNet11.Localization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddLocalization();
builder.Services.AddSingleton<IStringLocalizerFactory, JsonStringLocalizerFactory>();

builder.Services.AddValidation(options =>
{
    // Convention-based key fallback:
    // For attributes without explicit ErrorMessagem, generate a key like "RequiredError".
    options.ErrorMessageKeyProvider = ctx => ctx.Attribute.GetType().Name.Replace("Attribute", "Error");
});

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { new CultureInfo("en"), new CultureInfo("es") };
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

var app = builder.Build();

app.UseRequestLocalization();
app.UseAntiforgery();

// Cookie-based culture switching
app.MapGet("/Culture/Set", (HttpContext context, string culture, string redirectUri) =>
{
    context.Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
        new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });

    context.Response.Redirect(redirectUri);
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Minimal API endpoint: POST /api/feedback
// Try with Accept-Language: es header to get Spanish error messages.
app.MapPost("/api/feedback", (FeedbackModel model) =>
    Results.Ok(new { message = "Feedback received.", model.Name, model.Rating }));

app.Run();
