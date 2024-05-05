// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc.Rendering;

/// <summary>
/// An <see cref="IHtmlHelper"/> for Linq expressions.
/// </summary>
/// <typeparam name="TModel">The type of the model.</typeparam>
public interface IHtmlHelper<TModel> : IHtmlHelper
{
    /// <summary>
    /// Gets the current view data.
    /// </summary>
    new ViewDataDictionary<TModel> ViewData { get; }

    /// <summary>
    /// Returns an &lt;input&gt; element of type "checkbox" with value "true" and an &lt;input&gt; element of type
    /// "hidden" with value "false" for the specified <paramref name="expression"/>. Adds a "checked" attribute to
    /// the "checkbox" element based on the first non-<c>null</c> value found in:
    /// the <paramref name="htmlAttributes"/> dictionary entry with key "checked", or
    /// the <see cref="ActionContext.ModelState"/> entry with full name.
    /// If <paramref name="expression"/> evaluates to a non-<c>null</c> value, instead uses the first
    /// non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewDataDictionary.Model"/>.
    /// See <see cref="NameFor"/> for more information about a "full name".
    /// </summary>
    /// <param name="expression">An expression to be evaluated against the current model.</param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the checkbox element. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
    /// </param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; elements.</returns>
    /// <remarks>
    /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
    /// <paramref name="expression"/> to set checkbox element's "name" attribute. Sanitizes the string
    /// representation of the <paramref name="expression"/> to set checkbox element's "id" attribute.
    /// </remarks>
    IHtmlContent CheckBoxFor(Expression<Func<TModel, bool>> expression, object htmlAttributes);

    /// <summary>
    /// Returns HTML markup for the <paramref name="expression"/>, using a display template, specified HTML field
    /// name, and additional view data. The template name is taken from the <paramref name="templateName"/> or the
    /// <paramref name="expression"/>’s <see cref="Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata.TemplateHint"/>.
    /// If the template file is not found, a default template will be used.
    /// </summary>
    /// <param name="expression">An expression to be evaluated against the current model.</param>
    /// <param name="templateName">The name of the template used to create the HTML markup.</param>
    /// <param name="htmlFieldName">
    /// A <see cref="string"/> used to disambiguate the names of HTML elements that are created for properties
    /// that have the same name.
    /// </param>
    /// <param name="additionalViewData">
    /// An anonymous <see cref="object"/> or <see cref="IDictionary{String, Object}"/> that can contain additional
    /// view data that will be merged into the <see cref="ViewDataDictionary{TModel}"/> instance created for the
    /// template.
    /// </param>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>A new <see cref="IHtmlContent"/> containing the created HTML.</returns>
    /// <remarks>
    /// <para>
    /// For example the default <see cref="object"/> display template includes markup for each property in the
    /// <paramref name="expression"/> result.
    /// </para>
    /// <para>
    /// Custom templates are found under a <c>DisplayTemplates</c> folder within the
    /// <see href="https://aka.ms/aspnet/7.0/razorpages-pages-folder">Pages</see> folder.
    /// The folder name is case-sensitive on case-sensitive file systems.
    /// </para>
    /// </remarks>
    IHtmlContent DisplayFor<TResult>(
        Expression<Func<TModel, TResult>> expression,
        string templateName,
        string htmlFieldName,
        object additionalViewData);

    /// <summary>
    /// Returns the display name for the specified <paramref name="expression"/>.
    /// </summary>
    /// <param name="expression">An expression to be evaluated against the current model.</param>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>A <see cref="string"/> containing the display name.</returns>
    string DisplayNameFor<TResult>(Expression<Func<TModel, TResult>> expression);

    /// <summary>
    /// Returns the display name for the specified <paramref name="expression"/>
    /// if the current model represents a collection.
    /// </summary>
    /// <param name="expression">An expression to be evaluated against an item in the current model.</param>
    /// <typeparam name="TModelItem">The type of items in the model collection.</typeparam>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>A <see cref="string"/> containing the display name.</returns>
    string DisplayNameForInnerType<TModelItem, TResult>(
        Expression<Func<TModelItem, TResult>> expression);

    /// <summary>
    /// Returns the simple display text for the specified <paramref name="expression"/>.
    /// </summary>
    /// <param name="expression">An expression to be evaluated against the current model.</param>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>
    /// A <see cref="string"/> containing the simple display text.
    /// If the <paramref name="expression"/> result is <c>null</c>, returns
    /// <see cref="ModelBinding.ModelMetadata.NullDisplayText"/>.
    /// </returns>
    string DisplayTextFor<TResult>(Expression<Func<TModel, TResult>> expression);

