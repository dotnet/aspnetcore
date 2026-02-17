// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Validation;
using Microsoft.Extensions.Validation.Localization.Attributes;
using StandardAttributeLocalization.Resources;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for adding localization of standard
/// <see cref="System.ComponentModel.DataAnnotations.ValidationAttribute"/> error messages.
/// </summary>
public static class StandardAttributeLocalizationExtensions
{
    /// <summary>
    /// Adds localization support for standard <see cref="System.ComponentModel.DataAnnotations"/>
    /// validation attribute error messages. Pre-translated resource files are included for
    /// Arabic, Chinese (Simplified), Czech, English, French, German, Italian, Japanese, Korean,
    /// Polish, Portuguese, Russian, Spanish, and Turkish.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method calls <see cref="ValidationServiceCollectionExtensions.AddValidationLocalization{TResource}"/>
    /// using a shared resource file that maps each standard attribute type to a
    /// <c>{AttributeTypeName}_ValidationError</c> resource key.
    /// </para>
    /// <para>
    /// Users do not need to set <see cref="System.ComponentModel.DataAnnotations.ValidationAttribute.ErrorMessage"/>
    /// on individual attribute instances; the library automatically resolves the correct key.
    /// </para>
    /// <para>
    /// <see cref="ValidationServiceCollectionExtensions.AddValidation"/> must be called separately,
    /// either before or after this method, so the source generator can intercept the call and
    /// register the validatable type information.
    /// </para>
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    /// <example>
    /// <code>
    /// var builder = Host.CreateApplicationBuilder();
    /// builder.Services.AddValidation();
    /// builder.Services.AddStandardAttributeLocalization();
    /// </code>
    /// </example>
    public static IServiceCollection AddStandardAttributeLocalization(this IServiceCollection services)
    {
        services.PostConfigure<ValidationOptions>(options =>
        {
            var originalMessageProvider = options.ErrorMessageProvider;

            options.ErrorMessageProvider = (context) =>
            {
                if (context.Attribute.ErrorMessageResourceType is not null || !string.IsNullOrEmpty(context.Attribute.ErrorMessage))
                {
                    return originalMessageProvider?.Invoke(context);
                }

                var localizer = context.Services?.GetService<IStringLocalizerFactory>()?.Create(typeof(StandardValidationMessages));

                if (localizer is null)
                {
                    return originalMessageProvider?.Invoke(context);
                }

                var lookupKey = $"{context.Attribute.GetType().Name}_ValidationError";
                var localizedTemplate = localizer[lookupKey];

                if (localizedTemplate.ResourceNotFound)
                {
                    return originalMessageProvider?.Invoke(context);
                }

                var displayName = context.DisplayName ?? context.MemberName;
                var attributeFormatterProvider = context.Services?.GetService<IValidationAttributeFormatterProvider>();

                // Format the localized template with attribute-specific arguments
                var attributeFormatter = context.Attribute is IValidationAttributeFormatter formatter
                    ? formatter
                    : attributeFormatterProvider?.GetFormatter(context.Attribute);

                return attributeFormatter?.FormatErrorMessage(CultureInfo.CurrentCulture, localizedTemplate, displayName)
                    ?? string.Format(CultureInfo.CurrentCulture, localizedTemplate, displayName);
            };
        });

        return services;
    }
}
