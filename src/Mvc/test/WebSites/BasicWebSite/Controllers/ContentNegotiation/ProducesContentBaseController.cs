// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicWebSite.Formatters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BasicWebSite.Controllers.ContentNegotiation
{
    [Produces("application/custom_ProducesContentBaseController")]
    public class ProducesContentBaseController : Controller
    {
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            var result = context.Result as ObjectResult;
            if (result != null)
            {
                result.Formatters.Add(new PlainTextFormatter());
                result.Formatters.Add(new CustomFormatter("application/custom_ProducesContentBaseController"));
                result.Formatters.Add(new CustomFormatter("application/custom_ProducesContentBaseController_Action"));
            }

            base.OnActionExecuted(context);
        }

        [Produces("application/custom_ProducesContentBaseController_Action")]
        public virtual string ReturnClassName()
        {
            // Should be written using the action's content type. Overriding the one at the class.
            return "ProducesContentBaseController";
        }

        public virtual string ReturnClassNameWithNoContentTypeOnAction()
        {
            // Should be written using the action's content type. Overriding the one at the class.
            return "ProducesContentBaseController";
        }

        public virtual string ReturnClassNameContentTypeOnDerivedAction()
        {
            return "ProducesContentBaseController";
        }
    }
}