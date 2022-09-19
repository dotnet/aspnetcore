// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc.Rendering;

/// <summary>
/// Base HTML helpers.
/// </summary>
public interface IHtmlHelper
{
    /// <summary>
    /// Set this property to <see cref="Html5DateRenderingMode.CurrentCulture" /> to have templated helpers such as
    /// <see cref="Editor" /> and <see cref="IHtmlHelper{TModel}.EditorFor" /> render date and time
    /// values using the current culture. By default, these helpers render dates and times as RFC 3339 compliant strings.
    /// </summary>
    Html5DateRenderingMode Html5DateRenderingMode { get; set; }

    /// <summary>
    /// Gets the <see cref="string"/> that replaces periods in the ID attribute of an element.
    /// </summary>
    string IdAttributeDotReplacement { get; }

    /// <summary>
    /// Gets the metadata provider. Intended for use in <see cref="IHtmlHelper"/> extension methods.
    /// </summary>
    IModelMetadataProvider MetadataProvider { get; }

    /// <summary>
    /// Gets the view bag.
    /// </summary>
    dynamic ViewBag { get; }

    /// <summary>
    /// Gets the context information about the view.
    /// </summary>
    ViewContext ViewContext { get; }

    /// <summary>
    /// Gets the current view data.
    /// </summary>
    ViewDataDictionary ViewData { get; }

    /// <summary>
    /// Gets the current <see cref="ITempDataDictionary"/> instance.
    /// </summary>
    ITempDataDictionary TempData { get; }

    /// <summary>
    /// Gets the <see cref="UrlEncoder"/> to be used for encoding a URL.
    /// </summary>
    UrlEncoder UrlEncoder { get; }

    /// <summary>
    /// Returns an anchor (&lt;a&gt;) element that contains a URL path to the specified action.
    /// </summary>
    /// <param name="linkText">The inner text of the anchor element. Must not be <c>null</c>.</param>
    /// <param name="actionName">The name of the action.</param>
    /// <param name="controllerName">The name of the controller.</param>
    /// <param name="protocol">The protocol for the URL, such as &quot;http&quot; or &quot;https&quot;.</param>
    /// <param name="hostname">The host name for the URL.</param>
    /// <param name="fragment">The URL fragment name (the anchor name).</param>
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
    /// <returns>A new <see cref="IHtmlContent"/> containing the anchor element.</returns>
    IHtmlContent ActionLink(
        string linkText,
        string actionName,
        string controllerName,
        string protocol,
        string hostname,
        string fragment,
        object routeValues,
        object htmlAttributes);

    /// <summary>
    /// Returns a &lt;hidden&gt; element (antiforgery token) that will be validated when the containing
    /// &lt;form&gt; is submitted.
    /// </summary>
    /// <returns><see cref="IHtmlContent"/> containing the &lt;hidden&gt; element.</returns>
    IHtmlContent AntiForgeryToken();

    /// <summary>
    /// Renders a &lt;form&gt; start tag to the response. When the user submits the form, the action with name
    /// <paramref name="actionName"/> will process the request.
    /// </summary>
    /// <param name="actionName">The name of the action method.</param>
    /// <param name="controllerName">The name of the controller.</param>
    /// <param name="routeValues">
    /// An <see cref="object"/> that contains the parameters for a route. The parameters are retrieved through
    /// reflection by examining the properties of the <see cref="object"/>. This <see cref="object"/> is typically
    /// created using <see cref="object"/> initializer syntax. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the route parameters.
    /// </param>
    /// <param name="method">The HTTP method for processing the form, either GET or POST.</param>
    /// <param name="antiforgery">
    /// If <c>true</c>, &lt;form&gt; elements will include an antiforgery token.
    /// If <c>false</c>, suppresses the generation an &lt;input&gt; of type "hidden" with an antiforgery token.
    /// If <c>null</c>, &lt;form&gt; elements will include an antiforgery token only if
    /// <paramref name="method"/> is not <see cref="FormMethod.Get"/>.
    /// </param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
    /// </param>
    /// <returns>
    /// An <see cref="MvcForm"/> instance which renders the &lt;/form&gt; end tag when disposed.
    /// </returns>
    /// <remarks>
    /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
    /// </remarks>
    MvcForm BeginForm(
        string actionName,
        string controllerName,
        object routeValues,
        FormMethod method,
        bool? antiforgery,
        object htmlAttributes);

