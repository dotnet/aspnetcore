// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <summary>
/// Contract for a service supporting <see cref="IHtmlHelper"/> and <c>ITagHelper</c> implementations.
/// </summary>
public interface IHtmlGenerator
{
    /// <summary>
    /// Gets the replacement for '.' in an Id attribute.
    /// </summary>
    string IdAttributeDotReplacement { get; }

    /// <summary>
    /// Encodes a value.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The encoded value.</returns>
    string Encode(string value);

    /// <summary>
    /// Encodes a value.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The encoded value.</returns>
    string Encode(object value);

    /// <summary>
    /// Format a value.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <param name="format">The format to use.</param>
    /// <returns>The formatted value.</returns>
    string FormatValue(object value, string format);

    /// <summary>
    /// Generate a &lt;a&gt; element for a link to an action.
    /// </summary>
    /// <param name="viewContext">The <see cref="ViewContext"/> instance for the current scope.</param>
    /// <param name="linkText">The text to insert inside the element.</param>
    /// <param name="actionName">The name of the action method.</param>
    /// <param name="controllerName">The name of the controller.</param>
    /// <param name="protocol">The protocol (scheme) for the generated link.</param>
    /// <param name="hostname">The hostname for the generated link.</param>
    /// <param name="fragment">The fragment for the generated link.</param>
    /// <param name="routeValues">
    /// An <see cref="object"/> that contains the parameters for a route. The parameters are retrieved through
    /// reflection by examining the properties of the <see cref="object"/>. This <see cref="object"/> is typically
    /// created using <see cref="object"/> initializer syntax. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the route parameters.
    /// </param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
    /// </param>
    /// <returns>
    /// A <see cref="TagBuilder"/> instance for the &lt;a&gt; element.
    /// </returns>
    TagBuilder GenerateActionLink(
        ViewContext viewContext,
        string linkText,
        string actionName,
        string controllerName,
        string protocol,
        string hostname,
        string fragment,
        object routeValues,
        object htmlAttributes);

    /// <summary>
    /// Generate a &lt;a&gt; element for a link to an action.
    /// </summary>
    /// <param name="viewContext">The <see cref="ViewContext"/> instance for the current scope.</param>
    /// <param name="linkText">The text to insert inside the element.</param>
    /// <param name="pageName">The page name.</param>
    /// <param name="pageHandler">The page handler.</param>
    /// <param name="protocol">The protocol (scheme) for the generated link.</param>
    /// <param name="hostname">The hostname for the generated link.</param>
    /// <param name="fragment">The fragment for the generated link.</param>
    /// <param name="routeValues">
    /// An <see cref="object"/> that contains the parameters for a route. The parameters are retrieved through
    /// reflection by examining the properties of the <see cref="object"/>. This <see cref="object"/> is typically
    /// created using <see cref="object"/> initializer syntax. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the route parameters.
    /// </param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
    /// </param>
    /// <returns>
    /// A <see cref="TagBuilder"/> instance for the &lt;a&gt; element.
    /// </returns>
    TagBuilder GeneratePageLink(
        ViewContext viewContext,
        string linkText,
        string pageName,
        string pageHandler,
        string protocol,
        string hostname,
        string fragment,
        object routeValues,
        object htmlAttributes);

    /// <summary>
    /// Generate an &lt;input type="hidden".../&gt; element containing an antiforgery token.
    /// </summary>
    /// <param name="viewContext">The <see cref="ViewContext"/> instance for the current scope.</param>
    /// <returns>
    /// An <see cref="IHtmlContent"/> instance for the &lt;input type="hidden".../&gt; element. Intended to be used
    /// inside a &lt;form&gt; element.
    /// </returns>
    IHtmlContent GenerateAntiforgery(ViewContext viewContext);

    /// <summary>
    /// Generate a &lt;input type="checkbox".../&gt; element.
    /// </summary>
    /// <param name="viewContext">The <see cref="ViewContext"/> instance for the current scope.</param>
    /// <param name="modelExplorer">The <see cref="ModelExplorer"/> for the <paramref name="expression"/>.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="isChecked">The initial state of the checkbox element.</param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
    /// </param>
    /// <returns>
    /// A <see cref="TagBuilder"/> instance for the &lt;input type="checkbox".../&gt; element.
    /// </returns>
    TagBuilder GenerateCheckBox(
        ViewContext viewContext,
        ModelExplorer modelExplorer,
        string expression,
        bool? isChecked,
        object htmlAttributes);

