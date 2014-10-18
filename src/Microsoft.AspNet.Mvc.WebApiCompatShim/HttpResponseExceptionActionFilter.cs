// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Web.Http;

namespace Microsoft.AspNet.Mvc.WebApiCompatShim
{
    /// <summary>
    /// An action filter which sets <see cref="ActionExecutedContext.Result"/> to an <see cref="ObjectResult"/> 
    /// if the exception type is <see cref="HttpResponseException"/>.
    /// This filter runs immediately after the action.
    /// </summary>
    public class HttpResponseExceptionActionFilter : IActionFilter, IOrderedFilter
    {
        // Return a high number by default so that it runs closest to the action.
        public int Order { get; set; } = int.MaxValue - 10;

        public void OnActionExecuting([NotNull] ActionExecutingContext context)
        {
        }

        public void OnActionExecuted([NotNull] ActionExecutedContext context)
        {
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