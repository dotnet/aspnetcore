// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public static class HtmlHelperPartialExtensions
    {
        /// <summary>
        /// Renders the partial view with the parent's view data and model to a string.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="HtmlHelper"/> instance that this method extends.</param>
        /// <param name="partialViewName">The name of the partial view to render.</param>
        /// <returns>
        /// A <see cref="Task{T}"/> that represents when rendering to the <see cref="HtmlString"/> has completed.
        /// </returns>
        public static Task<HtmlString> PartialAsync(
            [NotNull] this IHtmlHelper htmlHelper,
            [NotNull] string partialViewName)
        {
            return htmlHelper.PartialAsync(partialViewName, htmlHelper.ViewData.Model, viewData: null);
        }

        /// <summary>
        /// Renders the partial view with the given view data and, implicitly, the given view data's model to a string.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="HtmlHelper"/> instance that this method extends.</param>
        /// <param name="partialViewName">The name of the partial view to render.</param>
        /// <param name="viewData">
        /// The <see cref="ViewDataDictionary"/> that is provided to the partial view that will be rendered.
        /// </param>
        /// <returns>
        /// A <see cref="Task{T}"/> that represents when rendering to the <see cref="HtmlString"/> has completed.
        /// </returns>
        public static Task<HtmlString> PartialAsync(
            [NotNull] this IHtmlHelper htmlHelper,
            [NotNull] string partialViewName,
            ViewDataDictionary viewData)
        {
            return htmlHelper.PartialAsync(partialViewName, htmlHelper.ViewData.Model, viewData: viewData);
        }

        /// <summary>
        /// Renders the partial view with an empty view data and the given model to a string.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="HtmlHelper"/> instance that this method extends.</param>
        /// <param name="partialViewName">The name of the partial view to render.</param>
        /// <param name="model">The model to provide to the partial view that will be rendered.</param>
        /// <returns>
        /// A <see cref="Task{T}"/> that represents when rendering to the <see cref="HtmlString"/> has completed.
        /// </returns>
        public static Task<HtmlString> PartialAsync(
            [NotNull] this IHtmlHelper htmlHelper,
            [NotNull] string partialViewName,
            object model)
        {
            return htmlHelper.PartialAsync(partialViewName, model, viewData: null);
        }

        /// <summary>
        /// Renders the partial view with the parent's view data and model.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="HtmlHelper"/> instance that this method extends.</param>
        /// <param name="partialViewName">The name of the partial view to render.</param>
        /// <returns>A <see cref="Task"/> that represents when rendering has completed.</returns>
        public static Task RenderPartialAsync(
            [NotNull] this IHtmlHelper htmlHelper,
            [NotNull] string partialViewName)
        {
            return htmlHelper.RenderPartialAsync(partialViewName, htmlHelper.ViewData.Model,
                                                 viewData: htmlHelper.ViewData);
        }

        /// <summary>
        /// Renders the partial view with the given view data and, implicitly, the given view data's model.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="HtmlHelper"/> instance that this method extends.</param>
        /// <param name="partialViewName">The name of the partial view to render.</param>
        /// <param name="viewData">
        /// The <see cref="ViewDataDictionary"/> that is provided to the partial view that will be rendered.
        /// </param>
        /// <returns>A <see cref="Task"/> that represents when rendering has completed.</returns>
        public static Task RenderPartialAsync(
            [NotNull] this IHtmlHelper htmlHelper,
            [NotNull] string partialViewName,
            ViewDataDictionary viewData)
        {
            return htmlHelper.RenderPartialAsync(partialViewName, htmlHelper.ViewData.Model, viewData: viewData);
        }

        /// <summary>
        /// Renders the partial view with an empty view data and the given model.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="HtmlHelper"/> instance that this method extends.</param>
        /// <param name="partialViewName">The name of the partial view to render.</param>
        /// <param name="model">The model to provide to the partial view that will be rendered.</param>
        /// <returns>A <see cref="Task"/> that represents when rendering has completed.</returns>
        public static Task RenderPartialAsync(
            [NotNull] this IHtmlHelper htmlHelper,
            [NotNull] string partialViewName,
            object model)
        {
            return htmlHelper.RenderPartialAsync(partialViewName, model, htmlHelper.ViewData);
        }
    }
}
