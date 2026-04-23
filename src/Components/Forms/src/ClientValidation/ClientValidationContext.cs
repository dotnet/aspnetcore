// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms.ClientValidation;

/// <summary>
/// Context for adding client-side validation HTML attributes.
/// </summary>
public class ClientValidationContext
{
    private readonly Dictionary<string, object> _attributes;

    internal ClientValidationContext(Dictionary<string, object> attributes, string errorMessage)
    {
        _attributes = attributes;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// The formatted error message for the validation attribute.
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// Adds an HTML attribute. Uses first-wins semantics: if the key already exists, the call is ignored.
    /// </summary>
    public void MergeAttribute(string key, string value)
    {
        _attributes.TryAdd(key, value);
    }
}
