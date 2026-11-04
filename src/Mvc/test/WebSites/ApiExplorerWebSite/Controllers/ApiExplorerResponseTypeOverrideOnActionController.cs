// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace ApiExplorerWebSite;

[Produces("application/json", Type = typeof(Product))]
[ProducesResponseType<ErrorInfo>(500)]
[Route("ApiExplorerResponseTypeOverrideOnAction")]
public class ApiExplorerResponseTypeOverrideOnActionController : Controller
{
    [HttpGet("Controller")]
    public void GetController()
    {
    }

    [HttpGet("Action")]
    [Produces<Customer>]
    [ProducesResponseType(typeof(ErrorInfoOverride), 500)] // overriding the type specified on the server
    public object GetAction()
    {
        return null;
    }

    [HttpGet("Action2")]
    [ProducesResponseType<Customer>(200, "text/plain")]
    public object GetActionWithContentTypeOverride()
    {
        return null;
    }
}

public class ErrorInfo
{
    public string Message { get; set; }
}

public class ErrorInfoOverride { }
