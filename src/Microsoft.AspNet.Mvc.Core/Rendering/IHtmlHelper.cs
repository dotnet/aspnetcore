// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.Framework.Internal;
using Microsoft.Framework.WebEncoders;

namespace Microsoft.AspNet.Mvc.Rendering
{
    /// <summary>
    /// Base HTML helpers.
    /// </summary>
    public interface IHtmlHelper
    {
        /// <summary>
        /// Set this property to <see cref="Html5DateRenderingMode.Rfc3339"/> to have templated helpers such as
        /// <see cref="Editor"/> and <see cref="IHtmlHelper{TModel}.EditorFor"/> render date and time values as RFC
        /// 3339 compliant strings. By default these helpers render dates and times using the current culture.
        /// </summary>
        Html5DateRenderingMode Html5DateRenderingMode { get; set; }

        /// <summary>
        /// Gets or sets the character that replaces periods in the ID attribute of an element.
        /// </summary>
        string IdAttributeDotReplacement { get; set; }

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
        /// Gets the <see cref="IHtmlEncoder"/> to be used for encoding HTML.
        /// </summary>
        IHtmlEncoder HtmlEncoder { get; }

        /// <summary>
        /// Gets the <see cref="IUrlEncoder"/> to be used for encoding a URL.
        /// </summary>
        IUrlEncoder UrlEncoder { get; }

        /// <summary>
        /// Gets the <see cref="IJavaScriptStringEncoder"/> to be used for encoding JavaScript.
        /// </summary>
        IJavaScriptStringEncoder JavaScriptStringEncoder { get; }

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
        /// <see cref="IDictionary{string, object}"/> instance containing the route parameters.
        /// </param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the anchor element.</returns>
        HtmlString ActionLink(
            [NotNull] string linkText,
            string actionName,
            string controllerName,
            string protocol,
            string hostname,
            string fragment,
            object routeValues,
            object htmlAttributes);

        /// <summary>
        /// Returns a &lt;hidden&gt; element (anti-forgery token) that will be validated when the containing
        /// &lt;form&gt; is submitted.
        /// </summary>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;hidden&gt; element.</returns>
        HtmlString AntiForgeryToken();

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
        /// <see cref="IDictionary{string, object}"/> instance containing the route parameters.
        /// </param>
        /// <param name="method">The HTTP method for processing the form, either GET or POST.</param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
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
        /// <see cref="IDictionary{string, object}"/> instance containing the route parameters.
        /// </param>
        /// <param name="method">The HTTP method for processing the form, either GET or POST.</param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
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
            object htmlAttributes);

