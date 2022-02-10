// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Supplies CSS class names for form fields to represent their validation state or other
/// state information from an <see cref="EditContext"/>.
/// </summary>
public class FieldCssClassProvider
{
    internal static readonly FieldCssClassProvider Instance = new FieldCssClassProvider();

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
