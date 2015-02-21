// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Principal;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// A base class for view components.
    /// </summary>
    [ViewComponent]
    public abstract class ViewComponent
    {
        private dynamic _viewBag;

        /// <summary>
        /// Gets the <see cref="HttpContext"/>.
        /// </summary>
        public HttpContext Context
        {
            get
            {
                return ViewContext?.HttpContext;
            }
        }

        /// <summary>
        /// Gets the <see cref="HttpRequest"/>.
        /// </summary>
        public HttpRequest Request
        {
            get
            {
                return ViewContext?.HttpContext?.Request;
            }
        }

        /// <summary>
        /// Gets the <see cref="IPrincipal"/> for the current user.
        /// </summary>
        public IPrincipal User
        {
            get
            {
                return ViewContext?.HttpContext?.User;
            }
        }

        /// <summary>
        /// Gets the <see cref="RouteData"/> for the current request.
        /// </summary>
        public RouteData RouteData
        {
            get
            {
                return ViewContext?.RouteData;
            }
        }

        /// <summary>
        /// Gets the view bag.
        /// </summary>
        public dynamic ViewBag
        {
            get
            {
                if (_viewBag == null)
                {
                    _viewBag = new DynamicViewData(() => ViewData);
                }

                return _viewBag;
            }
        }

        /// <summary>
        /// Gets the <see cref="ModelStateDictionary"/>.
        /// </summary>
        public ModelStateDictionary ModelState
        {
            get
            {
                return ViewData?.ModelState;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="IUrlHelper"/>.
        /// </summary>
        [Activate]
        public IUrlHelper Url { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ViewContext"/>.
        /// </summary>
        [Activate]
        public ViewContext ViewContext { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ViewDataDictionary"/>.
        /// </summary>
        [Activate]
        public ViewDataDictionary ViewData { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ICompositeViewEngine"/>.
        /// </summary>
        [Activate]
        public ICompositeViewEngine ViewEngine { get; set; }

        /// <summary>
        /// Returns a result which will render HTML encoded text.
        /// </summary>
        /// <param name="content">The content, will be HTML encoded before output.</param>
        /// <returns>A <see cref="ContentViewComponentResult"/>.</returns>
        public ContentViewComponentResult Content([NotNull] string content)
        {
            return new ContentViewComponentResult(content);
        }

        /// <summary>
        /// Returns a result which will render JSON text.
        /// </summary>
        /// <param name="value">The value to output in JSON text.</param>
        /// <returns>A <see cref="JsonViewComponentResult"/>.</returns>
        public JsonViewComponentResult Json([NotNull] object value)
        {
            return new JsonViewComponentResult(value);
        }

        /// <summary>
        /// Returns a result which will render the partial view with name <c>&quot;Default&quot;</c>.
        /// </summary>
        /// <returns>A <see cref="ViewViewComponentResult"/>.</returns>
        public ViewViewComponentResult View()
        {
            return View<object>(null, null);
        }

        /// <summary>
        /// Returns a result which will render the partial view with name <paramref name="viewName"/>.
        /// </summary>
        /// <param name="viewName">The name of the partial view to render.</param>
        /// <returns>A <see cref="ViewViewComponentResult"/>.</returns>
        public ViewViewComponentResult View(string viewName)
        {
            return View<object>(viewName, null);
        }

        /// <summary>
        /// Returns a result which will render the partial view with name <c>&quot;Default&quot;</c>.
        /// </summary>
        /// <param name="model">The model object for the view.</param>
        /// <returns>A <see cref="ViewViewComponentResult"/>.</returns>
        public ViewViewComponentResult View<TModel>(TModel model)
        {
            return View(null, model);
        }

        /// <summary>
        /// Returns a result which will render the partial view with name <paramref name="viewName"/>.
        /// </summary>
        /// <param name="viewName">The name of the partial view to render.</param>
        /// <param name="model">The model object for the view.</param>
        /// <returns>A <see cref="ViewViewComponentResult"/>.</returns>
        public ViewViewComponentResult View<TModel>(string viewName, TModel model)
        {
            var viewData = new ViewDataDictionary<TModel>(ViewData, model);
            return new ViewViewComponentResult
            {
                ViewEngine = ViewEngine,
                ViewName = viewName,
                ViewData = viewData
            };
        }
    }
}
