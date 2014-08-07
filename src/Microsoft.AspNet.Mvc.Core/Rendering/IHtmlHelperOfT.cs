// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.AspNet.Mvc.Rendering
{
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
        /// Returns the display name for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">The expression to be evaluated against the current model.</param>
        /// <typeparam name="TValue">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A <see cref="String"/> containing the display name.</returns>
        string DisplayNameFor<TValue>([NotNull] Expression<Func<TModel, TValue>> expression);

        /// <summary>
        /// Returns the display name for the specified <paramref name="expression"/>
        /// if the current model represents a collection.
        /// </summary>
        /// <param name="expression">The expression to be evaluated against an item in the current model.</param>
        /// <typeparam name="TModelItem">The type of items in the model collection.</typeparam>
        /// <typeparam name="TValue">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A <see cref="String"/> containing the display name.</returns>
        string DisplayNameForInnerType<TModelItem, TValue>(
            [NotNull] Expression<Func<TModelItem, TValue>> expression);

        /// <summary>
        /// Returns the simple display text for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">The expression to be evaluated against the current model.</param>
        /// <typeparam name="TValue">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>
        /// A <see cref="String"/> containing the simple display text.
        /// If the <paramref name="expression"/> result is <c>null</c>, returns
        /// <see cref="ModelBinding.ModelMetadata.NullDisplayText"/>.
        /// </returns>
        string DisplayTextFor<TValue>([NotNull] Expression<Func<TModel, TValue>> expression);

        /// <summary>
        /// Returns a single-selection HTML &lt;select&gt; element for the object that is represented
        /// by the specified expression using the specified list items, option label, and HTML attributes.
        /// </summary>
        /// <typeparam name="TProperty">The type of the value.</typeparam>
        /// <param name="expression">An expression that identifies the value to display.</param>
        /// <param name="selectList">A collection of <see cref="SelectListItem"/> objects that are used to populate the
        /// drop-down list.</param>
        /// <param name="optionLabel">The text for a default empty item. This parameter can be null.</param>
        /// <param name="htmlAttributes">
        /// An object that contains the HTML attributes to set for the &lt;select&gt; element. Alternatively, an
        /// <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>
        /// An HTML &lt;select&gt; element with an &lt;option&gt; subelement for each item in the list.
        /// </returns>
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
        /// Returns the HTML element Id for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">The expression to be evaluated against the current model.</param>
        /// <typeparam name="TProperty">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A <see cref="String"/> containing the element Id.</returns>
        string IdFor<TProperty>([NotNull] Expression<Func<TModel, TProperty>> expression);

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
        /// Returns a multi-selection HTML &lt;select&gt; element for the object that is represented by the specified
        /// expression using the specified list items and HTML attributes.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <param name="expression">An expression that identifies the object that contains the properties to
        /// display.</param>
        /// <param name="selectList">A collection of <see cref="SelectListItem"/> objects that are used to populate the
        /// drop-down list.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes to set for the element.
        /// Alternatively, an <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>
        /// An HTML &lt;select&gt; element with an &lt;option&gt; subelement for each item in the list.
        /// </returns>
        HtmlString ListBoxFor<TProperty>(
            [NotNull] Expression<Func<TModel, TProperty>> expression,
            IEnumerable<SelectListItem> selectList,
            object htmlAttributes);

        /// <summary>
        /// Returns the full HTML element name for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">The expression to be evaluated against the current model.</param>
        /// <typeparam name="TProperty">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A <see cref="String"/> containing the element name.</returns>
        string NameFor<TProperty>([NotNull] Expression<Func<TModel, TProperty>> expression);

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
        /// Value to compare with current expression value to determine whether radio button is checked.
        /// </param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes to set for the element.
        /// Alternatively, an <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>New <see cref="HtmlString"/> containing the rendered HTML.</returns>
        HtmlString RadioButtonFor<TProperty>(
            [NotNull] Expression<Func<TModel, TProperty>> expression,
            [NotNull] object value,
            object htmlAttributes);

        /// <summary>
        /// Render a textarea.
        /// </summary>
        /// <param name="expression">An expression, relative to the current model.</param>
        /// <param name="rows">Number of rows in the textarea.</param>
        /// <param name="columns">Number of columns in the textarea.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes to set for the element.
        /// Alternatively, an <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
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
        /// <param name="htmlAttributes">An object that contains the HTML attributes to set for the element.
        /// Alternatively, an <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <returns>New <see cref="HtmlString"/> containing the rendered HTML.</returns>
        HtmlString TextBoxFor<TProperty>([NotNull] Expression<Func<TModel, TProperty>> expression, string format,
            object htmlAttributes);

        /// <summary>
        /// Returns the validation message for the specified expression
        /// </summary>
        /// <param name="expression">An expression, relative to the current model.</param>
        /// <param name="message">The message to be displayed. This will always be visible but client-side
        /// validation may update the associated CSS class.</param>
        /// <param name="htmlAttributes"> An object that contains the HTML attributes to set for the element.
        /// Alternatively, an <see cref="IDictionary{string, object}"/> instance containing the HTML attributes.
        /// </param>
        /// <param name="tag">The tag to wrap the <paramref name="message"/> in the generated HTML.
        /// Its default value is <see cref="ViewContext.ValidationMessageElement" />.</param>
        /// <returns>An <see cref="HtmlString"/> that contains the validation message</returns>
        HtmlString ValidationMessageFor<TProperty>([NotNull] Expression<Func<TModel, TProperty>> expression,
            string message,
            object htmlAttributes,
            string tag);

        /// <summary>
        /// Returns the formatted value for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">The expression to be evaluated against the current model.</param>
        /// <param name="format">
        /// The composite format <see cref="String"/> (see http://msdn.microsoft.com/en-us/library/txafckwd.aspx).
        /// </param>
        /// <typeparam name="TProperty">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A <see cref="String"/> containing the formatted value.</returns>
        /// <remarks>
        /// Converts the <paramref name="expression"/> result to a <see cref="String"/> directly if
        /// <paramref name="format"/> is <c>null</c> or empty.
        /// </remarks>
        string ValueFor<TProperty>([NotNull] Expression<Func<TModel, TProperty>> expression, string format);
    }
}
