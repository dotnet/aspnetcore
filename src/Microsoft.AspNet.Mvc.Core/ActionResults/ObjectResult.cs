// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    public class ObjectResult : ActionResult
    {
        public object Value { get; set; }

        public ObjectResult(object value)
        {
            Value = value;
        }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            ActionResult result;
            var actionReturnString = Value as string;
            if (actionReturnString != null)
            {
                result  = new ContentResult
                {
                    ContentType = "text/plain",
                    Content = actionReturnString,
                };
            }
            else
            {
                result = new JsonResult(Value);
            }

            await result.ExecuteResultAsync(context);
        }
    }
}