// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

/// <summary>
/// An entry in a <see cref="ValidationStateDictionary"/>. Records state information to override the default
/// behavior of validation for an object.
/// </summary>
public class ValidationStateEntry
{
    /// <summary>
    /// Gets or sets the model prefix associated with the entry.
    /// </summary>
    public string Key { get; set; } = default!;

    /// <summary>
    /// Gets or sets the <see cref="ModelMetadata"/> associated with the entry.
    /// </summary>
    public ModelMetadata Metadata { get; set; } = default!;

    /// <summary>
    /// Gets or sets a value indicating whether the associated model object should be validated.
    /// </summary>
    public bool SuppressValidation { get; set; }

    /// <summary>
    /// Gets or sets an <see cref="IValidationStrategy"/> for enumerating child entries of the associated
    /// model object.
    /// </summary>
    public IValidationStrategy Strategy { get; set; } = default!;
}