    /// <summary>
    /// Returns a single-selection HTML &lt;select&gt; element for the <paramref name="expression"/>. Adds
    /// &lt;option&gt; elements based on <paramref name="optionLabel"/> and <paramref name="selectList"/>. Adds a
    /// "selected" attribute to an &lt;option&gt; if its <see cref="SelectListItem.Value"/> (if non-<c>null</c>) or
    /// <see cref="SelectListItem.Text"/> matches the first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewDataDictionary.Model"/>.
    /// See <see cref="NameFor"/> for more information about a "full name".
    /// </summary>
    /// <param name="expression">An expression to be evaluated against the current model.</param>
    /// <param name="selectList">
    /// A collection of <see cref="SelectListItem"/> objects used to populate the &lt;select&gt; element with
    /// &lt;optgroup&gt; and &lt;option&gt; elements. If <c>null</c>, uses the <see cref="ViewData"/> entry with
    /// full name and that entry must be a collection of <see cref="SelectListItem"/> objects.
    /// </param>
    /// <param name="optionLabel">
    /// The text for a default empty item. Does not include such an item if argument is <c>null</c>.
    /// </param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the &lt;select&gt; element. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
    /// </param>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;select&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
    /// <paramref name="expression"/> to set &lt;select&gt; element's "name" attribute. Sanitizes the string
    /// representation of the <paramref name="expression"/> to set element's "id" attribute.
    /// </remarks>
    IHtmlContent DropDownListFor<TResult>(
        Expression<Func<TModel, TResult>> expression,
        IEnumerable<SelectListItem> selectList,
        string optionLabel,
        object htmlAttributes);

    /// <summary>
    /// Returns HTML markup for the <paramref name="expression"/>, using an editor template, specified HTML field
    /// name, and additional view data. The template is found using the <paramref name="templateName"/> or the
    /// <paramref name="expression"/>'s <see cref="ModelBinding.ModelMetadata"/>.
    /// </summary>
    /// <param name="expression">An expression to be evaluated against the current model.</param>
    /// <param name="templateName">The name of the template that is used to create the HTML markup.</param>
    /// <param name="htmlFieldName">
    /// A <see cref="string"/> used to disambiguate the names of HTML elements that are created for properties
    /// that have the same name.
    /// </param>
    /// <param name="additionalViewData">
    /// An anonymous <see cref="object"/> or <see cref="IDictionary{String, Object}"/> that can contain additional
    /// view data that will be merged into the <see cref="ViewDataDictionary{TModel}"/> instance created for the
    /// template.
    /// </param>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; element(s).</returns>
    /// <remarks>
    /// <para>
    /// For example the default <see cref="object"/> editor template includes &lt;label&gt; and &lt;input&gt;
    /// elements for each property in the <paramref name="expression"/> result.
    /// </para>
    /// <para>
    /// Custom templates are found under a <c>EditorTemplates</c> folder. The folder name is case-sensitive on
    /// case-sensitive file systems.
    /// </para>
    /// </remarks>
    IHtmlContent EditorFor<TResult>(
        Expression<Func<TModel, TResult>> expression,
        string templateName,
        string htmlFieldName,
        object additionalViewData);

    /// <inheritdoc cref="IHtmlHelper.Encode(object)"/>
    new string Encode(object value);

    /// <inheritdoc cref="IHtmlHelper.Encode(string)"/>
    new string Encode(string value);

    /// <summary>
    /// Returns an &lt;input&gt; element of type "hidden" for the specified <paramref name="expression"/>. Adds a
    /// "value" attribute to the element containing the first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewDataDictionary.Model"/>.
    /// See <see cref="NameFor"/> for more information about a "full name".
    /// </summary>
    /// <param name="expression">An expression to be evaluated against the current model.</param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
    /// </param>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
    /// <paramref name="expression"/> to set &lt;input&gt; element's "name" attribute. Sanitizes the string
    /// representation of the <paramref name="expression"/> to set element's "id" attribute.
    /// </remarks>
    IHtmlContent HiddenFor<TResult>(
        Expression<Func<TModel, TResult>> expression,
        object htmlAttributes);

    /// <summary>
    /// Returns the HTML element Id for the specified <paramref name="expression"/>.
    /// </summary>
    /// <param name="expression">An expression to be evaluated against the current model.</param>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>A <see cref="string"/> containing the element Id.</returns>
    string IdFor<TResult>(Expression<Func<TModel, TResult>> expression);

