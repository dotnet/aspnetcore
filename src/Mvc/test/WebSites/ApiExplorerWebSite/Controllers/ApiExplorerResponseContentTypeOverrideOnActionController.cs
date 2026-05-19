// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace ApiExplorerWebSite;

[Produces("text/xml")]
[Route("ApiExplorerResponseContentTypeOverrideOnAction")]
public class ApiExplorerResponseContentTypeOverrideOnActionController : Controller
{
    [HttpGet("Controller")]
    public Product GetController()
    {
        return null;
    }

    [HttpGet("Action")]
    [Produces("application/json")]
    public Product GetAction()
    {
        return null;
    }
}
