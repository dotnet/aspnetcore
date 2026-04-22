// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class CacheBoundaryKeyResolverTest
{
    [Fact]
    public void ComputeKey_IsDeterministic()
    {
        var component = CreateComponent();
        var httpContext = CreateHttpContext();

        var key1 = CacheBoundaryKeyResolver.ComputeKey(component, httpContext);
        var key2 = CacheBoundaryKeyResolver.ComputeKey(component, httpContext);

        Assert.Equal(key1, key2);
    }

    [Fact]
    public void ComputeKey_DifferentTreePosition_ProducesDifferentKeys()
    {
        var httpContext = CreateHttpContext();
        var component1 = CreateComponent(treePositionKey: "ParentA.CacheBoundary");
        var component2 = CreateComponent(treePositionKey: "ParentB.CacheBoundary");

        var key1 = CacheBoundaryKeyResolver.ComputeKey(component1, httpContext);
        var key2 = CacheBoundaryKeyResolver.ComputeKey(component2, httpContext);

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void ComputeKey_CacheKey_ChangesOutput()
    {
        var httpContext = CreateHttpContext();
        var component1 = CreateComponent(cacheKey: "v1");
        var component2 = CreateComponent(cacheKey: "v2");

        var key1 = CacheBoundaryKeyResolver.ComputeKey(component1, httpContext);
        var key2 = CacheBoundaryKeyResolver.ComputeKey(component2, httpContext);

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void ComputeKey_VaryByQuery_DifferentValues_ProducesDifferentKeys()
    {
        var component = CreateComponent(varyByQuery: "page");
        var ctx1 = CreateHttpContext(queryString: "?page=1");
        var ctx2 = CreateHttpContext(queryString: "?page=2");

        var key1 = CacheBoundaryKeyResolver.ComputeKey(component, ctx1);
        var key2 = CacheBoundaryKeyResolver.ComputeKey(component, ctx2);

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void ComputeKey_VaryByRoute_DifferentValues_ProducesDifferentKeys()
    {
        var component = CreateComponent(varyByRoute: "id");
        var ctx1 = CreateHttpContext(routeValues: new RouteValueDictionary { ["id"] = "1" });
        var ctx2 = CreateHttpContext(routeValues: new RouteValueDictionary { ["id"] = "2" });

        var key1 = CacheBoundaryKeyResolver.ComputeKey(component, ctx1);
        var key2 = CacheBoundaryKeyResolver.ComputeKey(component, ctx2);

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void ComputeKey_VaryByHeader_DifferentValues_ProducesDifferentKeys()
    {
        var component = CreateComponent(varyByHeader: "Accept-Language");
        var ctx1 = CreateHttpContext(headers: new Dictionary<string, string> { ["Accept-Language"] = "en-US" });
        var ctx2 = CreateHttpContext(headers: new Dictionary<string, string> { ["Accept-Language"] = "fr-FR" });

        var key1 = CacheBoundaryKeyResolver.ComputeKey(component, ctx1);
        var key2 = CacheBoundaryKeyResolver.ComputeKey(component, ctx2);

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void ComputeKey_VaryByCookie_DifferentValues_ProducesDifferentKeys()
    {
        var component = CreateComponent(varyByCookie: "session");
        var ctx1 = CreateHttpContext(cookieHeader: "session=abc");
        var ctx2 = CreateHttpContext(cookieHeader: "session=xyz");

        var key1 = CacheBoundaryKeyResolver.ComputeKey(component, ctx1);
        var key2 = CacheBoundaryKeyResolver.ComputeKey(component, ctx2);

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void ComputeKey_VaryByUser_DifferentUsers_ProducesDifferentKeys()
    {
        var component = CreateComponent(varyByUser: true);
        var ctx1 = CreateHttpContext(userName: "alice");
        var ctx2 = CreateHttpContext(userName: "bob");

        var key1 = CacheBoundaryKeyResolver.ComputeKey(component, ctx1);
        var key2 = CacheBoundaryKeyResolver.ComputeKey(component, ctx2);

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void ComputeKey_VaryByUser_Disabled_SameKeyRegardlessOfUser()
    {
        var component = CreateComponent(varyByUser: false);
        var ctx1 = CreateHttpContext(userName: "alice");
        var ctx2 = CreateHttpContext(userName: "bob");

        var key1 = CacheBoundaryKeyResolver.ComputeKey(component, ctx1);
        var key2 = CacheBoundaryKeyResolver.ComputeKey(component, ctx2);

        Assert.Equal(key1, key2);
    }

    [Fact]
    public void ComputeKey_VaryByCulture_DifferentCultures_ProducesDifferentKeys()
    {
        var component = CreateComponent(varyByCulture: true);
        var httpContext = CreateHttpContext();

        var originalCulture = CultureInfo.CurrentCulture;
        var originalUICulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            var key1 = CacheBoundaryKeyResolver.ComputeKey(component, httpContext);

            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
            var key2 = CacheBoundaryKeyResolver.ComputeKey(component, httpContext);

            Assert.NotEqual(key1, key2);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }
    }

    [Fact]
    public void ComputeKey_VaryBy_CustomString_ChangesKey()
    {
        var httpContext = CreateHttpContext();
        var component1 = CreateComponent(varyBy: "dark-theme");
        var component2 = CreateComponent(varyBy: "light-theme");

        var key1 = CacheBoundaryKeyResolver.ComputeKey(component1, httpContext);
        var key2 = CacheBoundaryKeyResolver.ComputeKey(component2, httpContext);

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void ComputeKey_NoVaryBy_SameKeyForDifferentRequests()
    {
        var component = CreateComponent();
        var ctx1 = CreateHttpContext(queryString: "?page=1");
        var ctx2 = CreateHttpContext(queryString: "?page=2");

        var key1 = CacheBoundaryKeyResolver.ComputeKey(component, ctx1);
        var key2 = CacheBoundaryKeyResolver.ComputeKey(component, ctx2);

        Assert.Equal(key1, key2);
    }

    [Fact]
    public void ComputeKey_MultipleVaryBy_AllContribute()
    {
        var component = CreateComponent(varyByQuery: "page", varyByHeader: "Accept");
        var ctx1 = CreateHttpContext(queryString: "?page=1", headers: new Dictionary<string, string> { ["Accept"] = "text/html" });
        var ctx2 = CreateHttpContext(queryString: "?page=1", headers: new Dictionary<string, string> { ["Accept"] = "application/json" });

        var key1 = CacheBoundaryKeyResolver.ComputeKey(component, ctx1);
        var key2 = CacheBoundaryKeyResolver.ComputeKey(component, ctx2);

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void ComputeKey_DifferentVaryByDimensions_DoNotCollide()
    {
        // A query param named "user" with value "alice" should not collide
        // with VaryByUser=true when the username is "alice"
        var componentWithQuery = CreateComponent(varyByQuery: "user");
        var ctxWithQueryUser = CreateHttpContext(queryString: "?user=alice");

        var componentWithUser = CreateComponent(varyByUser: true);
        var ctxWithAuthUser = CreateHttpContext(userName: "alice");

        var key1 = CacheBoundaryKeyResolver.ComputeKey(componentWithQuery, ctxWithQueryUser);
        var key2 = CacheBoundaryKeyResolver.ComputeKey(componentWithUser, ctxWithAuthUser);

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void ComputeKey_DifferentCollectionDimensions_DoNotCollide()
    {
        // A cookie named "lang" with value "en" should not collide
        // with a header named "lang" with value "en"
        var componentWithCookie = CreateComponent(varyByCookie: "lang");
        var ctxWithCookie = CreateHttpContext(cookieHeader: "lang=en");

        var componentWithHeader = CreateComponent(varyByHeader: "lang");
        var ctxWithHeader = CreateHttpContext(headers: new Dictionary<string, string> { ["lang"] = "en" });

        var key1 = CacheBoundaryKeyResolver.ComputeKey(componentWithCookie, ctxWithCookie);
        var key2 = CacheBoundaryKeyResolver.ComputeKey(componentWithHeader, ctxWithHeader);

        Assert.NotEqual(key1, key2);
    }

    private static RenderFragment DefaultChildContent => builder => builder.AddContent(0, "test");

    private static CacheBoundary CreateComponent(
        RenderFragment childContent = null,
        string cacheKey = null,
        string varyByQuery = null,
        string varyByRoute = null,
        string varyByHeader = null,
        string varyByCookie = null,
        bool? varyByUser = null,
        bool? varyByCulture = null,
        string varyBy = null,
        string treePositionKey = "DefaultParent.CacheBoundary")
    {
        var component = new CacheBoundary
        {
            ChildContent = childContent ?? DefaultChildContent,
            CacheKey = cacheKey,
            VaryByQuery = varyByQuery,
            VaryByRoute = varyByRoute,
            VaryByHeader = varyByHeader,
            VaryByCookie = varyByCookie,
            VaryByUser = varyByUser,
            VaryByCulture = varyByCulture,
            VaryBy = varyBy,
            TreePositionKeyFactory = () => treePositionKey,
        };
        return component;
    }

    private static DefaultHttpContext CreateHttpContext(
        string queryString = null,
        RouteValueDictionary routeValues = null,
        Dictionary<string, string> headers = null,
        string cookieHeader = null,
        string userName = null)
    {
        var httpContext = new DefaultHttpContext();

        if (queryString is not null)
        {
            httpContext.Request.QueryString = new QueryString(queryString);
        }

        if (routeValues is not null)
        {
            httpContext.Request.RouteValues = routeValues;
        }

        if (headers is not null)
        {
            foreach (var (key, value) in headers)
            {
                httpContext.Request.Headers[key] = value;
            }
        }

        if (cookieHeader is not null)
        {
            httpContext.Request.Headers["Cookie"] = cookieHeader;
        }

        if (userName is not null)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, userName)], "test"));
        }

        return httpContext;
    }
}
