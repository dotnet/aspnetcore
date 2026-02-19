// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ConsoleValidationSample;
using ConsoleValidationSample.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder();

builder.Services.AddOptions();
builder.Services.AddLogging();

// Use the StandardAttributeLocalization library to automatically localize
// all standard DataAnnotations validation error messages without needing
// to specify ErrorMessage keys on each attribute instance.
builder.Services.AddValidation();
builder.Services.AddValidationLocalization<ValidationMessages>(options =>
{
    options.ErrorMessageKeyProvider = (in context) => $"{context.Attribute.GetType().Name}_ValidationError";
});
builder.Services.AddStandardAttributeLocalization();

builder.Services.AddHostedService<DemoService>();

await builder.Build().RunAsync();
