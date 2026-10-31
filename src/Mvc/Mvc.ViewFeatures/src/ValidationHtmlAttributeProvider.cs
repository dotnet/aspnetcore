// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <summary>
/// Contract for a service providing validation attributes for expressions.
/// </summary>
public abstract class ValidationHtmlAttributeProvider
{
    /// <summary>
    /// Adds validation-related HTML attributes to the <paramref name="attributes" /> if client validation is
    /// enabled.
    /// </summary>
    /// <param name="viewContext">A <see cref="ViewContext"/> instance for the current scope.</param>
    /// <param name="modelExplorer">The <see cref="ModelExplorer"/> for an expression.</param>
    /// <param name="attributes">
    /// The <see cref="Dictionary{TKey, TValue}"/> to receive the validation attributes. Maps the validation
    /// attribute names to their <see cref="string"/> values. Values must be HTML encoded before they are written
    /// to an HTML document or response.
    /// </param>
    /// <remarks>
    /// Adds nothing to <paramref name="attributes"/> if client-side validation is disabled.
    /// </remarks>
    public abstract void AddValidationAttributes(
        ViewContext viewContext,
        ModelExplorer modelExplorer,
        IDictionary<string, string> attributes);

    /// <summary>
    /// Adds validation-related HTML attributes to the <paramref name="attributes" /> if client validation is
    /// enabled and validation attributes have not yet been added for this <paramref name="expression"/> in the
    /// current &lt;form&gt;.
    /// </summary>
    /// <param name="viewContext">A <see cref="ViewContext"/> instance for the current scope.</param>
    /// <param name="modelExplorer">The <see cref="ModelExplorer"/> for the <paramref name="expression"/>.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="attributes">
    /// The <see cref="Dictionary{TKey, TValue}"/> to receive the validation attributes. Maps the validation
    /// attribute names to their <see cref="string"/> values. Values must be HTML encoded before they are written
    /// to an HTML document or response.
    /// </param>
    /// <remarks>
    /// Tracks the <paramref name="expression"/> in the current <see cref="FormContext"/> to avoid generating
    /// duplicate validation attributes. That is, validation attributes are added only if no previous call has
    /// added them for a field with this name in the &lt;form&gt;.
    /// </remarks>
    public virtual void AddAndTrackValidationAttributes(
        ViewContext viewContext,
        ModelExplorer modelExplorer,
        string expression,
        IDictionary<string, string> attributes)
    {
        ArgumentNullException.ThrowIfNull(viewContext);
        ArgumentNullException.ThrowIfNull(modelExplorer);
        ArgumentNullException.ThrowIfNull(attributes);

        // Don't track fields when client-side validation is disabled.
        var formContext = viewContext.ClientValidationEnabled ? viewContext.FormContext : null;
        if (formContext == null)
        {
            return;
        }

        var fullName = viewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(expression);
        if (formContext.RenderedField(fullName))
        {
            return;
        }

        formContext.RenderedField(fullName, true);

        AddValidationAttributes(viewContext, modelExplorer, attributes);
    }
}
