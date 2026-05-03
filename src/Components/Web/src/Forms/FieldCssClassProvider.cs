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
    /// The result includes some combination of <c>"modified"</c>, <c>"valid"</c> / <c>"invalid"</c>,
    /// <c>"pending"</c>, and <c>"faulted"</c>:
    /// <list type="bullet">
    /// <item><c>"modified"</c> is added when <see cref="EditContext.IsModified(in FieldIdentifier)"/> is <c>true</c>.</item>
    /// <item><c>"valid"</c> or <c>"invalid"</c> reflects whether any validation messages exist for the field.</item>
    /// <item><c>"pending"</c> is added while an async validation task registered via
    /// <see cref="EditContext.AddValidationTask"/> is in flight.</item>
    /// <item><c>"faulted"</c> is added when the field's last async validation threw a non-cancellation exception.</item>
    /// </list>
    /// </summary>
    /// <param name="editContext">The <see cref="EditContext"/>.</param>
    /// <param name="fieldIdentifier">The <see cref="FieldIdentifier"/>.</param>
    /// <returns>A CSS class name string.</returns>
    public virtual string GetFieldCssClass(EditContext editContext, in FieldIdentifier fieldIdentifier)
    {
        var isValid = !editContext.GetValidationMessages(fieldIdentifier).Any();
        var isModified = editContext.IsModified(fieldIdentifier);
        var isPending = editContext.IsValidationPending(fieldIdentifier);
        var isFaulted = editContext.IsValidationFaulted(fieldIdentifier);

        // Fast path preserves the historical literals for the synchronous-only case.
        if (!isPending && !isFaulted)
        {
            if (isModified)
            {
                return isValid ? "modified valid" : "modified invalid";
            }

            return isValid ? "valid" : "invalid";
        }

        var modified = isModified ? "modified " : "";
        var validity = isValid ? "valid" : "invalid";
        var pending = isPending ? " pending" : "";
        var faulted = isFaulted ? " faulted" : "";
        return $"{modified}{validity}{pending}{faulted}";
    }
}
