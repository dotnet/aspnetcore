// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace ControllersFromServicesClassLibrary;

public class ConstructorInjectionController
{
    public ConstructorInjectionController(IUrlHelperFactory urlHelperFactory, QueryValueService queryService)
    {
        UrlHelperFactory = urlHelperFactory;
        QueryService = queryService;
    }

    [ActionContext]
    public ActionContext ActionContext { get; set; }

    private QueryValueService QueryService { get; }

    private IUrlHelperFactory UrlHelperFactory { get; }

    [HttpGet("/constructorinjection")]
    public IActionResult Index()
    {
        var urlHelper = UrlHelperFactory.GetUrlHelper(ActionContext);

        var content = string.Join(
            " ",
            urlHelper.Action(),
            QueryService.GetValue(),
            ActionContext.HttpContext.Request.Headers["Test-Header"]);

        return new ContentResult { Content = content };
    }
}
