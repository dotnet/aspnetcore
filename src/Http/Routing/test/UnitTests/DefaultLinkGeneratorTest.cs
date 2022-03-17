// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing;

// Tests LinkGenerator functionality using GetXyzByAddress - see tests for the extension
// methods for more E2E tests.
//
// Does not cover template processing in detail, those scenarios are validated by TemplateBinderTests
// and DefaultLinkGeneratorProcessTemplateTest
public class DefaultLinkGeneratorTest : LinkGeneratorTestBase
{
    [Fact]
    public void GetPathByAddress_WithoutHttpContext_NoMatches_ReturnsNull()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

        var linkGenerator = CreateLinkGenerator(endpoint);

        // Act
        var path = linkGenerator.GetPathByAddress(0, values: null);

        // Assert
        Assert.Null(path);
    }

    [Fact]
    public void GetPathByAddress_WithHttpContext_NoMatches_ReturnsNull()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

        var linkGenerator = CreateLinkGenerator(endpoint);

        // Act
        var path = linkGenerator.GetPathByAddress(CreateHttpContext(), 0, values: null);

        // Assert
        Assert.Null(path);
    }

    [Fact]
    public void GetUriByAddress_WithoutHttpContext_NoMatches_ReturnsNull()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

        var linkGenerator = CreateLinkGenerator(endpoint);

        // Act
        var uri = linkGenerator.GetUriByAddress(0, values: null, "http", new HostString("example.com"));

        // Assert
        Assert.Null(uri);
    }

    [Fact]
    public void GetUriByAddress_WithHttpContext_NoMatches_ReturnsNull()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

        var linkGenerator = CreateLinkGenerator(endpoint);

        // Act
        var uri = linkGenerator.GetUriByAddress(CreateHttpContext(), 0, values: null);

        // Assert
        Assert.Null(uri);
    }

    [Fact]
    public void GetPathByAddress_WithoutHttpContext_HasMatches_ReturnsFirstSuccessfulTemplateResult()
    {
        // Arrange
        var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
        var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

        var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

        // Act
        var path = linkGenerator.GetPathByAddress(1, values: new RouteValueDictionary(new { controller = "Home", action = "Index", }));

        // Assert
        Assert.Equal("/Home/Index", path);
    }

    [Fact]
    public void GetPathByAddress_WithHttpContext_HasMatches_ReturnsFirstSuccessfulTemplateResult()
    {
        // Arrange
        var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
        var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

        var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

        // Act
        var path = linkGenerator.GetPathByAddress(CreateHttpContext(), 1, values: new RouteValueDictionary(new { controller = "Home", action = "Index", }));

        // Assert
        Assert.Equal("/Home/Index", path);
    }

    [Fact]
    public void GetUriByAddress_WithoutHttpContext_HasMatches_ReturnsFirstSuccessfulTemplateResult()
    {
        // Arrange
        var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
        var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

        var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

        // Act
        var path = linkGenerator.GetUriByAddress(
            1,
            values: new RouteValueDictionary(new { controller = "Home", action = "Index", }),
            "http",
            new HostString("example.com"));

        // Assert
        Assert.Equal("http://example.com/Home/Index", path);
    }

    [Fact]
    public void GetUriByAddress_WithHttpContext_HasMatches_ReturnsFirstSuccessfulTemplateResult()
    {
        // Arrange
        var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
        var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

        var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

        var httpContext = CreateHttpContext();
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("example.com");

        // Act
        var uri = linkGenerator.GetUriByAddress(httpContext, 1, values: new RouteValueDictionary(new { controller = "Home", action = "Index", }));

        // Assert
        Assert.Equal("http://example.com/Home/Index", uri);
    }

    [Fact]
    public void GetPathByAddress_WithoutHttpContext_WithLinkOptions()
    {
        // Arrange
        var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
        var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

        var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

        // Act
        var path = linkGenerator.GetPathByAddress(
            1,
            values: new RouteValueDictionary(new { controller = "Home", action = "Index", }),
            options: new LinkOptions() { AppendTrailingSlash = true, });

        // Assert
        Assert.Equal("/Home/Index/", path);
    }

    [Fact]
    public void GetPathByAddress_WithParameterTransformer()
    {
        // Arrange
        var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller:slugify}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
        var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller:slugify}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

        Action<IServiceCollection> configureServices = s =>
        {
            s.Configure<RouteOptions>(o =>
            {
                o.ConstraintMap["slugify"] = typeof(SlugifyParameterTransformer);
            });
        };

        var linkGenerator = CreateLinkGenerator(configureServices, endpoint1, endpoint2);

        // Act
        var path = linkGenerator.GetPathByAddress(
            1,
            values: new RouteValueDictionary(new { controller = "TestController", action = "Index", }));

        // Assert
        Assert.Equal("/test-controller/Index", path);
    }

    [Fact]
    public void GetPathByAddress_WithParameterTransformer_WithLowercaseUrl()
    {
        // Arrange
        var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller:slugify}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
        var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller:slugify}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

        Action<IServiceCollection> configureServices = s =>
        {
            s.Configure<RouteOptions>(o =>
            {
                o.ConstraintMap["slugify"] = typeof(SlugifyParameterTransformer);
            });
        };

        var linkGenerator = CreateLinkGenerator(configureServices, endpoint1, endpoint2);

        // Act
        var path = linkGenerator.GetPathByAddress(
            1,
            values: new RouteValueDictionary(new { controller = "TestController", action = "Index", }),
            options: new LinkOptions() { LowercaseUrls = true, });

        // Assert
        Assert.Equal("/test-controller/index", path);
    }

    [Fact]
    public void GetPathByAddress_WithHttpContext_WithLinkOptions()
    {
        // Arrange
        var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
        var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

        var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

        // Act
        var path = linkGenerator.GetPathByAddress(
            CreateHttpContext(),
            1,
            values: new RouteValueDictionary(new { controller = "Home", action = "Index", }),
            options: new LinkOptions() { AppendTrailingSlash = true, });

        // Assert
        Assert.Equal("/Home/Index/", path);
    }

    [Fact]
    public void GetUriByAddress_WithoutHttpContext_WithLinkOptions()
    {
        // Arrange
        var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
        var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

        var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

        // Act
        var path = linkGenerator.GetUriByAddress(
            1,
            values: new RouteValueDictionary(new { controller = "Home", action = "Index", }),
            "http",
            new HostString("example.com"),
            options: new LinkOptions() { AppendTrailingSlash = true, });

        // Assert
        Assert.Equal("http://example.com/Home/Index/", path);
    }

    [Fact]
    public void GetUriByAddress_WithHttpContext_WithLinkOptions()
    {
        // Arrange
        var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
        var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

        var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

        var httpContext = CreateHttpContext();
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("example.com");

        // Act
        var uri = linkGenerator.GetUriByAddress(
            httpContext,
            1,
            values: new RouteValueDictionary(new { controller = "Home", action = "Index", }),
            options: new LinkOptions() { AppendTrailingSlash = true, });

        // Assert
        Assert.Equal("http://example.com/Home/Index/", uri);
    }

    // Includes characters that need to be encoded
    [Fact]
    public void GetPathByAddress_WithoutHttpContext_WithPathBaseAndFragment()
    {
        // Arrange
        var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
        var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

        var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

        // Act
        var path = linkGenerator.GetPathByAddress(
            1,
            values: new RouteValueDictionary(new { controller = "Home", action = "In?dex", query = "some?query" }),
            new PathString("/Foo/Bar?encodeme?"),
            new FragmentString("#Fragment?"));

        // Assert
        Assert.Equal("/Foo/Bar%3Fencodeme%3F/Home/In%3Fdex?query=some%3Fquery#Fragment?", path);
    }

    [Fact]
    public void GetLink_ParameterTransformer()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{controller:upper-case}/{name}", requiredValues: new { controller = "Home", name = "Test" });

        Action<IServiceCollection> configure = (s) =>
        {
            s.Configure<RouteOptions>(o =>
            {
                o.ConstraintMap["upper-case"] = typeof(UpperCaseParameterTransform);
            });
        };

        var linkGenerator = CreateLinkGenerator(configure, endpoint);

        // Act
        var link = linkGenerator.GetPathByRouteValues(routeName: null, new { controller = "Home", name = "Test" });

        // Assert
        Assert.Equal("/HOME/Test", link);
    }

    [Fact]
    public void GetLink_ParameterTransformer_ForQueryString()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint(
            "{controller:upper-case}/{name}",
            requiredValues: new { controller = "Home", name = "Test", },
            policies: new { c = new UpperCaseParameterTransform(), });

        Action<IServiceCollection> configure = (s) =>
        {
            s.Configure<RouteOptions>(o =>
            {
                o.ConstraintMap["upper-case"] = typeof(UpperCaseParameterTransform);
            });
        };

        var linkGenerator = CreateLinkGenerator(configure, endpoint);

        // Act
        var link = linkGenerator.GetPathByRouteValues(routeName: null, new { controller = "Home", name = "Test", c = "hithere", });

        // Assert
        Assert.Equal("/HOME/Test?c=HITHERE", link);
    }

    // Includes characters that need to be encoded
    [Fact]
    public void GetPathByAddress_WithHttpContext_WithPathBaseAndFragment()
    {
        // Arrange
        var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
        var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

        var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

        var httpContext = CreateHttpContext();
        httpContext.Request.PathBase = new PathString("/Foo/Bar?encodeme?");

        // Act
        var path = linkGenerator.GetPathByAddress(
            httpContext,
            1,
            values: new RouteValueDictionary(new { controller = "Home", action = "In?dex", query = "some?query" }),
            fragment: new FragmentString("#Fragment?"));

        // Assert
        Assert.Equal("/Foo/Bar%3Fencodeme%3F/Home/In%3Fdex?query=some%3Fquery#Fragment?", path);
    }

    // Includes characters that need to be encoded
    [Fact]
    public void GetUriByAddress_WithoutHttpContext_WithPathBaseAndFragment()
    {
        // Arrange
        var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
        var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

        var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

        // Act
        var path = linkGenerator.GetUriByAddress(
            1,
            values: new RouteValueDictionary(new { controller = "Home", action = "In?dex", query = "some?query" }),
            "http",
            new HostString("example.com"),
            new PathString("/Foo/Bar?encodeme?"),
            new FragmentString("#Fragment?"));

        // Assert
        Assert.Equal("http://example.com/Foo/Bar%3Fencodeme%3F/Home/In%3Fdex?query=some%3Fquery#Fragment?", path);
    }

    // Includes characters that need to be encoded
    [Fact]
    public void GetUriByAddress_WithHttpContext_WithPathBaseAndFragment()
    {
        // Arrange
        var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
        var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

        var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

        var httpContext = CreateHttpContext();
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("example.com");
        httpContext.Request.PathBase = new PathString("/Foo/Bar?encodeme?");

        // Act
        var uri = linkGenerator.GetUriByAddress(
            httpContext,
            1,
            values: new RouteValueDictionary(new { controller = "Home", action = "In?dex", query = "some?query" }),
            fragment: new FragmentString("#Fragment?"));

        // Assert
        Assert.Equal("http://example.com/Foo/Bar%3Fencodeme%3F/Home/In%3Fdex?query=some%3Fquery#Fragment?", uri);
    }

    [Fact]
    public void GetPathByAddress_WithHttpContext_IncludesAmbientValues()
    {
        // Arrange
        var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
        var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

        var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

        var httpContext = CreateHttpContext();
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("example.com");

        // Act
        var uri = linkGenerator.GetPathByAddress(
            httpContext,
            1,
            values: new RouteValueDictionary(new { action = "Index", }),
            ambientValues: new RouteValueDictionary(new { controller = "Home", }));

        // Assert
        Assert.Equal("/Home/Index", uri);
    }

    [Fact]
    public void GetUriByAddress_WithHttpContext_IncludesAmbientValues()
    {
        // Arrange
        var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
        var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

        var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

        var httpContext = CreateHttpContext();
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("example.com");

        // Act
        var uri = linkGenerator.GetUriByAddress(
            httpContext,
            1,
            values: new RouteValueDictionary(new { action = "Index", }),
            ambientValues: new RouteValueDictionary(new { controller = "Home", }));

        // Assert
        Assert.Equal("http://example.com/Home/Index", uri);
    }

    [Fact]
    public void GetPathByAddress_WithHttpContext_CanOverrideUriParts()
    {
        // Arrange
        var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
        var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

        var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

        var httpContext = CreateHttpContext();
        httpContext.Request.PathBase = "/Foo";

        // Act
        var uri = linkGenerator.GetPathByAddress(
            httpContext,
            1,
            values: new RouteValueDictionary(new { action = "Index", controller = "Home", }),
            pathBase: "/");

        // Assert
        Assert.Equal("/Home/Index", uri);
    }

    [Fact]
    public void GetUriByAddress_WithHttpContext_CanOverrideUriParts()
    {
        // Arrange
        var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
        var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

        var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

        var httpContext = CreateHttpContext();
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("example.com");
        httpContext.Request.PathBase = "/Foo";

        // Act
        var uri = linkGenerator.GetUriByAddress(
            httpContext,
            1,
            values: new RouteValueDictionary(new { action = "Index", controller = "Home", }),
            scheme: "ftp",
            host: new HostString("example.com:5000"),
            pathBase: "/");

        // Assert
        Assert.Equal("ftp://example.com:5000/Home/Index", uri);
    }

    [Fact]
    public void GetPathByAddress_WithHttpContext_ContextPassedToConstraint()
    {
        // Arrange
        var constraint = new TestRouteConstraint();

        var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", policies: new { controller = constraint }, metadata: new object[] { new IntMetadata(1), });

        var linkGenerator = CreateLinkGenerator(endpoint1);

        var httpContext = CreateHttpContext();
        httpContext.Request.PathBase = "/Foo";

        // Act
        var uri = linkGenerator.GetPathByAddress(
            httpContext,
            1,
            values: new RouteValueDictionary(new { action = "Index", controller = "Home", }),
            pathBase: "/");

        // Assert
        Assert.Equal("/Home/Index", uri);
        Assert.True(constraint.HasHttpContext);
    }

    private class TestRouteConstraint : IRouteConstraint
    {
        public bool HasHttpContext { get; set; }

        public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
        {
            HasHttpContext = (httpContext != null);
            return true;
        }
    }

    [Fact]
    public void GetTemplateBinder_CanCache()
    {
        // Arrange
        var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
        var dataSource = new DynamicEndpointDataSource(endpoint1);

        var linkGenerator = CreateLinkGenerator(dataSources: new[] { dataSource });

        var expected = linkGenerator.GetTemplateBinder(endpoint1);

        // Act
        var actual = linkGenerator.GetTemplateBinder(endpoint1);

        // Assert
        Assert.Same(expected, actual);
    }

    [Fact]
    public void GetTemplateBinder_CanClearCache()
    {
        // Arrange
        var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
        var dataSource = new DynamicEndpointDataSource(endpoint1);

        var linkGenerator = CreateLinkGenerator(dataSources: new[] { dataSource });
        var original = linkGenerator.GetTemplateBinder(endpoint1);

        var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
        dataSource.AddEndpoint(endpoint2);

        // Act
        var actual = linkGenerator.GetTemplateBinder(endpoint1);

        // Assert
        Assert.NotSame(original, actual);
    }

    [Theory]
    [InlineData(new string[] { }, new string[] { }, "/")]
    [InlineData(new string[] { "id" }, new string[] { "3" }, "/Home/Index/3")]
    [InlineData(new string[] { "custom" }, new string[] { "Custom" }, "/?custom=Custom")]
    public void GetPathByRouteValues_UsesFirstTemplateThatSucceeds(string[] routeNames, string[] routeValues, string expectedPath)
    {
        // Arrange
        var endpointControllerAction = EndpointFactory.CreateRouteEndpoint(
            "Home/Index",
            order: 3,
            defaults: new { controller = "Home", action = "Index", },
            requiredValues: new { controller = "Home", action = "Index", });
        var endpointController = EndpointFactory.CreateRouteEndpoint(
            "Home",
            order: 2,
            defaults: new { controller = "Home", action = "Index", },
            requiredValues: new { controller = "Home", action = "Index", });
        var endpointEmpty = EndpointFactory.CreateRouteEndpoint(
            "",
            order: 1,
            defaults: new { controller = "Home", action = "Index", },
            requiredValues: new { controller = "Home", action = "Index", });

        // This endpoint should be used to generate the link when an id is present
        var endpointControllerActionParameter = EndpointFactory.CreateRouteEndpoint(
            "Home/Index/{id}",
            order: 0,
            defaults: new { controller = "Home", action = "Index", },
            requiredValues: new { controller = "Home", action = "Index", });

        var linkGenerator = CreateLinkGenerator(endpointControllerAction, endpointController, endpointEmpty, endpointControllerActionParameter);

        var httpContext = CreateHttpContext();
        httpContext.Request.RouteValues = new RouteValueDictionary(new { controller = "Home", action = "Index" });

        var values = new RouteValueDictionary();
        for (int i = 0; i < routeNames.Length; i++)
        {
            values[routeNames[i]] = routeValues[i];
        }

        // Act
        var generatedPath = linkGenerator.GetPathByRouteValues(
            httpContext,
            routeName: null,
            values: values);

        // Assert
        Assert.Equal(expectedPath, generatedPath);
    }

    [Theory]
    [InlineData(new string[] { }, new string[] { }, "/")]
    [InlineData(new string[] { "id" }, new string[] { "3" }, "/Home/Index/3")]
    [InlineData(new string[] { "custom" }, new string[] { "Custom" }, "/?custom=Custom")]
    [InlineData(new string[] { "controller", "action", "id" }, new string[] { "Home", "Login", "3" }, "/Home/Login/3")]
    [InlineData(new string[] { "controller", "action", "id" }, new string[] { "Home", "Fake", "3" }, null)]
    public void GetPathByRouteValues_ParameterMatchesRequireValues_HasAmbientValues(string[] routeNames, string[] routeValues, string expectedPath)
    {
        // Arrange
        var homeIndex = EndpointFactory.CreateRouteEndpoint(
            "{controller}/{action}/{id?}",
            defaults: new { controller = "Home", action = "Index", },
            requiredValues: new { controller = "Home", action = "Index", });
        var homeLogin = EndpointFactory.CreateRouteEndpoint(
            "{controller}/{action}/{id?}",
            defaults: new { controller = "Home", action = "Index", },
            requiredValues: new { controller = "Home", action = "Login", });

        var linkGenerator = CreateLinkGenerator(homeIndex, homeLogin);

        var httpContext = CreateHttpContext();
        httpContext.Request.RouteValues = new RouteValueDictionary(new { controller = "Home", action = "Index", });

        var values = new RouteValueDictionary();
        for (int i = 0; i < routeNames.Length; i++)
        {
            values[routeNames[i]] = routeValues[i];
        }

        // Act
        var generatedPath = linkGenerator.GetPathByRouteValues(
            httpContext,
            routeName: null,
            values: values);

        // Assert
        Assert.Equal(expectedPath, generatedPath);
    }

    [Theory]
    [InlineData(new string[] { }, new string[] { }, null)]
    [InlineData(new string[] { "id" }, new string[] { "3" }, null)]
    [InlineData(new string[] { "custom" }, new string[] { "Custom" }, null)]
    [InlineData(new string[] { "controller", "action", "id" }, new string[] { "Home", "Login", "3" }, "/Home/Login/3")]
    [InlineData(new string[] { "controller", "action", "id" }, new string[] { "Home", "Fake", "3" }, null)]
    public void GetPathByRouteValues_ParameterMatchesRequireValues_NoAmbientValues(string[] routeNames, string[] routeValues, string expectedPath)
    {
        // Arrange
        var homeIndex = EndpointFactory.CreateRouteEndpoint(
            "{controller}/{action}/{id?}",
            defaults: new { controller = "Home", action = "Index", },
            requiredValues: new { controller = "Home", action = "Index", });
        var homeLogin = EndpointFactory.CreateRouteEndpoint(
            "{controller}/{action}/{id?}",
            defaults: new { controller = "Home", action = "Index", },
            requiredValues: new { controller = "Home", action = "Login", });

        var linkGenerator = CreateLinkGenerator(homeIndex, homeLogin);

        var httpContext = CreateHttpContext();

        var values = new RouteValueDictionary();
        for (int i = 0; i < routeNames.Length; i++)
        {
            values[routeNames[i]] = routeValues[i];
        }

        // Act
        var generatedPath = linkGenerator.GetPathByRouteValues(
            httpContext,
            routeName: null,
            values: values);

        // Assert
        Assert.Equal(expectedPath, generatedPath);
    }

    protected override void AddAdditionalServices(IServiceCollection services)
    {
        services.AddSingleton<IEndpointAddressScheme<int>, IntAddressScheme>();
    }

    private class IntAddressScheme : IEndpointAddressScheme<int>
    {
        private readonly EndpointDataSource _dataSource;

        public IntAddressScheme(EndpointDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public IEnumerable<Endpoint> FindEndpoints(int address)
        {
            return _dataSource.Endpoints.Where(e => e.Metadata.GetMetadata<IntMetadata>().Value == address);
        }
    }

    private class IntMetadata
    {
        public IntMetadata(int value)
        {
            Value = value;
        }
        public int Value { get; }
    }
}
