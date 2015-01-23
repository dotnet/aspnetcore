// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ConnegWebSite
{
    [Produces("application/FormatFilterController")]
    public class FormatFilterController : Controller
    {
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            var result = context.Result as ObjectResult;
            if (result != null)
            {
                result.Formatters.Add(new CustomFormatter("application/FormatFilterController"));
            }

            base.OnActionExecuted(context);
        }

        [FormatFilter]
        public string MethodWithFormatFilter()
        {
            return "MethodWithFormatFilter";
        }
    }
}