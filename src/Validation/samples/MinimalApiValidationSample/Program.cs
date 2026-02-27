// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Localization;
using MinimalApiValidationSample;
using MinimalApiValidationSample.Models;

var builder = WebApplication.CreateBuilder(args);

// Register the hardcoded localizer factory so validation messages resolve through it.
builder.Services.AddSingleton<IStringLocalizerFactory, HardcodedStringLocalizerFactory>();

// Wire up validation with localization support. AddValidationLocalization() configures
// ValidationOptions.ErrorMessageProvider to look up error message keys via IStringLocalizer.
builder.Services.AddValidation();
builder.Services.AddValidationLocalization();

var app = builder.Build();

// Range validation on a route parameter
app.MapGet("/customers/{id}", ([Range(1, int.MaxValue)] int id) =>
    $"Getting customer with ID: {id}");

// [ValidatableType] with nested object validation
app.MapPost("/customers", (Customer customer) =>
    TypedResults.Created($"/customers/{customer.Name}", customer));

// IValidatableObject with custom Validate() logic
app.MapPost("/orders", (Order order) =>
    TypedResults.Created($"/orders/{order.OrderId}", order));

// DisableValidation() bypasses the validation endpoint filter
app.MapPost("/products",
    ([EvenNumber(ErrorMessage = "Product ID must be even")] int productId, [Required] string name) =>
        TypedResults.Ok(new { productId, name }))
    .DisableValidation();

// ContactFormModel uses ErrorMessage keys ("RequiredError", "LengthError", etc.)
// that the HardcodedStringLocalizer resolves into culture-specific messages.
app.MapPost("/contact", (ContactFormModel model) =>
    TypedResults.Ok(new { message = "Contact form submitted.", model.Email }));

app.Run();

