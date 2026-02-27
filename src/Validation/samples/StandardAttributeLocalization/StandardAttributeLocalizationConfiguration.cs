// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation;
using Microsoft.Extensions.Validation.Localization;
using StandardAttributeLocalization.Resources;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Post-configures <see cref="ValidationOptions"/> to provide localized error messages
/// for standard <see cref="System.ComponentModel.DataAnnotations.ValidationAttribute"/> types
/// using the <see cref="StandardValidationMessages"/> resource file.
/// </summary>
internal sealed class StandardAttributeLocalizationConfiguration(
    IOptions<ValidationAttributeFormatterRegistry> formatterRegistryOptions,
    ILoggerFactory loggerFactory)
    : IPostConfigureOptions<ValidationOptions>
{
    public void PostConfigure(string? name, ValidationOptions options)
    {
        // Manually create ResourceManagerStringLocalizerFactory instead of retrieving the factory from DI
        // so that the user can register other localizers not based on resource files.
        var localizationOptions = new OptionsWrapper<LocalizationOptions>(new LocalizationOptions());
        var resourceLocalizerFactory = new ResourceManagerStringLocalizerFactory(localizationOptions, loggerFactory);
        var localizer = resourceLocalizerFactory.Create(typeof(StandardValidationMessages));
        var originalProvider = options.ErrorMessageProvider ?? ((context) => null);
        var formatterRegistry = formatterRegistryOptions.Value;

        options.ErrorMessageProvider = (context) =>
        {
            if (!string.IsNullOrEmpty(context.Attribute.ErrorMessage))
            {
                return originalProvider(context);
            }

            var lookupKey = $"{context.Attribute.GetType().Name}_ValidationError";
            var localizedTemplate = localizer[lookupKey];

            if (localizedTemplate.ResourceNotFound)
            {
                return originalProvider(context);
            }

            var attributeFormatter = formatterRegistry.GetFormatter(context.Attribute);

            return attributeFormatter?.FormatErrorMessage(CultureInfo.CurrentCulture, localizedTemplate, context.DisplayName)
                ?? string.Format(CultureInfo.CurrentCulture, localizedTemplate, context.DisplayName);
        };
    }
}
