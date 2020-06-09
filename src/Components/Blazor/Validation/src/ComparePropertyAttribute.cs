// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    /// A <see cref="ValidationAttribute"/> that compares two properties
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class ComparePropertyAttribute : CompareAttribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="BlazorCompareAttribute"/>.
        /// </summary>
        /// <param name="otherProperty">The property to compare with the current property.</param>
        public ComparePropertyAttribute(string otherProperty)
            : base(otherProperty)
        {
        }

        /// <inheritdoc />
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var validationResult = base.IsValid(value, validationContext);
            if (validationResult == ValidationResult.Success)
            {
                return validationResult;
            }

            return new ValidationResult(validationResult.ErrorMessage, new[] { validationContext.MemberName });
        }
    }
}

