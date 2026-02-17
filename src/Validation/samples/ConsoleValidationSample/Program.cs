// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using ConsoleValidationSample;
using ConsoleValidationSample.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder();

builder.Services.AddOptions();
builder.Services.AddLogging();

builder.Services.AddValidation();
builder.Services.AddValidationLocalization<ValidationMessages>(options =>
{
    options.ErrorMessageKeySelector = (context) => $"{context.Attribute.GetType().Name}_Error";
});

builder.Services.AddHostedService<DemoService>();

await builder.Build().RunAsync();
