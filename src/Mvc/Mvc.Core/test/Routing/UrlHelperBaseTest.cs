// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Routing;

public class UrlHelperBaseTest
{
    public static TheoryData<string, string, string> GeneratePathFromRoute_HandlesLeadingAndTrailingSlashesData =>
        new TheoryData<string, string, string>
        {
                {  null, "", "/" },
                {  null, "/", "/"  },
                {  null, "Hello", "/Hello" },
                {  null, "/Hello", "/Hello" },
                { "/", "", "/" },
                { "/", "hello", "/hello" },
                { "/", "/hello", "/hello" },
                { "/hello", "", "/hello" },
                { "/hello/", "", "/hello/" },
                { "/hello", "/", "/hello/" },
                { "/hello/", "world", "/hello/world" },
                { "/hello/", "/world", "/hello/world" },
                { "/hello/", "/world 123", "/hello/world 123" },
                { "/hello/", "/world%20123", "/hello/world%20123" },
        };

    [Theory]
    [MemberData(nameof(GeneratePathFromRoute_HandlesLeadingAndTrailingSlashesData))]
    public void AppendPathAndFragment_HandlesLeadingAndTrailingSlashes(
        string appBase,
        string virtualPath,
        string expected)
    {
        // Arrange
        var services = CreateServices();
        var httpContext = CreateHttpContext(services, appBase, host: null, protocol: null);
        var builder = new StringBuilder();

        // Act
        UrlHelperBase.AppendPathAndFragment(builder, httpContext.Request.PathBase, virtualPath, string.Empty);

        // Assert
        Assert.Equal(expected, builder.ToString());
    }

    [Theory]
    [MemberData(nameof(GeneratePathFromRoute_HandlesLeadingAndTrailingSlashesData))]
    public void AppendPathAndFragment_AppendsFragments(
        string appBase,
        string virtualPath,
        string expected)
    {
        // Arrange
        var fragmentValue = "fragment-value";
        expected += $"#{fragmentValue}";
        var services = CreateServices();
        var httpContext = CreateHttpContext(services, appBase, host: null, protocol: null);
        var builder = new StringBuilder();

        // Act
        UrlHelperBase.AppendPathAndFragment(builder, httpContext.Request.PathBase, virtualPath, fragmentValue);

        // Assert
        Assert.Equal(expected, builder.ToString());
    }

    [Theory]
    [InlineData(null, null, null, "/", null, "/")]
    [InlineData(null, null, null, "/Hello", null, "/Hello")]
    [InlineData(null, null, null, "Hello", null, "/Hello")]
    [InlineData("/", null, null, "", null, "/")]
    [InlineData("/hello/", null, null, "/world", null, "/hello/world")]
    [InlineData("/hello/", "https", "myhost", "/world", "fragment-value", "https://myhost/hello/world#fragment-value")]
    public void GenerateUrl_FastAndSlowPathsReturnsExpected(
        string appBase,
        string protocol,
        string host,
        string virtualPath,
        string fragment,
        string expected)
    {
        // Arrange
        var services = CreateServices();
        var httpContext = CreateHttpContext(services, appBase, host, protocol);
        var actionContext = CreateActionContext(httpContext);
        var urlHelper = new TestUrlHelper(actionContext);

        // Act
        var url = urlHelper.GenerateUrl(protocol, host, virtualPath, fragment);

        // Assert
        Assert.Equal(expected, url);
    }

    private static IServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();
        services.AddRouting();
        services
            .AddSingleton<UrlEncoder>(UrlEncoder.Default);

        return services.BuildServiceProvider();
    }

    private static HttpContext CreateHttpContext(
       IServiceProvider services,
       string appRoot,
       string host,
       string protocol)
    {
        appRoot = string.IsNullOrEmpty(appRoot) ? string.Empty : appRoot;
        host = string.IsNullOrEmpty(host) ? "localhost" : host;

        var context = new DefaultHttpContext();
        context.RequestServices = services;
        context.Request.PathBase = new PathString(appRoot);
        context.Request.Host = new HostString(host);
        context.Request.Scheme = protocol;
        return context;
    }

    private static ActionContext CreateActionContext(HttpContext context)
    {
        return new ActionContext(context, new RouteData(), new ActionDescriptor());
    }

    private class TestUrlHelper : UrlHelperBase
    {
        public TestUrlHelper(ActionContext actionContext) :
            base(actionContext)
        {
        }

        public override string Action(UrlActionContext actionContext)
        {
            throw new NotImplementedException();
        }

        public override string RouteUrl(UrlRouteContext routeContext)
        {
            throw new NotImplementedException();
        }

        public new string GenerateUrl(
            string protocol,
            string host,
            string virtualPath,
            string fragment)
        {
            return base.GenerateUrl(
                protocol,
                host,
                virtualPath,
                fragment);
        }
    }
}
