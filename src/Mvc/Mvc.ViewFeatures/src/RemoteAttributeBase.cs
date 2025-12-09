// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Resources = Microsoft.AspNetCore.Mvc.ViewFeatures.Resources;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// A <see cref="ValidationAttribute"/> which configures Unobtrusive validation to send an Ajax request to the
/// web site. The invoked endpoint should return JSON indicating whether the value is valid.
/// </summary>
/// <remarks>Does no server-side validation of the final form submission.</remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public abstract class RemoteAttributeBase : ValidationAttribute, IClientModelValidator
{
    private string _additionalFields = string.Empty;
    private string[] _additionalFieldsSplit = Array.Empty<string>();
    private bool _checkedForLocalizer;
    private IStringLocalizer? _stringLocalizer;

    /// <summary>
    /// Initialize a new instance of <see cref="RemoteAttributeBase"/>.
    /// </summary>
    protected RemoteAttributeBase()
        : base(errorMessageAccessor: () => Resources.RemoteAttribute_RemoteValidationFailed)
    {
        RouteData = new RouteValueDictionary();
    }

    /// <summary>
    /// Gets the <see cref="RouteValueDictionary"/> used when generating the URL where client should send a
    /// validation request.
    /// </summary>
    protected RouteValueDictionary RouteData { get; }

    /// <summary>
    /// Gets or sets the HTTP method (<c>"Get"</c> or <c>"Post"</c>) client should use when sending a validation
    /// request.
    /// </summary>
    public string? HttpMethod { get; set; }

    /// <summary>
    /// Gets or sets the comma-separated names of fields the client should include in a validation request.
    /// </summary>
    [AllowNull]
    public string AdditionalFields
    {
        get => _additionalFields;
        set
        {
            _additionalFields = value ?? string.Empty;
            _additionalFieldsSplit = SplitAndTrimPropertyNames(value)
                .Select(FormatPropertyForClientValidation)
                .ToArray();
        }
    }

    /// <summary>
    /// Formats <paramref name="property"/> and <see cref="AdditionalFields"/> for use in generated HTML.
    /// </summary>
    /// <param name="property">
    /// Name of the property associated with this <see cref="RemoteAttribute"/> instance.
    /// </param>
    /// <returns>Comma-separated names of fields the client should include in a validation request.</returns>
    /// <remarks>
    /// Excludes any whitespace from <see cref="AdditionalFields"/> in the return value.
    /// Prefixes each field name in the return value with <c>"*."</c>.
    /// </remarks>
    public string FormatAdditionalFieldsForClientValidation(string property)
    {
        ArgumentException.ThrowIfNullOrEmpty(property);

        var delimitedAdditionalFields = string.Join(",", _additionalFieldsSplit);
        if (!string.IsNullOrEmpty(delimitedAdditionalFields))
        {
            delimitedAdditionalFields = "," + delimitedAdditionalFields;
        }

        var formattedString = FormatPropertyForClientValidation(property) + delimitedAdditionalFields;

        return formattedString;
    }

    /// <summary>
    /// Formats <paramref name="property"/> for use in generated HTML.
    /// </summary>
    /// <param name="property">One field name the client should include in a validation request.</param>
    /// <returns>Name of a field the client should include in a validation request.</returns>
    /// <remarks>Returns <paramref name="property"/> with a <c>"*."</c> prefix.</remarks>
    public static string FormatPropertyForClientValidation(string property)
    {
        ArgumentException.ThrowIfNullOrEmpty(property);

        return "*." + property;
    }

    /// <summary>
    /// Returns the URL where the client should send a validation request.
    /// </summary>
    /// <param name="context">The <see cref="ClientModelValidationContext"/> used to generate the URL.</param>
    /// <returns>The URL where the client should send a validation request.</returns>
    protected abstract string GetUrl(ClientModelValidationContext context);

    /// <inheritdoc />
    public override string FormatErrorMessage(string name)
    {
        return string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Always returns <c>true</c> since this <see cref="ValidationAttribute"/> does no validation itself.
    /// Related validations occur only when the client sends a validation request.
    /// </remarks>
    public override bool IsValid(object? value)
    {
        return true;
    }

    /// <summary>
    /// Adds Unobtrusive validation HTML attributes to <see cref="ClientModelValidationContext"/>.
    /// </summary>
    /// <param name="context">
    /// <see cref="ClientModelValidationContext"/> to add Unobtrusive validation HTML attributes to.
    /// </param>
    /// <remarks>
    /// Calls derived <see cref="ValidationAttribute"/> implementation of <see cref="GetUrl(ClientModelValidationContext)"/>.
    /// </remarks>
    public virtual void AddValidation(ClientModelValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        MergeAttribute(context.Attributes, "data-val", "true");

        CheckForLocalizer(context);
        var errorMessage = GetErrorMessage(context.ModelMetadata.GetDisplayName());
        MergeAttribute(context.Attributes, "data-val-remote", errorMessage);

        MergeAttribute(context.Attributes, "data-val-remote-url", GetUrl(context));

        if (!string.IsNullOrEmpty(HttpMethod))
        {
            MergeAttribute(context.Attributes, "data-val-remote-type", HttpMethod);
        }

        var additionalFields = FormatAdditionalFieldsForClientValidation(context.ModelMetadata.PropertyName!);
        MergeAttribute(context.Attributes, "data-val-remote-additionalfields", additionalFields);
    }

    private static void MergeAttribute(IDictionary<string, string> attributes, string key, string value)
    {
        if (!attributes.ContainsKey(key))
        {
            attributes.Add(key, value);
        }
    }

    private static IEnumerable<string> SplitAndTrimPropertyNames(string? original)
        => original?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

    private void CheckForLocalizer(ClientModelValidationContext context)
    {
        if (!_checkedForLocalizer)
        {
            _checkedForLocalizer = true;

            var services = context.ActionContext.HttpContext.RequestServices;
            var options = services.GetRequiredService<IOptions<MvcDataAnnotationsLocalizationOptions>>();
            var factory = services.GetService<IStringLocalizerFactory>();

            var provider = options.Value.DataAnnotationLocalizerProvider;
            if (factory != null && provider != null)
            {
                _stringLocalizer = provider(
                    context.ModelMetadata.ContainerType ?? context.ModelMetadata.ModelType,
                    factory);
            }
        }
    }

    private string GetErrorMessage(string displayName)
    {
        if (_stringLocalizer != null &&
            !string.IsNullOrEmpty(ErrorMessage) &&
            string.IsNullOrEmpty(ErrorMessageResourceName) &&
            ErrorMessageResourceType == null)
        {
            return _stringLocalizer[ErrorMessage, displayName];
        }

        return FormatErrorMessage(displayName);
    }
}

