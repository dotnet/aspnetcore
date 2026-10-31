// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Shared;
using Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.FileSystemGlobbing.Internal;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Tests;

public class JsonTranscodingRouteAdapterTests
{
    [Fact]
    public void ParseMultipleVariables()
    {
        var pattern = HttpRoutePattern.Parse("/shelves/{shelf}/books/{book}");
        var adapter = JsonTranscodingRouteAdapter.Parse(pattern);

        Assert.Equal("/shelves/{shelf}/books/{book}", adapter.ResolvedRouteTemplate);
        Assert.Empty(adapter.RewriteVariableActions);
    }

    [Fact]
    public void ParseComplexVariable()
    {
        var route = HttpRoutePattern.Parse("/v1/{book.name=shelves/*/books/*}");
        var adapter = JsonTranscodingRouteAdapter.Parse(route);

        Assert.Equal("/v1/shelves/{__Complex_book.name_2}/books/{__Complex_book.name_4}", adapter.ResolvedRouteTemplate);
        Assert.Single(adapter.RewriteVariableActions);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.RouteValues = new RouteValueDictionary
        {
            { "__Complex_book.name_2", "first" },
            { "__Complex_book.name_4", "second" }
        };

        adapter.RewriteVariableActions[0](httpContext);

        Assert.Equal("shelves/first/books/second", httpContext.Request.RouteValues["book.name"]);
    }

    [Fact]
    public void ParseCatchAllSegment()
    {
        var pattern = HttpRoutePattern.Parse("/shelves/**");
        var adapter = JsonTranscodingRouteAdapter.Parse(pattern);

        Assert.Equal("/shelves/{**__Discard_1}", adapter.ResolvedRouteTemplate);
        Assert.Empty(adapter.RewriteVariableActions);
    }

    [Fact]
    public void ParseAnySegment()
    {
        var pattern = HttpRoutePattern.Parse("/*")!;
        var adapter = JsonTranscodingRouteAdapter.Parse(pattern);

        Assert.Equal("/{__Discard_0}", adapter.ResolvedRouteTemplate);
        Assert.Empty(adapter.RewriteVariableActions);
    }

    [Fact]
    public void ParseVerb()
    {
        var pattern = HttpRoutePattern.Parse("/a:foo");
        var adapter = JsonTranscodingRouteAdapter.Parse(pattern);

        Assert.Equal("/a:foo", adapter.ResolvedRouteTemplate);
        Assert.Empty(adapter.RewriteVariableActions);
    }

    [Fact]
    public void ParseAnyAndCatchAllSegment()
    {
        var pattern = HttpRoutePattern.Parse("/*/**");
        var adapter = JsonTranscodingRouteAdapter.Parse(pattern);

        Assert.Equal("/{__Discard_0}/{**__Discard_1}", adapter.ResolvedRouteTemplate);
        Assert.Empty(adapter.RewriteVariableActions);
    }

    [Fact]
    public void ParseAnyAndCatchAllSegment2()
    {
        var pattern = HttpRoutePattern.Parse("/*/a/**");
        var adapter = JsonTranscodingRouteAdapter.Parse(pattern);

        Assert.Equal("/{__Discard_0}/a/{**__Discard_2}", adapter.ResolvedRouteTemplate);
        Assert.Empty(adapter.RewriteVariableActions);
    }

    [Fact]
    public void ParseNestedFieldPath()
    {
        var pattern = HttpRoutePattern.Parse("/a/{a.b.c}");
        var adapter = JsonTranscodingRouteAdapter.Parse(pattern);

        Assert.Equal("/a/{a.b.c}", adapter.ResolvedRouteTemplate);
        Assert.Empty(adapter.RewriteVariableActions);
    }

    [Fact]
    public void ParseComplexNestedFieldPath()
    {
        var pattern = HttpRoutePattern.Parse("/a/{a.b.c=*}");
        var adapter = JsonTranscodingRouteAdapter.Parse(pattern);

        Assert.Equal("/a/{a.b.c}", adapter.ResolvedRouteTemplate);
        Assert.Empty(adapter.RewriteVariableActions);
    }

    [Fact]
    public void ParseComplexCatchAll()
    {
        var pattern = HttpRoutePattern.Parse("/a/{b=**}");
        var adapter = JsonTranscodingRouteAdapter.Parse(pattern);

        Assert.Equal("/a/{**b}", adapter.ResolvedRouteTemplate);
        Assert.Empty(adapter.RewriteVariableActions);
    }

    [Fact]
    public void ParseComplexPrefixSuffixCatchAll()
    {
        var pattern = HttpRoutePattern.Parse("/{x.y.z=a/**/b}/c/d");
        var adapter = JsonTranscodingRouteAdapter.Parse(pattern);

        Assert.Equal("/a/{**__Complex_x.y.z_1:regex(/b/c/d$)}", adapter.ResolvedRouteTemplate);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.RouteValues = new RouteValueDictionary
        {
            { "__Complex_x.y.z_1", "my/value/b/c/d" }
        };

        adapter.RewriteVariableActions[0](httpContext);

        Assert.Equal("a/my/value/b", httpContext.Request.RouteValues["x.y.z"]);
    }

