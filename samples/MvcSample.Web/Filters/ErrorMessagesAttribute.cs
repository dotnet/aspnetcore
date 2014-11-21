// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace MvcSample.Web
{
    public class ErrorMessagesAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception != null && !context.ExceptionHandled)
            {
                context.ExceptionHandled = true;

                context.Result = new ContentResult
                {
                    ContentType = "text/plain",
                    Content = "Boom " + context.Exception.Message
                };
            }
        }
    }
}