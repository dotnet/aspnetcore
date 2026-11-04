// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.Rendering;

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
    public static IHtmlContent Display(this IHtmlHelper htmlHelper, string expression)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

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
    /// An anonymous <see cref="object"/> or <see cref="System.Collections.Generic.IDictionary{String, Object}"/>
    /// that can contain additional view data that will be merged into the
    /// <see cref="ViewFeatures.ViewDataDictionary{TModel}"/> instance created for the template.
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
    public static IHtmlContent Display(
        this IHtmlHelper htmlHelper,
        string expression,
        object additionalViewData)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

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
    public static IHtmlContent Display(
        this IHtmlHelper htmlHelper,
        string expression,
        string templateName)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

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
    /// An anonymous <see cref="object"/> or <see cref="System.Collections.Generic.IDictionary{String, Object}"/>
    /// that can contain additional view data that will be merged into the
    /// <see cref="ViewFeatures.ViewDataDictionary{TModel}"/> instance created for the template.
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
    public static IHtmlContent Display(
        this IHtmlHelper htmlHelper,
        string expression,
        string templateName,
        object additionalViewData)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

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
    public static IHtmlContent Display(
        this IHtmlHelper htmlHelper,
        string expression,
        string templateName,
        string htmlFieldName)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

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
    /// <returns>A new <see cref="IHtmlContent"/> containing the created HTML.</returns>
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
    public static IHtmlContent DisplayFor<TModel, TResult>(
        this IHtmlHelper<TModel> htmlHelper,
        Expression<Func<TModel, TResult>> expression)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);
        ArgumentNullException.ThrowIfNull(expression);

        return htmlHelper.DisplayFor(
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
    /// An anonymous <see cref="object"/> or <see cref="System.Collections.Generic.IDictionary{String, Object}"/>
    /// that can contain additional view data that will be merged into the
    /// <see cref="ViewFeatures.ViewDataDictionary{TModel}"/> instance created for the template.
    /// </param>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>A new <see cref="IHtmlContent"/> containing the created HTML.</returns>
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
    public static IHtmlContent DisplayFor<TModel, TResult>(
        this IHtmlHelper<TModel> htmlHelper,
        Expression<Func<TModel, TResult>> expression,
        object additionalViewData)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);
        ArgumentNullException.ThrowIfNull(expression);

        return htmlHelper.DisplayFor(
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
    /// <returns>A new <see cref="IHtmlContent"/> containing the created HTML.</returns>
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
    public static IHtmlContent DisplayFor<TModel, TResult>(
        this IHtmlHelper<TModel> htmlHelper,
        Expression<Func<TModel, TResult>> expression,
        string templateName)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);
        ArgumentNullException.ThrowIfNull(expression);

        return htmlHelper.DisplayFor(
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
    /// An anonymous <see cref="object"/> or <see cref="System.Collections.Generic.IDictionary{String, Object}"/>
    /// that can contain additional view data that will be merged into the
    /// <see cref="ViewFeatures.ViewDataDictionary{TModel}"/> instance created for the template.
    /// </param>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>A new <see cref="IHtmlContent"/> containing the created HTML.</returns>
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
    public static IHtmlContent DisplayFor<TModel, TResult>(
        this IHtmlHelper<TModel> htmlHelper,
        Expression<Func<TModel, TResult>> expression,
        string templateName,
        object additionalViewData)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);
        ArgumentNullException.ThrowIfNull(expression);

        return htmlHelper.DisplayFor(
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
    /// <returns>A new <see cref="IHtmlContent"/> containing the created HTML.</returns>
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
    public static IHtmlContent DisplayFor<TModel, TResult>(
        this IHtmlHelper<TModel> htmlHelper,
        Expression<Func<TModel, TResult>> expression,
        string templateName,
        string htmlFieldName)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);
        ArgumentNullException.ThrowIfNull(expression);

        return htmlHelper.DisplayFor(
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
    /// <returns>A new <see cref="IHtmlContent"/> containing the created HTML.</returns>
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
    public static IHtmlContent DisplayForModel(this IHtmlHelper htmlHelper)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

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
    /// An anonymous <see cref="object"/> or <see cref="System.Collections.Generic.IDictionary{String, Object}"/>
    /// that can contain additional view data that will be merged into the
    /// <see cref="ViewFeatures.ViewDataDictionary{TModel}"/> instance created for the template.
    /// </param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the created HTML.</returns>
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
    public static IHtmlContent DisplayForModel(this IHtmlHelper htmlHelper, object additionalViewData)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

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
    /// <returns>A new <see cref="IHtmlContent"/> containing the created HTML.</returns>
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
    public static IHtmlContent DisplayForModel(this IHtmlHelper htmlHelper, string templateName)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

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
    /// An anonymous <see cref="object"/> or <see cref="System.Collections.Generic.IDictionary{String, Object}"/>
    /// that can contain additional view data that will be merged into the
    /// <see cref="ViewFeatures.ViewDataDictionary{TModel}"/> instance created for the template.
    /// </param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the created HTML.</returns>
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
    public static IHtmlContent DisplayForModel(
        this IHtmlHelper htmlHelper,
        string templateName,
        object additionalViewData)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

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
    /// <returns>A new <see cref="IHtmlContent"/> containing the created HTML.</returns>
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
    public static IHtmlContent DisplayForModel(
        this IHtmlHelper htmlHelper,
        string templateName,
        string htmlFieldName)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

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
    /// An anonymous <see cref="object"/> or <see cref="System.Collections.Generic.IDictionary{String, Object}"/>
    /// that can contain additional view data that will be merged into the
    /// <see cref="ViewFeatures.ViewDataDictionary{TModel}"/> instance created for the template.
    /// </param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the created HTML.</returns>
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
    public static IHtmlContent DisplayForModel(
        this IHtmlHelper htmlHelper,
        string templateName,
        string htmlFieldName,
        object additionalViewData)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.Display(
            expression: null,
            templateName: templateName,
            htmlFieldName: htmlFieldName,
            additionalViewData: additionalViewData);
    }
}