    /// <summary>
    /// Renders a &lt;form&gt; start tag to the response. The route with name <paramref name="routeName"/>
    /// generates the &lt;form&gt;'s <c>action</c> attribute value.
    /// </summary>
    /// <param name="routeName">The name of the route.</param>
    /// <param name="routeValues">
    /// An <see cref="object"/> that contains the parameters for a route. The parameters are retrieved through
    /// reflection by examining the properties of the <see cref="object"/>. This <see cref="object"/> is typically
    /// created using <see cref="object"/> initializer syntax. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the route parameters.
    /// </param>
    /// <param name="method">The HTTP method for processing the form, either GET or POST.</param>
    /// <param name="antiforgery">
    /// If <c>true</c>, &lt;form&gt; elements will include an antiforgery token.
    /// If <c>false</c>, suppresses the generation an &lt;input&gt; of type "hidden" with an antiforgery token.
    /// If <c>null</c>, &lt;form&gt; elements will include an antiforgery token only if
    /// <paramref name="method"/> is not <see cref="FormMethod.Get"/>.
    /// </param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
    /// </param>
    /// <returns>
    /// An <see cref="MvcForm"/> instance which renders the &lt;/form&gt; end tag when disposed.
    /// </returns>
    /// <remarks>
    /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
    /// </remarks>
    MvcForm BeginRouteForm(
        string routeName,
        object routeValues,
        FormMethod method,
        bool? antiforgery,
        object htmlAttributes);

    /// <summary>
    /// Returns an &lt;input&gt; element of type "checkbox" with value "true" and an &lt;input&gt; element of type
    /// "hidden" with value "false" for the specified <paramref name="expression"/>. Adds a "checked" attribute to
    /// the "checkbox" element based on the first non-<c>null</c> value found in:
    /// the <paramref name="htmlAttributes"/> dictionary entry with key "checked",
    /// the <see cref="ActionContext.ModelState"/> entry with full name,
    /// the <see cref="ViewData"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewDataDictionary.Model"/>.
    /// If <paramref name="isChecked"/> is non-<c>null</c>, instead uses the first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name, or
    /// the <paramref name="isChecked"/> parameter.
    /// See <see cref="Name"/> for more information about a "full name".
    /// </summary>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="isChecked">If <c>true</c>, checkbox is initially checked.</param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the checkbox element. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
    /// </param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; elements.</returns>
    /// <remarks>
    /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set checkbox
    /// element's "name" attribute. Sanitizes <paramref name="expression"/> to set checkbox element's "id"
    /// attribute.
    /// </remarks>
    IHtmlContent CheckBox(string expression, bool? isChecked, object htmlAttributes);

    /// <summary>
    /// Returns HTML markup for the <paramref name="expression"/>, using a display template, specified HTML field
    /// name, and additional view data. The template is found using the <paramref name="templateName"/> or the
    /// <paramref name="expression"/>'s <see cref="ModelMetadata"/>.
    /// </summary>
    /// <param name="expression">
    /// Expression name, relative to the current model. May identify a single property or an
    /// <see cref="object"/> that contains the properties to display.
    /// </param>
    /// <param name="templateName">The name of the template used to create the HTML markup.</param>
    /// <param name="htmlFieldName">
    /// A <see cref="string"/> used to disambiguate the names of HTML elements that are created for
    /// properties that have the same name.
    /// </param>
    /// <param name="additionalViewData">
    /// An anonymous <see cref="object"/> or <see cref="IDictionary{String, Object}"/> that can contain additional
    /// view data that will be merged into the <see cref="ViewDataDictionary{TModel}"/> instance created for the
    /// template.
    /// </param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the created HTML.</returns>
    /// <remarks>
    /// <para>
    /// For example the default <see cref="object"/> display template includes markup for each property in the
    /// <paramref name="expression"/>'s value.
    /// </para>
    /// <para>
    /// Example <paramref name="expression"/>s include <c>string.Empty</c> which identifies the current model and
    /// <c>"prop"</c> which identifies the current model's "prop" property.
    /// </para>
    /// <para>
    /// Custom templates are found under a <c>DisplayTemplates</c> folder. The folder name is case-sensitive on
    /// case-sensitive file systems.
    /// </para>
    /// </remarks>
    IHtmlContent Display(
        string expression,
        string templateName,
        string htmlFieldName,
        object additionalViewData);

