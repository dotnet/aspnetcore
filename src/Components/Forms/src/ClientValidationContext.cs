// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Context passed to <see cref="IClientValidationAdapter"/> implementations
/// to emit <c>data-val-*</c> HTML attributes. A single context instance is
/// reused across all adapters for a given field; the per-attribute error
/// message is passed as a separate argument to
/// <see cref="IClientValidationAdapter.AddClientValidation"/>.
/// </summary>
/// <example>
/// <code>
/// public void AddClientValidation(in ClientValidationContext context, string errorMessage)
/// {
///     context.MergeAttribute("data-val", "true");
///     context.MergeAttribute("data-val-required", errorMessage);
/// }
/// </code>
/// </example>
public readonly struct ClientValidationContext
{
    private readonly IDictionary<string, string> _attributes;

    /// <summary>
    /// Initializes a new instance of <see cref="ClientValidationContext"/>.
    /// </summary>
    /// <param name="attributes">The HTML attributes dictionary to populate.</param>
    public ClientValidationContext(IDictionary<string, string> attributes)
    {
        ArgumentNullException.ThrowIfNull(attributes);
        _attributes = attributes;
    }

    /// <summary>
    /// Adds an attribute to the dictionary if the key does not already exist.
    /// Analogous to MVC's <c>MergeAttribute</c>.
    /// </summary>
    /// <param name="key">The HTML attribute name (e.g., <c>data-val-required</c>).</param>
    /// <param name="value">The HTML attribute value.</param>
    /// <returns><see langword="true"/> if the attribute was added; <see langword="false"/> if the key already existed.</returns>
    public bool MergeAttribute(string key, string value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        if (_attributes.ContainsKey(key))
        {
            return false;
        }

        _attributes[key] = value;

        return true;
    }
}