    /// <summary>
    /// Returns a &lt;label&gt; element for the specified <paramref name="expression"/>.
    /// </summary>
    /// <param name="expression">An expression to be evaluated against the current model.</param>
    /// <param name="labelText">The inner text of the element.</param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
    /// </param>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;label&gt; element.</returns>
    IHtmlContent LabelFor<TResult>(
        Expression<Func<TModel, TResult>> expression,
        string labelText,
        object htmlAttributes);

    /// <summary>
    /// Returns a multi-selection &lt;select&gt; element for the <paramref name="expression"/>. Adds
    /// &lt;option&gt; elements based on <paramref name="selectList"/>. Adds a "selected" attribute to an
    /// &lt;option&gt; if its <see cref="SelectListItem.Value"/> (if non-<c>null</c>) or
    /// <see cref="SelectListItem.Text"/> matches an entry in the first non-<c>null</c> collection found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewDataDictionary.Model"/>.
    /// See <see cref="NameFor"/> for more information about a "full name".
    /// </summary>
    /// <param name="expression">An expression to be evaluated against the current model.</param>
    /// <param name="selectList">
    /// A collection of <see cref="SelectListItem"/> objects used to populate the &lt;select&gt; element with
    /// &lt;optgroup&gt; and &lt;option&gt; elements. If <c>null</c>, finds the <see cref="SelectListItem"/>
    /// collection with name <paramref name="expression"/> in <see cref="ViewData"/>.
    /// </param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the &lt;select&gt; element. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
    /// </param>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;select&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
    /// <paramref name="expression"/> to set &lt;select&gt; element's "name" attribute. Sanitizes the string
    /// representation of the <paramref name="expression"/> to set element's "id" attribute.
    /// </remarks>
    IHtmlContent ListBoxFor<TResult>(
        Expression<Func<TModel, TResult>> expression,
        IEnumerable<SelectListItem> selectList,
        object htmlAttributes);

    /// <summary>
    /// Returns the full HTML element name for the specified <paramref name="expression"/>. Uses
    /// <see cref="TemplateInfo.HtmlFieldPrefix"/> (if non-empty) to reflect relationship between current
    /// <see cref="ViewDataDictionary.Model"/> and the top-level view's model.
    /// </summary>
    /// <param name="expression">An expression to be evaluated against the current model.</param>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>A <see cref="string"/> containing the element name.</returns>
    string NameFor<TResult>(Expression<Func<TModel, TResult>> expression);

    /// <summary>
    /// Returns an &lt;input&gt; element of type "password" for the specified <paramref name="expression"/>. Adds a
    /// "value" attribute containing the <paramref name="htmlAttributes"/> dictionary entry with key "value" (if
    /// any).
    /// </summary>
    /// <param name="expression">An expression to be evaluated against the current model.</param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
    /// </param>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
    /// <paramref name="expression"/> to set &lt;input&gt; element's "name" attribute. Sanitizes the string
    /// representation of the <paramref name="expression"/> to set element's "id" attribute.
    /// </remarks>
    IHtmlContent PasswordFor<TResult>(
        Expression<Func<TModel, TResult>> expression,
        object htmlAttributes);

    /// <summary>
    /// Returns an &lt;input&gt; element of type "radio" for the specified <paramref name="expression"/>.
    /// Adds a "value" attribute to the element containing the first non-<c>null</c> value found in:
    /// the <paramref name="value"/> parameter, or
    /// the <paramref name="htmlAttributes"/> dictionary entry with key "value".
    /// Adds a "checked" attribute to the element if <paramref name="value"/> matches the first non-<c>null</c>
    /// value found in:
    /// the <paramref name="htmlAttributes"/> dictionary entry with key "checked", or
    /// the <see cref="ActionContext.ModelState"/> entry with full name.
    /// If <paramref name="expression"/> evaluates to a non-<c>null</c> value, instead uses the first
    /// non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewDataDictionary.Model"/>.
    /// See <see cref="NameFor"/> for more information about a "full name".
    /// Adds a "value" attribute to the element containing the <paramref name="value"/> parameter.
    /// </summary>
    /// <param name="expression">An expression to be evaluated against the current model.</param>
    /// <param name="value">Value to include in the element. Must not be <c>null</c>.</param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
    /// </param>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
    /// <paramref name="expression"/> to set &lt;select&gt; element's "name" attribute. Sanitizes the string
    /// representation of the <paramref name="expression"/> to set element's "id" attribute.
    /// </remarks>
    IHtmlContent RadioButtonFor<TResult>(
        Expression<Func<TModel, TResult>> expression,
        object value,
        object htmlAttributes);

