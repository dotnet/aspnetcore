// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;

#nullable enable

namespace Microsoft.AspNetCore.Components.Endpoints;

public class BasePathTest
{
    [Fact]
    public void PreservesCasingFromNavigationManagerBaseUri()
    {
        _ = CreateServices(out var renderer, "https://example.com/Dashboard/");
        var componentId = RenderBasePath(renderer);

        Assert.Equal("/Dashboard/", GetHref(renderer, componentId));
    }

    [Theory]
    [InlineData("https://example.com/a/b/", "/a/b/")]
    [InlineData("https://example.com/a/b", "/a/")]
    public void RendersBaseUriPathExactly(string baseUri, string expected)
    {
        _ = CreateServices(out var renderer, baseUri);

        var componentId = RenderBasePath(renderer);

        Assert.Equal(expected, GetHref(renderer, componentId));
    }

    private static TestServiceProvider CreateServices(out TestRenderer renderer, string baseUri = "https://example.com/app/")
    {
        var services = new TestServiceProvider();
        var uri = baseUri.EndsWith('/') ? baseUri + "dashboard" : baseUri + "/dashboard";
        var navigationManager = new TestNavigationManager(baseUri, uri);
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
