// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace RoutingWebSite.Controllers;

public class LinkParserController : Controller
{
    private readonly LinkParser _linkParser;

    public LinkParserController(LinkParser linkParser)
    {
        _linkParser = linkParser;
    }

    public JObject Index()
    {
        var parsed = _linkParser.ParsePathByEndpointName("default", HttpContext.Request.Path);
        if (parsed == null)
        {
            throw new Exception("Parsing failed.");
        }

        return ToJObject(parsed);
    }

    public JObject Another(string path)
    {
        var parsed = _linkParser.ParsePathByEndpointName("AnotherRoute", path);
        if (parsed == null)
        {
            throw new Exception("Parsing failed.");
        }

        return ToJObject(parsed);
    }

    [Route("some-path/{x}/{y}/{z?}", Name = "AnotherRoute")]
    public void AnotherRoute()
    {
        throw null;
    }

    private static JObject ToJObject(RouteValueDictionary values)
    {
        var obj = new JObject();
        foreach (var kvp in values)
        {
            obj.Add(kvp.Key, new JValue(kvp.Value.ToString()));
        }

        return obj;
    }
}