    /// <inheritdoc cref="IHtmlHelper.Raw(object)"/>
    new IHtmlContent Raw(object value);

    /// <inheritdoc cref="IHtmlHelper.Raw(string)"/>
    new IHtmlContent Raw(string value);

    /// <summary>
    /// Returns a &lt;textarea&gt; element for the specified <paramref name="expression"/>. Adds content to the
    /// element body based on the first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewDataDictionary.Model"/>.
    /// See <see cref="NameFor"/> for more information about a "full name".
    /// </summary>
    /// <param name="expression">An expression to be evaluated against the current model.</param>
    /// <param name="rows">Number of rows in the textarea.</param>
    /// <param name="columns">Number of columns in the textarea.</param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
    /// </param>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;textarea&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
    /// <paramref name="expression"/> to set &lt;textarea&gt; element's "name" attribute. Sanitizes the string
    /// representation of the <paramref name="expression"/> to set element's "id" attribute.
    /// </remarks>
    IHtmlContent TextAreaFor<TResult>(
        Expression<Func<TModel, TResult>> expression,
        int rows,
        int columns,
        object htmlAttributes);

    /// <summary>
    /// Returns an &lt;input&gt; element of type "text" for the specified <paramref name="expression"/>. Adds a
    /// "value" attribute to the element containing the first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name,
    /// the <paramref name="expression"/> evaluated against <see cref="ViewDataDictionary.Model"/>, or
    /// the <paramref name="htmlAttributes"/> dictionary entry with key "value".
    /// See <see cref="NameFor"/> for more information about a "full name".
    /// </summary>
    /// <param name="expression">An expression to be evaluated against the current model.</param>
    /// <param name="format">
    /// The format string (see <see href="https://msdn.microsoft.com/en-us/library/txafckwd.aspx"/>) used to format the
    /// <paramref name="expression"/> value when using that in the "value" attribute.
    /// </param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
    /// </param>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and the string representation of the
    /// <paramref name="expression"/> to set &lt;input&gt; element's "name" attribute. Sanitizes the string
    /// representation of the <paramref name="expression"/> to set element's "id" attribute.
    /// </remarks>
    IHtmlContent TextBoxFor<TResult>(
        Expression<Func<TModel, TResult>> expression,
        string format,
        object htmlAttributes);

    /// <summary>
    /// Returns the validation message if an error exists in the <see cref="ModelBinding.ModelStateDictionary"/>
    /// object for the specified <paramref name="expression"/>.
    /// </summary>
    /// <param name="expression">An expression to be evaluated against the current model.</param>
    /// <param name="message">
    /// The message to be displayed. If <c>null</c> or empty, method extracts an error string from the
    /// <see cref="ModelBinding.ModelStateDictionary"/> object. Message will always be visible but client-side
    /// validation may update the associated CSS class.
    /// </param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the <paramref name="tag"/> element.
    /// Alternatively, an <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
    /// </param>
    /// <param name="tag">
    /// The tag to wrap the <paramref name="message"/> in the generated HTML. Its default value is
    /// <see cref="ViewContext.ValidationMessageElement"/>.
    /// </param>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>
    /// A new <see cref="IHtmlContent"/> containing the <paramref name="tag"/> element. <c>null</c> if the
    /// <paramref name="expression"/> is valid and client-side validation is disabled.
    /// </returns>
    IHtmlContent ValidationMessageFor<TResult>(
        Expression<Func<TModel, TResult>> expression,
        string message,
        object htmlAttributes,
        string tag);

    /// <summary>
    /// Returns the formatted value for the specified <paramref name="expression"/>. Specifically, returns the
    /// first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewDataDictionary.Model"/>.
    /// See <see cref="NameFor"/> for more information about a "full name".
    /// </summary>
    /// <param name="expression">An expression to be evaluated against the current model.</param>
    /// <param name="format">
    /// The format string (see <see href="https://msdn.microsoft.com/en-us/library/txafckwd.aspx"/>) used to format the
    /// <paramref name="expression"/> value when returning that value.
    /// </param>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>A <see cref="string"/> containing the formatted value.</returns>
    /// <remarks>
    /// Converts the <paramref name="expression"/> result to a <see cref="string"/> directly if
    /// <paramref name="format"/> is <c>null</c> or empty.
    /// </remarks>
    string ValueFor<TResult>(
        Expression<Func<TModel, TResult>> expression,
        string format);
}
