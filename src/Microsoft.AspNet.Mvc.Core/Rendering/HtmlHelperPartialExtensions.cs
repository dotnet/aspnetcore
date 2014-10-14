// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Rendering
{
    /// <summary>
    /// PartialView-related extensions for <see cref="IHtmlHelper"/>.
    /// </summary>
    public static class HtmlHelperPartialExtensions
    {
        /// <summary>
        /// Returns HTML markup for the specified partial view.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="partialViewName">
        /// The name of the partial view used to create the HTML markup. Must not be <c>null</c>.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that on completion returns a new <see cref="HtmlString"/> containing
        /// the created HTML.
        /// </returns>
        public static Task<HtmlString> PartialAsync(
            [NotNull] this IHtmlHelper htmlHelper,
            [NotNull] string partialViewName)
        {
            return htmlHelper.PartialAsync(partialViewName, htmlHelper.ViewData.Model, viewData: null);
        }

        /// <summary>
        /// Returns HTML markup for the specified partial view.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="partialViewName">
        /// The name of the partial view used to create the HTML markup. Must not be <c>null</c>.
        /// </param>
        /// <param name="viewData">A <see cref="ViewDataDictionary"/> to pass into the partial view.</param>
        /// <returns>
        /// A <see cref="Task"/> that on completion returns a new <see cref="HtmlString"/> containing
        /// the created HTML.
        /// </returns>
        public static Task<HtmlString> PartialAsync(
            [NotNull] this IHtmlHelper htmlHelper,
            [NotNull] string partialViewName,
            ViewDataDictionary viewData)
        {
            return htmlHelper.PartialAsync(partialViewName, htmlHelper.ViewData.Model, viewData: viewData);
        }

        /// <summary>
        /// Returns HTML markup for the specified partial view.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="partialViewName">
        /// The name of the partial view used to create the HTML markup. Must not be <c>null</c>.
        /// </param>
        /// <param name="model">A model to pass into the partial view.</param>
        /// <returns>
        /// A <see cref="Task"/> that on completion returns a new <see cref="HtmlString"/> containing
        /// the created HTML.
        /// </returns>
        public static Task<HtmlString> PartialAsync(
            [NotNull] this IHtmlHelper htmlHelper,
            [NotNull] string partialViewName,
            object model)
        {
            return htmlHelper.PartialAsync(partialViewName, model, viewData: null);
        }

        /// <summary>
        /// Returns HTML markup for the specified partial view.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="partialViewName">
        /// The name of the partial view used to create the HTML markup. Must not be <c>null</c>.
        /// </param>
        /// <returns>
        /// Returns a new <see cref="HtmlString"/> containing the created HTML.
        /// </returns>
        /// <remarks>
        /// This method synchronously calls and blocks on
        /// <see cref="IHtmlHelper.PartialAsync(string, object, ViewDataDictionary)"/>
        /// </remarks>
        public static HtmlString Partial(
            [NotNull] this IHtmlHelper htmlHelper,
            [NotNull] string partialViewName)
        {
            return Partial(htmlHelper, partialViewName, htmlHelper.ViewData.Model, viewData: null);
        }

        /// <summary>
        /// Returns HTML markup for the specified partial view.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="partialViewName">
        /// The name of the partial view used to create the HTML markup. Must not be <c>null</c>.
        /// </param>
        /// <param name="viewData">A <see cref="ViewDataDictionary"/> to pass into the partial view.</param>
        /// <returns>
        /// Returns a new <see cref="HtmlString"/> containing the created HTML.
        /// </returns>
        /// <remarks>
        /// This method synchronously calls and blocks on
        /// <see cref="IHtmlHelper.PartialAsync(string, object, ViewDataDictionary)"/>
        /// </remarks>
        public static HtmlString Partial(
            [NotNull] this IHtmlHelper htmlHelper,
            [NotNull] string partialViewName,
            ViewDataDictionary viewData)
        {
            return Partial(htmlHelper, partialViewName, htmlHelper.ViewData.Model, viewData);
        }

        /// <summary>
        /// Returns HTML markup for the specified partial view.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="partialViewName">
        /// The name of the partial view used to create the HTML markup. Must not be <c>null</c>.
        /// </param>
        /// <param name="model">A model to pass into the partial view.</param>
        /// <returns>
        /// Returns a new <see cref="HtmlString"/> containing the created HTML.
        /// </returns>
        /// <remarks>
        /// This method synchronously calls and blocks on
        /// <see cref="IHtmlHelper.PartialAsync(string, object, ViewDataDictionary)"/>
        /// </remarks>
        public static HtmlString Partial(
            [NotNull] this IHtmlHelper htmlHelper,
            [NotNull] string partialViewName,
            object model)
        {
            return Partial(htmlHelper, partialViewName, model, viewData: null);
        }

        /// <summary>
        /// Returns HTML markup for the specified partial view.
        /// </summary>
        /// <param name="partialViewName">
        /// The name of the partial view used to create the HTML markup. Must not be <c>null</c>.
        /// </param>
        /// <param name="model">A model to pass into the partial view.</param>
        /// <param name="viewData">A <see cref="ViewDataDictionary"/> to pass into the partial view.</param>
        /// <returns>
        /// Returns a new <see cref="HtmlString"/> containing the created HTML.
        /// </returns>
        /// <remarks>
        /// This method synchronously calls and blocks on
        /// <see cref="IHtmlHelper.PartialAsync(string, object, ViewDataDictionary)"/>
        /// </remarks>
        public static HtmlString Partial(
            [NotNull] this IHtmlHelper htmlHelper,
            [NotNull] string partialViewName,
            object model,
            ViewDataDictionary viewData)
        {
            var result = htmlHelper.PartialAsync(partialViewName, model, viewData);
            return TaskHelper.WaitAndThrowIfFaulted(result);
        }

        /// <summary>
        /// Renders HTML markup for the specified partial view.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="partialViewName">
        /// The name of the partial view used to create the HTML markup. Must not be <c>null</c>.
        /// </param>
        /// <returns>A <see cref="Task"/> that renders the created HTML when it executes.</returns>
        /// <remarks>
        /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
        /// </remarks>
        public static Task RenderPartialAsync(
            [NotNull] this IHtmlHelper htmlHelper,
            [NotNull] string partialViewName)
        {
            return htmlHelper.RenderPartialAsync(partialViewName, htmlHelper.ViewData.Model,
                                                 viewData: htmlHelper.ViewData);
        }

        /// <summary>
        /// Renders HTML markup for the specified partial view.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="partialViewName">
        /// The name of the partial view used to create the HTML markup. Must not be <c>null</c>.
        /// </param>
        /// <param name="viewData">A <see cref="ViewDataDictionary"/> to pass into the partial view.</param>
        /// <returns>A <see cref="Task"/> that renders the created HTML when it executes.</returns>
        /// <remarks>
        /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
        /// </remarks>
        public static Task RenderPartialAsync(
            [NotNull] this IHtmlHelper htmlHelper,
            [NotNull] string partialViewName,
            ViewDataDictionary viewData)
        {
            return htmlHelper.RenderPartialAsync(partialViewName, htmlHelper.ViewData.Model, viewData: viewData);
        }

        /// <summary>
        /// Renders HTML markup for the specified partial view.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="partialViewName">
        /// The name of the partial view used to create the HTML markup. Must not be <c>null</c>.
        /// </param>
        /// <param name="model">A model to pass into the partial view.</param>
        /// <returns>A <see cref="Task"/> that renders the created HTML when it executes.</returns>
        /// <remarks>
        /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
        /// </remarks>
        public static Task RenderPartialAsync(
            [NotNull] this IHtmlHelper htmlHelper,
            [NotNull] string partialViewName,
            object model)
        {
            return htmlHelper.RenderPartialAsync(partialViewName, model, htmlHelper.ViewData);
        }
    }
}
