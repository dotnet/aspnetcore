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
using Microsoft.AspNet.Mvc;
using MvcSample.Web.Models;

namespace MvcSample.Web.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class InspectResultPageAttribute : Attribute, IFilterFactory
    {
        public IFilter CreateInstance(IServiceProvider serviceProvider)
        {
            return new InspectResultPageFilter();
        }

        private class InspectResultPageFilter : IResultFilter
        {
            public void OnResultExecuting(ResultExecutingContext context)
            {
                var viewResult = context.Result as ViewResult;

                if (viewResult != null)
                {
                    var user = viewResult.ViewData.Model as User;

                    if (user != null)
                    {
                        user.Name += "**" + user.Name + "**";
                    }
                }
            }

            public void OnResultExecuted(ResultExecutedContext context)
            {
            }
        }
    }
}
