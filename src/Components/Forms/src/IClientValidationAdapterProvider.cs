// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Factory that creates <see cref="IClientValidationAdapter"/> instances
/// for <see cref="ValidationAttribute"/>s.
/// </summary>
public interface IClientValidationAdapterProvider
{
    /// <summary>
    /// Returns an <see cref="IClientValidationAdapter"/> for the given
    /// <paramref name="attribute"/>, or <see langword="null"/> if no adapter
    /// exists for that attribute type.
    /// </summary>
    /// <param name="attribute">The validation attribute to adapt.</param>
    /// <returns>An adapter, or <see langword="null"/>.</returns>
    IClientValidationAdapter? GetAdapter(ValidationAttribute attribute);
}