    /// <summary>
    /// Generate an additional &lt;input type="hidden".../&gt; for checkboxes. This addresses scenarios where
    /// unchecked checkboxes are not sent in the request. Sending a hidden input makes it possible to know that the
    /// checkbox was present on the page when the request was submitted.
    /// </summary>
    TagBuilder GenerateHiddenForCheckbox(
        ViewContext viewContext,
        ModelExplorer modelExplorer,
        string expression);

    /// <summary>
    /// Generate a &lt;form&gt; element. When the user submits the form, the action with name
    /// <paramref name="actionName"/> will process the request.
    /// </summary>
    /// <param name="viewContext">A <see cref="ViewContext"/> instance for the current scope.</param>
    /// <param name="actionName">The name of the action method.</param>
    /// <param name="controllerName">The name of the controller.</param>
    /// <param name="routeValues">
    /// An <see cref="object"/> that contains the parameters for a route. The parameters are retrieved through
    /// reflection by examining the properties of the <see cref="object"/>. This <see cref="object"/> is typically
    /// created using <see cref="object"/> initializer syntax. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the route parameters.
    /// </param>
    /// <param name="method">The HTTP method for processing the form, either GET or POST.</param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
    /// </param>
    /// <returns>
    /// A <see cref="TagBuilder"/> instance for the &lt;/form&gt; element.
    /// </returns>
    TagBuilder GenerateForm(
        ViewContext viewContext,
        string actionName,
        string controllerName,
        object routeValues,
        string method,
        object htmlAttributes);

    /// <summary>
    /// Generate a &lt;form&gt; element. When the user submits the form, the page with name
    /// <paramref name="pageName"/> will process the request.
    /// </summary>
    /// <param name="viewContext">A <see cref="ViewContext"/> instance for the current scope.</param>
    /// <param name="pageName">The name of the page.</param>
    /// <param name="pageHandler">The page handler to generate a form for.</param>
    /// <param name="routeValues">
    /// An <see cref="object"/> that contains the parameters for a route. The parameters are retrieved through
    /// reflection by examining the properties of the <see cref="object"/>. This <see cref="object"/> is typically
    /// created using <see cref="object"/> initializer syntax. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the route parameters.
    /// </param>
    /// <param name="fragment">The url fragment.</param>
    /// <param name="method">The HTTP method for processing the form, either GET or POST.</param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
    /// </param>
    /// <returns>
    /// A <see cref="TagBuilder"/> instance for the &lt;/form&gt; element.
    /// </returns>
    TagBuilder GeneratePageForm(
        ViewContext viewContext,
        string pageName,
        string pageHandler,
        object routeValues,
        string fragment,
        string method,
        object htmlAttributes);

    /// <summary>
    /// Generate a &lt;form&gt; element. The route with name <paramref name="routeName"/> generates the
    /// &lt;form&gt;'s <c>action</c> attribute value.
    /// </summary>
    /// <param name="viewContext">A <see cref="ViewContext"/> instance for the current scope.</param>
    /// <param name="routeName">The name of the route.</param>
    /// <param name="routeValues">
    /// An <see cref="object"/> that contains the parameters for a route. The parameters are retrieved through
    /// reflection by examining the properties of the <see cref="object"/>. This <see cref="object"/> is typically
    /// created using <see cref="object"/> initializer syntax. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the route parameters.
    /// </param>
    /// <param name="method">The HTTP method for processing the form, either GET or POST.</param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
    /// </param>
    /// <returns>
    /// A <see cref="TagBuilder"/> instance for the &lt;/form&gt; element.
    /// </returns>
    TagBuilder GenerateRouteForm(
        ViewContext viewContext,
        string routeName,
        object routeValues,
        string method,
        object htmlAttributes);

    /// <summary>
    /// Generate a &lt;input type="hidden"&gt; element
    /// </summary>
    /// <param name="viewContext">A <see cref="ViewContext"/> instance for the current scope.</param>
    /// <param name="modelExplorer">The <see cref="ModelExplorer"/> for the <paramref name="expression"/>.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="value">The value which is injected into the element</param>
    /// <param name="useViewData">Whether to use the ViewData to generate this element</param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the parameters for a route. The parameters are retrieved through
    /// reflection by examining the properties of the <see cref="object"/>. This <see cref="object"/> is typically
    /// created using <see cref="object"/> initializer syntax. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the route parameters.
    /// </param>
    /// <returns></returns>
    TagBuilder GenerateHidden(
        ViewContext viewContext,
        ModelExplorer modelExplorer,
        string expression,
        object value,
        bool useViewData,
        object htmlAttributes);

