// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Rendering
{
    /// <summary>
    /// Display-related extensions for <see cref="IHtmlHelper"/> and <see cref="IHtmlHelper{TModel}"/>.
    /// </summary>
    public static class HtmlHelperDisplayExtensions
    {
        /// <summary>
        /// Returns HTML markup for the <paramref name="expression"/>, using a display template. The template is found
        /// using the <paramref name="expression"/>'s <see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">
        /// Expression name, relative to the current model. May identify a single property or an
        /// <see cref="object"/> that contains the properties to display.
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
        /// <para>
        /// Custom templates are found under a <c>DisplayTemplates</c> folder. The folder name is case-sensitive on
        /// case-sensitive file systems.
        /// </para>
        /// </remarks>
        public static HtmlString Display([NotNull] this IHtmlHelper htmlHelper, string expression)
        {
            return htmlHelper.Display(expression, templateName: null, htmlFieldName: null, additionalViewData: null);
        }

        /// <summary>
        /// Returns HTML markup for the <paramref name="expression"/>, using a display template and specified
        /// additional view data. The template is found using the <paramref name="expression"/>'s
        /// <see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">
        /// Expression name, relative to the current model. May identify a single property or an
        /// <see cref="object"/> that contains the properties to display.
        /// </param>
        /// <param name="additionalViewData">
        /// An anonymous <see cref="object"/> or <see cref="System.Collections.Generic.IDictionary{string, object}"/>
        /// that can contain additional view data that will be merged into the <see cref="ViewDataDictionary{TModel}"/>
        /// instance created for the template.
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
        /// <para>
        /// Custom templates are found under a <c>DisplayTemplates</c> folder. The folder name is case-sensitive on
        /// case-sensitive file systems.
        /// </para>
        /// </remarks>
        public static HtmlString Display(
            [NotNull] this IHtmlHelper htmlHelper,
            string expression,
            object additionalViewData)
        {
            return htmlHelper.Display(
                expression,
                templateName: null,
                htmlFieldName: null,
                additionalViewData: additionalViewData);
        }

        /// <summary>
        /// Returns HTML markup for the <paramref name="expression"/>, using a display template. The template is found
        /// using the <paramref name="templateName"/> or the <paramref name="expression"/>'s
        /// <see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">
        /// Expression name, relative to the current model. May identify a single property or an
        /// <see cref="object"/> that contains the properties to display.
        /// </param>
        /// <param name="templateName">The name of the template used to create the HTML markup.</param>
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
        /// <para>
        /// Custom templates are found under a <c>DisplayTemplates</c> folder. The folder name is case-sensitive on
        /// case-sensitive file systems.
        /// </para>
        /// </remarks>
        public static HtmlString Display(
            [NotNull] this IHtmlHelper htmlHelper,
            string expression,
            string templateName)
        {
            return htmlHelper.Display(expression, templateName, htmlFieldName: null, additionalViewData: null);
        }

        /// <summary>
        /// Returns HTML markup for the <paramref name="expression"/>, using a display template and specified
        /// additional view data. The template is found using the <paramref name="templateName"/> or the
        /// <paramref name="expression"/>'s <see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">
        /// Expression name, relative to the current model. May identify a single property or an
        /// <see cref="object"/> that contains the properties to display.
        /// </param>
        /// <param name="templateName">The name of the template used to create the HTML markup.</param>
        /// <param name="additionalViewData">
        /// An anonymous <see cref="object"/> or <see cref="System.Collections.Generic.IDictionary{string, object}"/>
        /// that can contain additional view data that will be merged into the <see cref="ViewDataDictionary{TModel}"/>
        /// instance created for the template.
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
        /// <para>
        /// Custom templates are found under a <c>DisplayTemplates</c> folder. The folder name is case-sensitive on
        /// case-sensitive file systems.
        /// </para>
        /// </remarks>
        public static HtmlString Display(
            [NotNull] this IHtmlHelper htmlHelper,
            string expression,
            string templateName,
            object additionalViewData)
        {
            return htmlHelper.Display(
                expression,
                templateName,
                htmlFieldName: null,
                additionalViewData: additionalViewData);
        }

        /// <summary>
        /// Returns HTML markup for the <paramref name="expression"/>, using a display template and specified HTML
        /// field name. The template is found using the <paramref name="templateName"/> or the
        /// <paramref name="expression"/>'s<see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">
        /// Expression name, relative to the current model. May identify a single property or an
        /// <see cref="object"/> that contains the properties to display.
        /// </param>
        /// <param name="templateName">The name of the template used to create the HTML markup.</param>
        /// <param name="htmlFieldName">
        /// A <see cref="string"/> used to disambiguate the names of HTML elements that are created for
        /// properties that have the same name.
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
        /// <para>
        /// Custom templates are found under a <c>DisplayTemplates</c> folder. The folder name is case-sensitive on
        /// case-sensitive file systems.
        /// </para>
        /// </remarks>
        public static HtmlString Display(
            [NotNull] this IHtmlHelper htmlHelper,
            string expression,
            string templateName,
            string htmlFieldName)
        {
            return htmlHelper.Display(expression, templateName, htmlFieldName, additionalViewData: null);
        }

        /// <summary>
        /// Returns HTML markup for the <paramref name="expression"/>, using a display template. The template is found
        /// using the <paramref name="expression"/>'s <see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="HtmlString"/> containing the created HTML.</returns>
        /// <remarks>
        /// <para>
        /// For example the default <see cref="object"/> display template includes markup for each property in the
        /// <paramref name="expression"/> result.
        /// </para>
        /// <para>
        /// Custom templates are found under a <c>DisplayTemplates</c> folder. The folder name is case-sensitive on
        /// case-sensitive file systems.
        /// </para>
        /// </remarks>
        public static HtmlString DisplayFor<TModel, TResult>(
            [NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TResult>> expression)
        {
            return htmlHelper.DisplayFor<TResult>(
                expression,
                templateName: null,
                htmlFieldName: null,
                additionalViewData: null);
        }

        /// <summary>
        /// Returns HTML markup for the <paramref name="expression"/>, using a display template and specified
        /// additional view data. The template is found using the <paramref name="expression"/>'s
        /// <see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="additionalViewData">
        /// An anonymous <see cref="object"/> or <see cref="System.Collections.Generic.IDictionary{string, object}"/>
        /// that can contain additional view data that will be merged into the <see cref="ViewDataDictionary{TModel}"/>
        /// instance created for the template.
        /// </param>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="HtmlString"/> containing the created HTML.</returns>
        /// <remarks>
        /// <para>
        /// For example the default <see cref="object"/> display template includes markup for each property in the
        /// <paramref name="expression"/> result.
        /// </para>
        /// <para>
        /// Custom templates are found under a <c>DisplayTemplates</c> folder. The folder name is case-sensitive on
        /// case-sensitive file systems.
        /// </para>
        /// </remarks>
        public static HtmlString DisplayFor<TModel, TResult>(
            [NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TResult>> expression,
            object additionalViewData)
        {
            return htmlHelper.DisplayFor<TResult>(
                expression,
                templateName: null,
                htmlFieldName: null,
                additionalViewData: additionalViewData);
        }

        /// <summary>
        /// Returns HTML markup for the <paramref name="expression"/>, using a display template. The template is found
        /// using the <paramref name="templateName"/> or the <paramref name="expression"/>'s
        /// <see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="templateName">The name of the template used to create the HTML markup.</param>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="HtmlString"/> containing the created HTML.</returns>
        /// <remarks>
        /// <para>
        /// For example the default <see cref="object"/> display template includes markup for each property in the
        /// <paramref name="expression"/> result.
        /// </para>
        /// <para>
        /// Custom templates are found under a <c>DisplayTemplates</c> folder. The folder name is case-sensitive on
        /// case-sensitive file systems.
        /// </para>
        /// </remarks>
        public static HtmlString DisplayFor<TModel, TResult>(
            [NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TResult>> expression,
            string templateName)
        {
            return htmlHelper.DisplayFor<TResult>(
                expression,
                templateName,
                htmlFieldName: null,
                additionalViewData: null);
        }

        /// <summary>
        /// Returns HTML markup for the <paramref name="expression"/>, using a display template and specified
        /// additional view data. The template is found using the <paramref name="templateName"/> or the
        /// <paramref name="expression"/>'s <see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="templateName">The name of the template used to create the HTML markup.</param>
        /// <param name="additionalViewData">
        /// An anonymous <see cref="object"/> or <see cref="System.Collections.Generic.IDictionary{string, object}"/>
        /// that can contain additional view data that will be merged into the <see cref="ViewDataDictionary{TModel}"/>
        /// instance created for the template.
        /// </param>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="HtmlString"/> containing the created HTML.</returns>
        /// <remarks>
        /// <para>
        /// For example the default <see cref="object"/> display template includes markup for each property in the
        /// <paramref name="expression"/> result.
        /// </para>
        /// <para>
        /// Custom templates are found under a <c>DisplayTemplates</c> folder. The folder name is case-sensitive on
        /// case-sensitive file systems.
        /// </para>
        /// </remarks>
        public static HtmlString DisplayFor<TModel, TResult>(
            [NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TResult>> expression,
            string templateName,
            object additionalViewData)
        {
            return htmlHelper.DisplayFor<TResult>(
                expression,
                templateName,
                htmlFieldName: null,
                additionalViewData: additionalViewData);
        }

        /// <summary>
        /// Returns HTML markup for the <paramref name="expression"/>, using a display template and specified HTML
        /// field name. The template is found using the <paramref name="templateName"/> or the
        /// <paramref name="expression"/>'s <see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="templateName">The name of the template used to create the HTML markup.</param>
        /// <param name="htmlFieldName">
        /// A <see cref="string"/> used to disambiguate the names of HTML elements that are created for properties
        /// that have the same name.
        /// </param>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
        /// <returns>A new <see cref="HtmlString"/> containing the created HTML.</returns>
        /// <remarks>
        /// <para>
        /// For example the default <see cref="object"/> display template includes markup for each property in the
        /// <paramref name="expression"/> result.
        /// </para>
        /// <para>
        /// Custom templates are found under a <c>DisplayTemplates</c> folder. The folder name is case-sensitive on
        /// case-sensitive file systems.
        /// </para>
        /// </remarks>
        public static HtmlString DisplayFor<TModel, TResult>(
            [NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TResult>> expression,
            string templateName,
            string htmlFieldName)
        {
            return htmlHelper.DisplayFor<TResult>(
                expression,
                templateName: templateName,
                htmlFieldName: htmlFieldName,
                additionalViewData: null);
        }

        /// <summary>
        /// Returns HTML markup for the current model, using a display template. The template is found using the
        /// model's <see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <returns>A new <see cref="HtmlString"/> containing the created HTML.</returns>
        /// <remarks>
        /// <para>
        /// For example the default <see cref="object"/> display template includes markup for each property in the
        /// current model.
        /// </para>
        /// <para>
        /// Custom templates are found under a <c>DisplayTemplates</c> folder. The folder name is case-sensitive on
        /// case-sensitive file systems.
        /// </para>
        /// </remarks>
        public static HtmlString DisplayForModel([NotNull] this IHtmlHelper htmlHelper)
        {
            return htmlHelper.Display(
                expression: null,
                templateName: null,
                htmlFieldName: null,
                additionalViewData: null);
        }

        /// <summary>
        /// Returns HTML markup for the current model, using a display template and specified additional view data. The
        /// template is found using the model's <see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="additionalViewData">
        /// An anonymous <see cref="object"/> or <see cref="System.Collections.Generic.IDictionary{string, object}"/>
        /// that can contain additional view data that will be merged into the <see cref="ViewDataDictionary{TModel}"/>
        /// instance created for the template.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the created HTML.</returns>
        /// <remarks>
        /// <para>
        /// For example the default <see cref="object"/> display template includes markup for each property in the
        /// current model.
        /// </para>
        /// <para>
        /// Custom templates are found under a <c>DisplayTemplates</c> folder. The folder name is case-sensitive on
        /// case-sensitive file systems.
        /// </para>
        /// </remarks>
        public static HtmlString DisplayForModel([NotNull] this IHtmlHelper htmlHelper, object additionalViewData)
        {
            return htmlHelper.Display(
                expression: null,
                templateName: null,
                htmlFieldName: null,
                additionalViewData: additionalViewData);
        }

        /// <summary>
        /// Returns HTML markup for the current model, using a display template. The template is found using the
        /// <paramref name="templateName"/> or the model's <see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="templateName">The name of the template used to create the HTML markup.</param>
        /// <returns>A new <see cref="HtmlString"/> containing the created HTML.</returns>
        /// <remarks>
        /// <para>
        /// For example the default <see cref="object"/> display template includes markup for each property in the
        /// current model.
        /// </para>
        /// <para>
        /// Custom templates are found under a <c>DisplayTemplates</c> folder. The folder name is case-sensitive on
        /// case-sensitive file systems.
        /// </para>
        /// </remarks>
        public static HtmlString DisplayForModel([NotNull] this IHtmlHelper htmlHelper, string templateName)
        {
            return htmlHelper.Display(
                expression: null,
                templateName: templateName,
                htmlFieldName: null,
                additionalViewData: null);
        }

        /// <summary>
        /// Returns HTML markup for the current model, using a display template and specified additional view data. The
        /// template is found using the <paramref name="templateName"/> or the model's
        /// <see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="templateName">The name of the template used to create the HTML markup.</param>
        /// <param name="additionalViewData">
        /// An anonymous <see cref="object"/> or <see cref="System.Collections.Generic.IDictionary{string, object}"/>
        /// that can contain additional view data that will be merged into the <see cref="ViewDataDictionary{TModel}"/>
        /// instance created for the template.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the created HTML.</returns>
        /// <remarks>
        /// <para>
        /// For example the default <see cref="object"/> display template includes markup for each property in the
        /// current model.
        /// </para>
        /// <para>
        /// Custom templates are found under a <c>DisplayTemplates</c> folder. The folder name is case-sensitive on
        /// case-sensitive file systems.
        /// </para>
        /// </remarks>
        public static HtmlString DisplayForModel(
            [NotNull] this IHtmlHelper htmlHelper,
            string templateName,
            object additionalViewData)
        {
            return htmlHelper.Display(
                expression: null,
                templateName: templateName,
                htmlFieldName: null,
                additionalViewData: additionalViewData);
        }

        /// <summary>
        /// Returns HTML markup for the current model, using a display template and specified HTML field name. The
        /// template is found using the <paramref name="templateName"/> or the model's
        /// <see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="templateName">The name of the template used to create the HTML markup.</param>
        /// <param name="htmlFieldName">
        /// A <see cref="string"/> used to disambiguate the names of HTML elements that are created for
        /// properties that have the same name.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the created HTML.</returns>
        /// <remarks>
        /// <para>
        /// For example the default <see cref="object"/> display template includes markup for each property in the
        /// current model.
        /// </para>
        /// <para>
        /// Custom templates are found under a <c>DisplayTemplates</c> folder. The folder name is case-sensitive on
        /// case-sensitive file systems.
        /// </para>
        /// </remarks>
        public static HtmlString DisplayForModel(
            [NotNull] this IHtmlHelper htmlHelper,
            string templateName,
            string htmlFieldName)
        {
            return htmlHelper.Display(
                expression: null,
                templateName: templateName,
                htmlFieldName: htmlFieldName,
                additionalViewData: null);
        }

        /// <summary>
        /// Returns HTML markup for the current model, using a display template, specified HTML field name, and
        /// additional view data. The template is found using the <paramref name="templateName"/> or the model's
        /// <see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="templateName">The name of the template used to create the HTML markup.</param>
        /// <param name="htmlFieldName">
        /// A <see cref="string"/> used to disambiguate the names of HTML elements that are created for
        /// properties that have the same name.
        /// </param>
        /// <param name="additionalViewData">
        /// An anonymous <see cref="object"/> or <see cref="System.Collections.Generic.IDictionary{string, object}"/>
        /// that can contain additional view data that will be merged into the <see cref="ViewDataDictionary{TModel}"/>
        /// instance created for the template.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the created HTML.</returns>
        /// <remarks>
        /// <para>
        /// For example the default <see cref="object"/> display template includes markup for each property in the
        /// current model.
        /// </para>
        /// <para>
        /// Custom templates are found under a <c>DisplayTemplates</c> folder. The folder name is case-sensitive on
        /// case-sensitive file systems.
        /// </para>
        /// </remarks>
        public static HtmlString DisplayForModel(
            [NotNull] this IHtmlHelper htmlHelper,
            string templateName,
            string htmlFieldName,
            object additionalViewData)
        {
            return htmlHelper.Display(
                expression: null,
                templateName: templateName,
                htmlFieldName: htmlFieldName,
                additionalViewData: additionalViewData);
        }
    }
}
