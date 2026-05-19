// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;
public class RoutingServiceCollectionExtensionsTests
{
    [Fact]
    public void AddRouting_ThrowsOnNull_ServicesParameter()
    {
        var ex = Record.Exception(() =>
        {
            RoutingServiceCollectionExtensions.AddRouting(null);
        });

        Assert.IsType<ArgumentNullException>(ex);
        Assert.Equal("services", (ex as ArgumentNullException).ParamName);
    }

    [Fact]
    public void AddRoutingWithOptions_ThrowsOnNull_ConfigureOptionsParameter()
    {
        var services = new ServiceCollection();

        var ex = Record.Exception(() =>
        {
            RoutingServiceCollectionExtensions.AddRouting(services, null);
        });

        Assert.IsType<ArgumentNullException>(ex);
        Assert.Equal("configureOptions", (ex as ArgumentNullException).ParamName);
    }

    [Fact]
    public void AddRoutingWithOptions_ThrowsOnNull_ServicesParameter()
    {
        var ex = Record.Exception(() =>
        {
            RoutingServiceCollectionExtensions.AddRouting(null, options => { });
        });

        Assert.IsType<ArgumentNullException>(ex);
        Assert.Equal("services", (ex as ArgumentNullException).ParamName);
    }

    public class DummyRegexRouteConstraint : IRouteConstraint
    {
        public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
        {
            return true;
        }
    }

    [Fact]
    public void AddRouting_DoesNot_Replace_Existing_RouteConstraint()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions();

        services.Configure<RouteOptions>(options =>
        {
            options.SetParameterPolicy<DummyRegexRouteConstraint>("regex");
        });

        // Act
        services.AddRouting();
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetService<IOptions<RouteOptions>>();
        var regexRouteConstraintType = options.Value.ConstraintMap["regex"];
        Assert.Equal(typeof(DummyRegexRouteConstraint), regexRouteConstraintType);
    }
}
