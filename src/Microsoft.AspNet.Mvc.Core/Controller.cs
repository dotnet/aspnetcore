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

using System;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc
{
    public class Controller : IActionFilter, IAsyncActionFilter
    {
        private DynamicViewData _viewBag;

        public void Initialize(IActionResultHelper actionResultHelper)
        {
            Result = actionResultHelper;
        }

        public HttpContext Context
        {
            get
            {
                return ActionContext.HttpContext;
            }
        }

        public ModelStateDictionary ModelState
        {
            get
            {
                return ViewData.ModelState;
            }
        }

        public ActionContext ActionContext { get; set; }

        public IActionResultHelper Result { get; private set; }

        public IUrlHelper Url { get; set; }
        public IPrincipal User
        {
            get
            {
                if (Context == null)
                {
                    return null;
                }

                return Context.User;
            }
        }

        public ViewDataDictionary ViewData { get; set; }

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

        public ViewResult View()
        {
            return View(view: null);
        }

        public ViewResult View(string view)
        {
            return View(view, model: null);
        }

        // TODO #110: May need <TModel> here and in the overload below.
        public ViewResult View(object model)
        {
            return View(view: null, model: model);
        }

        public ViewResult View(string view, object model)
        {
            // Do not override ViewData.Model unless passed a non-null value.
            if (model != null)
            {
                ViewData.Model = model;
            }

            return Result.View(view, ViewData);
        }

        public ContentResult Content(string content)
        {
            return Content(content, contentType: null);
        }

        public ContentResult Content(string content, string contentType)
        {
            return Content(content, contentType, contentEncoding: null);
        }

        public ContentResult Content(string content, string contentType, Encoding contentEncoding)
        {
            return Result.Content(content, contentType, contentEncoding);
        }

        public JsonResult Json(object value)
        {
            return Result.Json(value);
        }

        public virtual RedirectResult Redirect(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, "url");
            }

            return new RedirectResult(url);
        }

        public virtual RedirectResult RedirectPermanent(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, "url");
            }

            return new RedirectResult(url, permanent: true);
        }

        public RedirectToActionResult RedirectToAction(string actionName)
        {
            return RedirectToAction(actionName, routeValues: null);
        }

        public RedirectToActionResult RedirectToAction(string actionName, object routeValues)
        {
            return RedirectToAction(actionName, controllerName: null, routeValues: routeValues);
        }

        public RedirectToActionResult RedirectToAction(string actionName, string controllerName)
        {
            return RedirectToAction(actionName, controllerName, routeValues: null);
        }

        public RedirectToActionResult RedirectToAction(string actionName, string controllerName,
                                        object routeValues)
        {
            return new RedirectToActionResult(Url, actionName, controllerName,
                                                TypeHelper.ObjectToDictionary(routeValues));
        }

        public RedirectToActionResult RedirectToActionPermanent(string actionName)
        {
            return RedirectToActionPermanent(actionName, routeValues: null);
        }

        public RedirectToActionResult RedirectToActionPermanent(string actionName, object routeValues)
        {
            return RedirectToActionPermanent(actionName, controllerName: null, routeValues: routeValues);
        }

        public RedirectToActionResult RedirectToActionPermanent(string actionName, string controllerName)
        {
            return RedirectToActionPermanent(actionName, controllerName, routeValues: null);
        }

        public RedirectToActionResult RedirectToActionPermanent(string actionName, string controllerName,
                                        object routeValues)
        {
            return new RedirectToActionResult(Url, actionName, controllerName,
                                                TypeHelper.ObjectToDictionary(routeValues), permanent: true);
        }

        public RedirectToRouteResult RedirectToRoute(string routeName)
        {
            return RedirectToRoute(routeName, routeValues: null);
        }

        public RedirectToRouteResult RedirectToRoute(object routeValues)
        {
            return RedirectToRoute(routeName: null, routeValues: routeValues);
        }

        public RedirectToRouteResult RedirectToRoute(string routeName, object routeValues)
        {
            return new RedirectToRouteResult(Url, routeName, routeValues);
        }

        public RedirectToRouteResult RedirectToRoutePermanent(string routeName)
        {
            return RedirectToRoutePermanent(routeName, routeValues: null);
        }

        public RedirectToRouteResult RedirectToRoutePermanent(object routeValues)
        {
            return RedirectToRoutePermanent(routeName: null, routeValues: routeValues);
        }

        public RedirectToRouteResult RedirectToRoutePermanent(string routeName, object routeValues)
        {
            return new RedirectToRouteResult(Url, routeName, routeValues, permanent: true);
        }

        public virtual void OnActionExecuting([NotNull] ActionExecutingContext context)
        {
        }

        public virtual void OnActionExecuted([NotNull] ActionExecutedContext context)
        {
        }

        public virtual async Task OnActionExecutionAsync(
            [NotNull] ActionExecutingContext context, 
            [NotNull] ActionExecutionDelegate next)
        {
            OnActionExecuting(context);
            if (context.Result == null)
            {
                OnActionExecuted(await next());
            }
        }
    }
}
