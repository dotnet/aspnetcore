// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Routing;

// Detailed coverage for how DefaultLinkGenerator processes templates
public class DefaultLinkGeneratorProcessTemplateTest : LinkGeneratorTestBase
{
    [Fact]
    public void TryProcessTemplate_EncodesIntermediate_DefaultValues()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{p1}/{p2=a b}/{p3=foo}");
        var linkGenerator = CreateLinkGenerator(endpoint);

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: null,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { p1 = "Home", p3 = "bar", }),
            ambientValues: null,
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/Home/a%20b/bar", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());
    }

    [Theory]
    [InlineData("a/b/c", "/Home/Index/a%2Fb%2Fc")]
    [InlineData("a/b b1/c c1", "/Home/Index/a%2Fb%20b1%2Fc%20c1")]
    public void TryProcessTemplate_EncodesValue_OfSingleAsteriskCatchAllParameter(string routeValue, string expected)
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{*path}");
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues: new { controller = "Home", action = "Index" });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { path = routeValue, }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal(expected, result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());
    }

    [Theory]
    [InlineData("/", "/Home/Index//")]
    [InlineData("a", "/Home/Index/a")]
    [InlineData("a/", "/Home/Index/a/")]
    [InlineData("a/b", "/Home/Index/a/b")]
    [InlineData("a/b/c", "/Home/Index/a/b/c")]
    [InlineData("a/b/cc", "/Home/Index/a/b/cc")]
    [InlineData("a/b/c/", "/Home/Index/a/b/c/")]
    [InlineData("a/b/c//", "/Home/Index/a/b/c//")]
    [InlineData("a//b//c", "/Home/Index/a//b//c")]
    public void TryProcessTemplate_DoesNotEncodeSlashes_OfDoubleAsteriskCatchAllParameter(string routeValue, string expected)
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{**path}");
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues: new { controller = "Home", action = "Index" });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { path = routeValue, }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal(expected, result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());
    }

    [Fact]
    public void TryProcessTemplate_EncodesContentOtherThanSlashes_OfDoubleAsteriskCatchAllParameter()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{**path}");
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues: new { controller = "Home", action = "Index" });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { path = "a/b b1/c c1" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/Home/Index/a/b%20b1/c%20c1", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());
    }

    [Fact]
    public void TryProcessTemplate_EncodesValues()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}");
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues: new { controller = "Home", action = "Index" });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { name = "name with %special #characters" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/Home/Index", result.path.ToUriComponent());
        Assert.Equal("?name=name%20with%20%25special%20%23characters", result.query.ToUriComponent());
    }

    [Fact]
    public void TryProcessTemplate_ForListOfStrings()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}");
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues: new { controller = "Home", action = "Index" });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { color = new List<string> { "red", "green", "blue" } }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/Home/Index", result.path.ToUriComponent());
        Assert.Equal("?color=red&color=green&color=blue", result.query.ToUriComponent());
    }

    [Fact]
    public void TryProcessTemplate_ForListOfInts()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}");
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues: new { controller = "Home", action = "Index" });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { items = new List<int> { 10, 20, 30 } }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/Home/Index", result.path.ToUriComponent());
        Assert.Equal("?items=10&items=20&items=30", result.query.ToUriComponent());
    }

    [Fact]
    public void TryProcessTemplate_ForList_Empty()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}");
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues: new { controller = "Home", action = "Index" });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { color = new List<string> { } }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/Home/Index", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());
    }

    [Fact]
    public void TryProcessTemplate_ForList_StringWorkaround()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}");
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues: new { controller = "Home", action = "Index" });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { page = 1, color = new List<string> { "red", "green", "blue" }, message = "textfortest" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/Home/Index", result.path.ToUriComponent());
        Assert.Equal("?page=1&color=red&color=green&color=blue&message=textfortest", result.query.ToUriComponent());
    }

    [Fact]
    public void TryProcessTemplate_Success_AmbientValues()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}");
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues: new { controller = "Home" });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { action = "Index" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/Home/Index", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());
    }

    [Fact]
    public void TryProcessTemplate_GeneratesLowercaseUrl_SetOnRouteOptions()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}");
        Action<IServiceCollection> configure = (s) =>
        {
            s.Configure<RouteOptions>(o => o.LowercaseUrls = true);
        };

        var linkGenerator = CreateLinkGenerator(configure, endpoints: new[] { endpoint, });
        var httpContext = CreateHttpContext(ambientValues: new { controller = "Home" });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { action = "Index" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/home/index", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());
    }

    // Regression test for https://github.com/aspnet/Routing/issues/802
    [Fact]
    public void TryProcessTemplate_GeneratesLowercaseUrl_Includes_BufferedValues_SetOnRouteOptions()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("Foo/{bar=BAR}/{id?}");
        Action<IServiceCollection> configure = (s) =>
        {
            s.Configure<RouteOptions>(o => o.LowercaseUrls = true);
        };

        var linkGenerator = CreateLinkGenerator(configure, endpoints: new[] { endpoint, });
        var httpContext = CreateHttpContext();

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { id = "18" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/foo/bar/18", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());
    }

    // Regression test for https://github.com/aspnet/Routing/issues/802
    [Fact]
    public void TryProcessTemplate_ParameterPolicy_Includes_BufferedValues()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("Foo/{bar=MyBar}/{id?}", policies: new { bar = new SlugifyParameterTransformer(), });
        var linkGenerator = CreateLinkGenerator(endpoints: new[] { endpoint, });
        var httpContext = CreateHttpContext();

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { id = "18" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/Foo/my-bar/18", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());
    }

    // Regression test for aspnet/Routing#435
    //
    // In this issue we used to lowercase URLs after parameters were encoded, meaning that if a character needed
    // encoding (such as a cyrillic character, it would not be encoded).
    [Fact]
    public void TryProcessTemplate_GeneratesLowercaseUrl_SetOnRouteOptions_CanLowercaseCharactersThatNeedEncoding()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}");
        Action<IServiceCollection> configure = (s) =>
        {
            s.Configure<RouteOptions>(o => o.LowercaseUrls = true);
        };

        var linkGenerator = CreateLinkGenerator(configure, endpoints: new[] { endpoint, });
        var httpContext = CreateHttpContext(ambientValues: new { controller = "Home" });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { action = "П" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext), // Cryillic uppercase Pe
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/home/%D0%BF", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());

        // Convert back to decoded.
        //
        // This is Cyrillic lowercase Pe (not an n).
        Assert.Equal("/home/п", PathString.FromUriComponent(result.path.ToUriComponent()).Value);
    }

    [Fact]
    public void TryProcessTemplate_GeneratesLowercaseQueryString_SetOnRouteOptions()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}");
        Action<IServiceCollection> configure = (s) =>
        {
            s.Configure<RouteOptions>(o =>
            {
                o.LowercaseUrls = true;
                o.LowercaseQueryStrings = true;
            });
        };

        var linkGenerator = CreateLinkGenerator(
            configure,
            endpoints: new[] { endpoint, });
        var httpContext = CreateHttpContext(ambientValues: new { controller = "Home" });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { action = "Index", ShowStatus = "True", INFO = "DETAILED" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/home/index", result.path.ToUriComponent());
        Assert.Equal("?showstatus=true&info=detailed", result.query.ToUriComponent());
    }

    [Fact]
    public void TryProcessTemplate_AppendsTrailingSlash_SetOnRouteOptions()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}");
        Action<IServiceCollection> configure = (s) =>
        {
            s.Configure<RouteOptions>(o => o.AppendTrailingSlash = true);
        };

        var linkGenerator = CreateLinkGenerator(
            configure,
            endpoints: new[] { endpoint });
        var httpContext = CreateHttpContext(ambientValues: new { controller = "Home" });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { action = "Index" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/Home/Index/", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());
    }

    [Fact]
    public void TryProcessTemplate_GeneratesLowercaseQueryStringAndTrailingSlash_SetOnRouteOptions()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}");
        Action<IServiceCollection> configure = (s) =>
        {
            s.Configure<RouteOptions>(o =>
            {
                o.LowercaseUrls = true;
                o.LowercaseQueryStrings = true;
                o.AppendTrailingSlash = true;
            });
        };

        var linkGenerator = CreateLinkGenerator(
            configure,
            endpoints: new[] { endpoint });
        var httpContext = CreateHttpContext(ambientValues: new { controller = "Home" });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { action = "Index", ShowStatus = "True", INFO = "DETAILED" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/home/index/", result.path.ToUriComponent());
        Assert.Equal("?showstatus=true&info=detailed", result.query.ToUriComponent());
    }

    [Fact]
    public void TryProcessTemplate_LowercaseUrlSetToTrue_OnRouteOptions_OverridenByCallsiteValue()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}");
        Action<IServiceCollection> configure = (s) =>
        {
            s.Configure<RouteOptions>(o => o.LowercaseUrls = true);
        };

        var linkGenerator = CreateLinkGenerator(
            configure,
            endpoints: new[] { endpoint });
        var httpContext = CreateHttpContext(ambientValues: new { controller = "HoMe" });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { action = "InDex" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: new LinkOptions
            {
                LowercaseUrls = false
            },
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/HoMe/InDex", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());
    }

    [Fact]
    public void TryProcessTemplate_LowercaseUrlSetToFalse_OnRouteOptions_OverridenByCallsiteValue()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}");
        Action<IServiceCollection> configure = (s) =>
        {
            s.Configure<RouteOptions>(o => o.LowercaseUrls = false);
        };

        var linkGenerator = CreateLinkGenerator(
            configure,
            endpoints: new[] { endpoint });
        var httpContext = CreateHttpContext(ambientValues: new { controller = "HoMe" });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { action = "InDex" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: new LinkOptions()
            {
                LowercaseUrls = true
            },
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/home/index", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());
    }

    [Fact]
    public void TryProcessTemplate_LowercaseUrlQueryStringsSetToTrue_OnRouteOptions_OverridenByCallsiteValue()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}");
        Action<IServiceCollection> configure = (s) =>
        {
            s.Configure<RouteOptions>(o =>
            {
                o.LowercaseUrls = true;
                o.LowercaseQueryStrings = true;
            });
        };

        var linkGenerator = CreateLinkGenerator(
            configure,
            endpoints: new[] { endpoint });
        var httpContext = CreateHttpContext(ambientValues: new { controller = "Home" });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { action = "Index", ShowStatus = "True", INFO = "DETAILED" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: new LinkOptions
            {
                LowercaseUrls = false,
                LowercaseQueryStrings = false
            },
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/Home/Index", result.path.ToUriComponent());
        Assert.Equal("?ShowStatus=True&INFO=DETAILED", result.query.ToUriComponent());
    }

    [Fact]
    public void TryProcessTemplate_LowercaseUrlQueryStringsSetToFalse_OnRouteOptions_OverridenByCallsiteValue()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}");
        Action<IServiceCollection> configure = (s) =>
        {
            s.Configure<RouteOptions>(o =>
            {
                o.LowercaseUrls = false;
                o.LowercaseQueryStrings = false;
            });
        };

        var linkGenerator = CreateLinkGenerator(
            configure,
            endpoints: new[] { endpoint });
        var httpContext = CreateHttpContext(ambientValues: new { controller = "Home" });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { action = "Index", ShowStatus = "True", INFO = "DETAILED" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: new LinkOptions()
            {
                LowercaseUrls = true,
                LowercaseQueryStrings = true,
            },
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/home/index", result.path.ToUriComponent());
        Assert.Equal("?showstatus=true&info=detailed", result.query.ToUriComponent());
    }

    [Fact]
    public void TryProcessTemplate_AppendTrailingSlashSetToFalse_OnRouteOptions_OverridenByCallsiteValue()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}");
        Action<IServiceCollection> configure = (s) =>
        {
            s.Configure<RouteOptions>(o => o.AppendTrailingSlash = false);
        };

        var linkGenerator = CreateLinkGenerator(
            configure,
            endpoints: new[] { endpoint });
        var httpContext = CreateHttpContext(ambientValues: new { controller = "Home" });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { action = "Index" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: new LinkOptions() { AppendTrailingSlash = true, },
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/Home/Index/", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());
    }

    [Fact]
    public void RouteGenerationRejectsConstraints()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint(
            "{p1}/{p2}",
            defaults: new { p2 = "catchall" },
            policies: new { p2 = "\\d{4}" });
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues: new { });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { p1 = "abcd" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.False(success);
    }

    [Fact]
    public void RouteGenerationAcceptsConstraints()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint(
            "{p1}/{p2}",
            defaults: new { p2 = "catchall" },
            policies: new { p2 = new RegexRouteConstraint("\\d{4}"), });
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues: new { });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { p1 = "hello", p2 = "1234" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/hello/1234", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());
    }

    [Fact]
    public void RouteWithCatchAllRejectsConstraints()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint(
            "{p1}/{*p2}",
            defaults: new { p2 = "catchall" },
            policies: new { p2 = new RegexRouteConstraint("\\d{4}") });
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues: new { });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { p1 = "abcd" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.False(success);
    }

    [Fact]
    public void RouteWithCatchAllAcceptsConstraints()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint(
            "{p1}/{*p2}",
            defaults: new { p2 = "catchall" },
            policies: new { p2 = new RegexRouteConstraint("\\d{4}") });
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues: new { });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { p1 = "hello", p2 = "1234" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/hello/1234", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());
    }

    [Fact]
    public void GetLinkWithNonParameterConstraintReturnsUrlWithoutQueryString()
    {
        // Arrange
        var target = new Mock<IRouteConstraint>();
        target
            .Setup(
                e => e.Match(
                    It.IsAny<HttpContext>(),
                    It.IsAny<IRouter>(),
                    It.IsAny<string>(),
                    It.IsAny<RouteValueDictionary>(),
                    It.IsAny<RouteDirection>()))
            .Returns(true)
            .Verifiable();
        var endpoint = EndpointFactory.CreateRouteEndpoint(
            "{p1}/{p2}",
            defaults: new { p2 = "catchall" },
            policies: new { p2 = target.Object });
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues: new { });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { p1 = "hello", p2 = "1234" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/hello/1234", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());

        target.VerifyAll();
    }

    // Any ambient values from the current request should be visible to constraint, even
    // if they have nothing to do with the route generating a link
    [Fact]
    public void TryProcessTemplate_ConstraintsSeeAmbientValues()
    {
        // Arrange
        var constraint = new CapturingConstraint();
        var endpoint = EndpointFactory.CreateRouteEndpoint(
            template: "slug/Home/Store",
            defaults: new { controller = "Home", action = "Store" },
            policies: new { c = constraint });
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(
            ambientValues: new { controller = "Home", action = "Blog", extra = "42" });
        var expectedValues = new RouteValueDictionary(
            new { controller = "Home", action = "Store", extra = "42" });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { action = "Store" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/slug/Home/Store", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());

        Assert.Equal(expectedValues.OrderBy(kvp => kvp.Key), constraint.Values.OrderBy(kvp => kvp.Key));
    }

    // Non-parameter default values from the routing generating a link are not in the 'values'
    // collection when constraints are processed.
    [Fact]
    public void TryProcessTemplate_ConstraintsDontSeeDefaults_WhenTheyArentParameters()
    {
        // Arrange
        var constraint = new CapturingConstraint();
        var endpoint = EndpointFactory.CreateRouteEndpoint(
            template: "slug/Home/Store",
            defaults: new { controller = "Home", action = "Store", otherthing = "17" },
            policies: new { c = constraint });
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues: new { controller = "Home", action = "Blog" });
        var expectedValues = new RouteValueDictionary(
            new { controller = "Home", action = "Store" });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { action = "Store" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/slug/Home/Store", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());

        Assert.Equal(expectedValues.OrderBy(kvp => kvp.Key), constraint.Values.OrderBy(kvp => kvp.Key));
    }

    // Default values are visible to the constraint when they are used to fill a parameter.
    [Fact]
    public void TryProcessTemplate_ConstraintsSeesDefault_WhenThereItsAParameter()
    {
        // Arrange
        var constraint = new CapturingConstraint();
        var endpoint = EndpointFactory.CreateRouteEndpoint(
            template: "slug/{controller}/{action}",
            defaults: new { action = "Index" },
            policies: new { c = constraint, });
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues: new { controller = "Home", action = "Blog" });
        var expectedValues = new RouteValueDictionary(
            new { controller = "Shopping", action = "Index" });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { controller = "Shopping" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/slug/Shopping", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());
        Assert.Equal(expectedValues, constraint.Values);
    }

    // Default values from the routing generating a link are in the 'values' collection when
    // constraints are processed - IFF they are specified as values or ambient values.
    [Fact]
    public void TryProcessTemplate_ConstraintsSeeDefaults_IfTheyAreSpecifiedOrAmbient()
    {
        // Arrange
        var constraint = new CapturingConstraint();
        var endpoint = EndpointFactory.CreateRouteEndpoint(
            template: "slug/Home/Store",
            defaults: new { controller = "Home", action = "Store", otherthing = "17", thirdthing = "13" },
            policies: new { c = constraint, });
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(
            ambientValues: new { controller = "Home", action = "Blog", otherthing = "17" });

        var expectedValues = new RouteValueDictionary(
            new { controller = "Home", action = "Store", otherthing = "17", thirdthing = "13" });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { action = "Store", thirdthing = "13" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/slug/Home/Store", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());

        Assert.Equal(expectedValues.OrderBy(kvp => kvp.Key), constraint.Values.OrderBy(kvp => kvp.Key));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TryProcessTemplate_InlineConstraints_Success(bool hasHttpContext)
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint(
            template: "Home/Index/{id:int}",
            defaults: new { controller = "Home", action = "Index" },
            policies: new { });
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = hasHttpContext ? CreateHttpContext(new { }) : null;

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { action = "Index", controller = "Home", id = 4 }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/Home/Index/4", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());
    }

    [Fact]
    public void TryProcessTemplate_InlineConstraints_NonMatchingvalue()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint(
            template: "Home/Index/{id}",
            defaults: new { controller = "Home", action = "Index" },
            policies: new { id = "int" });
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues: new { });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { action = "Index", controller = "Home", id = "not-an-integer" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.False(success);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TryProcessTemplate_InlineConstraints_OptionalParameter_ValuePresent(bool hasHttpContext)
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint(
            template: "Home/Index/{id:int?}",
            defaults: new { controller = "Home", action = "Index" },
            policies: new { });
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = hasHttpContext ? CreateHttpContext(new { }) : null;

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { action = "Index", controller = "Home", id = 98 }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/Home/Index/98", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());
    }

    [Fact]
    public void TryProcessTemplate_InlineConstraints_OptionalParameter_ValueNotPresent()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint(
            template: "Home/Index/{id?}",
            defaults: new { controller = "Home", action = "Index" },
            policies: new { id = "int" });
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues: new { });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { action = "Index", controller = "Home" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/Home/Index", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());
    }

    [Fact]
    public void TryProcessTemplate_InlineConstraints_OptionalParameter_ValuePresent_ConstraintFails()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint(
            template: "Home/Index/{id?}",
            defaults: new { controller = "Home", action = "Index" },
            policies: new { id = "int" });
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues: new { });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { action = "Index", controller = "Home", id = "not-an-integer" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.False(success);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TryProcessTemplate_InlineConstraints_MultipleInlineConstraints(bool hasHttpContext)
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint(
            template: "Home/Index/{id:int:range(1,20)}",
            defaults: new { controller = "Home", action = "Index" },
            policies: new { });
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = hasHttpContext ? CreateHttpContext(new { }) : null;

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { action = "Index", controller = "Home", id = 14 }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/Home/Index/14", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TryProcessTemplate_InlineConstraints_CompositeInlineConstraint_Fails(bool hasHttpContext)
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint(
            template: "Home/Index/{id:int:range(1,20)}",
            defaults: new { controller = "Home", action = "Index" },
            policies: new { });
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = hasHttpContext ? CreateHttpContext(new { }) : null;

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { action = "Index", controller = "Home", id = 50 }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.False(success);
    }

    [Fact]
    public void TryProcessTemplate_InlineConstraints_CompositeConstraint_FromConstructor()
    {
        // Arrange
        var constraint = new MaxLengthRouteConstraint(20);
        var endpoint = EndpointFactory.CreateRouteEndpoint(
            template: "Home/Index/{name}",
            defaults: new { controller = "Home", action = "Index" },
            policies: new { name = constraint });
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues: new { });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { action = "Index", controller = "Home", name = "products" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/Home/Index/products", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());
    }

    [Fact]
    public void TryProcessTemplate_OptionalParameter_ParameterPresentInValues()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{name?}");
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues: new { });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { action = "Index", controller = "Home", name = "products" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/Home/Index/products", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());
    }

    [Fact]
    public void TryProcessTemplate_OptionalParameter_ParameterNotPresentInValues()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{name?}");
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues: new { });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { action = "Index", controller = "Home" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/Home/Index", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());
    }

    [Fact]
    public void TryProcessTemplate_OptionalParameter_ParameterPresentInValuesAndDefaults()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint(
            template: "{controller}/{action}/{name}",
            defaults: new { name = "default-products" });
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues: new { });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { action = "Index", controller = "Home", name = "products" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/Home/Index/products", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());
    }

    [Fact]
    public void TryProcessTemplate_OptionalParameter_ParameterNotPresentInValues_PresentInDefaults()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint(
            template: "{controller}/{action}/{name}",
            defaults: new { name = "products" });
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues: new { });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { action = "Index", controller = "Home" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/Home/Index", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());
    }

    [Fact]
    public void TryProcessTemplate_ParameterNotPresentInTemplate_PresentInValues()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{name}");
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues: new { });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { action = "Index", controller = "Home", name = "products", format = "json" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/Home/Index/products", result.path.ToUriComponent());
        Assert.Equal("?format=json", result.query.ToUriComponent());
    }

    [Fact]
    public void TryProcessTemplate_OptionalParameter_FollowedByDotAfterSlash_ParameterPresent()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint(
            template: "{controller}/{action}/.{name?}");
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues: new { });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { action = "Index", controller = "Home", name = "products" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/Home/Index/.products", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());
    }

    [Fact]
    public void TryProcessTemplate_OptionalParameter_FollowedByDotAfterSlash_ParameterNotPresent()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/.{name?}");
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues: new { });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { action = "Index", controller = "Home" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        Assert.True(success);
        Assert.Equal("/Home/Index/", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());
    }

    [Fact]
    public void TryProcessTemplate_OptionalParameter_InSimpleSegment()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{name?}");
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues: new { });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { action = "Index", controller = "Home" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/Home/Index", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());
    }

    [Fact]
    public void TryProcessTemplate_TwoOptionalParameters_OneValueFromAmbientValues()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("a/{b=15}/{c?}/{d?}");
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues: new { c = "17" });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/a/15/17", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());
    }

    [Fact]
    public void TryProcessTemplate_OptionalParameterAfterDefault_OneValueFromAmbientValues()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("a/{b=15}/{c?}");
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues: new { c = "17" });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/a/15/17", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());
    }

    [Fact]
    public void TryProcessTemplate_TwoOptionalParametersAfterDefault_LastValueFromAmbientValues()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("a/{b=15}/{c?}/{d?}");
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues: new { d = "17" });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/a", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());
    }

    public static TheoryData<object, object, object, object> DoesNotDiscardAmbientValuesData
    {
        get
        {
            // - ambient values
            // - explicit values
            // - required values
            // - defaults
            return new TheoryData<object, object, object, object>
                {
                    // link to same action on same controller
                    {
                        new { controller = "Products", action = "Edit", id = 10 },
                        new { controller = "Products", action = "Edit" },
                        new { area = (string)null, controller = "Products", action = "Edit", page = (string)null },
                        new { area = (string)null, controller = "Products", action = "Edit", page = (string)null }
                    },

                    // link to same action on same controller - ignoring case
                    {
                        new { controller = "ProDUcts", action = "EDit", id = 10 },
                        new { controller = "ProDUcts", action = "EDit" },
                        new { area = (string)null, controller = "Products", action = "Edit", page = (string)null },
                        new { area = (string)null, controller = "Products", action = "Edit", page = (string)null }
                    },

                    // link to same action and same controller on same area
                    {
                        new { area = "Admin", controller = "Products", action = "Edit", id = 10 },
                        new { area = "Admin", controller = "Products", action = "Edit" },
                        new { area = "Admin", controller = "Products", action = "Edit", page = (string)null },
                        new { area = "Admin", controller = "Products", action = "Edit", page = (string)null }
                    },

                    // link to same action and same controller on same area
                    {
                        new { area = "Admin", controller = "Products", action = "Edit", id = 10 },
                        new { controller = "Products", action = "Edit" },
                        new { area = "Admin", controller = "Products", action = "Edit", page = (string)null },
                        new { area = "Admin", controller = "Products", action = "Edit", page = (string)null }
                    },

                    // link to same action and same controller
                    {
                        new { controller = "Products", action = "Edit", id = 10 },
                        new { controller = "Products", action = "Edit" },
                        new { area = (string)null, controller = "Products", action = "Edit", page = (string)null },
                        new { area = (string)null, controller = "Products", action = "Edit", page = (string)null }
                    },
                    {
                        new { controller = "Products", action = "Edit", id = 10 },
                        new { controller = "Products", action = "Edit" },
                        new { area = (string)null, controller = "Products", action = "Edit", page = (string)null },
                        new { area = (string)null, controller = "Products", action = "Edit", page = (string)null }
                    },
                    {
                        new { controller = "Products", action = "Edit", id = 10 },
                        new { controller = "Products", action = "Edit" },
                        new { area = "", controller = "Products", action = "Edit", page = "" },
                        new { area = "", controller = "Products", action = "Edit", page = "" }
                    },

                    // link to same page
                    {
                        new { page = "Products/Edit", id = 10 },
                        new { page = "Products/Edit" },
                        new { area = (string)null, controller = (string)null, action = (string)null, page = "Products/Edit" },
                        new { area = (string)null, controller = (string)null, action = (string)null, page = "Products/Edit" }
                    },
                };
        }
    }

    [Theory]
    [MemberData(nameof(DoesNotDiscardAmbientValuesData))]
    public void TryProcessTemplate_DoesNotDiscardAmbientValues_IfAllRequiredKeysMatch(
        object ambientValues,
        object explicitValues,
        object requiredValues,
        object defaults)
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint(
            "Products/Edit/{id}",
            requiredValues: requiredValues,
            defaults: defaults);
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues);

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(explicitValues),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/Products/Edit/10", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());
    }

    [Fact]
    public void TryProcessTemplate_DoesNotDiscardAmbientValues_IfAllRequiredValuesMatch_ForGenericKeys()
    {
        // Verifying that discarding works in general usage case i.e when keys are not like controller, action etc.

        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint(
            "Products/Edit/{id}",
            requiredValues: new { c = "Products", a = "Edit" },
            defaults: new { c = "Products", a = "Edit" });
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues: new { c = "Products", a = "Edit", id = 10 });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { c = "Products", a = "Edit" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("/Products/Edit/10", result.path.ToUriComponent());
        Assert.Equal(string.Empty, result.query.ToUriComponent());
    }

    [Fact]
    public void TryProcessTemplate_DiscardsAmbientValues_ForGenericKeys()
    {
        // Verifying that discarding works in general usage case i.e when keys are not like controller, action etc.

        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint(
            "Products/Edit/{id}",
            requiredValues: new { c = "Products", a = "Edit" },
            defaults: new { c = "Products", a = "Edit" });
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues: new { c = "Products", a = "Edit", id = 10 });

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(new { c = "Products", a = "List" }),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.False(success);
    }

    public static TheoryData<object, object, object, object> DiscardAmbientValuesData
    {
        get
        {
            // - ambient values
            // - explicit values
            // - required values
            // - defaults
            return new TheoryData<object, object, object, object>
                {
                    // link to different action on same controller
                    {
                        new { controller = "Products", action = "Edit", id = 10 },
                        new { controller = "Products", action = "List" },
                        new { area = (string)null, controller = "Products", action = "List", page = (string)null },
                        new { area = (string)null, controller = "Products", action = "List", page = (string)null }
                    },

                    // link to different action on same controller and same area
                    {
                        new { area = "Customer", controller = "Products", action = "Edit", id = 10 },
                        new { area = "Customer", controller = "Products", action = "List" },
                        new { area = "Customer", controller = "Products", action = "List", page = (string)null },
                        new { area = "Customer", controller = "Products", action = "List", page = (string)null }
                    },

                    // link from one area to a different one
                    {
                        new { area = "Admin", controller = "Products", action = "Edit", id = 10 },
                        new { area = "Consumer", controller = "Products", action = "Edit" },
                        new { area = "Consumer", controller = "Products", action = "Edit", page = (string)null },
                        new { area = "Consumer", controller = "Products", action = "Edit", page = (string)null }
                    },

                    // link from non-area to a area one
                    {
                        new { controller = "Products", action = "Edit", id = 10 },
                        new { area = "Consumer", controller = "Products", action = "Edit" },
                        new { area = "Consumer", controller = "Products", action = "Edit", page = (string)null },
                        new { area = "Consumer", controller = "Products", action = "Edit", page = (string)null }
                    },

                    // link from area to a non-area based action
                    {
                        new { area = "Admin", controller = "Products", action = "Edit", id = 10 },
                        new { area = "", controller = "Products", action = "Edit" },
                        new { area = "", controller = "Products", action = "Edit", page = (string)null },
                        new { area = "", controller = "Products", action = "Edit", page = (string)null }
                    },

                    // link from controller-action to a page
                    {
                        new { controller = "Products", action = "Edit", id = 10 },
                        new { page = "Products/Edit" },
                        new { area = (string)null, controller = (string)null, action = (string)null, page = "Products/Edit"},
                        new { area = (string)null, controller = (string)null, action = (string)null, page = "Products/Edit"}
                    },

                    // link from a page to controller-action
                    {
                        new { page = "Products/Edit", id = 10 },
                        new { controller = "Products", action = "Edit" },
                        new { area = (string)null, controller = "Products", action = "Edit", page = (string)null },
                        new { area = (string)null, controller = "Products", action = "Edit", page = (string)null }
                    },

                    // link from one page to a different page
                    {
                        new { page = "Products/Details", id = 10 },
                        new { page = "Products/Edit" },
                        new { area = (string)null, controller = (string)null, action = (string)null, page = "Products/Edit" },
                        new { area = (string)null, controller = (string)null, action = (string)null, page = "Products/Edit" }
                    },
                };
        }
    }

    [Theory]
    [MemberData(nameof(DiscardAmbientValuesData))]
    public void TryProcessTemplate_DiscardsAmbientValues_IfAnyAmbientValue_IsDifferentThan_EndpointRequiredValues(
        object ambientValues,
        object explicitValues,
        object requiredValues,
        object defaults)
    {
        // Linking to a different action on the same controller

        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint(
            "Products/Edit/{id}",
            requiredValues: requiredValues,
            defaults: defaults);
        var linkGenerator = CreateLinkGenerator(endpoint);
        var httpContext = CreateHttpContext(ambientValues);

        // Act
        var success = linkGenerator.TryProcessTemplate(
            httpContext: httpContext,
            endpoint: endpoint,
            values: new RouteValueDictionary(explicitValues),
            ambientValues: DefaultLinkGenerator.GetAmbientValues(httpContext),
            options: null,
            result: out var result);

        // Assert
        Assert.False(success);
    }
}
