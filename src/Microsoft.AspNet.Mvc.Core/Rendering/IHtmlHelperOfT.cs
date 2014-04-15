using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.AspNet.Mvc.Rendering
{
    /// <summary>
    /// An <see cref="IHtmlHelper"/> for Linq expressions.
    /// </summary>
    /// <typeparam name="TModel">The <see cref="Type"/> of the model.</typeparam>
    public interface IHtmlHelper<TModel> : IHtmlHelper
    {
        /// <summary>
        /// Gets the current view data.
        /// </summary>
        new ViewDataDictionary<TModel> ViewData { get; }

        /// <summary>
        /// Render an input element of type "checkbox" with value "true" and an input element of type "hidden" with
        /// value "false".
        /// </summary>
        /// <param name="expression">
        /// An expression that identifies the object that contains the properties to render.
        /// </param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes to set for the element.
        /// Alternatively, an <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>New <see cref="HtmlString"/> containing the rendered HTML.</returns>
        HtmlString CheckBoxFor([NotNull] Expression<Func<TModel, bool>> expression, object htmlAttributes);

        /// <summary>
        /// Returns HTML markup for each property in the object that is represented by the specified expression, using
        /// the template, an HTML field ID, and additional view data.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
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
        HtmlString DisplayFor<TValue>([NotNull] Expression<Func<TModel, TValue>> expression,
                                      string templateName,
                                      string htmlFieldName,
                                      object additionalViewData);

        /// <summary>
        /// Gets the display name for the model.
        /// </summary>
        /// <param name="expression">An expression that identifies the object that contains the display name.</param>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <returns>
        /// The display name for the model.
        /// </returns>
        HtmlString DisplayNameFor<TValue>([NotNull] Expression<Func<TModel, TValue>> expression);

        /// <summary>
        /// Gets the display name for the inner model if the current model represents a collection.
        /// </summary>
        /// <typeparam name="TInnerModel">The type of the inner model</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="expression">An expression that identifies the object that contains the display name.</param>
        /// <returns>The display name for the inner model.</returns>
        HtmlString DisplayNameForInnerType<TInnerModel, TValue>(
            [NotNull] Expression<Func<TInnerModel, TValue>> expression);

        /// <summary>
        /// Returns the HtmlString corresponding to the expression specified.
        /// </summary>
        /// <param name="expression">
        /// The expression identifies the object for which the HtmlString should be returned.
        /// </param>
        /// <returns>
        /// New <see cref="HtmlString"/> containing the display text. If the value is null,
        /// then it returns the ModelMetadata.NullDisplayText.
        /// </returns>
        HtmlString DisplayTextFor<TValue>([NotNull] Expression<Func<TModel, TValue>> expression);

        /// <summary>
        /// Returns a single-selection HTML {select} element for the object that is represented
        /// by the specified expression using the specified list items, option label, and HTML attributes.
        /// </summary>
        /// <typeparam name="TProperty">The type of the value.</typeparam>
        /// <param name="expression">An expression that identifies the value to display.</param>
        /// <param name="selectList">A collection of <see href="SelectListItem"/> objects that are used to populate the
        /// drop-down list.</param>
        /// <param name="optionLabel">The text for a default empty item. This parameter can be null.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes to set for the {select} element.
        /// Alternatively, an <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>An HTML {select} element with an {option} subelement for each item in the list.</returns>
        HtmlString DropDownListFor<TProperty>(
            [NotNull] Expression<Func<TModel, TProperty>> expression,
            IEnumerable<SelectListItem> selectList,
            string optionLabel,
            object htmlAttributes);

        /// <summary>
        /// Returns an HTML input element for each property in the object that is represented by the specified
        /// expression, using the specified template, an HTML field ID, and additional view data.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
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
        HtmlString EditorFor<TValue>([NotNull] Expression<Func<TModel, TValue>> expression,
            string templateName,
            string htmlFieldName,
            object additionalViewData);

        /// <summary>
        /// Render an input element of type "hidden".
        /// </summary>
        /// <param name="expression">
        /// An expression that identifies the object that contains the properties to render.
        /// </param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes to set for the element.
        /// Alternatively, an <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>New <see cref="HtmlString"/> containing the rendered HTML.</returns>
        HtmlString HiddenFor<TProperty>([NotNull] Expression<Func<TModel, TProperty>> expression,
            object htmlAttributes);

        /// <summary>
        /// Gets the Id of the given expression.
        /// </summary>
        /// <param name="expression">The expression identifies the object for which the Id should be returned.</param>
        /// <returns>New <see cref="HtmlString"/> containing the Id.</returns>
        HtmlString IdFor<TProperty>([NotNull] Expression<Func<TModel, TProperty>> expression);

        /// <summary>
        /// Returns an HTML label element and the property name of the property that is represented by the specified
        /// expression.
        /// </summary>
        /// <param name="expression">An expression that identifies the property to display.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes to set for the element.</param>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <returns>
        /// An HTML label element and the property name of the property that is represented by the expression.
        /// </returns>
        HtmlString LabelFor<TValue>([NotNull] Expression<Func<TModel, TValue>> expression,
                                    string labelText,
                                    object htmlAttributes);

        /// <summary>
        /// Gets the full HTML field name for the given <paramref name="expression"/>.
        /// </summary>
        /// <typeparam name="TProperty">The <see cref="Type"/> the <paramref name="expression"/> returns.</typeparam>
        /// <param name="expression">An expression, relative to the current model.</param>
        /// <returns>An <see cref="HtmlString"/> that represents HTML markup.</returns>
        HtmlString NameFor<TProperty>([NotNull] Expression<Func<TModel, TProperty>> expression);

        /// <summary>
        /// Render an input element of type "password".
        /// </summary>
        /// <param name="expression">
        /// An expression that identifies the object that contains the properties to render.
        /// </param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes to set for the element.
        /// Alternatively, an <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>New <see cref="HtmlString"/> containing the rendered HTML.</returns>
        HtmlString PasswordFor<TProperty>([NotNull] Expression<Func<TModel, TProperty>> expression,
            object htmlAttributes);

        /// <summary>
        /// Render an input element of type "radio".
        /// </summary>
        /// <param name="expression">
        /// An expression that identifies the object that contains the properties to render.
        /// </param>
        /// <param name="value">
        /// If non-<c>null</c>, value to compare with current expression value to determine whether radio button is
        /// checked.
        /// </param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes to set for the element.
        /// Alternatively, an <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>New <see cref="HtmlString"/> containing the rendered HTML.</returns>
        HtmlString RadioButtonFor<TProperty>([NotNull] Expression<Func<TModel, TProperty>> expression, object value,
            object htmlAttributes);

        /// <summary>
        /// Render a textarea.
        /// </summary>
        /// <param name="expression">An expression, relative to the current model.</param>
        /// <param name="rows">Number of rows in the textarea.</param>
        /// <param name="columns">Number of columns in the textarea.</param>
        /// <param name="htmlAttributes">
        /// <see cref="IDictionary{string, object}"/> containing additional HTML attributes.
        /// </param>
        /// <returns>New <see cref="HtmlString"/> containing the rendered HTML.</returns>
        HtmlString TextAreaFor<TProperty>([NotNull] Expression<Func<TModel, TProperty>> expression,
            int rows, int columns, object htmlAttributes);

        /// <summary>
        /// Render an input element of type "text".
        /// </summary>
        /// <param name="expression">
        /// An expression that identifies the object that contains the properties to render.
        /// </param>
        /// <param name="format"></param>
        /// <param name="htmlAttributes">
        /// <see cref="IDictionary{string, object}"/> containing additional HTML attributes.
        /// </param>
        /// <returns>New <see cref="HtmlString"/> containing the rendered HTML.</returns>
        HtmlString TextBoxFor<TProperty>([NotNull] Expression<Func<TModel, TProperty>> expression, string format,
            IDictionary<string, object> htmlAttributes);

        /// <summary>
        /// Returns the validation message if an error exists in the <see cref="ModelStateDictionary"/> object.
        /// </summary>
        /// <param name="modelName">The name of the property that is being validated.</param>
        /// <param name="message">The message to be displayed if the specified field contains an error.</param>
        /// <param name="htmlAttributes">Dictionary that contains the HTML attributes which should
        /// be applied on the element</param>
        /// <returns></returns>
        HtmlString ValidationMessage(string modelName, string message, object htmlAttributes);

        /// <summary>
        /// Returns the model value for the given expression <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">An expression, relative to the current model.</param>
        /// <param name="format">The optional format string to apply to the value.</param>
        /// <returns>An <see cref="HtmlString"/> that represents HTML markup.</returns>
        HtmlString ValueFor<TProperty>([NotNull] Expression<Func<TModel, TProperty>> expression, string format);
    }
}
