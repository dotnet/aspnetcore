// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation;
using StandardAttributeLocalization.Resources;

namespace StandardAttributeLocalization;

/// <summary>
/// Post-configures <see cref="ValidationOptions"/> to provide localized error messages
/// for standard <see cref="System.ComponentModel.DataAnnotations.ValidationAttribute"/> types
/// using the <see cref="StandardValidationMessages"/> resource file.
/// </summary>
internal sealed class StandardAttributeLocalizationConfiguration(
    ILoggerFactory loggerFactory)
    : IPostConfigureOptions<ValidationOptions>
{
    public void PostConfigure(string? name, ValidationOptions options)
    {
        // Wrap the existing LocalizerProvider to add our fallback resources.
        // User's per-type resources are checked first; if not found, the library's
        // pre-translated messages are used as a fallback.
        var originalProvider = options.LocalizerProvider;
        var localizationOptions = new OptionsWrapper<LocalizationOptions>(new LocalizationOptions());
        var resourceLocalizerFactory = new ResourceManagerStringLocalizerFactory(localizationOptions, loggerFactory);
        var packageLocalizer = resourceLocalizerFactory.Create(typeof(StandardValidationMessages));

        options.LocalizerProvider = (type, factory) =>
        {
            var originalLocalizer = originalProvider?.Invoke(type, factory) ?? factory.Create(type);
            return new FallbackStringLocalizer(originalLocalizer, packageLocalizer);
        };

        // Compose with any existing key provider - add our convention as a fallback
        // for attributes that don't have an explicit ErrorMessage.
        var originalKeyProvider = options.ErrorMessageKeyProvider;

        options.ErrorMessageKeyProvider = ctx =>
            originalKeyProvider?.Invoke(ctx)
            ?? $"{ctx.Attribute.GetType().Name}_ValidationError";
    }
}
