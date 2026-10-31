// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Localization;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations;

/// <summary>
/// Provider for supplying <see cref="IAttributeAdapter"/>'s.
/// </summary>
public interface IValidationAttributeAdapterProvider
{
    /// <summary>
    /// Returns the <see cref="IAttributeAdapter"/> for the given <see cref=" ValidationAttribute"/>.
    /// </summary>
    /// <param name="attribute">The <see cref="ValidationAttribute"/> to create an <see cref="IAttributeAdapter"/>
    /// for.</param>
    /// <param name="stringLocalizer">The <see cref="IStringLocalizer"/> which will be used to create messages.
    /// </param>
    /// <returns>An <see cref="IAttributeAdapter"/> for the given <paramref name="attribute"/>.</returns>
    IAttributeAdapter? GetAttributeAdapter(ValidationAttribute attribute, IStringLocalizer? stringLocalizer);
}
