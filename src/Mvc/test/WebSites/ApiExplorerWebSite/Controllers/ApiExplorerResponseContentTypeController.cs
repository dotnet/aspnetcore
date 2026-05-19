// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace ApiExplorerWebSite;

[Route("ApiExplorerResponseContentType/[Action]")]
public class ApiExplorerResponseContentTypeController : Controller
{
    [HttpGet]
    public Product Unset()
    {
        return null;
    }

    [HttpGet]
    [Produces("application/json", "text/json")]
    public Product Specific()
    {
        return null;
    }

    [HttpGet]
    [Produces("application/hal+custom", "application/hal+json")]
    public Product WildcardMatch()
    {
        return null;
    }

    [HttpGet]
    [Produces("application/custom", "text/hal+bson")]
    public Product NoMatch()
    {
        return null;
    }
}