    /// <summary>
    /// Returns the display name for the specified <paramref name="expression"/>.
    /// </summary>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <returns>A <see cref="string"/> containing the display name.</returns>
    string DisplayName(string expression);

    /// <summary>
    /// Returns the simple display text for the specified <paramref name="expression"/>.
    /// </summary>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <returns>
    /// A <see cref="string"/> containing the simple display text.
    /// If the expression result is <c>null</c>, returns <see cref="ModelMetadata.NullDisplayText"/>.
    /// </returns>
    string DisplayText(string expression);

    /// <summary>
    /// Returns a single-selection HTML &lt;select&gt; element for the <paramref name="expression"/>. Adds
    /// &lt;option&gt; elements based on <paramref name="optionLabel"/> and <paramref name="selectList"/>. Adds a
    /// "selected" attribute to an &lt;option&gt; if its <see cref="SelectListItem.Value"/> (if non-<c>null</c>) or
    /// <see cref="SelectListItem.Text"/> matches the first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name,
    /// the <see cref="ViewData"/> entry with full name (unless used instead of <paramref name="selectList"/>), or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewDataDictionary.Model"/>.
    /// See <see cref="Name"/> for more information about a "full name".
    /// </summary>
    /// <param name="expression">Expression name, relative to the current model.</param>
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
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;select&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
    /// &lt;select&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
    /// attribute.
    /// </remarks>
    IHtmlContent DropDownList(
        string expression,
        IEnumerable<SelectListItem> selectList,
        string optionLabel,
        object htmlAttributes);

    /// <summary>
    /// Returns HTML markup for the <paramref name="expression"/>, using an editor template, specified HTML field
    /// name, and additional view data. The template is found using the <paramref name="templateName"/> or the
    /// <paramref name="expression"/>'s <see cref="ModelMetadata"/>.
    /// </summary>
    /// <param name="expression">
    /// Expression name, relative to the current model. May identify a single property or an
    /// <see cref="object"/> that contains the properties to edit.
    /// </param>
    /// <param name="templateName">The name of the template used to create the HTML markup.</param>
    /// <param name="htmlFieldName">
    /// A <see cref="string"/> used to disambiguate the names of HTML elements that are created for
    /// properties that have the same name.
    /// </param>
    /// <param name="additionalViewData">
    /// An anonymous <see cref="object"/> or <see cref="IDictionary{String, Object}"/> that can contain additional
    /// view data that will be merged into the <see cref="ViewDataDictionary{TModel}"/> instance created for the
    /// template.
    /// </param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; element(s).</returns>
    /// <remarks>
    /// <para>
    /// For example the default <see cref="object"/> editor template includes &lt;label&gt; and &lt;input&gt;
    /// elements for each property in the <paramref name="expression"/>'s value.
    /// </para>
    /// <para>
    /// Example <paramref name="expression"/>s include <c>string.Empty</c> which identifies the current model and
    /// <c>"prop"</c> which identifies the current model's "prop" property.
    /// </para>
    /// <para>
    /// Custom templates are found under a <c>EditorTemplates</c> folder. The folder name is case-sensitive on
    /// case-sensitive file systems.
    /// </para>
    /// </remarks>
    IHtmlContent Editor(string expression, string templateName, string htmlFieldName, object additionalViewData);

