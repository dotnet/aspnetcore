// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation;
using Microsoft.Extensions.Validation.Localization.Attributes;
using StandardAttributeLocalization.Resources;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Post-configures <see cref="ValidationOptions"/> to provide localized error messages
/// for standard <see cref="System.ComponentModel.DataAnnotations.ValidationAttribute"/> types
/// using the <see cref="StandardValidationMessages"/> resource file.
/// </summary>
internal sealed class StandardAttributeLocalizationConfiguration(
    IStringLocalizerFactory? stringLocalizerFactory = null,
    IValidationAttributeFormatterProvider? attributeFormatterProvider = null)
    : IPostConfigureOptions<ValidationOptions>
{
    public void PostConfigure(string? name, ValidationOptions options)
    {
        if (stringLocalizerFactory is null)
        {
            return;
        }

        var originalMessageProvider = options.ErrorMessageProvider;

        options.ErrorMessageProvider = (context) =>
        {
            if (context.Attribute.ErrorMessageResourceType is not null || !string.IsNullOrEmpty(context.Attribute.ErrorMessage))
            {
                return originalMessageProvider?.Invoke(context);
            }

            var localizer = stringLocalizerFactory.Create(typeof(StandardValidationMessages));

            var lookupKey = $"{context.Attribute.GetType().Name}_ValidationError";
            var localizedTemplate = localizer[lookupKey];

            if (localizedTemplate.ResourceNotFound)
            {
                return originalMessageProvider?.Invoke(context);
            }

            var displayName = context.DisplayName ?? context.MemberName;

            // Format the localized template with attribute-specific arguments
            var attributeFormatter = context.Attribute is IValidationAttributeFormatter formatter
                ? formatter
                : attributeFormatterProvider?.GetFormatter(context.Attribute);

            return attributeFormatter?.FormatErrorMessage(CultureInfo.CurrentCulture, localizedTemplate, displayName)
                ?? string.Format(CultureInfo.CurrentCulture, localizedTemplate, displayName);
        };
    }
}