    /// <summary>
    /// Generate a &lt;label&gt; element
    /// </summary>
    /// <param name="viewContext">A <see cref="ViewContext"/> instance for the current scope.</param>
    /// <param name="modelExplorer">The <see cref="ModelExplorer"/> for the <paramref name="expression"/>.</param>
    /// <param name="expression">Expression name, relative to the current model. Used to set the target of the label.</param>
    /// <param name="labelText">Text used to render this label.</param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the parameters for a route. The parameters are retrieved through
    /// reflection by examining the properties of the <see cref="object"/>. This <see cref="object"/> is typically
    /// created using <see cref="object"/> initializer syntax. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the route parameters.
    /// </param>
    /// <returns></returns>
    TagBuilder GenerateLabel(
        ViewContext viewContext,
        ModelExplorer modelExplorer,
        string expression,
        string labelText,
        object htmlAttributes);

    /// <summary>
    /// Generate a &lt;input type="password"&gt; element
    /// </summary>
    /// <param name="viewContext">A <see cref="ViewContext"/> instance for the current scope.</param>
    /// <param name="modelExplorer">The <see cref="ModelExplorer"/> for the <paramref name="expression"/>.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="value">Value used to prefill the checkbox</param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the parameters for a route. The parameters are retrieved through
    /// reflection by examining the properties of the <see cref="object"/>. This <see cref="object"/> is typically
    /// created using <see cref="object"/> initializer syntax. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the route parameters.
    /// </param>
    /// <returns></returns>
    TagBuilder GeneratePassword(
        ViewContext viewContext,
        ModelExplorer modelExplorer,
        string expression,
        object value,
        object htmlAttributes);

    /// <summary>
    /// Generate a &lt;input type="radio"&gt; element
    /// </summary>
    /// <param name="viewContext">A <see cref="ViewContext"/> instance for the current scope.</param>
    /// <param name="modelExplorer">The <see cref="ModelExplorer"/> for the <paramref name="expression"/>.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="value">value of the given radio button</param>
    /// <param name="isChecked">Whether or not the radio button is checked</param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the parameters for a route. The parameters are retrieved through
    /// reflection by examining the properties of the <see cref="object"/>. This <see cref="object"/> is typically
    /// created using <see cref="object"/> initializer syntax. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the route parameters.
    /// </param>
    /// <returns></returns>
    TagBuilder GenerateRadioButton(
        ViewContext viewContext,
        ModelExplorer modelExplorer,
        string expression,
        object value,
        bool? isChecked,
        object htmlAttributes);

    /// <summary>
    /// Generate a &lt;a&gt; element for a link to an action.
    /// </summary>
    /// <param name="viewContext">The <see cref="ViewContext"/> instance for the current scope.</param>
    /// <param name="linkText">The text to insert inside the element.</param>
    /// <param name="routeName">The name of the route to use for link generation.</param>
    /// <param name="protocol">The protocol (scheme) for the generated link.</param>
    /// <param name="hostName">The hostname for the generated link.</param>
    /// <param name="fragment">The fragment for the generated link.</param>
    /// <param name="routeValues">
    /// An <see cref="object"/> that contains the parameters for a route. The parameters are retrieved through
    /// reflection by examining the properties of the <see cref="object"/>. This <see cref="object"/> is typically
    /// created using <see cref="object"/> initializer syntax. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the route parameters.
    /// </param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
    /// </param>
    /// <returns>
    /// A <see cref="TagBuilder"/> instance for the &lt;a&gt; element.
    /// </returns>
    TagBuilder GenerateRouteLink(
        ViewContext viewContext,
        string linkText,
        string routeName,
        string protocol,
        string hostName,
        string fragment,
        object routeValues,
        object htmlAttributes);

