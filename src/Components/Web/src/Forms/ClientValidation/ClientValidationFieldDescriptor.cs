// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms.ClientValidation;

/// <summary>
/// Describes the client-side validation rules for one field within a form.
/// </summary>
public sealed class ClientValidationFieldDescriptor
{
    /// <summary>
    /// Creates a field descriptor.
    /// </summary>
    /// <param name="name">The field name as it appears in form posts. Should use dotted path for nested fields (e.g. <c>Address.Street</c>).</param>
    /// <param name="rules">The ordered list of client-side validation rules for this field.</param>
    public ClientValidationFieldDescriptor(
        string name,
        IReadOnlyList<ClientValidationRule> rules)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(rules);
        Name = name;
        Rules = rules;
    }

    /// <summary>Field name as it appears in form posts. Dotted path for nested fields (e.g. <c>Address.Street</c>).</summary>
    public string Name { get; }

    /// <summary>Ordered list of client-side validation rules.</summary>
    public IReadOnlyList<ClientValidationRule> Rules { get; }
}
