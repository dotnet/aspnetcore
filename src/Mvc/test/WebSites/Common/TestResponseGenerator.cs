// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Microsoft.AspNetCore.Mvc;

// Generates a response based on the expected URL and action context
public class TestResponseGenerator
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TestResponseGenerator(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
        if (_httpContextAccessor.HttpContext == null)
        {
            throw new InvalidOperationException("HttpContext should not be null here.");
        }
    }

    public ActionResult Generate(params string[] expectedUrls)
    {
        var httpContext = _httpContextAccessor.HttpContext!;
        var link = (string)null;
        var query = httpContext.Request.Query;
        if (query.ContainsKey("link"))
        {
            var values = query
                .Where(kvp => kvp.Key != "link" && kvp.Key != "link_action" && kvp.Key != "link_controller")
                .ToDictionary(kvp => kvp.Key.Substring("link_".Length), kvp => (object)kvp.Value[0]);

            var urlHelper = GetUrlHelper(httpContext);
            link = urlHelper.Action(query["link_action"], query["link_controller"], values);
        }

        var endpoint = httpContext.GetEndpoint();
        var actionDescriptor = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();
        var attributeRoutingInfo = actionDescriptor?.AttributeRouteInfo;

        return new OkObjectResult(new
        {
            expectedUrls = expectedUrls,
            actualUrl = httpContext.Request.Path.Value,
            routeName = attributeRoutingInfo?.Name,
            routeValues = new Dictionary<string, object>(httpContext.GetRouteData().Values),

            action = actionDescriptor?.ActionName,
            controller = actionDescriptor?.ControllerName,

            link,
        });
    }

    private IUrlHelper GetUrlHelper(HttpContext httpContext)
    {
        var services = httpContext.RequestServices;
        var urlHelperFactory = services.GetRequiredService<IUrlHelperFactory>();
        
        // Create ActionContext from HttpContext for URL generation
        var actionContext = new ActionContext(
            httpContext,
            httpContext.GetRouteData(),
            httpContext.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>() ?? new ControllerActionDescriptor());
        
        var urlHelper = urlHelperFactory.GetUrlHelper(actionContext);
        return urlHelper;
    }
}
