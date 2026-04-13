// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation;
using StandardAttributeLocalization;
using StandardAttributeLocalization.Resources;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

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
    /// This method wraps the existing <see cref="ValidationOptions.LocalizerProvider"/> to add
    /// a fallback localizer that looks up pre-translated messages from the library's resource files.
    /// It also composes with any existing <see cref="ValidationOptions.ErrorMessageKeyProvider"/>
    /// to add a convention-based key fallback (<c>{AttributeTypeName}_ValidationError</c>).
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
    /// builder.Services.AddLocalization();
    /// builder.Services.AddStandardAttributeLocalization();
    /// </code>
    /// </example>
    public static IServiceCollection AddStandardAttributeLocalization(this IServiceCollection services)
    {
        services.AddLocalization();
        services.TryAddEnumerable(ServiceDescriptor.Transient<IPostConfigureOptions<ValidationOptions>, StandardAttributeLocalizationConfiguration>());

        return services;
    }
}

/// <summary>
/// An <see cref="IStringLocalizer"/> that tries a primary localizer first
/// and falls back to a secondary one when the resource is not found.
/// </summary>
internal sealed class FallbackStringLocalizer(IStringLocalizer primary, IStringLocalizer fallback) : IStringLocalizer
{
    public LocalizedString this[string name]
    {
        get
        {
            var result = primary[name];

            return result.ResourceNotFound ? fallback[name] : result;
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var result = primary[name, arguments];

            return result.ResourceNotFound ? fallback[name, arguments] : result;
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        => primary.GetAllStrings(includeParentCultures)
            .Concat(fallback.GetAllStrings(includeParentCultures));
}
