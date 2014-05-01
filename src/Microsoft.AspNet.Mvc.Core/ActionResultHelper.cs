// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc
{
    public class ActionResultHelper : IActionResultHelper
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IViewEngine _viewEngine;

        public ActionResultHelper(IServiceProvider serviceProvider, IViewEngine viewEngine)
        {
            _serviceProvider = serviceProvider;
            _viewEngine = viewEngine;
        }

        public ContentResult Content(string value, string contentType, Encoding contentEncoding)
        {
            var result = new ContentResult
            {
                Content = value,
            };

            if (contentType != null)
            {
                result.Content = contentType;
            }

            if (contentEncoding != null)
            {
                result.ContentEncoding = contentEncoding;
            }

            return result;
        }

        public JsonResult Json(object value)
        {
            return new JsonResult(value);
        }

        public ViewResult View(string view, ViewDataDictionary viewData)
        {
            return new ViewResult(_serviceProvider, _viewEngine)
            {
                ViewName = view,
                ViewData = viewData
            };
        }
    }
}
