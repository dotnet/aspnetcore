// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite;

public class RequestIdViewComponent : ViewComponent
{
    public RequestIdViewComponent(RequestIdService requestIdService)
    {
        RequestIdService = requestIdService;
    }

    private RequestIdService RequestIdService { get; }

    public IViewComponentResult Invoke()
    {
        return Content(RequestIdService.RequestId);
    }
}
