// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Validation.Tests;

internal static class GeneratedValidationTestHelpers
{
    public static (IServiceProvider Provider, ValidationOptions Options) CreateValidationServices(Action<ValidationOptions>? configureOptions = null)
    {
        var services = new ServiceCollection();
        services.AddValidation(configureOptions);
        var provider = services.BuildServiceProvider();
        return (provider, provider.GetRequiredService<IOptions<ValidationOptions>>().Value);
    }

    public static IValidatableTypeInfo GetTypeInfo<T>(ValidationOptions options)
    {
        Assert.True(options.TryGetValidatableTypeInfo(typeof(T), out var typeInfo));
        return typeInfo;
    }

    public static ValidateContext CreateContext(IServiceProvider provider, ValidationOptions options)
        => new()
        {
            ServiceProvider = provider,
            ValidationOptions = options,
        };
}

internal sealed class CannedValidatableTypeInfo : IValidatableTypeInfo
{
    public void Validate(object? value, ValidateContext context) { }
    public Task ValidateAsync(object? value, ValidateContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    public bool TryFindProperty(string propertyName, ValidationOptions validationOptions, [NotNullWhen(true)] out IValidatablePropertyInfo? validatablePropertyInfo)
    {
        validatablePropertyInfo = null;
        return false;
    }
}

internal sealed class RecordingValidationLocalizer : IValidationLocalizer
{
    public string? DisplayNameResult { get; set; }
    public string? ErrorMessageResult { get; set; }
    public List<DisplayNameLocalizationContext> DisplayNameCalls { get; } = [];
    public List<ErrorMessageLocalizationContext> ErrorMessageCalls { get; } = [];

    public string? ResolveDisplayName(in DisplayNameLocalizationContext context)
    {
        DisplayNameCalls.Add(context);
        return DisplayNameResult;
    }

    public string? ResolveErrorMessage(in ErrorMessageLocalizationContext context)
    {
        ErrorMessageCalls.Add(context);
        return ErrorMessageResult;
    }
}
