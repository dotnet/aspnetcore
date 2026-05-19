// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Localization;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations;

/// <summary>
/// An implementation of <see cref="IClientModelValidator"/> which understands data annotation attributes.
/// </summary>
/// <typeparam name="TAttribute">The type of the attribute.</typeparam>
public abstract class ValidationAttributeAdapter<TAttribute> : IClientModelValidator
    where TAttribute : ValidationAttribute
{
    private readonly IStringLocalizer? _stringLocalizer;
    /// <summary>
    /// Create a new instance of <see cref="ValidationAttributeAdapter{TAttribute}"/>.
    /// </summary>
    /// <param name="attribute">The <typeparamref name="TAttribute"/> instance to validate.</param>
    /// <param name="stringLocalizer">The <see cref="IStringLocalizer"/>.</param>
    public ValidationAttributeAdapter(TAttribute attribute, IStringLocalizer? stringLocalizer)
    {
        Attribute = attribute;
        _stringLocalizer = stringLocalizer;
    }

    /// <summary>
    /// Gets the <typeparamref name="TAttribute"/> instance.
    /// </summary>
    public TAttribute Attribute { get; }

    /// <inheritdoc />
    public abstract void AddValidation(ClientModelValidationContext context);

    /// <summary>
    /// Adds the given <paramref name="key"/> and <paramref name="value"/> into
    /// <paramref name="attributes"/> if <paramref name="attributes"/> does not contain a value for
    /// <paramref name="key"/>.
    /// </summary>
    /// <param name="attributes">The HTML attributes dictionary.</param>
    /// <param name="key">The attribute key.</param>
    /// <param name="value">The attribute value.</param>
    /// <returns><c>true</c> if an attribute was added, otherwise <c>false</c>.</returns>
    protected static bool MergeAttribute(IDictionary<string, string> attributes, string key, string value)
    {
        if (attributes.ContainsKey(key))
        {
            return false;
        }

        attributes.Add(key, value);
        return true;
    }

    /// <summary>
    /// Gets the error message formatted using the <see cref="Attribute"/>.
    /// </summary>
    /// <param name="modelMetadata">The <see cref="ModelMetadata"/> associated with the model annotated with
    /// <see cref="Attribute"/>.</param>
    /// <param name="arguments">The value arguments which will be used in constructing the error message.</param>
    /// <returns>Formatted error string.</returns>
    protected virtual string GetErrorMessage(ModelMetadata modelMetadata, params object[] arguments)
    {
        ArgumentNullException.ThrowIfNull(modelMetadata);

        if (_stringLocalizer != null &&
            !string.IsNullOrEmpty(Attribute.ErrorMessage) &&
            string.IsNullOrEmpty(Attribute.ErrorMessageResourceName) &&
            Attribute.ErrorMessageResourceType == null)
        {
            return _stringLocalizer[Attribute.ErrorMessage, arguments];
        }

        return Attribute.FormatErrorMessage(modelMetadata.GetDisplayName());
    }
}
