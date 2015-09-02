// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.ActionResults;
using Microsoft.AspNet.Mvc.Filters;
using MvcSample.Web.Models;

namespace MvcSample.Web.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class InspectResultPageAttribute : Attribute, IFilterFactory
    {
        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
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
