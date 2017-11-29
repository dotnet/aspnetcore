// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc.Rendering
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
        /// The name or path of the partial view used to create the HTML markup. Must not be <c>null</c>.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that on completion returns a new <see cref="IHtmlContent"/> instance containing
        /// the created HTML.
        /// </returns>
        public static Task<IHtmlContent> PartialAsync(
            this IHtmlHelper htmlHelper,
            string partialViewName)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            if (partialViewName == null)
            {
                throw new ArgumentNullException(nameof(partialViewName));
            }

            return htmlHelper.PartialAsync(partialViewName, htmlHelper.ViewData.Model, viewData: null);
        }

        /// <summary>
        /// Returns HTML markup for the specified partial view.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="partialViewName">
        /// The name or path of the partial view used to create the HTML markup. Must not be <c>null</c>.
        /// </param>
        /// <param name="viewData">A <see cref="ViewDataDictionary"/> to pass into the partial view.</param>
        /// <returns>
        /// A <see cref="Task"/> that on completion returns a new <see cref="IHtmlContent"/> instance containing
        /// the created HTML.
        /// </returns>
        public static Task<IHtmlContent> PartialAsync(
            this IHtmlHelper htmlHelper,
            string partialViewName,
            ViewDataDictionary viewData)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            if (partialViewName == null)
            {
                throw new ArgumentNullException(nameof(partialViewName));
            }

            return htmlHelper.PartialAsync(partialViewName, htmlHelper.ViewData.Model, viewData);
        }

        /// <summary>
        /// Returns HTML markup for the specified partial view.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="partialViewName">
        /// The name or path of the partial view used to create the HTML markup. Must not be <c>null</c>.
        /// </param>
        /// <param name="model">A model to pass into the partial view.</param>
        /// <returns>
        /// A <see cref="Task"/> that on completion returns a new <see cref="IHtmlContent"/> instance containing
        /// the created HTML.
        /// </returns>
        public static Task<IHtmlContent> PartialAsync(
            this IHtmlHelper htmlHelper,
            string partialViewName,
            object model)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            if (partialViewName == null)
            {
                throw new ArgumentNullException(nameof(partialViewName));
            }

            return htmlHelper.PartialAsync(partialViewName, model, viewData: null);
        }

        /// <summary>
        /// Returns HTML markup for the specified partial view.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="partialViewName">
        /// The name or path of the partial view used to create the HTML markup. Must not be <c>null</c>.
        /// </param>
        /// <returns>
        /// Returns a new <see cref="IHtmlContent"/> instance containing the created HTML.
        /// </returns>
        /// <remarks>
        /// This method synchronously calls and blocks on
        /// <see cref="IHtmlHelper.PartialAsync(string, object, ViewDataDictionary)"/>
        /// </remarks>
        public static IHtmlContent Partial(
            this IHtmlHelper htmlHelper,
            string partialViewName)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            if (partialViewName == null)
            {
                throw new ArgumentNullException(nameof(partialViewName));
            }

            return Partial(htmlHelper, partialViewName, htmlHelper.ViewData.Model, viewData: null);
        }

        /// <summary>
        /// Returns HTML markup for the specified partial view.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="partialViewName">
        /// The name or path of the partial view used to create the HTML markup. Must not be <c>null</c>.
        /// </param>
        /// <param name="viewData">A <see cref="ViewDataDictionary"/> to pass into the partial view.</param>
        /// <returns>
        /// Returns a new <see cref="IHtmlContent"/> instance containing the created HTML.
        /// </returns>
        /// <remarks>
        /// This method synchronously calls and blocks on
        /// <see cref="IHtmlHelper.PartialAsync(string, object, ViewDataDictionary)"/>
        /// </remarks>
        public static IHtmlContent Partial(
            this IHtmlHelper htmlHelper,
            string partialViewName,
            ViewDataDictionary viewData)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            if (partialViewName == null)
            {
                throw new ArgumentNullException(nameof(partialViewName));
            }

            return Partial(htmlHelper, partialViewName, htmlHelper.ViewData.Model, viewData);
        }

        /// <summary>
        /// Returns HTML markup for the specified partial view.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="partialViewName">
        /// The name or path of the partial view used to create the HTML markup. Must not be <c>null</c>.
        /// </param>
        /// <param name="model">A model to pass into the partial view.</param>
        /// <returns>
        /// Returns a new <see cref="IHtmlContent"/> instance containing the created HTML.
        /// </returns>
        /// <remarks>
        /// This method synchronously calls and blocks on
        /// <see cref="IHtmlHelper.PartialAsync(string, object, ViewDataDictionary)"/>
        /// </remarks>
        public static IHtmlContent Partial(
            this IHtmlHelper htmlHelper,
            string partialViewName,
            object model)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            if (partialViewName == null)
            {
                throw new ArgumentNullException(nameof(partialViewName));
            }

            return Partial(htmlHelper, partialViewName, model, viewData: null);
        }

        /// <summary>
        /// Returns HTML markup for the specified partial view.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="partialViewName">
        /// The name or path of the partial view used to create the HTML markup. Must not be <c>null</c>.
        /// </param>
        /// <param name="model">A model to pass into the partial view.</param>
        /// <param name="viewData">A <see cref="ViewDataDictionary"/> to pass into the partial view.</param>
        /// <returns>
        /// Returns a new <see cref="IHtmlContent"/> instance containing the created HTML.
        /// </returns>
        /// <remarks>
        /// This method synchronously calls and blocks on
        /// <see cref="IHtmlHelper.PartialAsync(string, object, ViewDataDictionary)"/>
        /// </remarks>
        public static IHtmlContent Partial(
            this IHtmlHelper htmlHelper,
            string partialViewName,
            object model,
            ViewDataDictionary viewData)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            if (partialViewName == null)
            {
                throw new ArgumentNullException(nameof(partialViewName));
            }

            var result = htmlHelper.PartialAsync(partialViewName, model, viewData);
            return result.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Renders HTML markup for the specified partial view.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="partialViewName">
        /// The name or path of the partial view used to create the HTML markup. Must not be <c>null</c>.
        /// </param>
        /// <remarks>
        /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
        /// </remarks>
        public static void RenderPartial(
            this IHtmlHelper htmlHelper,
            string partialViewName)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            if (partialViewName == null)
            {
                throw new ArgumentNullException(nameof(partialViewName));
            }

            var result = htmlHelper.RenderPartialAsync(partialViewName, htmlHelper.ViewData.Model, viewData: null);
            result.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Renders HTML markup for the specified partial view.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="partialViewName">
        /// The name or path of the partial view used to create the HTML markup. Must not be <c>null</c>.
        /// </param>
        /// <param name="viewData">A <see cref="ViewDataDictionary"/> to pass into the partial view.</param>
        /// <remarks>
        /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
        /// </remarks>
        public static void RenderPartial(
            this IHtmlHelper htmlHelper,
            string partialViewName,
            ViewDataDictionary viewData)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            if (partialViewName == null)
            {
                throw new ArgumentNullException(nameof(partialViewName));
            }

            var result = htmlHelper.RenderPartialAsync(partialViewName, htmlHelper.ViewData.Model, viewData);
            result.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Renders HTML markup for the specified partial view.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="partialViewName">
        /// The name or path of the partial view used to create the HTML markup. Must not be <c>null</c>.
        /// </param>
        /// <param name="model">A model to pass into the partial view.</param>
        /// <remarks>
        /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
        /// </remarks>
        public static void RenderPartial(
            this IHtmlHelper htmlHelper,
            string partialViewName,
            object model)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            if (partialViewName == null)
            {
                throw new ArgumentNullException(nameof(partialViewName));
            }

            var result = htmlHelper.RenderPartialAsync(partialViewName, model, viewData: null);
            result.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Renders HTML markup for the specified partial view.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="partialViewName">
        /// The name or path of the partial view used to create the HTML markup. Must not be <c>null</c>.
        /// </param>
        /// <returns>A <see cref="Task"/> that renders the created HTML when it executes.</returns>
        /// <remarks>
        /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
        /// </remarks>
        public static Task RenderPartialAsync(
            this IHtmlHelper htmlHelper,
            string partialViewName)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            if (partialViewName == null)
            {
                throw new ArgumentNullException(nameof(partialViewName));
            }

            return htmlHelper.RenderPartialAsync(partialViewName, htmlHelper.ViewData.Model, viewData: null);
        }

        /// <summary>
        /// Renders HTML markup for the specified partial view.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="partialViewName">
        /// The name or path of the partial view used to create the HTML markup. Must not be <c>null</c>.
        /// </param>
        /// <param name="viewData">A <see cref="ViewDataDictionary"/> to pass into the partial view.</param>
        /// <returns>A <see cref="Task"/> that renders the created HTML when it executes.</returns>
        /// <remarks>
        /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
        /// </remarks>
        public static Task RenderPartialAsync(
            this IHtmlHelper htmlHelper,
            string partialViewName,
            ViewDataDictionary viewData)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            if (partialViewName == null)
            {
                throw new ArgumentNullException(nameof(partialViewName));
            }

            return htmlHelper.RenderPartialAsync(partialViewName, htmlHelper.ViewData.Model, viewData);
        }

        /// <summary>
        /// Renders HTML markup for the specified partial view.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="partialViewName">
        /// The name or path of the partial view used to create the HTML markup. Must not be <c>null</c>.
        /// </param>
        /// <param name="model">A model to pass into the partial view.</param>
        /// <returns>A <see cref="Task"/> that renders the created HTML when it executes.</returns>
        /// <remarks>
        /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
        /// </remarks>
        public static Task RenderPartialAsync(
            this IHtmlHelper htmlHelper,
            string partialViewName,
            object model)
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            if (partialViewName == null)
            {
                throw new ArgumentNullException(nameof(partialViewName));
            }

            return htmlHelper.RenderPartialAsync(partialViewName, model, viewData: null);
        }
    }
}
