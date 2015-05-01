// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;

namespace MvcSample.Web.Filters
{
    public class AgeEnhancerFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            object age = null;

            var controller = context.Controller as FiltersController;

            if (controller != null)
            {
                controller.CustomUser.Log += "Age Enhanced!" + Environment.NewLine;
            }

            if (context.ActionArguments.TryGetValue("age", out age))
            {
                if (age is int)
                {
                    var intAge = (int)age;

                    if (intAge < 21)
                    {
                        intAge += 5;
                    }
                    else if (intAge > 30)
                    {
                        intAge = 29;
                    }

                    context.ActionArguments["age"] = intAge;
                }
            }
        }
    }
}