    /// <summary>
    /// Converts the <paramref name="value"/> to an HTML-encoded <see cref="string"/>.
    /// </summary>
    /// <param name="value">The <see cref="object"/> to encode.</param>
    /// <returns>The HTML-encoded <see cref="string"/>.</returns>
    string Encode(object value);

    /// <summary>
    /// Converts the specified <see cref="string"/> to an HTML-encoded <see cref="string"/>.
    /// </summary>
    /// <param name="value">The <see cref="string"/> to encode.</param>
    /// <returns>The HTML-encoded <see cref="string"/>.</returns>
    string Encode(string value);

    /// <summary>
    /// Renders the &lt;/form&gt; end tag to the response.
    /// </summary>
    /// <remarks>
    /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
    /// </remarks>
    void EndForm();

    /// <summary>
    /// Formats the value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="format">
    /// The format string (see <see href="https://msdn.microsoft.com/en-us/library/txafckwd.aspx"/>) used to format the return
    /// value.
    /// </param>
    /// <returns>A <see cref="string"/> containing the formatted value.</returns>
    /// <remarks>
    /// Converts <paramref name="value"/> to a <see cref="string"/> directly if
    /// <paramref name="format"/> is <c>null</c> or empty.
    /// </remarks>
    string FormatValue(object value, string format);

    /// <summary>
    /// Returns an HTML element Id for the specified expression <paramref name="fullName"/>.
    /// </summary>
    /// <param name="fullName">
    /// Fully-qualified expression name, ignoring the current model. Must not be <c>null</c>. See
    /// <see cref="Name"/> for more information about a "full name".
    /// </param>
    /// <returns>A <see cref="string"/> containing the element Id.</returns>
    string GenerateIdFromName(string fullName);

    /// <summary>
    /// Returns a select list for the given <typeparamref name="TEnum"/>.
    /// </summary>
    /// <typeparam name="TEnum">Type to generate a select list for.</typeparam>
    /// <returns>
    /// An <see cref="IEnumerable{SelectListItem}"/> containing the select list for the given
    /// <typeparamref name="TEnum"/>,
    /// with a decimal representation of the ordinal as <see cref="SelectListItem.Value"/>
    /// and the display name as <see cref="SelectListItem.Text"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if <typeparamref name="TEnum"/> is not an <see cref="Enum"/> or if it has a
    /// <see cref="FlagsAttribute"/>.
    /// </exception>
    IEnumerable<SelectListItem> GetEnumSelectList<TEnum>() where TEnum : struct;

    /// <summary>
    /// Returns a select list for the given <paramref name="enumType"/>.
    /// </summary>
    /// <param name="enumType"><see cref="Type"/> to generate a select list for.</param>
    /// <returns>
    /// An <see cref="IEnumerable{SelectListItem}"/> containing the select list for the given
    /// <paramref name="enumType"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="enumType"/> is not an <see cref="Enum"/> or if it has a
    /// <see cref="FlagsAttribute"/>.
    /// </exception>
    IEnumerable<SelectListItem> GetEnumSelectList(Type enumType);

    /// <summary>
    /// Returns an &lt;input&gt; element of type "hidden" for the specified <paramref name="expression"/>. Adds a
    /// "value" attribute to the element containing the first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name,
    /// the <paramref name="value"/> parameter,
    /// the <see cref="ViewData"/> entry with full name,
    /// the <paramref name="expression"/> evaluated against <see cref="ViewDataDictionary.Model"/>, or
    /// the <paramref name="htmlAttributes"/> dictionary entry with key "value".
    /// See <see cref="Name"/> for more information about a "full name".
    /// </summary>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="value">If non-<c>null</c>, value to include in the element.</param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
    /// </param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
    /// &lt;input&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
    /// attribute.
    /// </remarks>
    IHtmlContent Hidden(string expression, object value, object htmlAttributes);

    /// <summary>
    /// Returns the HTML element Id for the specified <paramref name="expression"/>.
    /// </summary>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <returns>A <see cref="string"/> containing the element Id.</returns>
    string Id(string expression);

