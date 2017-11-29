// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// A base class for an MVC controller with view support.
    /// </summary>
    public abstract class Controller : ControllerBase, IActionFilter, IAsyncActionFilter, IDisposable
    {
        private ITempDataDictionary _tempData;
        private DynamicViewData _viewBag;
        private ViewDataDictionary _viewData;

        /// <summary>
        /// Gets or sets <see cref="ViewDataDictionary"/> used by <see cref="ViewResult"/> and <see cref="ViewBag"/>.
        /// </summary>
        /// <remarks>
        /// By default, this property is activated when <see cref="Controllers.IControllerActivator"/> activates
        ///  controllers. However, when controllers are directly instantiated in user code, this property is
        /// initialized with <see cref="EmptyModelMetadataProvider"/>.
        /// </remarks>
        [ViewDataDictionary]
        public ViewDataDictionary ViewData
        {
            get
            {
                if (_viewData == null)
                {
                    // This should run only for the controller unit test scenarios
                    _viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), ControllerContext.ModelState);
                }

                return _viewData;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(ViewData));
                }

                _viewData = value;
            }
        }

        /// <summary>
        /// Gets or sets <see cref="ITempDataDictionary"/> used by <see cref="ViewResult"/>.
        /// </summary>
        public ITempDataDictionary TempData
        {
            get
            {
                if (_tempData == null)
                {
                    var factory = HttpContext?.RequestServices?.GetRequiredService<ITempDataDictionaryFactory>();
                    _tempData = factory?.GetTempData(HttpContext);
                }

                return _tempData;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _tempData = value;
            }
        }

        /// <summary>
        /// Gets the dynamic view bag.
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
        /// Creates a <see cref="ViewResult"/> object that renders a view to the response.
        /// </summary>
        /// <returns>The created <see cref="ViewResult"/> object for the response.</returns>
        [NonAction]
        public virtual ViewResult View()
        {
            return View(viewName: null);
        }

        /// <summary>
        /// Creates a <see cref="ViewResult"/> object by specifying a <paramref name="viewName"/>.
        /// </summary>
        /// <param name="viewName">The name or path of the view that is rendered to the response.</param>
        /// <returns>The created <see cref="ViewResult"/> object for the response.</returns>
        [NonAction]
        public virtual ViewResult View(string viewName)
        {
            return View(viewName, model: ViewData.Model);
        }

        /// <summary>
        /// Creates a <see cref="ViewResult"/> object by specifying a <paramref name="model"/>
        /// to be rendered by the view.
        /// </summary>
        /// <param name="model">The model that is rendered by the view.</param>
        /// <returns>The created <see cref="ViewResult"/> object for the response.</returns>
        [NonAction]
        public virtual ViewResult View(object model)
        {
            return View(viewName: null, model: model);
        }

        /// <summary>
        /// Creates a <see cref="ViewResult"/> object by specifying a <paramref name="viewName"/>
        /// and the <paramref name="model"/> to be rendered by the view.
        /// </summary>
        /// <param name="viewName">The name or path of the view that is rendered to the response.</param>
        /// <param name="model">The model that is rendered by the view.</param>
        /// <returns>The created <see cref="ViewResult"/> object for the response.</returns>
        [NonAction]
        public virtual ViewResult View(string viewName, object model)
        {
            ViewData.Model = model;

            return new ViewResult()
            {
                ViewName = viewName,
                ViewData = ViewData,
                TempData = TempData
            };
        }

        /// <summary>
        /// Creates a <see cref="PartialViewResult"/> object that renders a partial view to the response.
        /// </summary>
        /// <returns>The created <see cref="PartialViewResult"/> object for the response.</returns>
        [NonAction]
        public virtual PartialViewResult PartialView()
        {
            return PartialView(viewName: null);
        }

        /// <summary>
        /// Creates a <see cref="PartialViewResult"/> object by specifying a <paramref name="viewName"/>.
        /// </summary>
        /// <param name="viewName">The name or path of the partial view that is rendered to the response.</param>
        /// <returns>The created <see cref="PartialViewResult"/> object for the response.</returns>
        [NonAction]
        public virtual PartialViewResult PartialView(string viewName)
        {
            return PartialView(viewName, model: ViewData.Model);
        }

        /// <summary>
        /// Creates a <see cref="PartialViewResult"/> object by specifying a <paramref name="model"/>
        /// to be rendered by the partial view.
        /// </summary>
        /// <param name="model">The model that is rendered by the partial view.</param>
        /// <returns>The created <see cref="PartialViewResult"/> object for the response.</returns>
        [NonAction]
        public virtual PartialViewResult PartialView(object model)
        {
            return PartialView(viewName: null, model: model);
        }

        /// <summary>
        /// Creates a <see cref="PartialViewResult"/> object by specifying a <paramref name="viewName"/>
        /// and the <paramref name="model"/> to be rendered by the partial view.
        /// </summary>
        /// <param name="viewName">The name or path of the partial view that is rendered to the response.</param>
        /// <param name="model">The model that is rendered by the partial view.</param>
        /// <returns>The created <see cref="PartialViewResult"/> object for the response.</returns>
        [NonAction]
        public virtual PartialViewResult PartialView(string viewName, object model)
        {
            ViewData.Model = model;

            return new PartialViewResult()
            {
                ViewName = viewName,
                ViewData = ViewData,
                TempData = TempData
            };
        }

        /// <summary>
        /// Creates a <see cref="ViewComponentResult"/> by specifying the name of a view component to render.
        /// </summary>
        /// <param name="componentName">
        /// The view component name. Can be a view component
        /// <see cref="ViewComponents.ViewComponentDescriptor.ShortName"/> or
        /// <see cref="ViewComponents.ViewComponentDescriptor.FullName"/>.</param>
        /// <returns>The created <see cref="ViewComponentResult"/> object for the response.</returns>
        [NonAction]
        public virtual ViewComponentResult ViewComponent(string componentName)
        {
            return ViewComponent(componentName, arguments: null);
        }

        /// <summary>
        /// Creates a <see cref="ViewComponentResult"/> by specifying the <see cref="Type"/> of a view component to
        /// render.
        /// </summary>
        /// <param name="componentType">The view component <see cref="Type"/>.</param>
        /// <returns>The created <see cref="ViewComponentResult"/> object for the response.</returns>
        [NonAction]
        public virtual ViewComponentResult ViewComponent(Type componentType)
        {
            return ViewComponent(componentType, arguments: null);
        }

        /// <summary>
        /// Creates a <see cref="ViewComponentResult"/> by specifying the name of a view component to render.
        /// </summary>
        /// <param name="componentName">
        /// The view component name. Can be a view component
        /// <see cref="ViewComponents.ViewComponentDescriptor.ShortName"/> or
        /// <see cref="ViewComponents.ViewComponentDescriptor.FullName"/>.</param>
        /// <param name="arguments">
        /// An <see cref="object"/> with properties representing arguments to be passed to the invoked view component
        /// method. Alternatively, an <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance
        /// containing the invocation arguments.
        /// </param>
        /// <returns>The created <see cref="ViewComponentResult"/> object for the response.</returns>
        [NonAction]
        public virtual ViewComponentResult ViewComponent(string componentName, object arguments)
        {
            return new ViewComponentResult
            {
                ViewComponentName = componentName,
                Arguments = arguments,
                ViewData = ViewData,
                TempData = TempData
            };
        }

        /// <summary>
        /// Creates a <see cref="ViewComponentResult"/> by specifying the <see cref="Type"/> of a view component to
        /// render.
        /// </summary>
        /// <param name="componentType">The view component <see cref="Type"/>.</param>
        /// <param name="arguments">
        /// An <see cref="object"/> with properties representing arguments to be passed to the invoked view component
        /// method. Alternatively, an <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance
        /// containing the invocation arguments.
        /// </param>
        /// <returns>The created <see cref="ViewComponentResult"/> object for the response.</returns>
        [NonAction]
        public virtual ViewComponentResult ViewComponent(Type componentType, object arguments)
        {
            return new ViewComponentResult
            {
                ViewComponentType = componentType,
                Arguments = arguments,
                ViewData = ViewData,
                TempData = TempData
            };
        }

        /// <summary>
        /// Creates a <see cref="JsonResult"/> object that serializes the specified <paramref name="data"/> object
        /// to JSON.
        /// </summary>
        /// <param name="data">The object to serialize.</param>
        /// <returns>The created <see cref="JsonResult"/> that serializes the specified <paramref name="data"/>
        /// to JSON format for the response.</returns>
        [NonAction]
        public virtual JsonResult Json(object data)
        {
            return new JsonResult(data);
        }

        /// <summary>
        /// Creates a <see cref="JsonResult"/> object that serializes the specified <paramref name="data"/> object
        /// to JSON.
        /// </summary>
        /// <param name="data">The object to serialize.</param>
        /// <param name="serializerSettings">The <see cref="JsonSerializerSettings"/> to be used by
        /// the formatter.</param>
        /// <returns>The created <see cref="JsonResult"/> that serializes the specified <paramref name="data"/>
        /// as JSON format for the response.</returns>
        /// <remarks>Callers should cache an instance of <see cref="JsonSerializerSettings"/> to avoid
        /// recreating cached data with each call.</remarks>
        [NonAction]
        public virtual JsonResult Json(object data, JsonSerializerSettings serializerSettings)
        {
            if (serializerSettings == null)
            {
                throw new ArgumentNullException(nameof(serializerSettings));
            }

            return new JsonResult(data, serializerSettings);
        }

        /// <summary>
        /// Called before the action method is invoked.
        /// </summary>
        /// <param name="context">The action executing context.</param>
        [NonAction]
        public virtual void OnActionExecuting(ActionExecutingContext context)
        {
        }

        /// <summary>
        /// Called after the action method is invoked.
        /// </summary>
        /// <param name="context">The action executed context.</param>
        [NonAction]
        public virtual void OnActionExecuted(ActionExecutedContext context)
        {
        }

        /// <summary>
        /// Called before the action method is invoked.
        /// </summary>
        /// <param name="context">The action executing context.</param>
        /// <param name="next">The <see cref="ActionExecutionDelegate"/> to execute. Invoke this delegate in the body
        /// of <see cref="OnActionExecutionAsync" /> to continue execution of the action.</param>
        /// <returns>A <see cref="Task"/> instance.</returns>
        [NonAction]
        public virtual async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            OnActionExecuting(context);
            if (context.Result == null)
            {
                OnActionExecuted(await next());
            }
        }

        /// <inheritdoc />
        public void Dispose() => Dispose(disposing: true);

        /// <summary>
        /// Releases all resources currently used by this <see cref="Controller"/> instance.
        /// </summary>
        /// <param name="disposing"><c>true</c> if this method is being invoked by the <see cref="Dispose()"/> method,
        /// otherwise <c>false</c>.</param>
        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
