// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;

namespace Microsoft.AspNetCore.Components.Forms.ClientValidation;

/// <summary>
/// Describes a single client-side validation rule produced by an <see cref="IClientValidationAdapter"/>.
/// </summary>
public sealed class ClientValidationRule
{
    private static readonly IReadOnlyDictionary<string, object?> EmptyParameters = ReadOnlyDictionary<string, object?>.Empty;

    private Dictionary<string, object?>? _parameters;

    /// <summary>
    /// Creates a new rule with the specified name and error message.
    /// </summary>
    /// <param name="name">
    /// The rule name. Must be non-empty and must match the name recognized by the client-side validator JavaScript.
    /// </param>
    /// <param name="errorMessage">
    /// The formatted error message displayed when the rule fails. Must not be <see langword="null"/>;
    /// pass <see cref="string.Empty"/> only if an empty message is intentional.
    /// </param>
    public ClientValidationRule(string name, string errorMessage)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(errorMessage);

        Name = name;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Gets the rule name. Rendered as the suffix of <c>data-val-&lt;Name&gt;</c>.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the formatted error message for this rule. Rendered as the value of <c>data-val-&lt;Name&gt;</c>.
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// Gets the parameters associated with this rule, keyed by parameter name.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Parameters => _parameters ?? EmptyParameters;

    /// <summary>
    /// Adds or replaces a parameter on the rule.
    /// </summary>
    /// <param name="name">The parameter name. Must be non-empty.</param>
    /// <param name="value">
    /// The parameter value. Must be JSON-primitive-shaped (string, bool, numeric primitives,
    /// or <see langword="null"/>).
    /// </param>
    /// <returns>The same rule, to allow fluent chaining.</returns>
    public ClientValidationRule WithParameter(string name, object? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        (_parameters ??= new Dictionary<string, object?>(StringComparer.Ordinal))[name] = value;
        return this;
    }
}