    /// <summary>
    /// Generate a &lt;select&gt; element for the <paramref name="expression"/>.
    /// </summary>
    /// <param name="viewContext">A <see cref="ViewContext"/> instance for the current scope.</param>
    /// <param name="modelExplorer">
    /// <see cref="ModelExplorer"/> for the <paramref name="expression"/>. If <c>null</c>, determines validation
    /// attributes using <paramref name="viewContext"/> and the <paramref name="expression"/>.
    /// </param>
    /// <param name="optionLabel">Optional text for a default empty &lt;option&gt; element.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="selectList">
    /// A collection of <see cref="SelectListItem"/> objects used to populate the &lt;select&gt; element with
    /// &lt;optgroup&gt; and &lt;option&gt; elements. If <c>null</c>, finds this collection at
    /// <c>ViewContext.ViewData[expression]</c>.
    /// </param>
    /// <param name="allowMultiple">
    /// If <c>true</c>, includes a <c>multiple</c> attribute in the generated HTML. Otherwise generates a
    /// single-selection &lt;select&gt; element.
    /// </param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the &lt;select&gt; element. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
    /// </param>
    /// <returns>A new <see cref="TagBuilder"/> describing the &lt;select&gt; element.</returns>
    /// <remarks>
    /// <para>
    /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
    /// &lt;select&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
    /// attribute.
    /// </para>
    /// <para>
    /// See <see cref="GetCurrentValues"/> for information about how current values are determined.
    /// </para>
    /// </remarks>
    TagBuilder GenerateSelect(
        ViewContext viewContext,
        ModelExplorer modelExplorer,
        string optionLabel,
        string expression,
        IEnumerable<SelectListItem> selectList,
        bool allowMultiple,
        object htmlAttributes);

    /// <summary>
    /// Generate a &lt;select&gt; element for the <paramref name="expression"/>.
    /// </summary>
    /// <param name="viewContext">A <see cref="ViewContext"/> instance for the current scope.</param>
    /// <param name="modelExplorer">
    /// <see cref="ModelExplorer"/> for the <paramref name="expression"/>. If <c>null</c>, determines validation
    /// attributes using <paramref name="viewContext"/> and the <paramref name="expression"/>.
    /// </param>
    /// <param name="optionLabel">Optional text for a default empty &lt;option&gt; element.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="selectList">
    /// A collection of <see cref="SelectListItem"/> objects used to populate the &lt;select&gt; element with
    /// &lt;optgroup&gt; and &lt;option&gt; elements. If <c>null</c>, finds this collection at
    /// <c>ViewContext.ViewData[expression]</c>.
    /// </param>
    /// <param name="currentValues">
    /// An <see cref="ICollection{String}"/> containing values for &lt;option&gt; elements to select. If
    /// <c>null</c>, selects &lt;option&gt; elements based on <see cref="SelectListItem.Selected"/> values in
    /// <paramref name="selectList"/>.
    /// </param>
    /// <param name="allowMultiple">
    /// If <c>true</c>, includes a <c>multiple</c> attribute in the generated HTML. Otherwise generates a
    /// single-selection &lt;select&gt; element.
    /// </param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the &lt;select&gt; element. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
    /// </param>
    /// <returns>A new <see cref="TagBuilder"/> describing the &lt;select&gt; element.</returns>
    /// <remarks>
    /// <para>
    /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
    /// &lt;select&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
    /// attribute.
    /// </para>
    /// <para>
    /// See <see cref="GetCurrentValues"/> for information about how the <paramref name="currentValues"/>
    /// collection may be created.
    /// </para>
    /// </remarks>
    TagBuilder GenerateSelect(
        ViewContext viewContext,
        ModelExplorer modelExplorer,
        string optionLabel,
        string expression,
        IEnumerable<SelectListItem> selectList,
        ICollection<string> currentValues,
        bool allowMultiple,
        object htmlAttributes);

    /// <summary>
    /// Generates &lt;optgroup&gt; and &lt;option&gt; elements.
    /// </summary>
    /// <param name="optionLabel">Optional text for a default empty &lt;option&gt; element.</param>
    /// <param name="selectList">
    /// A collection of <see cref="SelectListItem"/> objects used to generate &lt;optgroup&gt; and &lt;option&gt;
    /// elements.
    /// </param>
    /// <returns>
    /// An <see cref="IHtmlContent"/> instance for &lt;optgroup&gt; and &lt;option&gt; elements.
    /// </returns>
    IHtmlContent GenerateGroupsAndOptions(string optionLabel, IEnumerable<SelectListItem> selectList);

    /// <summary>
    /// Generates a &lt;textarea&gt; element
    /// </summary>
    /// <param name="viewContext">A <see cref="ViewContext"/> instance for the current scope.</param>
    /// <param name="modelExplorer">The <see cref="ModelExplorer"/> for the <paramref name="expression"/>.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="rows"></param>
    /// <param name="columns"></param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the parameters for a route. The parameters are retrieved through
    /// reflection by examining the properties of the <see cref="object"/>. This <see cref="object"/> is typically
    /// created using <see cref="object"/> initializer syntax. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the route parameters.
    /// </param>
    /// <returns></returns>
    TagBuilder GenerateTextArea(
        ViewContext viewContext,
        ModelExplorer modelExplorer,
        string expression,
        int rows,
        int columns,
        object htmlAttributes);

