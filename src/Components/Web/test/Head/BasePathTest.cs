// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;

#nullable enable

namespace Microsoft.AspNetCore.Components;

public class BasePathTest
{
    [Fact]
    public void UsesHttpContextPathBaseWhenPresent()
    {
        var services = CreateServices(out var renderer);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.PathBase = "/Dashboard";

        services.AddService<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = httpContext });

        var componentId = RenderBasePath(renderer);

        Assert.Equal("/Dashboard/", GetHref(renderer, componentId));
    }

    [Fact]
    public void UsesFallbackWhenContextUnavailable()
    {
        var services = CreateServices(out var renderer);

        var component = (BasePath)renderer.InstantiateComponent<BasePath>();
        var componentId = renderer.AssignRootComponentId(component);

        renderer.RenderRootComponent(componentId, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(BasePath.FallbackHref)] = "admin"
        }));

        Assert.Equal("/admin/", GetHref(renderer, componentId));
    }

    [Fact]
    public void FallsBackToNavigationManagerBaseUri()
    {
        var services = CreateServices(out var renderer);

        var component = (BasePath)renderer.InstantiateComponent<BasePath>();
        var componentId = renderer.AssignRootComponentId(component);

        renderer.RenderRootComponent(componentId);

        Assert.Equal("/app/", GetHref(renderer, componentId));
    }

    [Fact]
    public void ExplicitHrefOverridesOtherSources()
    {
        var services = CreateServices(out var renderer);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.PathBase = "/Dashboard";
        services.AddService<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = httpContext });

        var component = (BasePath)renderer.InstantiateComponent<BasePath>();
        var componentId = renderer.AssignRootComponentId(component);

        renderer.RenderRootComponent(componentId, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(BasePath.Href)] = "Reports"
        }));

        Assert.Equal("/Reports/", GetHref(renderer, componentId));
    }

    [Fact]
    public void HrefOverridesFallbackWhenBothProvided()
    {
        var services = CreateServices(out var renderer);

        var component = (BasePath)renderer.InstantiateComponent<BasePath>();
        var componentId = renderer.AssignRootComponentId(component);

        renderer.RenderRootComponent(componentId, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(BasePath.Href)] = "Reports",
            [nameof(BasePath.FallbackHref)] = "admin"
        }));

        Assert.Equal("/Reports/", GetHref(renderer, componentId));
    }

    [Theory]
    [InlineData("/a/b")]
    [InlineData("/a/b/")]
    [InlineData("https://contoso.com/a/b")]
    public void HonorsMultiSegmentHrefNormalization(string input)
    {
        var services = CreateServices(out var renderer);

        var component = (BasePath)renderer.InstantiateComponent<BasePath>();
        var componentId = renderer.AssignRootComponentId(component);

        renderer.RenderRootComponent(componentId, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(BasePath.Href)] = input
        }));

        Assert.Equal("/a/b/", GetHref(renderer, componentId));
    }

    private static TestServiceProvider CreateServices(out TestRenderer renderer)
    {
        var services = new TestServiceProvider();
        var navigationManager = new TestNavigationManager("https://example.com/app/", "https://example.com/app/dashboard");
        services.AddService<NavigationManager>(navigationManager);
        services.AddService<IServiceProvider>(services);

        renderer = new TestRenderer(services);
        return services;
    }

    private static int RenderBasePath(TestRenderer renderer)
    {
        var component = (BasePath)renderer.InstantiateComponent<BasePath>();
        var componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponent(componentId);
        return componentId;
    }

    private static string? GetHref(TestRenderer renderer, int componentId)
    {
        var frames = renderer.GetCurrentRenderTreeFrames(componentId);
        for (var i = 0; i < frames.Count; i++)
        {
            ref readonly var frame = ref frames.Array[i];
            if (frame.FrameType == RenderTreeFrameType.Element && frame.ElementName == "base")
            {
                for (var j = i + 1; j < frames.Count; j++)
                {
                    ref readonly var attribute = ref frames.Array[j];
                    if (attribute.FrameType == RenderTreeFrameType.Attribute && attribute.AttributeName == "href")
                    {
                        return attribute.AttributeValue?.ToString();
                    }

                    if (attribute.FrameType != RenderTreeFrameType.Attribute)
                    {
                        break;
                    }
                }
            }
        }

        return null;
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager(string baseUri, string uri)
        {
            Initialize(baseUri, uri);
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            throw new NotImplementedException();
        }
    }
}
