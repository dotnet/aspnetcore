// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace RoutingWebSite;

[Area("Admin")]
[Route("[area]/[controller]/[action]/{id?}")]
public class LG3Controller : Controller
{
    private readonly LinkGenerator _linkGenerator;

    public LG3Controller(LinkGenerator linkGenerator)
    {
        _linkGenerator = linkGenerator;
    }

    public void SomeAction()
    {
    }

    public string LinkInsideOfArea()
    {
        return _linkGenerator.GetPathByAction(HttpContext, action: nameof(SomeAction));
    }

    public string LinkInsideOfAreaFail()
    {
        // No ambient values - this will fail.
        return _linkGenerator.GetPathByAction(controller: "LG3", action: nameof(SomeAction));
    }

    public string LinkOutsideOfArea()
    {
        return _linkGenerator.GetPathByAction(
            HttpContext,
            action: nameof(SomeAction),
            controller: "LG1",
            values: new { area = "", });
    }
}