    /// <summary>
    /// Generates a &lt;input type="text"&gt; element
    /// </summary>
    /// <param name="viewContext">A <see cref="ViewContext"/> instance for the current scope.</param>
    /// <param name="modelExplorer">The <see cref="ModelExplorer"/> for the <paramref name="expression"/>.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="value"></param>
    /// <param name="format"></param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the parameters for a route. The parameters are retrieved through
    /// reflection by examining the properties of the <see cref="object"/>. This <see cref="object"/> is typically
    /// created using <see cref="object"/> initializer syntax. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the route parameters.
    /// </param>
    /// <returns></returns>
    TagBuilder GenerateTextBox(
        ViewContext viewContext,
        ModelExplorer modelExplorer,
        string expression,
        object value,
        string format,
        object htmlAttributes);

    /// <summary>
    /// Generate a <paramref name="tag"/> element if the <paramref name="viewContext"/>'s
    /// <see cref="ActionContext.ModelState"/> contains an error for the <paramref name="expression"/>.
    /// </summary>
    /// <param name="viewContext">A <see cref="ViewContext"/> instance for the current scope.</param>
    /// <param name="modelExplorer">The <see cref="ModelExplorer"/> for the <paramref name="expression"/>.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="message">
    /// The message to be displayed. If <c>null</c> or empty, method extracts an error string from the
    /// <see cref="ModelBinding.ModelStateDictionary"/> object. Message will always be visible but client-side
    /// validation may update the associated CSS class.
    /// </param>
    /// <param name="tag">
    /// The tag to wrap the <paramref name="message"/> in the generated HTML. Its default value is
    /// <see cref="ViewContext.ValidationMessageElement"/>.
    /// </param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
    /// </param>
    /// <returns></returns>
    /// <remarks><see cref="ViewContext.ValidationMessageElement"/> is <c>"span"</c> by default.</remarks>
    TagBuilder GenerateValidationMessage(
        ViewContext viewContext,
        ModelExplorer modelExplorer,
        string expression,
        string message,
        string tag,
        object htmlAttributes);

    /// <summary>
    /// Generates a &lt;div&gt; element which contains a list of validation errors.
    /// </summary>
    /// <param name="viewContext"></param>
    /// <param name="excludePropertyErrors"></param>
    /// <param name="message"></param>
    /// <param name="headerTag"></param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the parameters for a route. The parameters are retrieved through
    /// reflection by examining the properties of the <see cref="object"/>. This <see cref="object"/> is typically
    /// created using <see cref="object"/> initializer syntax. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the route parameters.
    /// </param>
    /// <returns></returns>
    TagBuilder GenerateValidationSummary(
        ViewContext viewContext,
        bool excludePropertyErrors,
        string message,
        string headerTag,
        object htmlAttributes);

    /// <summary>
    /// Gets the collection of current values for the given <paramref name="expression"/>.
    /// </summary>
    /// <param name="viewContext">A <see cref="ViewContext"/> instance for the current scope.</param>
    /// <param name="modelExplorer">
    /// <see cref="ModelExplorer"/> for the <paramref name="expression"/>. If <c>null</c>, calculates the
    /// <paramref name="expression"/> result using <see cref="ViewDataDictionary.Eval(string)"/>.
    /// </param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="allowMultiple">
    /// If <c>true</c>, require a collection <paramref name="expression"/> result. Otherwise, treat result as a
    /// single value.
    /// </param>
    /// <returns>
    /// <para>
    /// <c>null</c> if no <paramref name="expression"/> result is found. Otherwise a
    /// <see cref="ICollection{String}"/> containing current values for the given
    /// <paramref name="expression"/>.
    /// </para>
    /// <para>
    /// Converts the <paramref name="expression"/> result to a <see cref="string"/>. If that result is an
    /// <see cref="System.Collections.IEnumerable"/> type, instead converts each item in the collection and returns
    /// them separately.
    /// </para>
    /// <para>
    /// If the <paramref name="expression"/> result or the element type is an <see cref="System.Enum"/>, returns a
    /// <see cref="string"/> containing the integer representation of the <see cref="System.Enum"/> value as well
    /// as all <see cref="System.Enum"/> names for that value. Otherwise returns the default <see cref="string"/>
    /// conversion of the value.
    /// </para>
    /// </returns>
    /// <remarks>
    /// See <see cref="M:GenerateSelect"/> for information about how the return value may be used.
    /// </remarks>
    ICollection<string> GetCurrentValues(
        ViewContext viewContext,
        ModelExplorer modelExplorer,
        string expression,
        bool allowMultiple);
}