    /// <summary>
    /// Returns a &lt;label&gt; element for the specified <paramref name="expression"/>.
    /// </summary>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="labelText">The inner text of the element.</param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
    /// </param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;label&gt; element.</returns>
    IHtmlContent Label(string expression, string labelText, object htmlAttributes);

    /// <summary>
    /// Returns a multi-selection &lt;select&gt; element for the <paramref name="expression"/>. Adds
    /// &lt;option&gt; elements based on <paramref name="selectList"/>. Adds a "selected" attribute to an
    /// &lt;option&gt; if its <see cref="SelectListItem.Value"/> (if non-<c>null</c>) or
    /// <see cref="SelectListItem.Text"/> matches an entry in the first non-<c>null</c> collection found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name,
    /// the <see cref="ViewData"/> entry with full name (unless used instead of <paramref name="selectList"/>), or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewDataDictionary.Model"/>.
    /// See <see cref="Name"/> for more information about a "full name".
    /// </summary>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="selectList">
    /// A collection of <see cref="SelectListItem"/> objects used to populate the &lt;select&gt; element with
    /// &lt;optgroup&gt; and &lt;option&gt; elements. If <c>null</c>, finds the <see cref="SelectListItem"/>
    /// collection with name <paramref name="expression"/> in <see cref="ViewData"/>.
    /// </param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the &lt;select&gt; element. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
    /// </param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;select&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
    /// &lt;select&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
    /// attribute.
    /// </remarks>
    IHtmlContent ListBox(string expression, IEnumerable<SelectListItem> selectList, object htmlAttributes);

    /// <summary>
    /// Returns the full HTML element name for the specified <paramref name="expression"/>. Uses
    /// <see cref="TemplateInfo.HtmlFieldPrefix"/> (if non-empty) to reflect relationship between current
    /// <see cref="ViewDataDictionary.Model"/> and the top-level view's model.
    /// </summary>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <returns>A <see cref="string"/> containing the element name.</returns>
    string Name(string expression);

    /// <summary>
    /// Returns HTML markup for the specified partial view.
    /// </summary>
    /// <param name="partialViewName">
    /// The name of the partial view used to create the HTML markup. Must not be <c>null</c>.
    /// </param>
    /// <param name="model">A model to pass into the partial view.</param>
    /// <param name="viewData">A <see cref="ViewDataDictionary"/> to pass into the partial view.</param>
    /// <returns>
    /// A <see cref="Task"/> that on completion returns a new <see cref="IHtmlContent"/> instance containing
    /// the created HTML.
    /// </returns>
    Task<IHtmlContent> PartialAsync(string partialViewName, object model, ViewDataDictionary viewData);

    /// <summary>
    /// Returns an &lt;input&gt; element of type "password" for the specified <paramref name="expression"/>. Adds a
    /// "value" attribute containing the first non-<c>null</c> value in:
    /// the <paramref name="value"/> parameter, or
    /// the <paramref name="htmlAttributes"/> dictionary entry with key "value".
    /// </summary>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="value">If non-<c>null</c>, value to include in the element.</param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
    /// </param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
    /// &lt;input&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
    /// attribute.
    /// </remarks>
    IHtmlContent Password(string expression, object value, object htmlAttributes);

