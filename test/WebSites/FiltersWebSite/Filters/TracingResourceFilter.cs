// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FiltersWebSite
{
    public class TracingResourceFilter : Attribute, IResourceFilter
    {
        public TracingResourceFilter(string name)
        {
            Name = name;
        }
        
        public string Name { get; }

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
            if (!context.HttpContext.Response.HasStarted)
            {
                context.HttpContext.Response.Headers.Append(
                    "filters",
                    Name + " - OnResourceExecuted");
            }
        }

        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            context.HttpContext.Response.Headers.Append(
                "filters",
                Name + " - OnResourceExecuting");
        }
    }
}