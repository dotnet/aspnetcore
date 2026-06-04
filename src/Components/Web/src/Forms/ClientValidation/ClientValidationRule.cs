// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms.ClientValidation;

/// <summary>
/// Describes a single client-side validation rule produced by an
/// <see cref="IClientValidationAdapter"/> or built by the framework.
/// </summary>
public sealed class ClientValidationRule
{
    /// <summary>
    /// Creates a rule with the specified name, error message, and optional parameters.
    /// </summary>
    /// <param name="name">
    /// The rule name. Must be non-empty and must match the name registered with the JS
    /// validator via <c>Blazor.formValidation.addValidator(name, ...)</c>.
    /// </param>
    /// <param name="errorMessage">
    /// The formatted error message displayed when the rule fails. Must not be <see langword="null"/>;
    /// pass <see cref="string.Empty"/> only if an empty message is intentional.
    /// </param>
    /// <param name="parameters">
    /// Optional parameters passed to the JS validator at runtime. All values are strings on the
    /// wire; validators that need numeric or boolean values parse them at validation time
    /// (<c>parseInt</c>, <c>parseFloat</c>, etc.). When <see langword="null"/> or empty, the
    /// <c>params</c> object is omitted from the wire format.
    /// </param>
    public ClientValidationRule(
        string name,
        string errorMessage,
        IReadOnlyDictionary<string, string>? parameters = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(errorMessage);

        Name = name;
        ErrorMessage = errorMessage;
        Parameters = parameters;
    }

    /// <summary>
    /// Gets the rule name. Matches the name registered with the JS validator.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the formatted error message for this rule.
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// Gets the parameters passed to the JS validator at runtime. <see langword="null"/> when
    /// no parameters apply.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Parameters { get; }
}
