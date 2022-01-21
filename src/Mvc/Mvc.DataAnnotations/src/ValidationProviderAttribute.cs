// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations;

/// <summary>
/// Abstract class for grouping attributes of type <see cref="ValidationAttribute"/> into
/// one <see cref="Attribute"/>
/// </summary>
public abstract class ValidationProviderAttribute : Attribute
{
    /// <summary>
    /// Gets <see cref="ValidationAttribute" /> instances associated with this attribute.
    /// </summary>
    /// <returns>Sequence of <see cref="ValidationAttribute" /> associated with this attribute.</returns>
    public abstract IEnumerable<ValidationAttribute> GetValidationAttributes();
}
