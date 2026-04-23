// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms.ClientValidation;

/// <summary>
/// Implemented by <see cref="System.ComponentModel.DataAnnotations.ValidationAttribute"/> subclasses
/// that support client-side validation by emitting <c>data-val-*</c> HTML attributes.
/// </summary>
public interface IClientValidationAdapter
{
    /// <summary>
    /// Adds client-side validation attributes to the rendering context.
    /// </summary>
    void AddClientValidationAttributes(in ClientValidationContext context);
}
