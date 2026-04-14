// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using BlazorInteractiveDemo;
using BlazorInteractiveDemo.Data;
using BlazorInteractiveDemo.Resources;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddLocalization();
builder.Services.AddValidation(options =>
{
    options.LocalizerProvider = (_, factory) => factory.Create(typeof(ValidationMessages));
    // Convention-based key fallback: for attributes without explicit ErrorMessage,
    // generate a key like "RequiredAttribute_ValidationError".
    options.ErrorMessageKeyProvider = ctx => $"{ctx.Attribute.GetType().Name}_ValidationError";
});

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { new CultureInfo("en"), new CultureInfo("es") };
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider
    {
        QueryStringKey = "culture",
        UIQueryStringKey = "culture"
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseRequestLocalization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Minimal API endpoint: demonstrates that the same AddValidation() + AddLocalization()
// setup also produces localized validation errors for Minimal API endpoints.
// Try: POST /api/contact with Accept-Language: es and an empty JSON body.
app.MapPost("/api/contact", (ContactModel model) =>
    Results.Ok(new { message = "Contact submitted.", model.Name, model.Email }));

app.Run();
