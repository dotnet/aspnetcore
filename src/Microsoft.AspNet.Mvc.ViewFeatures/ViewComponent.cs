// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Principal;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Mvc.ViewComponents;
using Microsoft.AspNet.Mvc.ViewEngines;
using Microsoft.AspNet.Mvc.ViewFeatures;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// A base class for view components.
    /// </summary>
    [ViewComponent]
    public abstract class ViewComponent
    {
        private IUrlHelper _url;
        private dynamic _viewBag;
        private ViewComponentContext _viewComponentContext;
        private ICompositeViewEngine _viewEngine;

        /// <summary>
        /// Gets the <see cref="Http.HttpContext"/>.
        /// </summary>
        public HttpContext HttpContext
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
        public IUrlHelper Url
        {
            get
            {
                if (_url == null)
                {
                    // May be null in unit-testing scenarios.
                    var services = ViewComponentContext.ViewContext?.HttpContext?.RequestServices;
                    var factory = services?.GetRequiredService<IUrlHelperFactory>();
                    _url = factory?.GetUrlHelper(ViewComponentContext.ViewContext);
                }

                return _url;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _url = value;
            }
        }

        [ViewComponentContext]
        public ViewComponentContext ViewComponentContext
        {
            get
            {
                // This should run only for the ViewComponent unit test scenarios.
                if (_viewComponentContext == null)
                {
                    _viewComponentContext = new ViewComponentContext();
                }

                return _viewComponentContext;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _viewComponentContext = value;
            }
        }

        /// <summary>
        /// Gets the <see cref="ViewContext"/>.
        /// </summary>
        public ViewContext ViewContext
        {
            get
            {
                return ViewComponentContext.ViewContext;
            }
        }

        /// <summary>
        /// Gets the <see cref="ViewDataDictionary"/>.
        /// </summary>
        public ViewDataDictionary ViewData
        {
            get
            {
                return ViewComponentContext.ViewData;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="ICompositeViewEngine"/>.
        /// </summary>
        public ICompositeViewEngine ViewEngine
        {
            get
            {
                if (_viewEngine == null)
                {
                    // May be null in unit-testing scenarios.
                    var services = ViewComponentContext.ViewContext?.HttpContext?.RequestServices;
                    _viewEngine = services?.GetRequiredService<ICompositeViewEngine>();
                }

                return _viewEngine;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _viewEngine = value;
            }
        }

        /// <summary>
        /// Returns a result which will render HTML encoded text.
        /// </summary>
        /// <param name="content">The content, will be HTML encoded before output.</param>
        /// <returns>A <see cref="ContentViewComponentResult"/>.</returns>
        public ContentViewComponentResult Content(string content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            return new ContentViewComponentResult(content);
        }

        /// <summary>
        /// Returns a result which will render JSON text.
        /// </summary>
        /// <param name="value">The value to output in JSON text.</param>
        /// <returns>A <see cref="JsonViewComponentResult"/>.</returns>
        public JsonViewComponentResult Json(object value)
        {
            return new JsonViewComponentResult(value);
        }

        /// <summary>
        /// Returns a result which will render JSON text.
        /// </summary>
        /// <param name="value">The value to output in JSON text.</param>
        /// <param name="serializerSettings">The <see cref="JsonSerializerSettings"/> to be used by
        /// the formatter.</param>
        /// <returns>A <see cref="JsonViewComponentResult"/>.</returns>
        /// <remarks>Callers should cache an instance of <see cref="JsonSerializerSettings"/> to avoid
        /// recreating cached data with each call.</remarks>
        public JsonViewComponentResult Json(object value, JsonSerializerSettings serializerSettings)
        {
            if (serializerSettings == null)
            {
                throw new ArgumentNullException(nameof(serializerSettings));
            }

            return new JsonViewComponentResult(value, serializerSettings);
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
