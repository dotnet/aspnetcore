// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http;

public class DefaultHttpRequestTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(9001)]
    [InlineData(65535)]
    public void GetContentLength_ReturnsParsedHeader(long value)
    {
        // Arrange
        var request = GetRequestWithContentLength(value.ToString(CultureInfo.InvariantCulture));

        // Act and Assert
        Assert.Equal(value, request.ContentLength);
    }

    [Fact]
    public void GetContentLength_ReturnsNullIfHeaderDoesNotExist()
    {
        // Arrange
        var request = GetRequestWithContentLength(contentLength: null);

        // Act and Assert
        Assert.Null(request.ContentLength);
    }

    [Theory]
    [InlineData("cant-parse-this")]
    [InlineData("-1000")]
    [InlineData("1000.00")]
    [InlineData("100/5")]
    public void GetContentLength_ReturnsNullIfHeaderCannotBeParsed(string contentLength)
    {
        // Arrange
        var request = GetRequestWithContentLength(contentLength);

        // Act and Assert
        Assert.Null(request.ContentLength);
    }

    [Fact]
    public void GetContentType_ReturnsNullIfHeaderDoesNotExist()
    {
        // Arrange
        var request = GetRequestWithContentType(contentType: null);

        // Act and Assert
        Assert.Null(request.ContentType);
    }

    [Fact]
    public void Host_GetsHostFromHeaders()
    {
        // Arrange
        const string expected = "localhost:9001";

        var headers = new HeaderDictionary()
            {
                { "Host", expected },
            };

        var request = CreateRequest(headers);

        // Act
        var host = request.Host;

        // Assert
        Assert.Equal(expected, host.Value);
    }

    [Fact]
    public void Host_DecodesPunyCode()
    {
        // Arrange
        const string expected = "löcalhöst";

        var headers = new HeaderDictionary()
            {
                { "Host", "xn--lcalhst-90ae" },
            };

        var request = CreateRequest(headers);

        // Act
        var host = request.Host;

        // Assert
        Assert.Equal(expected, host.Value);
    }

    [Fact]
    public void Host_EncodesPunyCode()
    {
        // Arrange
        const string expected = "xn--lcalhst-90ae";

        var headers = new HeaderDictionary();

        var request = CreateRequest(headers);

        // Act
        request.Host = new HostString("löcalhöst");

        // Assert
        Assert.Equal(expected, headers["Host"][0]);
    }

    [Fact]
    public void IsHttps_CorrectlyReflectsScheme()
    {
        var request = new DefaultHttpContext().Request;
        Assert.Equal(string.Empty, request.Scheme);
        Assert.False(request.IsHttps);
        request.IsHttps = true;
        Assert.Equal("https", request.Scheme);
        request.IsHttps = false;
        Assert.Equal("http", request.Scheme);
        request.Scheme = "ftp";
        Assert.False(request.IsHttps);
        request.Scheme = "HTTPS";
        Assert.True(request.IsHttps);
    }

    [Fact]
    public void Query_GetAndSet()
    {
        var request = new DefaultHttpContext().Request;
        var requestFeature = request.HttpContext.Features.Get<IHttpRequestFeature>();
        Assert.Equal(string.Empty, requestFeature.QueryString);
        Assert.Equal(QueryString.Empty, request.QueryString);
        var query0 = request.Query;
        Assert.NotNull(query0);
        Assert.Equal(0, query0.Count);

        requestFeature.QueryString = "?name0=value0&name1=value1";
        var query1 = request.Query;
        Assert.NotSame(query0, query1);
        Assert.Equal(2, query1.Count);
        Assert.Equal("value0", query1["name0"]);
        Assert.Equal("value1", query1["name1"]);

        var query2 = new QueryCollection(new Dictionary<string, StringValues>()
            {
                { "name2", "value2" }
            });

        request.Query = query2;
        Assert.Same(query2, request.Query);
        Assert.Equal("?name2=value2", requestFeature.QueryString);
        Assert.Equal(new QueryString("?name2=value2"), request.QueryString);
    }

    [Fact]
    public void Cookies_GetAndSet()
    {
        var request = new DefaultHttpContext().Request;
        var cookieHeaders = request.Headers["Cookie"];
        Assert.Equal(0, cookieHeaders.Count);
        var cookies0 = request.Cookies;
        Assert.Empty(cookies0);
        Assert.Null(cookies0["key0"]);
        Assert.False(cookies0.ContainsKey("key0"));

        var newCookies = new[] { "name0=value0%2C", "name1=value1" };
        request.Headers["Cookie"] = newCookies;

        cookies0 = RequestCookieCollection.Parse(newCookies);
        var cookies1 = request.Cookies;
        Assert.Equal(cookies0, cookies1);
        Assert.Equal(2, cookies1.Count);
        Assert.Equal("value0,", cookies1["name0"]);
        Assert.Equal("value1", cookies1["name1"]);
        Assert.Equal(newCookies, request.Headers["Cookie"].ToArray());

        var cookies2 = new RequestCookieCollection(new Dictionary<string, string>()
            {
                { "name2", "value2" }
            });
        request.Cookies = cookies2;
        Assert.Equal(cookies2, request.Cookies);
        Assert.Equal("value2", request.Cookies["name2"]);
        cookieHeaders = request.Headers["Cookie"];
        Assert.Equal(new[] { "name2=value2" }, cookieHeaders.ToArray());
    }

    [Fact]
    public void RouteValues_GetAndSet()
    {
        var context = new DefaultHttpContext();
        var request = context.Request;

        var routeValuesFeature = context.Features.Get<IRouteValuesFeature>();
        // No feature set for initial DefaultHttpRequest
        Assert.Null(routeValuesFeature);

        // Route values returns empty collection by default
        Assert.Empty(request.RouteValues);

        // Get and set value on request route values
        request.RouteValues["new"] = "setvalue";
        Assert.Equal("setvalue", request.RouteValues["new"]);

        routeValuesFeature = context.Features.Get<IRouteValuesFeature>();
        // Accessing DefaultHttpRequest.RouteValues creates feature
        Assert.NotNull(routeValuesFeature);

        request.RouteValues = new RouteValueDictionary(new { key = "value" });
        // Can set DefaultHttpRequest.RouteValues
        Assert.NotNull(request.RouteValues);
        Assert.Equal("value", request.RouteValues["key"]);

        // DefaultHttpRequest.RouteValues uses feature
        Assert.Equal(routeValuesFeature.RouteValues, request.RouteValues);

        // Setting route values to null sets empty collection on request
        routeValuesFeature.RouteValues = null;
        Assert.Empty(request.RouteValues);

        var customRouteValuesFeature = new CustomRouteValuesFeature
        {
            RouteValues = new RouteValueDictionary(new { key = "customvalue" })
        };
        context.Features.Set<IRouteValuesFeature>(customRouteValuesFeature);
        // Can override DefaultHttpRequest.RouteValues with custom feature
        Assert.Equal(customRouteValuesFeature.RouteValues, request.RouteValues);

        // Can clear feature
        context.Features.Set<IRouteValuesFeature>(null);
        Assert.Empty(request.RouteValues);
    }

    [Fact]
    public void BodyReader_CanGet()
    {
        var context = new DefaultHttpContext();
        var bodyPipe = context.Request.BodyReader;
        Assert.NotNull(bodyPipe);
    }

    [Fact]
    public void DebuggerToString_EmptyRequest()
    {
        var context = new DefaultHttpContext();

        var debugText = HttpContextDebugFormatter.RequestToString(context.Request);
        Assert.Equal("(unspecified)", debugText);
    }

    [Fact]
    public void DebuggerToString_HasMethod()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";

        var debugText = HttpContextDebugFormatter.RequestToString(context.Request);
        Assert.Equal("GET (unspecified)", debugText);
    }

    [Fact]
    public void DebuggerToString_HasProtocol()
    {
        var context = new DefaultHttpContext();
        context.Request.Protocol = "HTTP/2";

        var debugText = HttpContextDebugFormatter.RequestToString(context.Request);
        Assert.Equal("(unspecified) HTTP/2", debugText);
    }

    [Fact]
    public void DebuggerToString_HasContentType()
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/json";

        var debugText = HttpContextDebugFormatter.RequestToString(context.Request);
        Assert.Equal("(unspecified) application/json", debugText);
    }

    [Fact]
    public void DebuggerToString_HasQueryString()
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?query=true");

        var debugText = HttpContextDebugFormatter.RequestToString(context.Request);
        Assert.Equal("(unspecified)://(unspecified)?query=true", debugText);
    }

    [Fact]
    public void DebuggerToString_HasCompleteRequestUri()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("localhost", 8080);
        context.Request.Path = "/Path";
        context.Request.PathBase = "/PathBase";
        context.Request.QueryString = new QueryString("?test=true");

        var debugText = HttpContextDebugFormatter.RequestToString(context.Request);
        Assert.Equal("http://localhost:8080/PathBase/Path?test=true", debugText);
    }

    [Fact]
    public void DebuggerToString_HasPartialRequestUri()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("localhost");

        var debugText = HttpContextDebugFormatter.RequestToString(context.Request);
        Assert.Equal("http://localhost", debugText);
    }

    [Fact]
    public void DebuggerToString_HasEverything()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Protocol = "HTTP/2";
        context.Request.ContentType = "application/json";
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("localhost", 8080);
        context.Request.Path = "/Path";
        context.Request.PathBase = "/PathBase";
        context.Request.QueryString = new QueryString("?test=true");

        var debugText = HttpContextDebugFormatter.RequestToString(context.Request);
        Assert.Equal("GET http://localhost:8080/PathBase/Path?test=true HTTP/2 application/json", debugText);
    }

    private class CustomRouteValuesFeature : IRouteValuesFeature
    {
        public RouteValueDictionary RouteValues { get; set; }
    }

    private static HttpRequest CreateRequest(IHeaderDictionary headers)
    {
        var context = new DefaultHttpContext();
        context.Features.Get<IHttpRequestFeature>().Headers = headers;
        return context.Request;
    }

    private static HttpRequest GetRequestWithContentLength(string contentLength = null)
    {
        return GetRequestWithHeader("Content-Length", contentLength);
    }

    private static HttpRequest GetRequestWithContentType(string contentType = null)
    {
        return GetRequestWithHeader("Content-Type", contentType);
    }

    private static HttpRequest GetRequestWithAcceptHeader(string acceptHeader = null)
    {
        return GetRequestWithHeader("Accept", acceptHeader);
    }

    private static HttpRequest GetRequestWithAcceptCharsetHeader(string acceptCharset = null)
    {
        return GetRequestWithHeader("Accept-Charset", acceptCharset);
    }

    private static HttpRequest GetRequestWithHeader(string headerName, string headerValue)
    {
        var headers = new HeaderDictionary();
        if (headerValue != null)
        {
            headers.Add(headerName, headerValue);
        }

        return CreateRequest(headers);
    }
}
