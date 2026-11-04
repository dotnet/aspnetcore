// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace BasicWebSite.Controllers.ContentNegotiation;

public class NoContentDoNotTreatNullValueAsNoContentController : Controller
{
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        var result = context.Result as ObjectResult;
        if (result != null)
        {
            var noContentFormatter = new HttpNoContentOutputFormatter() { TreatNullValueAsNoContent = false };
            result.Formatters.Add(noContentFormatter);
        }

        base.OnActionExecuted(context);
    }

    public Task<string> ReturnTaskOfString_NullValue()
    {
        return Task.FromResult<string>(null);
    }

    public Task<object> ReturnTaskOfObject_NullValue()
    {
        return Task.FromResult<object>(null);
    }

    public object ReturnObject_NullValue()
    {
        return null;
    }

    public string ReturnString_NullValue()
    {
        return null;
    }
}
