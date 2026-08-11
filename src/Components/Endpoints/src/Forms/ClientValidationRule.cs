// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Describes a single client-side validation rule produced by an <see cref="IClientValidationRuleProvider"/>.
/// </summary>
public sealed class ClientValidationRule
{
    private static readonly IReadOnlyDictionary<string, string> EmptyParameters =
        ReadOnlyDictionary<string, string>.Empty;

    /// <summary>
    /// Creates a rule with the specified name and optional parameters.
    /// </summary>
    /// <param name="name">
    /// The rule name. Must be non-empty and must match the name registered with the JS
    /// validator via <c>Blazor.formValidation.addValidator(name, ...)</c>.
    /// </param>
    /// <param name="parameters">
    /// Optional parameters passed to the JS validator at runtime. All values are strings on the
    /// wire; validators that need numeric or boolean values parse them at validation time
    /// (<c>parseInt</c>, <c>parseFloat</c>, etc.). When <see langword="null"/> or empty, the
    /// <c>params</c> object is omitted from the wire format.
    /// </param>
    public ClientValidationRule(
        string name,
        IReadOnlyDictionary<string, string>? parameters = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        Name = name;
        Parameters = parameters ?? EmptyParameters;
    }

    /// <summary>
    /// Gets the rule name. Matches the name registered with the JS validator.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the parameters passed to the JS validator at runtime. Empty when no parameters apply.
    /// </summary>
    public IReadOnlyDictionary<string, string> Parameters { get; }
}
