// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// Provides extension methods to describe the state of <see cref="EditContext"/>
    /// fields as CSS class names.
    /// </summary>
    public static class EditContextFieldClassExtensions
    {
        /// <summary>
        /// Gets a string that indicates the status of the specified field as a CSS class. This will include
        /// some combination of "modified", "valid", or "invalid", depending on the status of the field.
        /// </summary>
        /// <param name="editContext">The <see cref="EditContext"/>.</param>
        /// <param name="accessor">An identifier for the field.</param>
        /// <returns>A string that indicates the status of the field.</returns>
        public static string FieldCssClass<TField>(this EditContext editContext, Expression<Func<TField>> accessor)
            => FieldCssClass(editContext, FieldIdentifier.Create(accessor));

        /// <summary>
        /// Gets a string that indicates the status of the specified field as a CSS class. This will include
        /// some combination of "modified", "valid", or "invalid", depending on the status of the field.
        /// </summary>
        /// <param name="editContext">The <see cref="EditContext"/>.</param>
        /// <param name="fieldIdentifier">An identifier for the field.</param>
        /// <returns>A string that indicates the status of the field.</returns>
        public static string FieldCssClass(this EditContext editContext, in FieldIdentifier fieldIdentifier)
        {
            var isValid = !editContext.GetValidationMessages(fieldIdentifier).Any();
            if (editContext.IsModified(fieldIdentifier))
            {
                return isValid ? "modified valid" : "modified invalid";
            }
            else
            {
                return isValid ? "valid" : "invalid";
            }
        }
    }
}
