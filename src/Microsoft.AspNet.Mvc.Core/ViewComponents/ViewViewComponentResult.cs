// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.ViewComponents;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// A <see cref="IViewComponentResult"/> that renders a partial view when executed.
    /// </summary>
    public class ViewViewComponentResult : IViewComponentResult
    {
        // {0} is the component name, {1} is the view name.
        private const string ViewPathFormat = "Components/{0}/{1}";

        /// <summary>
        /// Gets or sets the view name.
        /// </summary>
        public string ViewName { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ViewDataDictionary"/>.
        /// </summary>
        public ViewDataDictionary ViewData { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ITempDataDictionary"/> instance.
        /// </summary>
        public ITempDataDictionary TempData { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ViewEngine"/>.
        /// </summary>
        public IViewEngine ViewEngine { get; set; }

        /// <summary>
        /// Locates and renders a view specified by <see cref="ViewName"/>. If <see cref="ViewName"/> is <c>null</c>,
        /// then the view name searched for is<c>&quot;Default&quot;</c>.
        /// </summary>
        /// <param name="context">The <see cref="ViewComponentContext"/> for the current component execution.</param>
        /// <remarks>
        /// This method synchronously calls and blocks on <see cref="ExecuteAsync(ViewComponentContext)"/>.
        /// </remarks>
        public void Execute([NotNull] ViewComponentContext context)
        {
            var task = ExecuteAsync(context);
            TaskHelper.WaitAndThrowIfFaulted(task);
        }

        /// <summary>
        /// Locates and renders a view specified by <see cref="ViewName"/>. If <see cref="ViewName"/> is <c>null</c>,
        /// then the view name searched for is<c>&quot;Default&quot;</c>.
        /// </summary>
        /// <param name="context">The <see cref="ViewComponentContext"/> for the current component execution.</param>
        /// <returns>A <see cref="Task"/> which will complete when view rendering is completed.</returns>
        public async Task ExecuteAsync([NotNull] ViewComponentContext context)
        {
            var viewEngine = ViewEngine ?? ResolveViewEngine(context);
            var viewData = ViewData ?? context.ViewContext.ViewData;

            string qualifiedViewName;
            if (ViewName != null && ViewName.Length > 0 && ViewName[0] == '/')
            {
                // View name that was passed in is already a rooted path, the view engine will handle this.
                qualifiedViewName = ViewName;
            }
            else
            {
                // This will produce a string like:
                //
                //  Components/Cart/Default
                //
                // The view engine will combine this with other path info to search paths like:
                //
                //  Views/Shared/Components/Cart/Default.cshtml
                //  Views/Home/Components/Cart/Default.cshtml
                //  Areas/Blog/Views/Shared/Components/Cart/Default.cshtml
                //
                // This supports a controller or area providing an override for component views.
                qualifiedViewName = string.Format(
                    CultureInfo.InvariantCulture,
                    ViewPathFormat,
                    context.ViewComponentDescriptor.ShortName,
                    ViewName ?? "Default");
            }

            var view = FindView(context.ViewContext, viewEngine, qualifiedViewName);

            var childViewContext = new ViewContext(
                context.ViewContext,
                view,
                ViewData ?? context.ViewContext.ViewData,
                TempData ?? context.ViewContext.TempData,
                context.Writer);

            using (view as IDisposable)
            {
                await view.RenderAsync(childViewContext);
            }
        }

        private static IView FindView(ActionContext context, IViewEngine viewEngine, string viewName)
        {
            return viewEngine.FindPartialView(context, viewName).EnsureSuccessful().View;
        }

        private static IViewEngine ResolveViewEngine(ViewComponentContext context)
        {
            return context.ViewContext.HttpContext.RequestServices.GetRequiredService<ICompositeViewEngine>();
        }
    }
}
