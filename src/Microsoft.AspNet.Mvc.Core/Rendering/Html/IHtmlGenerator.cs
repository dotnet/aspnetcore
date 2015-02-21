// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Rendering
{
    /// <summary>
    /// Contract for a service supporting <see cref="IHtmlHelper"/> and <c>ITagHelper</c> implementations.
    /// </summary>
    public interface IHtmlGenerator
    {
        string IdAttributeDotReplacement { get; set; }

        string Encode(string value);

        string Encode(object value);

        string FormatValue(object value, string format);

        TagBuilder GenerateActionLink(
            [NotNull] string linkText,
            string actionName,
            string controllerName,
            string protocol,
            string hostname,
            string fragment,
            object routeValues,
            object htmlAttributes);

        TagBuilder GenerateAntiForgery([NotNull] ViewContext viewContext);

        TagBuilder GenerateCheckBox(
            [NotNull] ViewContext viewContext,
            ModelMetadata metadata,
            string expression,
            bool? isChecked,
            object htmlAttributes);

        /// <summary>
        /// Generate an additional &lt;input type="hidden".../&gt; for checkboxes. This addresses scenarios where
        /// unchecked checkboxes are not sent in the request. Sending a hidden input makes it possible to know that the
        /// checkbox was present on the page when the request was submitted.
        /// </summary>
        TagBuilder GenerateHiddenForCheckbox(
            [NotNull] ViewContext viewContext,
            ModelMetadata metadata,
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
        /// <see cref="IDictionary{string, object}"/> instance containing the route parameters.
        /// </param>
        /// <param name="method">The HTTP method for processing the form, either GET or POST.</param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>
        /// A <see cref="TagBuilder"/> instance for the &lt;/form&gt; element.
        /// </returns>
        TagBuilder GenerateForm(
            [NotNull] ViewContext viewContext,
            string actionName,
            string controllerName,
            object routeValues,
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
        /// <see cref="IDictionary{string, object}"/> instance containing the route parameters.
        /// </param>
        /// <param name="method">The HTTP method for processing the form, either GET or POST.</param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>
        /// A <see cref="TagBuilder"/> instance for the &lt;/form&gt; element.
        /// </returns>
        TagBuilder GenerateRouteForm(
            [NotNull] ViewContext viewContext,
            string routeName,
            object routeValues,
            string method,
            object htmlAttributes);

        TagBuilder GenerateHidden(
            [NotNull] ViewContext viewContext,
            ModelMetadata metadata,
            string expression,
            object value,
            bool useViewData,
            object htmlAttributes);

        TagBuilder GenerateLabel(
            [NotNull] ViewContext viewContext,
            [NotNull] ModelMetadata metadata,
            string expression,
            string labelText,
            object htmlAttributes);

        TagBuilder GeneratePassword(
            [NotNull] ViewContext viewContext,
            ModelMetadata metadata,
            string expression,
            object value,
            object htmlAttributes);

        TagBuilder GenerateRadioButton(
            [NotNull] ViewContext viewContext,
            ModelMetadata metadata,
            string expression,
            object value,
            bool? isChecked,
            object htmlAttributes);

        TagBuilder GenerateRouteLink(
            [NotNull] string linkText,
            string routeName,
            string protocol,
            string hostName,
            string fragment,
            object routeValues,
            object htmlAttributes);

        TagBuilder GenerateSelect(
            [NotNull] ViewContext viewContext,
            ModelMetadata metadata,
            string optionLabel,
            string expression,
            IEnumerable<SelectListItem> selectList,
            bool allowMultiple,
            object htmlAttributes);

        TagBuilder GenerateSelect(
            [NotNull] ViewContext viewContext,
            ModelMetadata metadata,
            string optionLabel,
            string expression,
            IEnumerable<SelectListItem> selectList,
            bool allowMultiple,
            object htmlAttributes,
            out ICollection<string> selectedValues);

        TagBuilder GenerateTextArea(
            [NotNull] ViewContext viewContext,
            ModelMetadata metadata,
            string expression,
            int rows,
            int columns,
            object htmlAttributes);

        TagBuilder GenerateTextBox(
            [NotNull] ViewContext viewContext,
            ModelMetadata metadata,
            string expression,
            object value,
            string format,
            object htmlAttributes);

        TagBuilder GenerateValidationMessage(
            [NotNull] ViewContext viewContext,
            string expression,
            string message,
            string tag,
            object htmlAttributes);

        TagBuilder GenerateValidationSummary(
            [NotNull] ViewContext viewContext,
            bool excludePropertyErrors,
            string message,
            string headerTag,
            object htmlAttributes);

        /// <remarks>
        /// Not used directly in <see cref="HtmlHelper"/>. Exposed publicly for use in other <see cref="IHtmlHelper"/>
        /// implementations.
        /// </remarks>
        IEnumerable<ModelClientValidationRule> GetClientValidationRules(
            [NotNull] ViewContext viewContext,
            ModelMetadata metadata,
            string expression);
    }
}