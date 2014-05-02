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
