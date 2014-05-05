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

using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNet.Mvc;

namespace MvcSample.Web
{
    public class SimplePocoController : IActionFilter, IResultFilter
    {
        private Stopwatch _timer;

        public string Index()
        {
            return "Hello world";
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            _timer = Stopwatch.StartNew();
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            var time = _timer.ElapsedMilliseconds;
            context.HttpContext.Response.Headers.Add(
                "ActionElapsedTime", 
                new string[] { time.ToString(CultureInfo.InvariantCulture) + " ms" });
        }

        public void OnResultExecuting(ResultExecutingContext context)
        {
            _timer = Stopwatch.StartNew();
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {
            var time = _timer.ElapsedMilliseconds;
            context.HttpContext.Response.Headers.Add(
                "ResultElapsedTime", 
                new string[] { time.ToString(CultureInfo.InvariantCulture) + " ms" });
        }
    }
}
