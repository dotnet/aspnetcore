// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// A <see cref="IViewComponentResult"/> that renders a partial view when executed.
    /// </summary>
    public class ViewViewComponentResult : IViewComponentResult
    {
        // {0} is the component name, {1} is the view name.
        private const string ViewPathFormat = "Components/{0}/{1}";
        private readonly IViewEngine _viewEngine;

        public ViewViewComponentResult([NotNull] IViewEngine viewEngine, string viewName,
            ViewDataDictionary viewData)
        {
            _viewEngine = viewEngine;
            ViewName = viewName;
            ViewData = viewData;
        }

        public string ViewName { get; private set; }

        public ViewDataDictionary ViewData { get; private set; }

        /// <summary>
        /// Locates and renders a view specified by <paramref name="context"/>.
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

        /// <inheritdoc />
        public async Task ExecuteAsync([NotNull] ViewComponentContext context)
        {
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
                    ViewComponentConventions.GetComponentName(context.ComponentType),
                    ViewName ?? "Default");
            }

            var view = FindView(context.ViewContext, qualifiedViewName);

            var childViewContext = new ViewContext(
                context.ViewContext,
                view,
                ViewData ?? context.ViewContext.ViewData,
                context.Writer);

            using (view as IDisposable)
            {
                await view.RenderAsync(childViewContext);
            }
        }

        private IView FindView(ActionContext context, string viewName)
        {
            var result = _viewEngine.FindPartialView(context, viewName);
            if (!result.Success)
            {
                var locations = string.Empty;
                if (result.SearchedLocations != null)
                {
                    locations = Environment.NewLine +
                        string.Join(Environment.NewLine, result.SearchedLocations);
                }

                throw new InvalidOperationException(Resources.FormatViewEngine_ViewNotFound(viewName, locations));
            }

            return result.View;
        }
    }
}
