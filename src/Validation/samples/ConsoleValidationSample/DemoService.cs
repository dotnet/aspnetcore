// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASP0029

using System.ComponentModel.DataAnnotations;
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
        var validationOptions = options.Value;

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

        lifetime.StopApplication();

        async Task ValidateAndPrint<T>(T instance)
        {
            var resultContext = await Validate(instance, validationOptions, cancellationToken);
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
            Console.WriteLine("Validation errors:");
            foreach (var (key, messages) in errors)
            {
                foreach (var message in messages)
                {
                    Console.WriteLine($"'{key}': '{message}'");
                }
            }
        }
    }
}
