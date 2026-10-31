// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicWebSite.Formatters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BasicWebSite.Controllers.ContentNegotiation;

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