    /// <summary>
    /// Returns an &lt;input&gt; element of type "radio" for the specified <paramref name="expression"/>.
    /// Adds a "value" attribute to the element containing the first non-<c>null</c> value found in:
    /// the <paramref name="value"/> parameter, or
    /// the <paramref name="htmlAttributes"/> dictionary entry with key "value".
    /// Adds a "checked" attribute to the element if <paramref name="value"/> matches the first non-<c>null</c>
    /// value found in:
    /// the <paramref name="htmlAttributes"/> dictionary entry with key "checked",
    /// the <see cref="ActionContext.ModelState"/> entry with full name,
    /// the <see cref="ViewData"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewDataDictionary.Model"/>.
    /// If <paramref name="isChecked"/> is non-<c>null</c>, instead uses the first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name, or
    /// the <paramref name="isChecked"/> parameter.
    /// See <see cref="Name"/> for more information about a "full name".
    /// </summary>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="value">
    /// If non-<c>null</c>, value to include in the element. Must not be <c>null</c> if
    /// <paramref name="isChecked"/> is also <c>null</c> and no "checked" entry exists in
    /// <paramref name="htmlAttributes"/>.
    /// </param>
    /// <param name="isChecked">
    /// If <c>true</c>, radio button is initially selected. Must not be <c>null</c> if
    /// <paramref name="value"/> is also <c>null</c> and no "checked" entry exists in
    /// <paramref name="htmlAttributes"/>.
    /// </param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
    /// </param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
    /// &lt;input&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
    /// attribute.
    /// </remarks>
    IHtmlContent RadioButton(string expression, object value, bool? isChecked, object htmlAttributes);

    /// <summary>
    /// Wraps HTML markup in an <see cref="HtmlString"/>, without HTML-encoding the specified
    /// <paramref name="value"/>.
    /// </summary>
    /// <param name="value">HTML markup <see cref="string"/>.</param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the wrapped <see cref="string"/>.</returns>
    IHtmlContent Raw(string value);

    /// <summary>
    /// Wraps HTML markup from the string representation of an <see cref="object"/> in an
    /// <see cref="HtmlString"/>, without HTML-encoding the string representation.
    /// </summary>
    /// <param name="value">The <see cref="object"/> to wrap.</param>
    /// <returns><see cref="IHtmlContent"/> containing the wrapped string representation.</returns>
    IHtmlContent Raw(object value);

    /// <summary>
    /// Renders HTML markup for the specified partial view.
    /// </summary>
    /// <param name="partialViewName">
    /// The name or path of the partial view used to create the HTML markup. Must not be <c>null</c>.
    /// </param>
    /// <param name="model">A model to pass into the partial view.</param>
    /// <param name="viewData">A <see cref="ViewDataDictionary"/> to pass into the partial view.</param>
    /// <returns>A <see cref="Task"/> that renders the created HTML when it executes.</returns>
    /// <remarks>
    /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
    /// </remarks>
    Task RenderPartialAsync(string partialViewName, object model, ViewDataDictionary viewData);

    /// <summary>
    /// Returns an anchor (&lt;a&gt;) element that contains a URL path to the specified route.
    /// </summary>
    /// <param name="linkText">The inner text of the anchor element. Must not be <c>null</c>.</param>
    /// <param name="routeName">The name of the route.</param>
    /// <param name="protocol">The protocol for the URL, such as &quot;http&quot; or &quot;https&quot;.</param>
    /// <param name="hostName">The host name for the URL.</param>
    /// <param name="fragment">The URL fragment name (the anchor name).</param>
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
    /// <returns>A new <see cref="IHtmlContent"/> containing the anchor element.</returns>
    IHtmlContent RouteLink(
        string linkText,
        string routeName,
        string protocol,
        string hostName,
        string fragment,
        object routeValues,
        object htmlAttributes);

    /// <summary>
    /// Returns a &lt;textarea&gt; element for the specified <paramref name="expression"/>. Adds content to the
    /// element body based on the first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name,
    /// the <paramref name="value"/> parameter,
    /// the <see cref="ViewData"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewDataDictionary.Model"/>.
    /// See <see cref="Name"/> for more information about a "full name".
    /// </summary>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="value">If non-<c>null</c>, value to include in the element.</param>
    /// <param name="rows">Number of rows in the textarea.</param>
    /// <param name="columns">Number of columns in the textarea.</param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
    /// </param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;textarea&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
    /// &lt;textarea&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
    /// attribute.
    /// </remarks>
    IHtmlContent TextArea(string expression, string value, int rows, int columns, object htmlAttributes);

