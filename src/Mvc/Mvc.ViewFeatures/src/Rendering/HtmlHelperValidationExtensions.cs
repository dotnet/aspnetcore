// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.Rendering;

/// <summary>
/// Validation-related extensions for <see cref="IHtmlHelper"/> and <see cref="IHtmlHelper{TModel}"/>.
/// </summary>
public static class HtmlHelperValidationExtensions
{
    /// <summary>
    /// Returns the validation message if an error exists in the <see cref="ModelBinding.ModelStateDictionary"/>
    /// object for the specified <paramref name="expression"/>.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <returns>
    /// A new <see cref="IHtmlContent"/> containing a <see cref="ViewContext.ValidationMessageElement"/> element.
    /// An empty <see cref="IHtmlContent"/> if the <paramref name="expression"/> is valid and client-side
    /// validation is disabled.
    /// </returns>
    /// <remarks>
    /// Method extracts an error string from the <see cref="ModelBinding.ModelStateDictionary"/> object. Message
    /// will always be visible but client-side validation may update the associated CSS class.
    /// </remarks>
    public static IHtmlContent ValidationMessage(
        this IHtmlHelper htmlHelper,
        string expression)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.ValidationMessage(expression, message: null, htmlAttributes: null, tag: null);
    }

    /// <summary>
    /// Returns the validation message if an error exists in the <see cref="ModelBinding.ModelStateDictionary"/>
    /// object for the specified <paramref name="expression"/>.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="message">
    /// The message to be displayed. If <c>null</c> or empty, method extracts an error string from the
    /// <see cref="ModelBinding.ModelStateDictionary"/> object. Message will always be visible but client-side
    /// validation may update the associated CSS class.
    /// </param>
    /// <returns>
    /// A new <see cref="IHtmlContent"/> containing a <see cref="ViewContext.ValidationMessageElement"/> element.
    /// An empty <see cref="IHtmlContent"/> if the <paramref name="expression"/> is valid and client-side
    /// validation is disabled.
    /// </returns>
    public static IHtmlContent ValidationMessage(
        this IHtmlHelper htmlHelper,
        string expression,
        string message)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.ValidationMessage(expression, message, htmlAttributes: null, tag: null);
    }

    /// <summary>
    /// Returns the validation message if an error exists in the <see cref="ModelBinding.ModelStateDictionary"/>
    /// object for the specified <paramref name="expression"/>.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the
    /// (<see cref="ViewContext.ValidationMessageElement"/>) element. Alternatively, an
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the HTML
    /// attributes.
    /// </param>
    /// <returns>
    /// A new <see cref="IHtmlContent"/> containing a <see cref="ViewContext.ValidationMessageElement"/> element.
    /// An empty <see cref="IHtmlContent"/> if the <paramref name="expression"/> is valid and client-side
    /// validation is disabled.
    /// </returns>
    /// <remarks>
    /// Method extracts an error string from the <see cref="ModelBinding.ModelStateDictionary"/> object. Message
    /// will always be visible but client-side validation may update the associated CSS class.
    /// </remarks>
    public static IHtmlContent ValidationMessage(
        this IHtmlHelper htmlHelper,
        string expression,
        object htmlAttributes)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.ValidationMessage(expression, message: null, htmlAttributes: htmlAttributes, tag: null);
    }

    /// <summary>
    /// Returns the validation message if an error exists in the <see cref="ModelBinding.ModelStateDictionary"/>
    /// object for the specified <paramref name="expression"/>.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
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
    /// <returns>
    /// A new <see cref="IHtmlContent"/> containing a <paramref name="tag"/> element. An empty
    /// <see cref="IHtmlContent"/> if the <paramref name="expression"/> is valid and client-side validation is
    /// disabled.
    /// </returns>
    public static IHtmlContent ValidationMessage(
        this IHtmlHelper htmlHelper,
        string expression,
        string message,
        string tag)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.ValidationMessage(expression, message, htmlAttributes: null, tag: tag);
    }

    /// <summary>
    /// Returns the validation message if an error exists in the <see cref="ModelBinding.ModelStateDictionary"/>
    /// object for the specified <paramref name="expression"/>.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="message">
    /// The message to be displayed. If <c>null</c> or empty, method extracts an error string from the
    /// <see cref="ModelBinding.ModelStateDictionary"/> object. Message will always be visible but client-side
    /// validation may update the associated CSS class.
    /// </param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the
    /// (<see cref="ViewContext.ValidationMessageElement"/>) element. Alternatively, an
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the HTML
    /// attributes.
    /// </param>
    /// <returns>
    /// A new <see cref="IHtmlContent"/> containing a <see cref="ViewContext.ValidationMessageElement"/> element.
    /// An empty <see cref="IHtmlContent"/> if the <paramref name="expression"/> is valid and client-side
    /// validation is disabled.
    /// </returns>
    public static IHtmlContent ValidationMessage(
        this IHtmlHelper htmlHelper,
        string expression,
        string message,
        object htmlAttributes)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.ValidationMessage(expression, message, htmlAttributes, tag: null);
    }

    /// <summary>
    /// Returns the validation message if an error exists in the <see cref="ModelBinding.ModelStateDictionary"/>
    /// object for the specified <paramref name="expression"/>.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="expression">An expression to be evaluated against the current model.</param>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>
    /// A new <see cref="IHtmlContent"/> containing a <see cref="ViewContext.ValidationMessageElement"/> element.
    /// An empty <see cref="IHtmlContent"/> if the <paramref name="expression"/> is valid and client-side
    /// validation is disabled.
    /// </returns>
    /// <remarks>
    /// Method extracts an error string from the <see cref="ModelBinding.ModelStateDictionary"/> object. Message
    /// will always be visible but client-side validation may update the associated CSS class.
    /// </remarks>
    public static IHtmlContent ValidationMessageFor<TModel, TResult>(
        this IHtmlHelper<TModel> htmlHelper,
        Expression<Func<TModel, TResult>> expression)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);
        ArgumentNullException.ThrowIfNull(expression);

        return htmlHelper.ValidationMessageFor(expression, message: null, htmlAttributes: null, tag: null);
    }

    /// <summary>
    /// Returns the validation message if an error exists in the <see cref="ModelBinding.ModelStateDictionary"/>
    /// object for the specified <paramref name="expression"/>.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="expression">An expression to be evaluated against the current model.</param>
    /// <param name="message">
    /// The message to be displayed. If <c>null</c> or empty, method extracts an error string from the
    /// <see cref="ModelBinding.ModelStateDictionary"/> object. Message will always be visible but client-side
    /// validation may update the associated CSS class.
    /// </param>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>
    /// A new <see cref="IHtmlContent"/> containing a <see cref="ViewContext.ValidationMessageElement"/> element.
    /// An empty <see cref="IHtmlContent"/> if the <paramref name="expression"/> is valid and client-side
    /// validation is disabled.
    /// </returns>
    public static IHtmlContent ValidationMessageFor<TModel, TResult>(
        this IHtmlHelper<TModel> htmlHelper,
        Expression<Func<TModel, TResult>> expression,
        string message)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);
        ArgumentNullException.ThrowIfNull(expression);

        return htmlHelper.ValidationMessageFor(expression, message, htmlAttributes: null, tag: null);
    }

    /// <summary>
    /// Returns the validation message if an error exists in the <see cref="ModelBinding.ModelStateDictionary"/>
    /// object for the specified <paramref name="expression"/>.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="expression">An expression to be evaluated against the current model.</param>
    /// <param name="message">
    /// The message to be displayed. If <c>null</c> or empty, method extracts an error string from the
    /// <see cref="ModelBinding.ModelStateDictionary"/> object. Message will always be visible but client-side
    /// validation may update the associated CSS class.
    /// </param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the
    /// (<see cref="ViewContext.ValidationMessageElement"/>) element. Alternatively, an
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the HTML
    /// attributes.
    /// </param>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>
    /// A new <see cref="IHtmlContent"/> containing a <see cref="ViewContext.ValidationMessageElement"/> element.
    /// An empty <see cref="IHtmlContent"/> if the <paramref name="expression"/> is valid and client-side
    /// validation is disabled.
    /// </returns>
    public static IHtmlContent ValidationMessageFor<TModel, TResult>(
        this IHtmlHelper<TModel> htmlHelper,
        Expression<Func<TModel, TResult>> expression,
        string message,
        object htmlAttributes)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);
        ArgumentNullException.ThrowIfNull(expression);

        return htmlHelper.ValidationMessageFor(expression, message, htmlAttributes, tag: null);
    }

    /// <summary>
    /// Returns the validation message if an error exists in the <see cref="ModelBinding.ModelStateDictionary"/>
    /// object for the specified <paramref name="expression"/>.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="expression">An expression to be evaluated against the current model.</param>
    /// <param name="message">
    /// The message to be displayed. If <c>null</c> or empty, method extracts an error string from the
    /// <see cref="ModelBinding.ModelStateDictionary"/> object. Message will always be visible but client-side
    /// validation may update the associated CSS class.
    /// </param>
    /// <param name="tag">
    /// The tag to wrap the <paramref name="message"/> in the generated HTML. Its default value is
    /// <see cref="ViewContext.ValidationMessageElement"/>.
    /// </param>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TResult">The type of the <paramref name="expression"/> result.</typeparam>
    /// <returns>
    /// A new <see cref="IHtmlContent"/> containing the <paramref name="tag"/> element. An empty
    /// <see cref="IHtmlContent"/> if the <paramref name="expression"/> is valid and client-side validation is
    /// disabled.
    /// </returns>
    public static IHtmlContent ValidationMessageFor<TModel, TResult>(
        this IHtmlHelper<TModel> htmlHelper,
        Expression<Func<TModel, TResult>> expression,
        string message,
        string tag)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);
        ArgumentNullException.ThrowIfNull(expression);

        return htmlHelper.ValidationMessageFor(expression, message, htmlAttributes: null, tag: tag);
    }

    /// <summary>
    /// Returns an unordered list (&lt;ul&gt; element) of validation messages that are in the
    /// <see cref="ModelBinding.ModelStateDictionary"/> object.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <returns>
    /// New <see cref="IHtmlContent"/> containing a &lt;div&gt; element wrapping the &lt;ul&gt; element.
    /// An empty <see cref="IHtmlContent"/> if the current model is valid and client-side validation is disabled.
    /// </returns>
    public static IHtmlContent ValidationSummary(this IHtmlHelper htmlHelper)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.ValidationSummary(
            excludePropertyErrors: false,
            message: null,
            htmlAttributes: null,
            tag: null);
    }

    /// <summary>
    /// Returns an unordered list (&lt;ul&gt; element) of validation messages that are in the
    /// <see cref="ModelBinding.ModelStateDictionary"/> object.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="excludePropertyErrors">
    /// If <c>true</c>, display model-level errors only; otherwise display all errors.
    /// </param>
    /// <returns>
    /// New <see cref="IHtmlContent"/> containing a &lt;div&gt; element wrapping the &lt;ul&gt; element.
    /// An empty <see cref="IHtmlContent"/> if the current model is valid and client-side validation is disabled.
    /// </returns>
    public static IHtmlContent ValidationSummary(this IHtmlHelper htmlHelper, bool excludePropertyErrors)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.ValidationSummary(
            excludePropertyErrors,
            message: null,
            htmlAttributes: null,
            tag: null);
    }

    /// <summary>
    /// Returns an unordered list (&lt;ul&gt; element) of validation messages that are in the
    /// <see cref="ModelBinding.ModelStateDictionary"/> object.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="message">The message to display with the validation summary.</param>
    /// <returns>
    /// New <see cref="IHtmlContent"/> containing a &lt;div&gt; element wrapping the
    /// <see cref="ViewContext.ValidationSummaryMessageElement" /> element (which wraps the
    /// <paramref name="message"/>) and the &lt;ul&gt; element. An empty <see cref="IHtmlContent"/> if the current
    /// model is valid and client-side validation is disabled.
    /// </returns>
    public static IHtmlContent ValidationSummary(this IHtmlHelper htmlHelper, string message)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.ValidationSummary(
            excludePropertyErrors: false,
            message: message,
            htmlAttributes: null,
            tag: null);
    }

    /// <summary>
    /// Returns an unordered list (&lt;ul&gt; element) of validation messages that are in the
    /// <see cref="ModelBinding.ModelStateDictionary"/> object.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="message">The message to display with the validation summary.</param>
    /// <param name="tag">
    /// The tag to wrap the <paramref name="message"/> in the generated HTML. Its default value is
    /// <see cref="ViewContext.ValidationSummaryMessageElement" />.
    /// </param>
    /// <returns>
    /// New <see cref="IHtmlContent"/> containing a &lt;div&gt; element wrapping the <paramref name="tag"/> element
    /// and the &lt;ul&gt; element. An empty <see cref="IHtmlContent"/> if the current model is valid and
    /// client-side validation is disabled.
    /// </returns>
    public static IHtmlContent ValidationSummary(this IHtmlHelper htmlHelper, string message, string tag)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.ValidationSummary(
            excludePropertyErrors: false,
            message: message,
            htmlAttributes: null,
            tag: tag);
    }

    /// <summary>
    /// Returns an unordered list (&lt;ul&gt; element) of validation messages that are in the
    /// <see cref="ModelBinding.ModelStateDictionary"/> object.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="excludePropertyErrors">
    /// If <c>true</c>, display model-level errors only; otherwise display all errors.
    /// </param>
    /// <param name="message">The message to display with the validation summary.</param>
    /// <returns>
    /// New <see cref="IHtmlContent"/> containing a &lt;div&gt; element wrapping the
    /// <see cref="ViewContext.ValidationSummaryMessageElement" /> element (which, in turn, wraps the
    /// <paramref name="message"/>) and the &lt;ul&gt; element. An empty <see cref="IHtmlContent"/> if the current
    /// model is valid and client-side validation is disabled.
    /// </returns>
    public static IHtmlContent ValidationSummary(
        this IHtmlHelper htmlHelper,
        bool excludePropertyErrors,
        string message)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.ValidationSummary(
            excludePropertyErrors,
            message,
            htmlAttributes: null,
            tag: null);
    }

    /// <summary>
    /// Returns an unordered list (&lt;ul&gt; element) of validation messages that are in the
    /// <see cref="ModelBinding.ModelStateDictionary"/> object.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="message">The message to display with the validation summary.</param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the topmost (&lt;div&gt;) element.
    /// Alternatively, an <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing
    /// the HTML attributes.
    /// </param>
    /// <returns>
    /// New <see cref="IHtmlContent"/> containing a &lt;div&gt; element wrapping the
    /// <see cref="ViewContext.ValidationSummaryMessageElement" /> element (which wraps the
    /// <paramref name="message"/>) and the &lt;ul&gt; element. An empty <see cref="IHtmlContent"/> if the current
    /// model is valid and client-side validation is disabled.
    /// </returns>
    public static IHtmlContent ValidationSummary(
        this IHtmlHelper htmlHelper,
        string message,
        object htmlAttributes)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.ValidationSummary(
            excludePropertyErrors: false,
            message: message,
            htmlAttributes: htmlAttributes,
            tag: null);
    }

    /// <summary>
    /// Returns an unordered list (&lt;ul&gt; element) of validation messages that are in the
    /// <see cref="ModelBinding.ModelStateDictionary"/> object.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="message">The message to display with the validation summary.</param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the topmost (&lt;div&gt;) element.
    /// Alternatively, an <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing
    /// the HTML attributes.
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
    public static IHtmlContent ValidationSummary(
        this IHtmlHelper htmlHelper,
        string message,
        object htmlAttributes,
        string tag)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.ValidationSummary(
            excludePropertyErrors: false,
            message: message,
            htmlAttributes: htmlAttributes,
            tag: tag);
    }

    /// <summary>
    /// Returns an unordered list (&lt;ul&gt; element) of validation messages that are in the
    /// <see cref="ModelBinding.ModelStateDictionary"/> object.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="excludePropertyErrors">
    /// If <c>true</c>, display model-level errors only; otherwise display all errors.
    /// </param>
    /// <param name="message">The message to display with the validation summary.</param>
    /// <param name="tag">
    /// The tag to wrap the <paramref name="message"/> in the generated HTML. Its default value is
    /// <see cref="ViewContext.ValidationSummaryMessageElement" />.
    /// </param>
    /// <returns>
    /// New <see cref="IHtmlContent"/> containing a &lt;div&gt; element wrapping the <paramref name="tag"/> element
    /// and the &lt;ul&gt; element. An empty <see cref="IHtmlContent"/> if the current model is valid and
    /// client-side validation is disabled.
    /// </returns>
    public static IHtmlContent ValidationSummary(
        this IHtmlHelper htmlHelper,
        bool excludePropertyErrors,
        string message,
        string tag)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.ValidationSummary(
            excludePropertyErrors,
            message,
            htmlAttributes: null,
            tag: tag);
    }

    /// <summary>
    /// Returns an unordered list (&lt;ul&gt; element) of validation messages that are in the
    /// <see cref="ModelBinding.ModelStateDictionary"/> object.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="excludePropertyErrors">
    /// If <c>true</c>, display model-level errors only; otherwise display all errors.
    /// </param>
    /// <param name="message">The message to display with the validation summary.</param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the topmost (&lt;div&gt;) element.
    /// Alternatively, an <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing
    /// the HTML attributes.
    /// </param>
    /// <returns>
    /// New <see cref="IHtmlContent"/> containing a &lt;div&gt; element wrapping the
    /// <see cref="ViewContext.ValidationSummaryMessageElement" /> element (which wraps the
    /// <paramref name="message"/>) and the &lt;ul&gt; element. An empty <see cref="IHtmlContent"/> if the current
    /// model is valid and client-side validation is disabled.
    /// </returns>
    public static IHtmlContent ValidationSummary(
        this IHtmlHelper htmlHelper,
        bool excludePropertyErrors,
        string message,
        object htmlAttributes)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.ValidationSummary(excludePropertyErrors, message, htmlAttributes, tag: null);
    }
}
