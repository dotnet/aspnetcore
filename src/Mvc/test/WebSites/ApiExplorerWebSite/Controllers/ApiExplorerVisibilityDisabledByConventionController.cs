// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace ApiExplorerWebSite;

[Route("ApiExplorerVisibilityDisabledByConvention")]
public class ApiExplorerVisibilityDisabledByConventionController : Controller
{
    [HttpGet]
    public void Get()
    {
    }
}