    /// <summary>
    /// Returns an &lt;input&gt; element of type "text" for the specified <paramref name="expression"/>. Adds a
    /// "value" attribute to the element containing the first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name,
    /// the <paramref name="value"/> parameter,
    /// the <see cref="ViewData"/> entry with full name,
    /// the <paramref name="expression"/> evaluated against <see cref="ViewDataDictionary.Model"/>, or
    /// the <paramref name="htmlAttributes"/> dictionary entry with key "value".
    /// See <see cref="Name"/> for more information about a "full name".
    /// </summary>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="value">If non-<c>null</c>, value to include in the element.</param>
    /// <param name="format">
    /// The format string (see <see href="https://msdn.microsoft.com/en-us/library/txafckwd.aspx"/>) used to format the "value"
    /// attribute unless that came from model binding.
    /// </param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
    /// </param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; element.</returns>
    /// <remarks>
    /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
    /// &lt;input&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
    /// attribute.
    /// </remarks>
    IHtmlContent TextBox(string expression, object value, string format, object htmlAttributes);

    /// <summary>
    /// Returns the validation message if an error exists in the <see cref="ModelStateDictionary"/> object
    /// for the specified <paramref name="expression"/>.
    /// </summary>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="message">
    /// The message to be displayed. If <c>null</c> or empty, method extracts an error string from the
    /// <see cref="ModelStateDictionary"/> object. Message will always be visible but client-side validation may
    /// update the associated CSS class.
    /// </param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the <paramref name="tag"/> element.
    /// Alternatively, an <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
    /// </param>
    /// <param name="tag">
    /// The tag to wrap the <paramref name="message"/> in the generated HTML. Its default value is
    /// <see cref="ViewContext.ValidationMessageElement"/>.
    /// </param>
    /// <returns>
    /// A new <see cref="IHtmlContent"/> containing a <paramref name="tag"/> element. An empty
    /// <see cref="IHtmlContent"/> if the <paramref name="expression"/> is valid and client-side validation is
    /// disabled.
    /// </returns>
    IHtmlContent ValidationMessage(string expression, string message, object htmlAttributes, string tag);

    /// <summary>
    /// Returns an unordered list (&lt;ul&gt; element) of validation messages that are in the
    /// <see cref="ModelStateDictionary"/> object.
    /// </summary>
    /// <param name="excludePropertyErrors">
    /// If <c>true</c>, display model-level errors only; otherwise display all errors.
    /// </param>
    /// <param name="message">The message to display with the validation summary.</param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the topmost (&lt;div&gt;) element.
    /// Alternatively, an <see cref="IDictionary{String, Object}"/> instance containing the HTML attributes.
    /// </param>
    /// <param name="tag">
    /// The tag to wrap the <paramref name="message"/> in the generated HTML. Its default value is
    /// <see cref="ViewContext.ValidationSummaryMessageElement" />.
    /// </param>
    /// <returns>
    /// New <see cref="IHtmlContent"/> containing a &lt;div&gt; element wrapping the <paramref name="tag"/> element
    /// and the &lt;ul&gt; element. An empty <see cref="IHtmlContent"/> if the current model is valid and
    /// client-side validation is disabled.
    /// </returns>
    IHtmlContent ValidationSummary(
        bool excludePropertyErrors,
        string message,
        object htmlAttributes,
        string tag);

    /// <summary>
    /// Returns the formatted value for the specified <paramref name="expression"/>. Specifically, returns the
    /// first non-<c>null</c> value found in:
    /// the <see cref="ActionContext.ModelState"/> entry with full name,
    /// the <see cref="ViewData"/> entry with full name, or
    /// the <paramref name="expression"/> evaluated against <see cref="ViewDataDictionary.Model"/>.
    /// See <see cref="Name"/> for more information about a "full name".
    /// </summary>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="format">
    /// The format string (see <see href="https://msdn.microsoft.com/en-us/library/txafckwd.aspx"/>) used to format the return
    /// value unless that came from model binding.
    /// </param>
    /// <returns>A <see cref="string"/> containing the formatted value.</returns>
    /// <remarks>
    /// Converts the expression result to a <see cref="string"/> directly if
    /// <paramref name="format"/> is <c>null</c> or empty.
    /// </remarks>
    string Value(string expression, string format);
}
