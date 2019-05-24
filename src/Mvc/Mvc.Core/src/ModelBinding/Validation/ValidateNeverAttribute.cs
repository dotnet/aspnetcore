// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// <see cref="IPropertyValidationFilter"/> implementation that unconditionally indicates a property should be
    /// excluded from validation. When applied to a property, the validation system excludes that property. When
    /// applied to a type, the validation system excludes all properties within that type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class ValidateNeverAttribute : Attribute, IPropertyValidationFilter
    {
        /// <inheritdoc />
        public bool ShouldValidateEntry(ValidationEntry entry, ValidationEntry parentEntry)
        {
            return false;
        }
    }
}
