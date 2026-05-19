// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Provides extension methods to describe the state of <see cref="EditContext"/>
/// fields as CSS class names.
/// </summary>
public static class EditContextFieldClassExtensions
{
    private static readonly object FieldCssClassProviderKey = new object();

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
    /// Gets a string that indicates the status of the specified field as a CSS class.
    /// </summary>
    /// <param name="editContext">The <see cref="EditContext"/>.</param>
    /// <param name="fieldIdentifier">An identifier for the field.</param>
    /// <returns>A string that indicates the status of the field.</returns>
    public static string FieldCssClass(this EditContext editContext, in FieldIdentifier fieldIdentifier)
    {
        var provider = editContext.Properties.TryGetValue(FieldCssClassProviderKey, out var customProvider)
            ? (FieldCssClassProvider)customProvider
            : FieldCssClassProvider.Instance;

        return provider.GetFieldCssClass(editContext, fieldIdentifier);
    }

    /// <summary>
    /// Associates the supplied <see cref="FieldCssClassProvider"/> with the supplied <see cref="EditContext"/>.
    /// This customizes the field CSS class names used within the <see cref="EditContext"/>.
    /// </summary>
    /// <param name="editContext">The <see cref="EditContext"/>.</param>
    /// <param name="fieldCssClassProvider">The <see cref="FieldCssClassProvider"/>.</param>
    public static void SetFieldCssClassProvider(this EditContext editContext, FieldCssClassProvider fieldCssClassProvider)
    {
        ArgumentNullException.ThrowIfNull(fieldCssClassProvider);

        editContext.Properties[FieldCssClassProviderKey] = fieldCssClassProvider;
    }
}
