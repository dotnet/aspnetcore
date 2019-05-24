// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    /// <summary>
    /// Editor-related extensions for <see cref="IHtmlHelper"/> and <see cref="IHtmlHelper{TModel}"/>.
    /// </summary>
    public static class HtmlHelperEditorExtensions
    {
        /// <summary>
        /// Returns HTML markup for the <paramref name="expression"/>, using an editor template. The template is found
        /// using the <paramref name="expression"/>'s <see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">
        /// Expression name, relative to the current model. May identify a single property or an
        /// <see cref="object"/> that contains the properties to edit.
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
        public static IHtmlContent Editor(this IHtmlHelper htmlHelper, string expression)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            return htmlHelper.Editor(expression, templateName: null, htmlFieldName: null, additionalViewData: null);
        }

        /// <summary>
        /// Returns HTML markup for the <paramref name="expression"/>, using an editor template and specified
        /// additional view data. The template is found using the <paramref name="expression"/>'s
        /// <see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">
        /// Expression name, relative to the current model. May identify a single property or an
        /// <see cref="object"/> that contains the properties to edit.
        /// </param>
        /// <param name="additionalViewData">
        /// An anonymous <see cref="object"/> or <see cref="System.Collections.Generic.IDictionary{String, Object}"/>
        /// that can contain additional view data that will be merged into the
        /// <see cref="ViewFeatures.ViewDataDictionary{TModel}"/> instance created for the template.
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
        public static IHtmlContent Editor(
            this IHtmlHelper htmlHelper,
            string expression,
            object additionalViewData)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            return htmlHelper.Editor(
                expression,
                templateName: null,
                htmlFieldName: null,
                additionalViewData: additionalViewData);
        }

        /// <summary>
        /// Returns HTML markup for the <paramref name="expression"/>, using an editor template. The template is found
        /// using the <paramref name="templateName"/> or the <paramref name="expression"/>'s
        /// <see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">
        /// Expression name, relative to the current model. May identify a single property or an
        /// <see cref="object"/> that contains the properties to edit.
        /// </param>
        /// <param name="templateName">The name of the template used to create the HTML markup.</param>
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
        public static IHtmlContent Editor(this IHtmlHelper htmlHelper, string expression, string templateName)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            return htmlHelper.Editor(expression, templateName, htmlFieldName: null, additionalViewData: null);
        }

        /// <summary>
        /// Returns HTML markup for the <paramref name="expression"/>, using an editor template and specified
        /// additional view data. The template is found using the <paramref name="templateName"/> or the
        /// <paramref name="expression"/>'s <see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">
        /// Expression name, relative to the current model. May identify a single property or an
        /// <see cref="object"/> that contains the properties to edit.
        /// </param>
        /// <param name="templateName">The name of the template used to create the HTML markup.</param>
        /// <param name="additionalViewData">
        /// An anonymous <see cref="object"/> or <see cref="System.Collections.Generic.IDictionary{String, Object}"/>
        /// that can contain additional view data that will be merged into the
        /// <see cref="ViewFeatures.ViewDataDictionary{TModel}"/> instance created for the template.
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
        public static IHtmlContent Editor(
            this IHtmlHelper htmlHelper,
            string expression,
            string templateName,
            object additionalViewData)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            return htmlHelper.Editor(
                expression,
                templateName,
                htmlFieldName: null,
                additionalViewData: additionalViewData);
        }

        /// <summary>
        /// Returns HTML markup for the <paramref name="expression"/>, using an editor template and specified HTML
        /// field name. The template is found using the <paramref name="templateName"/> or the
        /// <paramref name="expression"/>'s <see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="expression">
        /// Expression name, relative to the current model. May identify a single property or an
        /// <see cref="object"/> that contains the properties to edit.
        /// </param>
        /// <param name="templateName">The name of the template used to create the HTML markup.</param>
        /// <param name="htmlFieldName">
        /// A <see cref="string"/> used to disambiguate the names of HTML elements that are created for
        /// properties that have the same name.
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
        public static IHtmlContent Editor(
            this IHtmlHelper htmlHelper,
            string expression,
            string templateName,
            string htmlFieldName)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            return htmlHelper.Editor(expression, templateName, htmlFieldName, additionalViewData: null);
        }

        /// <summary>
        /// Returns HTML markup for the <paramref name="expression"/>, using an editor template. The template is found
        /// using the <paramref name="expression"/>'s <see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <typeparam name="TModel">The type of the model.</typeparam>
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
        public static IHtmlContent EditorFor<TModel, TResult>(
            this IHtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TResult>> expression)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            return htmlHelper.EditorFor(expression, templateName: null, htmlFieldName: null, additionalViewData: null);
        }

        /// <summary>
        /// Returns HTML markup for the <paramref name="expression"/>, using an editor template and specified
        /// additional view data. The template is found using the <paramref name="expression"/>'s
        /// <see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="additionalViewData">
        /// An anonymous <see cref="object"/> or <see cref="System.Collections.Generic.IDictionary{String, Object}"/>
        /// that can contain additional view data that will be merged into the
        /// <see cref="ViewFeatures.ViewDataDictionary{TModel}"/> instance created for the template.
        /// </param>
        /// <typeparam name="TModel">The type of the model.</typeparam>
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
        public static IHtmlContent EditorFor<TModel, TResult>(
            this IHtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TResult>> expression,
            object additionalViewData)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            return htmlHelper.EditorFor(
                expression,
                templateName: null,
                htmlFieldName: null,
                additionalViewData: additionalViewData);
        }

        /// <summary>
        /// Returns HTML markup for the <paramref name="expression"/>, using an editor template. The template is found
        /// using the <paramref name="templateName"/> or the <paramref name="expression"/>'s
        /// <see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="templateName">The name of the template that is used to create the HTML markup.</param>
        /// <typeparam name="TModel">The type of the model.</typeparam>
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
        public static IHtmlContent EditorFor<TModel, TResult>(
            this IHtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TResult>> expression,
            string templateName)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            return htmlHelper.EditorFor(expression, templateName, htmlFieldName: null, additionalViewData: null);
        }

        /// <summary>
        /// Returns HTML markup for the <paramref name="expression"/>, using an editor template and specified
        /// additional view data. The template is found using the <paramref name="templateName"/> or the
        /// <paramref name="expression"/>'s <see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="templateName">The name of the template that is used to create the HTML markup.</param>
        /// <param name="additionalViewData">
        /// An anonymous <see cref="object"/> or <see cref="System.Collections.Generic.IDictionary{String, Object}"/>
        /// that can contain additional view data that will be merged into the
        /// <see cref="ViewFeatures.ViewDataDictionary{TModel}"/> instance created for the template.
        /// </param>
        /// <typeparam name="TModel">The type of the model.</typeparam>
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
        public static IHtmlContent EditorFor<TModel, TResult>(
            this IHtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TResult>> expression,
            string templateName,
            object additionalViewData)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            return htmlHelper.EditorFor(
                expression,
                templateName,
                htmlFieldName: null,
                additionalViewData: additionalViewData);
        }

        /// <summary>
        /// Returns HTML markup for the <paramref name="expression"/>, using an editor template and specified HTML
        /// field name. The template is found using the <paramref name="templateName"/> or the
        /// <paramref name="expression"/>'s <see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper{TModel}"/> instance this method extends.</param>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="templateName">The name of the template that is used to create the HTML markup.</param>
        /// <param name="htmlFieldName">
        /// A <see cref="string"/> used to disambiguate the names of HTML elements that are created for properties
        /// that have the same name.
        /// </param>
        /// <typeparam name="TModel">The type of the model.</typeparam>
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
        public static IHtmlContent EditorFor<TModel, TResult>(
            this IHtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TResult>> expression,
            string templateName,
            string htmlFieldName)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            return htmlHelper.EditorFor(expression, templateName, htmlFieldName, additionalViewData: null);
        }

        /// <summary>
        /// Returns HTML markup for the current model, using an editor template. The template is found using the
        /// model's <see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; element(s).</returns>
        /// <remarks>
        /// <para>
        /// For example the default <see cref="object"/> editor template includes &lt;label&gt; and &lt;input&gt;
        /// elements for each property in the current model.
        /// </para>
        /// <para>
        /// Custom templates are found under a <c>EditorTemplates</c> folder. The folder name is case-sensitive on
        /// case-sensitive file systems.
        /// </para>
        /// </remarks>
        public static IHtmlContent EditorForModel(this IHtmlHelper htmlHelper)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            return htmlHelper.Editor(
                expression: null,
                templateName: null,
                htmlFieldName: null,
                additionalViewData: null);
        }

        /// <summary>
        /// Returns HTML markup for the current model, using an editor template and specified additional view data. The
        /// template is found using the model's <see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="additionalViewData">
        /// An anonymous <see cref="object"/> or <see cref="System.Collections.Generic.IDictionary{String, Object}"/>
        /// that can contain additional view data that will be merged into the
        /// <see cref="ViewFeatures.ViewDataDictionary{TModel}"/> instance created for the template.
        /// </param>
        /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; element(s).</returns>
        /// <remarks>
        /// <para>
        /// For example the default <see cref="object"/> editor template includes &lt;label&gt; and &lt;input&gt;
        /// elements for each property in the current model.
        /// </para>
        /// <para>
        /// Custom templates are found under a <c>EditorTemplates</c> folder. The folder name is case-sensitive on
        /// case-sensitive file systems.
        /// </para>
        /// </remarks>
        public static IHtmlContent EditorForModel(this IHtmlHelper htmlHelper, object additionalViewData)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            return htmlHelper.Editor(
                expression: null,
                templateName: null,
                htmlFieldName: null,
                additionalViewData: additionalViewData);
        }

        /// <summary>
        /// Returns HTML markup for the current model, using an editor template. The template is found using the
        /// <paramref name="templateName"/> or the model's <see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="templateName">The name of the template used to create the HTML markup.</param>
        /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; element(s).</returns>
        /// <remarks>
        /// <para>
        /// For example the default <see cref="object"/> editor template includes &lt;label&gt; and &lt;input&gt;
        /// elements for each property in the current model.
        /// </para>
        /// <para>
        /// Custom templates are found under a <c>EditorTemplates</c> folder. The folder name is case-sensitive on
        /// case-sensitive file systems.
        /// </para>
        /// </remarks>
        public static IHtmlContent EditorForModel(this IHtmlHelper htmlHelper, string templateName)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            return htmlHelper.Editor(
                expression: null,
                templateName: templateName,
                htmlFieldName: null,
                additionalViewData: null);
        }

        /// <summary>
        /// Returns HTML markup for the current model, using an editor template and specified additional view data. The
        /// template is found using the <paramref name="templateName"/> or the model's
        /// <see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="templateName">The name of the template used to create the HTML markup.</param>
        /// <param name="additionalViewData">
        /// An anonymous <see cref="object"/> or <see cref="System.Collections.Generic.IDictionary{String, Object}"/>
        /// that can contain additional view data that will be merged into the
        /// <see cref="ViewFeatures.ViewDataDictionary{TModel}"/> instance created for the template.
        /// </param>
        /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; element(s).</returns>
        /// <remarks>
        /// <para>
        /// For example the default <see cref="object"/> editor template includes &lt;label&gt; and &lt;input&gt;
        /// elements for each property in the current model.
        /// </para>
        /// <para>
        /// Custom templates are found under a <c>EditorTemplates</c> folder. The folder name is case-sensitive on
        /// case-sensitive file systems.
        /// </para>
        /// </remarks>
        public static IHtmlContent EditorForModel(
            this IHtmlHelper htmlHelper,
            string templateName,
            object additionalViewData)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            return htmlHelper.Editor(
                expression: null,
                templateName: templateName,
                htmlFieldName: null,
                additionalViewData: additionalViewData);
        }

        /// <summary>
        /// Returns HTML markup for the current model, using an editor template and specified HTML field name. The
        /// template is found using the <paramref name="templateName"/> or the model's
        /// <see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="templateName">The name of the template used to create the HTML markup.</param>
        /// <param name="htmlFieldName">
        /// A <see cref="string"/> used to disambiguate the names of HTML elements that are created for
        /// properties that have the same name.
        /// </param>
        /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; element(s).</returns>
        /// <remarks>
        /// <para>
        /// For example the default <see cref="object"/> editor template includes &lt;label&gt; and &lt;input&gt;
        /// elements for each property in the current model.
        /// </para>
        /// <para>
        /// Custom templates are found under a <c>EditorTemplates</c> folder. The folder name is case-sensitive on
        /// case-sensitive file systems.
        /// </para>
        /// </remarks>
        public static IHtmlContent EditorForModel(
            this IHtmlHelper htmlHelper,
            string templateName,
            string htmlFieldName)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            return htmlHelper.Editor(
                expression: null,
                templateName: templateName,
                htmlFieldName: htmlFieldName,
                additionalViewData: null);
        }

        /// <summary>
        /// Returns HTML markup for the current model, using an editor template, specified HTML field name, and
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
        /// An anonymous <see cref="object"/> or <see cref="System.Collections.Generic.IDictionary{String, Object}"/>
        /// that can contain additional view data that will be merged into the
        /// <see cref="ViewFeatures.ViewDataDictionary{TModel}"/> instance created for the template.
        /// </param>
        /// <returns>A new <see cref="IHtmlContent"/> containing the &lt;input&gt; element(s).</returns>
        /// <remarks>
        /// <para>
        /// For example the default <see cref="object"/> editor template includes &lt;label&gt; and &lt;input&gt;
        /// elements for each property in the current model.
        /// </para>
        /// <para>
        /// Custom templates are found under a <c>EditorTemplates</c> folder. The folder name is case-sensitive on
        /// case-sensitive file systems.
        /// </para>
        /// </remarks>
        public static IHtmlContent EditorForModel(
            this IHtmlHelper htmlHelper,
            string templateName,
            string htmlFieldName,
            object additionalViewData)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            return htmlHelper.Editor(
                expression: null,
                templateName: templateName,
                htmlFieldName: htmlFieldName,
                additionalViewData: additionalViewData);
        }
    }
}
