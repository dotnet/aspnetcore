// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation;

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
        services.TryAddEnumerable(ServiceDescriptor.Transient<IPostConfigureOptions<ValidationOptions>, StandardAttributeLocalizationConfiguration>());

        return services;
    }
}