    [Fact]
    public void ParseComplexPrefixSuffixCatchAllVerb()
    {
        var pattern = HttpRoutePattern.Parse("/{x.y.z=a/**/b}/c/d:verb");
        var adapter = JsonTranscodingRouteAdapter.Parse(pattern);

        Assert.Equal("/a/{**__Complex_x.y.z_1:regex(/b/c/d:verb$)}", adapter.ResolvedRouteTemplate);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.RouteValues = new RouteValueDictionary
        {
            { "__Complex_x.y.z_1", "my/value/b/c/d:verb" }
        };

        adapter.RewriteVariableActions[0](httpContext);

        Assert.Equal("a/my/value/b", httpContext.Request.RouteValues["x.y.z"]);
    }

    [Fact]
    public void ParseComplexPrefixSegment()
    {
        var pattern = HttpRoutePattern.Parse("/a/{b=c/*}");
        var adapter = JsonTranscodingRouteAdapter.Parse(pattern);

        Assert.Equal("/a/c/{__Complex_b_2}", adapter.ResolvedRouteTemplate);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.RouteValues = new RouteValueDictionary
        {
            { "__Complex_b_2", "value" }
        };

        adapter.RewriteVariableActions[0](httpContext);

        Assert.Equal("c/value", httpContext.Request.RouteValues["b"]);
    }

    [Fact]
    public void ParseComplexPrefixSuffixSegment()
    {
        var pattern = HttpRoutePattern.Parse("/a/{b=c/*/d}");
        var adapter = JsonTranscodingRouteAdapter.Parse(pattern);

        Assert.Equal("/a/c/{__Complex_b_2}/d", adapter.ResolvedRouteTemplate);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.RouteValues = new RouteValueDictionary
        {
            { "__Complex_b_2", "value" }
        };

        adapter.RewriteVariableActions[0](httpContext);

        Assert.Equal("c/value/d", httpContext.Request.RouteValues["b"]);
    }

    [Fact]
    public void ParseComplexPathCatchAll()
    {
        var pattern = HttpRoutePattern.Parse("/a/{b=c/**}");
        var adapter = JsonTranscodingRouteAdapter.Parse(pattern);

        Assert.Equal("/a/c/{**__Complex_b_2}", adapter.ResolvedRouteTemplate);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.RouteValues = new RouteValueDictionary
        {
            { "__Complex_b_2", "value" }
        };

        adapter.RewriteVariableActions[0](httpContext);

        Assert.Equal("c/value", httpContext.Request.RouteValues["b"]);
    }

    [Fact]
    public void ParseManyVariables()
    {
        var pattern = HttpRoutePattern.Parse("/{a}/{b}/{c}");
        var adapter = JsonTranscodingRouteAdapter.Parse(pattern);

        Assert.Equal("/{a}/{b}/{c}", adapter.ResolvedRouteTemplate);
    }

    [Fact]
    public void ParseCatchAllDiscardVerb()
    {
        var pattern = HttpRoutePattern.Parse("/a/{b=*}/**:verb");
        var adapter = JsonTranscodingRouteAdapter.Parse(pattern);

        Assert.Equal("/a/{b}/{**__Discard_2:regex(:verb$)}", adapter.ResolvedRouteTemplate);
    }

    [Fact]
    public void ParseCatchAllParameterVerb()
    {
        var pattern = HttpRoutePattern.Parse("/v1/greeter/{name=**}:verb");
        var adapter = JsonTranscodingRouteAdapter.Parse(pattern);

        Assert.Equal("/v1/greeter/{**__Complex_name_2:regex(:verb$)}", adapter.ResolvedRouteTemplate);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.RouteValues = new RouteValueDictionary
        {
            { "__Complex_name_2", "test/name:verb" }
        };

        adapter.RewriteVariableActions[0](httpContext);

        Assert.Equal("test/name", httpContext.Request.RouteValues["name"]);
    }

    [Fact]
    public void ParseCatchAllParameterVerb_TrailingSlash()
    {
        var pattern = HttpRoutePattern.Parse("/v1/greeter/{name=**}:verb");
        var adapter = JsonTranscodingRouteAdapter.Parse(pattern);

        Assert.Equal("/v1/greeter/{**__Complex_name_2:regex(:verb$)}", adapter.ResolvedRouteTemplate);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.RouteValues = new RouteValueDictionary
        {
            { "__Complex_name_2", "test/name/:verb" }
        };

        adapter.RewriteVariableActions[0](httpContext);

        Assert.Equal("test/name/", httpContext.Request.RouteValues["name"]);
    }

    [Fact]
    public void ParseParameterVerb()
    {
        var pattern = HttpRoutePattern.Parse("/v1/greeter/{name}:verb");
        var adapter = JsonTranscodingRouteAdapter.Parse(pattern);

        Assert.Equal("/v1/greeter/{name}:verb", adapter.ResolvedRouteTemplate);
    }
}