        /// <summary>
        /// Returns an &lt;input&gt; element of type "checkbox" with value "true" and an &lt;input&gt; element of type
        /// "hidden" with value "false".
        /// </summary>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <param name="isChecked">If <c>true</c>, checkbox is initially checked.</param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the checkbox element. Alternatively, an
        /// <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;input&gt; elements.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set checkbox
        /// element's "name" attribute. Sanitizes <paramref name="expression"/> to set checkbox element's "id"
        /// attribute.
        /// </para>
        /// <para>Determines checkbox element's "checked" attribute based on the following precedence:</para>
        /// <list type="number">
        /// <item>
        /// <see cref="ModelStateDictionary"/> entry for <paramref name="expression"/> (converted to a fully-qualified
        /// name) if entry exists and can be converted to a <see cref="bool"/>.
        /// </item>
        /// <item><paramref name="isChecked"/> if non-<c>null</c>.</item>
        /// <item>
        /// <see cref="ViewData"/> entry for <paramref name="expression"/> (converted to a fully-qualified name)
        /// if entry exists and can be converted to a <see cref="bool"/>.
        /// </item>
        /// <item>
        /// Linq expression based on <paramref name="expression"/> (converted to a fully-qualified name) run against
        /// current model if result is non-<c>null</c> and can be converted to a <see cref="bool"/>. For example
        /// <c>string.Empty</c> identifies the current model and <c>"prop"</c> identifies the current model's "prop"
        /// property.
        /// </item>
        /// <item>Existing "checked" entry in <paramref name="htmlAttributes"/> if any.</item>
        /// <item>Otherwise, does not include a "checked" attribute.</item>
        /// </list>
        /// <para>
        /// In all but the <paramref name="htmlAttributes"/> and default cases, includes a "checked" attribute with
        /// value "checked" if the <see cref="bool"/> values is <c>true</c>; does not include the attribute otherwise.
        /// </para>
        /// </remarks>
        HtmlString CheckBox(string expression, bool? isChecked, object htmlAttributes);

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
        /// An anonymous <see cref="object"/> or <see cref="IDictionary{string, object}"/> that can contain additional
        /// view data that will be merged into the <see cref="ViewDataDictionary{TModel}"/> instance created for the
        /// template.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the created HTML.</returns>
        /// <remarks>
        /// <para>
        /// For example the default <see cref="object"/> display template includes markup for each property in the
        /// <paramref name="expression"/>'s value.
        /// </para>
        /// <para>
        /// Example <paramref name="expression"/>s include <c>string.Empty</c> which identifies the current model and
        /// <c>"prop"</c> which identifies the current model's "prop" property.
        /// </para>
        /// </remarks>
        HtmlString Display(
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
        /// Returns a single-selection HTML &lt;select&gt; element for the <paramref name="expression"/>,
        /// using the specified list items, option label, and HTML attributes.
        /// </summary>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <param name="selectList">
        /// A collection of <see cref="SelectListItem"/> objects used to populate the &lt;select&gt; element with
        /// &lt;optgroup&gt; and &lt;option&gt; elements.
        /// </param>
        /// <param name="optionLabel">
        /// The text for a default empty item. Does not include such an item if argument is <c>null</c>.
        /// </param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the &lt;select&gt; element. Alternatively, an
        /// <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;select&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute.
        /// </remarks>
        HtmlString DropDownList(
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
        /// An anonymous <see cref="object"/> or <see cref="IDictionary{string, object}"/> that can contain additional
        /// view data that will be merged into the <see cref="ViewDataDictionary{TModel}"/> instance created for the
        /// template.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;input&gt; element(s).</returns>
        /// <remarks>
        /// <para>
        /// For example the default <see cref="object"/> editor template includes &lt;label&gt; and &lt;input&gt;
        /// elements for each property in the <paramref name="expression"/>'s value.
        /// </para>
        /// <para>
        /// Example <paramref name="expression"/>s include <c>string.Empty</c> which identifies the current model and
        /// <c>"prop"</c> which identifies the current model's "prop" property.
        /// </para>
        /// </remarks>
        HtmlString Editor(string expression, string templateName, string htmlFieldName, object additionalViewData);

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
        /// The composite format <see cref="string"/> (see http://msdn.microsoft.com/en-us/library/txafckwd.aspx).
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
        /// Fully-qualified expression name, ignoring the current model. Must not be <c>null</c>.
        /// </param>
        /// <returns>A <see cref="string"/> containing the element Id.</returns>
        string GenerateIdFromName([NotNull] string fullName);

        /// <summary>
        /// Returns information about about client validation rules for the specified <paramref name="metadata"/> or
        /// <paramref name="expression"/>. Intended for use in <see cref="IHtmlHelper"/> extension methods.
        /// </summary>
        /// <param name="metadata">Metadata about the <see cref="object"/> of interest.</param>
        /// <param name="expression">
        /// Expression name, relative to the current model. Used to determine <see cref="ModelMetadata"/> when
        /// <paramref name="metadata"/> is <c>null</c>; ignored otherwise.
        /// </param>
        /// <returns>An <see cref="IEnumerable{ModelClientValidationRule}"/> containing the relevant rules.</returns>
        IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelExplorer modelExplorer, string expression);

        /// <summary>
        /// Returns an &lt;input&gt; element of type "hidden" for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <param name="value">If non-<c>null</c>, value to include in the element.</param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;input&gt; element.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;input&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute.
        /// </para>
        /// <para>Determines &lt;input&gt; element's "value" attribute based on the following precedence:</para>
        /// <list type="number">
        /// <item>
        /// <see cref="ModelStateDictionary"/> entry for <paramref name="expression"/> (converted to a fully-qualified
        /// name) if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item><paramref name="value"/> if non-<c>null</c>.</item>
        /// <item>
        /// <see cref="ViewData"/> entry for <paramref name="expression"/> (converted to a fully-qualified name)
        /// if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item>
        /// Linq expression based on <paramref name="expression"/> (converted to a fully-qualified name) run against
        /// current model if result is non-<c>null</c> and can be converted to a <see cref="string"/>. For example
        /// <c>string.Empty</c> identifies the current model and <c>"prop"</c> identifies the current model's "prop"
        /// property.
        /// </item>
        /// <item>Existing "value" entry in <paramref name="htmlAttributes"/> if any.</item>
        /// <item>Otherwise, <c>string.Empty</c>.</item>
        /// </list>
        /// </remarks>
        HtmlString Hidden(string expression, object value, object htmlAttributes);

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
        /// <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;label&gt; element.</returns>
        HtmlString Label(string expression, string labelText, object htmlAttributes);

        /// <summary>
        /// Returns a multi-selection &lt;select&gt; element for the <paramref name="expression"/>, using the
        /// specified list items and HTML attributes.
        /// </summary>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <param name="selectList">
        /// A collection of <see cref="SelectListItem"/> objects used to populate the &lt;select&gt; element with
        /// &lt;optgroup&gt; and &lt;option&gt; elements.
        /// </param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the &lt;select&gt; element. Alternatively, an
        /// <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;select&gt; element.</returns>
        /// <remarks>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;select&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute.
        /// </remarks>
        HtmlString ListBox(string expression, IEnumerable<SelectListItem> selectList, object htmlAttributes);

        /// <summary>
        /// Returns the full HTML element name for the specified <paramref name="expression"/>.
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
        /// A <see cref="Task"/> that on completion returns a new <see cref="HtmlString"/> containing
        /// the created HTML.
        /// </returns>
        Task<HtmlString> PartialAsync([NotNull] string partialViewName, object model, ViewDataDictionary viewData);

        /// <summary>
        /// Returns an &lt;input&gt; element of type "password" for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <param name="value">If non-<c>null</c>, value to include in the element.</param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;input&gt; element.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;input&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute.
        /// </para>
        /// <para>Determines &lt;input&gt; element's "value" attribute based on the following precedence:</para>
        /// <list type="number">
        /// <item><paramref name="value"/> if non-<c>null</c>.</item>
        /// <item>Existing "value" entry in <paramref name="htmlAttributes"/> if any.</item>
        /// <item>Otherwise, <c>string.Empty</c>.</item>
        /// </list>
        /// </remarks>
        HtmlString Password(string expression, object value, object htmlAttributes);

        /// <summary>
        /// Returns an &lt;input&gt; element of type "radio" for the specified <paramref name="expression"/>.
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
        /// <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;input&gt; element.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;input&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute.
        /// </para>
        /// <para>Determines element's "value" attribute based on the following precedence:</para>
        /// <list type="number">
        /// <item><paramref name="value"/> if non-<c>null</c>.</item>
        /// <item>Existing "value" entry in <paramref name="htmlAttributes"/> if any.</item>
        /// <item>Otherwise, <c>string.Empty</c>.</item>
        /// </list>
        /// <para>Determines &lt;input&gt; element's "checked" attribute based on the following precedence:</para>
        /// <list type="number">
        /// <item>
        /// <see cref="ModelStateDictionary"/> entry for <paramref name="expression"/> (converted to a fully-qualified
        /// name) if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item><paramref name="isChecked"/> if non-<c>null</c>.</item>
        /// <item>Existing "checked" entry in <paramref name="htmlAttributes"/> if any.</item>
        /// <item>
        /// <see cref="ViewData"/> entry for <paramref name="expression"/> (converted to a fully-qualified name)
        /// if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item>
        /// Linq expression based on <paramref name="expression"/> (converted to a fully-qualified name) run against
        /// current model if result is non-<c>null</c> and can be converted to a <see cref="string"/>. For example
        /// <c>string.Empty</c> identifies the current model and <c>"prop"</c> identifies the current model's "prop"
        /// property.
        /// </item>
        /// <item>Otherwise, does not include a "checked" attribute.</item>
        /// </list>
        /// <para>
        /// In all but the <paramref name="htmlAttributes"/> and default cases, includes a "checked" attribute with
        /// value "checked" if the <see cref="string"/> values is equal to a converted <see cref="string"/> for
        /// <paramref name="value"/> or <paramref name="isChecked"/> is <c>true</c> (for that case); does not include
        /// the attribute otherwise.
        /// </para>
        /// </remarks>
        HtmlString RadioButton(string expression, object value, bool? isChecked, object htmlAttributes);

        /// <summary>
        /// Wraps HTML markup in an <see cref="HtmlString"/>, without HTML-encoding the specified
        /// <paramref name="value"/>.
        /// </summary>
        /// <param name="value">HTML markup <see cref="string"/>.</param>
        /// <returns>A new <see cref="HtmlString"/> containing the wrapped <see cref="string"/>.</returns>
        HtmlString Raw(string value);

        /// <summary>
        /// Wraps HTML markup from the string representation of an <see cref="object"/> in an
        /// <see cref="HtmlString"/>, without HTML-encoding the string representation.
        /// </summary>
        /// <param name="value">The <see cref="object"/> to wrap.</param>
        /// <returns>A new <see cref="HtmlString"/> containing the wrapped string representation.</returns>
        HtmlString Raw(object value);

        /// <summary>
        /// Renders HTML markup for the specified partial view.
        /// </summary>
        /// <param name="partialViewName">
        /// The name of the partial view used to create the HTML markup. Must not be <c>null</c>.
        /// </param>
        /// <param name="model">A model to pass into the partial view.</param>
        /// <param name="viewData">A <see cref="ViewDataDictionary"/> to pass into the partial view.</param>
        /// <returns>A <see cref="Task"/> that renders the created HTML when it executes.</returns>
        /// <remarks>
        /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
        /// </remarks>
        Task RenderPartialAsync([NotNull] string partialViewName, object model, ViewDataDictionary viewData);

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
        /// <see cref="IDictionary{string, object}"/> instance containing the route parameters.
        /// </param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the anchor element.</returns>
        HtmlString RouteLink(
            [NotNull] string linkText,
            string routeName,
            string protocol,
            string hostName,
            string fragment,
            object routeValues,
            object htmlAttributes);

        /// <summary>
        /// Returns a &lt;textarea&gt; element for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <param name="value">If non-<c>null</c>, value to include in the element.</param>
        /// <param name="rows">Number of rows in the textarea.</param>
        /// <param name="columns">Number of columns in the textarea.</param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;textarea&gt; element.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="expression"/> to set
        /// &lt;textarea&gt; element's "name" attribute. Sanitizes <paramref name="expression"/> to set element's "id"
        /// attribute.
        /// </para>
        /// <para>Determines &lt;textarea&gt; element's content based on the following precedence:</para>
        /// <list type="number">
        /// <item>
        /// <see cref="ModelStateDictionary"/> entry for <paramref name="expression"/> (converted to a fully-qualified
        /// name) if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item><paramref name="value"/> if non-<c>null</c>.</item>
        /// <item>
        /// <see cref="ViewData"/> entry for <paramref name="expression"/> (converted to a fully-qualified name)
        /// if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item>
        /// Linq expression based on <paramref name="expression"/> (converted to a fully-qualified name) run against
        /// current model if result is non-<c>null</c> and can be converted to a <see cref="string"/>. For example
        /// <c>string.Empty</c> identifies the current model and <c>"prop"</c> identifies the current model's "prop"
        /// property.
        /// </item>
        /// <item>Otherwise, <c>string.Empty</c>.</item>
        /// </list>
        /// </remarks>
        HtmlString TextArea(string expression, string value, int rows, int columns, object htmlAttributes);

        /// <summary>
        /// Returns an &lt;input&gt; element of type "text" for the specified <paramref name="current"/>.
        /// </summary>
        /// <param name="current">Expression name, relative to the current model.</param>
        /// <param name="value">If non-<c>null</c>, value to include in the element.</param>
        /// <param name="format">
        /// The composite format <see cref="string"/> (see http://msdn.microsoft.com/en-us/library/txafckwd.aspx).
        /// </param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the &lt;input&gt; element.</returns>
        /// <remarks>
        /// <para>
        /// Combines <see cref="TemplateInfo.HtmlFieldPrefix"/> and <paramref name="current"/> to set
        /// &lt;input&gt; element's "name" attribute. Sanitizes <paramref name="current"/> to set element's "id"
        /// attribute.
        /// </para>
        /// <para>Determines &lt;input&gt; element's "value" attribute based on the following precedence:</para>
        /// <list type="number">
        /// <item>
        /// <see cref="ModelStateDictionary"/> entry for <paramref name="current"/> (converted to a fully-qualified
        /// name) if entry exists and can be converted to a <see cref="string"/>.
        /// </item>
        /// <item>
        /// <paramref name="value"/> if non-<c>null</c>. Formats <paramref name="value"/> using
        /// <paramref name="format"/> or converts <paramref name="value"/> to a <see cref="string"/> directly if
        /// <paramref name="format"/> is <c>null</c> or empty.
        /// </item>
        /// <item>
        /// <see cref="ViewData"/> entry for <paramref name="current"/> (converted to a fully-qualified name) if entry
        /// exists and can be converted to a <see cref="string"/>. Formats entry using <paramref name="format"/> or
        /// converts entry to a <see cref="string"/> directly if <paramref name="format"/> is <c>null</c> or empty.
        /// </item>
        /// <item>
        /// Linq expression based on <paramref name="current"/> (converted to a fully-qualified name) run against
        /// current model if result is non-<c>null</c> and can be converted to a <see cref="string"/>. For example
        /// <c>string.Empty</c> identifies the current model and <c>"prop"</c> identifies the current model's "prop"
        /// property. Formats result using <paramref name="format"/> or converts result to a <see cref="string"/>
        /// directly if <paramref name="format"/> is <c>null</c> or empty.
        /// </item>
        /// <item>Existing "value" entry in <paramref name="htmlAttributes"/> if any.</item>
        /// <item>Otherwise, <c>string.Empty</c>.</item>
        /// </list>
        /// </remarks>
        HtmlString TextBox(string current, object value, string format, object htmlAttributes);

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
        /// Alternatively, an <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <param name="tag">
        /// The tag to wrap the <paramref name="message"/> in the generated HTML. Its default value is
        /// <see cref="ViewContext.ValidationMessageElement"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="HtmlString"/> containing a <paramref name="tag"/> element. <c>null</c> if the
        /// <paramref name="expression"/> is valid and client-side validation is disabled.
        /// </returns>
        HtmlString ValidationMessage(string expression, string message, object htmlAttributes, string tag);

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
        /// Alternatively, an <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <param name="tag">
        /// The tag to wrap the <paramref name="message"/> in the generated HTML. Its default value is
        /// <see cref="ViewContext.ValidationSummaryMessageElement" />.
        /// </param>
        /// <returns>
        /// New <see cref="HtmlString"/> containing a &lt;div&gt; element wrapping the <paramref name="tag"/> element
        /// and the &lt;ul&gt; element. <see cref="HtmlString.Empty"/> if the current model is valid and client-side
        /// validation is disabled).
        /// </returns>
        HtmlString ValidationSummary(
            bool excludePropertyErrors,
            string message,
            object htmlAttributes,
            string tag);

        /// <summary>
        /// Returns the formatted value for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <param name="format">
        /// The composite format <see cref="string"/> (see http://msdn.microsoft.com/en-us/library/txafckwd.aspx).
        /// </param>
        /// <returns>A <see cref="string"/> containing the formatted value.</returns>
        /// <remarks>
        /// Converts the expression result to a <see cref="string"/> directly if
        /// <paramref name="format"/> is <c>null</c> or empty.
        /// </remarks>
        string Value(string expression, string format);
    }
}
