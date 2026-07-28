// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Represents the context for validating a validatable object.
/// </summary>
public sealed class ValidateContext
{
    private Dictionary<string, IReadOnlyList<ValidationError>>? _validationErrors;

    /// <summary>
    /// Initializes a new instance of <see cref="ValidateContext"/>.
    /// </summary>
    public ValidateContext()
    {
    }

    /// <summary>
    /// Gets or sets the service provider. This will also be made available on the <see cref="System.ComponentModel.DataAnnotations.ValidationContext"/> instances.
    /// </summary>
    public IServiceProvider? ServiceProvider { get; init; }

    /// <summary>
    /// Gets or sets the prefix used to identify the current object being validated in a complex object graph.
    /// </summary>
    /// <remarks>
    /// This prefix is used to build property paths in validation error messages (for example, "Customer.Address.Street").
    /// </remarks>
    public string CurrentValidationPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the validation options that control validation behavior,
    /// including validation depth limits and resolver registration.
    /// </summary>
    public required ValidationOptions ValidationOptions { get; init; }

    /// <summary>
    /// Gets the dictionary of validation errors collected during validation.
    /// </summary>
    /// <remarks>
    /// Keys are property names or paths, and values are the collection of validation errors reported for that path.
    /// There are no guarantees whether or not this dictionary is lazy. Usages should treat null and empty dictionary the same.
    /// </remarks>
    public IReadOnlyDictionary<string, IReadOnlyList<ValidationError>>? ValidationErrors
        => _validationErrors;

    /// <summary>
    /// Gets or sets the current depth in the validation hierarchy.
    /// </summary>
    /// <remarks>
    /// This value is used to prevent stack overflows from circular references.
    /// </remarks>
    public int CurrentDepth { get; set; }

    /// <summary>
    /// Adds a validation error to <see cref="ValidationErrors"/>.
    /// </summary>
    /// <param name="validationError">The validation error to add.</param>
    public void AddValidationError(ValidationError validationError)
    {
        _validationErrors ??= new Dictionary<string, IReadOnlyList<ValidationError>>();

        if (!_validationErrors.TryGetValue(validationError.Path, out var existingErrors))
        {
            _validationErrors.Add(validationError.Path, new List<ValidationError> { validationError });
        }
        else
        {
            ((List<ValidationError>)existingErrors).Add(validationError);
        }
    }
}
