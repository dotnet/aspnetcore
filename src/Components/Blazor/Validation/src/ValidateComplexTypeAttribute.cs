// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.Forms;

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    /// A <see cref="ValidationAttribute"/> that indicates that the property is a complex or collection type that further needs to be validated.
    /// <para>
    /// By default <see cref="Validator"/> does not recurse in to complex property types during validation.
    /// When used in conjunction with <see cref="ObjectGraphDataAnnotationsValidator"/>, this property allows the validation system to validate
    /// complex or collection type properties.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class ValidateComplexTypeAttribute : ValidationAttribute
    {
        /// <inheritdoc />
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (!ObjectGraphDataAnnotationsValidator.TryValidateRecursive(value, validationContext))
            {
                throw new InvalidOperationException($"{nameof(ValidateComplexTypeAttribute)} can only used with {nameof(ObjectGraphDataAnnotationsValidator)}.");
            }

            return ValidationResult.Success;
        }
    }
}
