// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicWebSite.Formatters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BasicWebSite.Controllers.ContentNegotiation;

public class NoProducesContentOnClassController : ProducesContentBaseController
{
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        var result = context.Result as ObjectResult;
        if (result != null)
        {
            result.Formatters.Add(new CustomFormatter("application/custom_NoProducesContentOnClassController_Action"));
        }

        base.OnActionExecuted(context);
    }

    [Produces("application/custom_NoProducesContentOnClassController_Action")]
    public override string ReturnClassName()
    {
        // should be written using the formatter provided by this action and not the base action.
        return "NoProducesContentOnClassController";
    }
}
