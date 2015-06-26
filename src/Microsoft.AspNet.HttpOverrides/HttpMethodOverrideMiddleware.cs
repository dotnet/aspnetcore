// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.HttpOverrides
{
    public class HttpMethodOverrideMiddleware
    {
        private const string xHttpMethodOverride = "X-Http-Method-Override";
        private readonly RequestDelegate _next;
        private readonly string _formFieldName;

        public HttpMethodOverrideMiddleware(RequestDelegate next, string formFieldName = null)
        {
            _next = next;
            _formFieldName = formFieldName;
        }

        public async Task Invoke(HttpContext context)
        {
            if (string.Equals(context.Request.Method,"POST", StringComparison.OrdinalIgnoreCase))
            {
                if (_formFieldName != null)
                {
                    if (context.Request.HasFormContentType)
                    {
                        var form = await context.Request.ReadFormAsync();
                        var methodType = form[_formFieldName];
                        if (!string.IsNullOrEmpty(methodType))
                        {
                            context.Request.Method = methodType;
                        }
                    }
                }
                else
                {
                    var xHttpMethodOverrideValue = context.Request.Headers.Get(xHttpMethodOverride);
                    if (!string.IsNullOrEmpty(xHttpMethodOverrideValue))
                    {
                        context.Request.Method = xHttpMethodOverrideValue;
                    }
                }
            }
            await _next(context);
        }
    }
}
