// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace RoutingWebSite;

public class LG1Controller : Controller
{
    private readonly LinkGenerator _linkGenerator;

    public LG1Controller(LinkGenerator linkGenerator)
    {
        _linkGenerator = linkGenerator;
    }

    public string LinkToSelf()
    {
        return _linkGenerator.GetPathByAction(HttpContext, values: QueryToRouteValues(HttpContext.Request.Query));
    }

    public string LinkToAnotherAction()
    {
        return _linkGenerator.GetPathByAction(
            HttpContext,
            action: nameof(LinkToSelf),
            values: QueryToRouteValues(HttpContext.Request.Query));
    }

    public string LinkToAnotherController()
    {
        return _linkGenerator.GetPathByAction(
            HttpContext,
            controller: "LG2",
            action: nameof(LG2Controller.SomeAction),
            values: QueryToRouteValues(HttpContext.Request.Query));
    }

    public string LinkToAnArea()
    {
        var values = QueryToRouteValues(HttpContext.Request.Query);
        values["area"] = "Admin";

        return _linkGenerator.GetPathByAction(
            HttpContext,
            controller: "LG3",
            action: nameof(LG3Controller.SomeAction),
            values: values);
    }

    public string LinkToPage()
    {
        return _linkGenerator.GetPathByPage(
            HttpContext,
            page: "/LGPage",
            values: QueryToRouteValues(HttpContext.Request.Query));
    }

    public string LinkToPageWithTransformedPath()
    {
        return _linkGenerator.GetPathByPage(
            HttpContext,
            page: "/PageRouteTransformer/TestPage",
            values: QueryToRouteValues(HttpContext.Request.Query));
    }

    public string LinkToPageInArea()
    {
        var values = QueryToRouteValues(HttpContext.Request.Query);
        values["area"] = "Admin";
        return _linkGenerator.GetPathByPage(
            HttpContext,
            page: "/LGAreaPage",
            handler: "a-handler",
            values: values);
    }

    public string LinkWithFullUri()
    {
        return _linkGenerator.GetUriByAction(
            HttpContext,
            controller: "LG1",
            action: nameof(LinkWithFullUri),
            values: QueryToRouteValues(HttpContext.Request.Query),
            fragment: new FragmentString("#hi"));
    }

    public string LinkToPageWithFullUri()
    {
        return _linkGenerator.GetUriByPage(
            HttpContext,
            page: "/LGPage",
            values: QueryToRouteValues(HttpContext.Request.Query));
    }

    public string LinkWithFullUriWithoutHttpContext()
    {
        return _linkGenerator.GetUriByAction(
            scheme: "https",
            host: new HostString("www.example.com"),
            controller: "LG1",
            action: nameof(LinkWithFullUri),
            values: QueryToRouteValues(HttpContext.Request.Query),
            fragment: new FragmentString("#hi"));
    }

    public string LinkToPageWithFullUriWithoutHttpContext()
    {
        var values = QueryToRouteValues(HttpContext.Request.Query);
        values["area"] = "Admin";
        return _linkGenerator.GetUriByPage(
            scheme: "https",
            host: new HostString("www.example.com"),
            page: "/LGAreaPage",
            handler: "a-handler",
            values: values);
    }

    public string LinkToRouteWithNoMvcParameters(int? custom = null)
    {
        return _linkGenerator.GetUriByRouteValues(
            scheme: "https",
            host: new HostString("www.example.com"),
            routeName: "routewithnomvcparameters",
            values: new { custom = custom, });
    }

    private static RouteValueDictionary QueryToRouteValues(IQueryCollection query)
    {
        return new RouteValueDictionary(query.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString()));
    }
}
