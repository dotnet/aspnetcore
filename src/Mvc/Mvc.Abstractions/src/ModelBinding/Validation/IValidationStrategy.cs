// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

/// <summary>
/// Defines a strategy for enumerating the child entries of a model object which should be validated.
/// </summary>
public interface IValidationStrategy
{
    /// <summary>
    /// Gets an <see cref="IEnumerator{ValidationEntry}"/> containing a <see cref="ValidationEntry"/> for
    /// each child entry of the model object to be validated.
    /// </summary>
    /// <param name="metadata">The <see cref="ModelMetadata"/> associated with <paramref name="model"/>.</param>
    /// <param name="key">The model prefix associated with <paramref name="model"/>.</param>
    /// <param name="model">The model object.</param>
    /// <returns>An <see cref="IEnumerator{ValidationEntry}"/>.</returns>
    IEnumerator<ValidationEntry> GetChildren(ModelMetadata metadata, string key, object model);
}
