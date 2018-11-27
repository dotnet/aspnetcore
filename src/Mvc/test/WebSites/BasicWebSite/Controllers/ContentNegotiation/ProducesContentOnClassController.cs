// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicWebSite.Formatters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BasicWebSite.Controllers.ContentNegotiation
{
    [Produces("application/custom_ProducesContentOnClassController")]
    public class ProducesContentOnClassController : ProducesContentBaseController
    {
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            var result = context.Result as ObjectResult;
            if (result != null)
            {
                result.Formatters.Add(new CustomFormatter("application/custom_ProducesContentOnClassController"));
                result.Formatters.Add(
                    new CustomFormatter("application/custom_ProducesContentOnClassController_Action"));
            }

            base.OnActionExecuted(context);
        }

        // No Content type defined by the derived class action.
        public override string ReturnClassName()
        {
            // should be written using the content defined at base class's action.
            return "ProducesContentOnClassController";
        }

        public override string ReturnClassNameWithNoContentTypeOnAction()
        {
            // should be written using the content defined at derived class's class.
            return "ProducesContentOnClassController";
        }

        [Produces("application/custom_ProducesContentOnClassController_Action")]
        public override string ReturnClassNameContentTypeOnDerivedAction()
        {
            // should be written using the content defined at derived class's class.
            return "ProducesContentOnClassController";
        }
    }
}