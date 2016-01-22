// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebApiCompatShimWebSite
{
    public class HttpResponseExceptionController : ApiController
    {
        [HttpGet]
        public object ThrowsHttpResponseExceptionWithHttpStatusCode()
        {
            throw new HttpResponseException(HttpStatusCode.BadRequest);
        }

        [HttpGet]
        public object ThrowsHttpResponseExceptionWithHttpResponseMessage(string message)
        {
            var httpResponse = new HttpResponseMessage();
            httpResponse.Content = new StringContent(message);
            throw new HttpResponseException(httpResponse);
        }

        [TestActionFilter]
        [HttpGet]
        public object ThrowsHttpResponseExceptionEnsureGlobalFilterRunsLast()
        {
            throw new HttpResponseException(HttpStatusCode.BadRequest);
        }

        // Runs before the HttpResponseExceptionActionFilter's OnActionExecuted.
        [TestActionFilter(Order = int.MaxValue)]
        [HttpGet]
        public object ThrowsHttpResponseExceptionInjectAFilterToHandleHttpResponseException()
        {
            throw new HttpResponseException(HttpStatusCode.BadRequest);
        }
    }

    public class TestActionFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (!context.ExceptionHandled)
            {
                var httpResponseException = context.Exception as HttpResponseException;
                if (httpResponseException != null)
                {
                    context.Result = new NoContentResult();
                    context.ExceptionHandled = true;

                    // Null it out so that next filter do not handle it.
                    context.Exception = null;
                }
            }
        }
    }
}