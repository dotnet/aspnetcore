// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// Supplies CSS class names for form fields to represent their validation state or other
    /// state information from an <see cref="EditContext"/>.
    /// </summary>
    public class FieldCssClassProvider
    {
        internal readonly static FieldCssClassProvider Instance = new FieldCssClassProvider();

        /// <summary>
        /// Gets a string that indicates the status of the specified field as a CSS class.
        /// </summary>
        /// <param name="editContext">The <see cref="EditContext"/>.</param>
        /// <param name="fieldIdentifier">The <see cref="FieldIdentifier"/>.</param>
        /// <returns>A CSS class name string.</returns>
        public virtual string GetFieldCssClass(EditContext editContext, in FieldIdentifier fieldIdentifier)
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
