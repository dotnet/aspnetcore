// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ConsoleValidationSample;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddOptions();
        builder.Services.AddLogging();
        builder.Services.AddLocalization();
        builder.Services.AddValidation();

        builder.Services.AddHostedService<DemoService>();

        await builder.Build().RunAsync();
    }
}
