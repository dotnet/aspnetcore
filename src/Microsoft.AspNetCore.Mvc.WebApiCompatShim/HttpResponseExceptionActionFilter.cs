// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Web.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.WebApiCompatShim
{
    /// <summary>
    /// An action filter that sets <see cref="ActionExecutedContext.Result"/> to an <see cref="ObjectResult"/>
    /// if the exception type is <see cref="HttpResponseException"/>.
    /// This filter runs immediately after the action.
    /// </summary>
    public class HttpResponseExceptionActionFilter : IActionFilter, IOrderedFilter
    {
        /// <inheritdoc />
        // Return a high number by default so that it runs closest to the action.
        public int Order { get; set; } = int.MaxValue - 10;

        /// <inheritdoc />
        public void OnActionExecuting(ActionExecutingContext context)
        {
        }

        /// <inheritdoc />
        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var httpResponseException = context.Exception as HttpResponseException;
            if (httpResponseException != null)
            {
                var request = context.HttpContext.GetHttpRequestMessage();
                var response = httpResponseException.Response;

                if (response != null && response.RequestMessage == null)
                {
                    response.RequestMessage = request;
                }

                var objectResult = new ObjectResult(response)
                {
                    DeclaredType = typeof(HttpResponseMessage)
                };

                context.Result = objectResult;

                // Its marked as handled as in webapi because an HttpResponseException
                // was considered as a 'success' response.
                context.ExceptionHandled = true;
            }
        }
    }
}