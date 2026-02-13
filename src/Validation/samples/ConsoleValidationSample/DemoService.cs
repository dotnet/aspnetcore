// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASP0029

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using ConsoleValidationSample.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation;

namespace ConsoleValidationSample;

internal class DemoService(IOptions<ValidationOptions> options, ILogger<DemoService> logger, IHostApplicationLifetime lifetime, IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await RunValidations(CultureInfo.GetCultureInfo("en-US"));
        await RunValidations(CultureInfo.GetCultureInfo("es-MX"));

        lifetime.StopApplication();

        async Task RunValidations(CultureInfo culture)
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            Console.WriteLine($"\nRunning validations with culture: {culture.Name}");

            await ValidateAndPrint(new Customer
            {
                Id = 0
            });

            await ValidateAndPrint(new Customer
            {
                Id = 1,
                Name = "Bob"
            });

            await ValidateAndPrint(new InventoryItem
            {
                Id = 1,
                IsPremium = true
            });

            await ValidateAndPrint(new InventoryItem
            {
                Id = 1,
                Price = -100,
                IsPremium = true
            });
        }

        async Task ValidateAndPrint<T>(T instance)
        {
            var resultContext = await Validate(instance, options.Value, cancellationToken);
            PrintErrorMessages(resultContext);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task<ValidateContext> Validate<T>(T instance, ValidationOptions validationOptions, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var validationContext = new ValidationContext(instance, serviceProvider, null);
        var validateContext = new ValidateContext
        {
            ValidationContext = validationContext,
            ValidationOptions = validationOptions
        };

        if (!validationOptions.TryGetValidatableTypeInfo(typeof(T), out var typeInfo))
        {
            logger.LogError("Cannot resolve validatable type info ");
        }

        await typeInfo!.ValidateAsync(instance, validateContext, cancellationToken);

        return validateContext;
    }

    private static void PrintErrorMessages(ValidateContext validateContext)
    {
        var errors = validateContext.ValidationErrors ?? [];

        if (errors.Count == 0)
        {
            Console.WriteLine("No validation errors");
        }
        else
        {
            foreach (var (key, messages) in errors)
            {
                foreach (var message in messages)
                {
                    if (string.IsNullOrEmpty(key))
                    {
                        Console.WriteLine(message);
                    }
                    else
                    {
                        Console.WriteLine($"{key}: {message}");
                    }
                }
            }
        }
    }
}
