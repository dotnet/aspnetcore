// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ConsoleValidationSample;
using ConsoleValidationSample.Resources;
using ConsoleValidationSample.Validators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Validation.Localization;

var builder = Host.CreateApplicationBuilder();

builder.Services.AddOptions();
builder.Services.AddLogging();

builder.Services.AddValidation();
builder.Services.AddValidationLocalization<ValidationMessages>();
builder.Services.AddSingleton<IAttributeArgumentProvider, CustomAttributeArgumentProvider>();

builder.Services.AddHostedService<DemoService>();

await builder.Build().RunAsync();
