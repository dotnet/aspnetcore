using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Rendering
{
    /// <summary>
    /// Base HTML helpers.
    /// </summary>
    public interface IHtmlHelper
    {
        /// <summary>
        /// Set this property to <see cref="Mvc.Html5DateRenderingMode.Rfc3339"/> to have templated helpers such as
        /// <see cref="Editor"/> and <see cref="EditorFor"/> render date and time values as RFC 3339 compliant strings.
        /// By default these helpers render dates and times using the current culture.
        /// </summary>
        Html5DateRenderingMode Html5DateRenderingMode { get; set; }

        /// <summary>
        /// Gets or sets the character that replaces periods in the ID attribute of an element.
        /// </summary>
        string IdAttributeDotReplacement { get; set; }

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
        /// Returns an anchor element (a element) that contains a URL path to the specified action.
        /// </summary>
        /// <param name="linkText">The inner text of the anchor element.</param>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="controllerName">The name of the controller.</param>
        /// <param name="protocol">The protocol for the URL, such as &quot;http&quot; or &quot;https&quot;.</param>
        /// <param name="hostname">The host name for the URL.</param>
        /// <param name="fragment">The URL fragment name (the anchor name).</param>
        /// <param name="routeValues">
        /// An object that contains the parameters for a route. The parameters are retrieved through reflection by
        /// examining the properties of the object. This object is typically created using object initializer syntax.
        /// Alternatively, an <see cref="IDictionary{string, object}"/> instance containing the route parameters.
        /// </param>
        /// <param name="htmlAttributes">
        /// An object that contains the HTML attributes to set for the element. Alternatively, an
        /// <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>
        /// An anchor element (a element).
        /// </returns>
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
        /// Generates a hidden form field (anti-forgery token) that is validated when the form is submitted.
        /// </summary>
        /// <returns>
        /// The generated form field (anti-forgery token).
        /// </returns>
        HtmlString AntiForgeryToken();

        /// <summary>
        /// Writes an opening <form> tag to the response. When the user submits the form,
        /// the request will be processed by an action method.
        /// </summary>
        /// <param name="actionName">The name of the action method.</param>
        /// <param name="controllerName">The name of the controller.</param>
        /// <param name="routeValues">An object that contains the parameters for a route. The parameters are retrieved
        /// through reflection by examining the properties of the object. This object is typically created using object
        /// initializer syntax. Alternatively, an <see cref="IDictionary{string, object}"/> instance containing the
        /// route parameters.</param>
        /// <param name="method">The HTTP method for processing the form, either GET or POST.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes to set for the element.
        /// Alternatively, an <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>An <see cref="MvcForm"/> instance which emits the closing {form} tag when disposed.</returns>
        MvcForm BeginForm(
            string actionName,
            string controllerName,
            object routeValues,
            FormMethod method,
            object htmlAttributes);

        /// <summary>
        /// Render an input element of type "checkbox" with value "true" and an input element of type "hidden" with
        /// value "false".
        /// </summary>
        /// <param name="name">
        /// Rendered element's name. Also use this name to find value in submitted data or view data. Use view data
        /// only if value is not in submitted data and <paramref name="value"/> is <c>null</c>.
        /// </param>
        /// <param name="isChecked">
        /// If <c>true</c>, checkbox is initially checked. Ignore if named value is found in submitted data. Finally
        /// fall back to an existing "checked" value in <paramref name="htmlAttributes"/>.
        /// </param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes to set for the element.
        /// Alternatively, an <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>New <see cref="HtmlString"/> containing the rendered HTML.</returns>
        HtmlString CheckBox(string name, bool? isChecked, object htmlAttributes);

        /// <summary>
        /// Returns HTML markup for each property in the object that is represented by the expression, using the
        /// specified template, HTML field ID, and additional view data.
        /// </summary>
        /// <param name="expression">An expression that identifies the object that contains the properties to display.
        /// </param>
        /// <param name="templateName">The name of the template that is used to render the object.</param>
        /// <param name="htmlFieldName">
        /// A string that is used to disambiguate the names of HTML input elements that are rendered for properties
        /// that have the same name.
        /// </param>
        /// <param name="additionalViewData">
        /// An anonymous object or dictionary that can contain additional view data that will be merged into the
        /// <see cref="ViewDataDictionary{TModel}"/> instance that is created for the template.
        /// </param>
        /// <returns>The HTML markup for each property in the object that is represented by the expression.</returns>
        HtmlString Display(
            string expression,
            string templateName,
            string htmlFieldName,
            object additionalViewData);

        /// <summary>
        /// Returns HTML markup for each property in the model, using the specified template, an HTML field ID, and
        /// additional view data.
        /// </summary>
        /// <param name="templateName">The name of the template that is used to render the object.</param>
        /// <param name="htmlFieldName">
        /// A string that is used to disambiguate the names of HTML input elements that are rendered for properties
        /// that have the same name.
        /// </param>
        /// <param name="additionalViewData">
        /// An anonymous object or dictionary that can contain additional view data that will be merged into the
        /// <see cref="ViewDataDictionary{TModel}"/> instance that is created for the template.
        /// </param>
        /// <returns>The HTML markup for each property in the model.</returns>
        HtmlString DisplayForModel(string templateName, string htmlFieldName, object additionalViewData);

        /// <summary>
        /// Gets the display name.
        /// </summary>
        /// <param name="expression">An expression that identifies the object that contains the display name.</param>
        /// <returns>
        /// The display name.
        /// </returns>
        HtmlString DisplayName(string expression);

        /// <summary>
        /// Returns a single-selection HTML {select} element using the specified name of the form field,
        /// list items, option label, and HTML attributes.
        /// </summary>
        /// <param name="name">The name of the form field to return.</param>
        /// <param name="selectList">A collection of <see href="SelectListItem"/> objects that are used to populate the
        /// drop-down list.</param>
        /// <param name="optionLabel">The text for a default empty item. This parameter can be null.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes to set for the {select} element.
        /// Alternatively, an <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>An HTML {select} element with an {option} subelement for each item in the list.</returns>
        HtmlString DropDownList(
            string name,
            IEnumerable<SelectListItem> selectList,
            string optionLabel,
            object htmlAttributes);

        /// <summary>
        /// Returns an HTML input element for each property in the object that is represented by the expression, using
        /// the specified template, HTML field ID, and additional view data.
        /// </summary>
        /// <param name="expression">An expression that identifies the object that contains the properties to edit.
        /// </param>
        /// <param name="templateName">The name of the template that is used to render the object.</param>
        /// <param name="htmlFieldName">
        /// A string that is used to disambiguate the names of HTML input elements that are rendered for properties
        /// that have the same name.
        /// </param>
        /// <param name="additionalViewData">
        /// An anonymous object or dictionary that can contain additional view data that will be merged into the
        /// <see cref="ViewDataDictionary{TModel}"/> instance that is created for the template.
        /// </param>
        /// <returns>The HTML markup for the input elements for each property in the object that is represented by the
        /// expression.</returns>
        HtmlString Editor(string expression, string templateName, string htmlFieldName, object additionalViewData);

        /// <summary>
        /// Converts the value of the specified object to an HTML-encoded string.
        /// </summary>
        /// <param name="value">The object to encode.</param>
        /// <returns>The HTML-encoded string.</returns>
        string Encode(object value);

        /// <summary>
        /// Converts the specified string to an HTML-encoded string.
        /// </summary>
        /// <param name="value">The string to encode.</param>
        /// <returns>The HTML-encoded string.</returns>
        string Encode(string value);

        /// <summary>
        /// Renders the closing </form> tag to the response.
        /// </summary>
        void EndForm();

        /// <summary>
        /// Formats the value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The format string.</param>
        /// <returns>The formatted value.</returns>
        string FormatValue(object value, string format);

        /// <summary>
        /// Creates an HTML element ID using the specified element name.
        /// </summary>
        /// <param name="name">The name of the HTML element.</param>
        /// <returns>The ID of the HTML element.</returns>
        string GenerateIdFromName(string name);

        /// <summary>
        /// Render an input element of type "hidden".
        /// </summary>
        /// <param name="name">
        /// Rendered element's name. Also use this name to find value in submitted data or view data. Use view data
        /// only if value is not in submitted data and <paramref name="value"/> is <c>null</c>.
        /// </param>
        /// <param name="value">
        /// If non-<c>null</c>, value to include in the element. Ignore if named value is found in submitted data.
        /// </param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes to set for the element.
        /// Alternatively, an <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>New <see cref="HtmlString"/> containing the rendered HTML.</returns>
        HtmlString Hidden(string name, object value, object htmlAttributes);

        /// <summary>
        /// Returns an HTML label element and the property name of the property that is represented by the specified
        /// expression.
        /// </summary>
        /// <param name="expression">An expression that identifies the property to display.</param>
        /// <param name="labelText">The label text.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes to set for the element.</param>
        /// <returns>
        /// An HTML label element and the property name of the property that is represented by the expression.
        /// </returns>
        HtmlString Label(string expression, string labelText, object htmlAttributes);

        /// <summary>
        /// Gets the full HTML field name for the given expression <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Name of an expression, relative to the current model.</param>
        /// <returns>An <see cref="HtmlString"/> that represents HTML markup.</returns>
        HtmlString Name(string name);

        /// <summary>
        /// Returns a partial view in string format.
        /// </summary>
        /// <param name="partialViewName">The name of the partial view to render and return.</param>
        /// <param name="model">A model to pass into the partial view.</param>
        /// <param name="viewData">A <see cref="ViewDataDictionary"/> to pass into the partial view.</param>
        /// <returns>A task that represents when rendering of the partial view into a string has completed.</returns>
        Task<HtmlString> PartialAsync([NotNull] string partialViewName, object model, ViewDataDictionary viewData);

        /// <summary>
        /// Render an input element of type "password".
        /// </summary>
        /// <param name="name">
        /// Rendered element's name. Also use this name to find value in view data. Use view data
        /// only if value is not in submitted data and <paramref name="value"/> is <c>null</c>.
        /// </param>
        /// <param name="value">
        /// If non-<c>null</c>, value to include in the element.
        /// </param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes to set for the element.
        /// Alternatively, an <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>New <see cref="HtmlString"/> containing the rendered HTML.</returns>
        HtmlString Password(string name, object value, object htmlAttributes);

        /// <summary>
        /// Render an input element of type "radio".
        /// </summary>
        /// <param name="name">
        /// Rendered element's name. Also use this name to find value in submitted data or view data. Use view data
        /// only if value is not in submitted data and <paramref name="value"/> is <c>null</c>.
        /// </param>
        /// <param name="value">
        /// If non-<c>null</c>, value to include in the element. May be <c>null</c> only if
        /// <paramref name="isChecked"/> is also <c>null</c>. Also compared to value in submitted data or view data to
        /// determine <paramref name="isChecked"/> if that parameter is <c>null</c>. Ignore if named value is found in
        /// submitted data.
        /// </param>
        /// <param name="isChecked">
        /// If <c>true</c>, radio button is initially selected. Ignore if named value is found in submitted data. Fall
        /// back to comparing <paramref name="value"/> with view data if this parameter is <c>null</c>. Finally
        /// fall back to an existing "checked" value in <paramref name="htmlAttributes"/>.
        /// </param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes to set for the element.
        /// Alternatively, an <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>New <see cref="HtmlString"/> containing the rendered HTML.</returns>
        HtmlString RadioButton(string name, object value, bool? isChecked, object htmlAttributes);

        /// <summary>
        /// Wraps HTML markup in an <see cref="HtmlString"/>, which will enable HTML markup to be
        /// rendered to the output without getting HTML encoded.
        /// </summary>
        /// <param name="value">HTML markup string.</param>
        /// <returns>An <see cref="HtmlString"/> that represents HTML markup.</returns>
        HtmlString Raw(string value);

        /// <summary>
        /// Wraps HTML markup from the string representation of an object in an <see cref="HtmlString"/>,
        /// which will enable HTML markup to be rendered to the output without getting HTML encoded.
        /// </summary>
        /// <param name="value">object with string representation as HTML markup.</param>
        /// <returns>An <see cref="HtmlString"/> that represents HTML markup.</returns>
        HtmlString Raw(object value);

        /// <summary>
        /// Renders a partial view.
        /// </summary>
        /// <param name="partialViewName">The name of the partial view to render.</param>
        /// <param name="model">A model to pass into the partial view.</param>
        /// <param name="viewData">A <see cref="ViewDataDictionary"/> to pass into the partial view.</param>
        /// <returns>A task that represents when rendering has completed.</returns>
        Task RenderPartialAsync([NotNull] string partialViewName, object model, ViewDataDictionary viewData);

        /// <summary>
        /// Render an input element of type "text".
        /// </summary>
        /// <param name="name">
        /// Rendered element's name. Also use this name to find value in submitted data or view data. Use view data
        /// only if value is not in submitted data and <paramref name="value"/> is <c>null</c>.
        /// </param>
        /// <param name="value">
        /// If non-<c>null</c>, value to include in the element. Ignore if named value is found in submitted data.
        /// </param>
        /// <param name="format"></param>
        /// <param name="htmlAttributes">
        /// <see cref="IDictionary{string, object}"/> containing additional HTML attributes.
        /// </param>
        /// <returns>New <see cref="HtmlString"/> containing the rendered HTML.</returns>
        HtmlString TextBox(string name, object value, string format, IDictionary<string, object> htmlAttributes);

        /// <summary>
        /// Returns an unordered list (ul element) of validation messages that are in the
        /// <see cref="ModelStateDictionary"/> object.
        /// </summary>
        /// <param name="excludePropertyErrors">true to have the summary display model-level errors only, or false to
        /// have the summary display all errors.</param>
        /// <param name="message">The message to display with the validation summary.</param>
        /// <param name="htmlAttributes">A dictionary that contains the HTML attributes for the element.</param>
        /// <returns>An <see cref="HtmlString"/> that contains an unordered list (ul element) of validation messages.
        /// </returns>
        HtmlString ValidationSummary(
            bool excludePropertyErrors,
            string message,
            IDictionary<string, object> htmlAttributes);

        /// <summary>
        /// Returns the model value for the given expression <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Name of an expression, relative to the current model.</param>
        /// <param name="format">The optional format string to apply to the value.</param>
        /// <returns>An <see cref="HtmlString"/> that represents HTML markup.</returns>
        HtmlString Value([NotNull] string name, string format);
    }
}
