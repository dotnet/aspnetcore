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
    /// Gets a string that indicates the status of the specified field as a CSS class: one of
    /// <c>"valid"</c>, <c>"invalid"</c>, <c>"pending"</c>, or <c>"faulted"</c>, optionally prefixed with
    /// <c>"modified "</c> when <see cref="EditContext.IsModified(in FieldIdentifier)"/> is <c>true</c>.
    /// <c>"pending"</c> (async validation registered via <see cref="EditContext.TrackFieldValidation"/> in
    /// flight) and <c>"faulted"</c> (the last async validation threw) both supersede valid/invalid since
    /// the outcome is not yet known; otherwise <c>"valid"</c>/<c>"invalid"</c> reflects whether the field
    /// has any validation messages.
    /// </summary>
    /// <param name="editContext">The <see cref="EditContext"/>.</param>
    /// <param name="fieldIdentifier">The <see cref="FieldIdentifier"/>.</param>
    /// <returns>A CSS class name string.</returns>
    public virtual string GetFieldCssClass(EditContext editContext, in FieldIdentifier fieldIdentifier)
    {
        var modified = editContext.IsModified(fieldIdentifier);

        if (editContext.IsValidationPending(fieldIdentifier))
        {
            return modified ? "modified pending" : "pending";
        }

        if (editContext.IsValidationFaulted(fieldIdentifier))
        {
            return modified ? "modified faulted" : "faulted";
        }

        var isValid = !editContext.GetValidationMessages(fieldIdentifier).Any();
        if (modified)
        {
            return isValid ? "modified valid" : "modified invalid";
        }

        return isValid ? "valid" : "invalid";
    }
}
