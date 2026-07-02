// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Routing;

public class NavLinkTest
{
    [Theory]
    [InlineData("https://example.com/", "https://example.com/sub-site/page-a", "details", "https://example.com/sub-site/details")]
    [InlineData("https://example.com/", "https://example.com/a/b/c/page", "sibling", "https://example.com/a/b/c/sibling")]
    [InlineData("https://example.com/", "https://example.com/page", "other", "https://example.com/other")]
    [InlineData("https://example.com/", "https://example.com/folder/page?query=value#hash", "other", "https://example.com/folder/other")]
    [InlineData("https://example.com/org/project/app/", "https://example.com/org/project/app/admin/users", "roles", "https://example.com/org/project/app/admin/roles")]
    public async Task NavLink_WithRelativeToCurrentUri_ResolvesHrefCorrectly(
        string baseUri, string currentUri, string href, string expectedHref)
    {
        var renderedHref = await RenderNavLinkAndGetAttributeAsync(
            baseUri, currentUri, href, relativeToCurrentUri: true, attributeName: "href");

        Assert.Equal(expectedHref, renderedHref);
    }

    [Fact]
    public async Task NavLink_WithRelativeToCurrentUriFalse_DoesNotResolve()
    {
        var renderedHref = await RenderNavLinkAndGetAttributeAsync(
            "https://example.com/", "https://example.com/folder/page",
            href: "relative", relativeToCurrentUri: false, attributeName: "href");

        Assert.Equal("relative", renderedHref);
    }

    [Fact]
    public async Task NavLink_WithRelativeToCurrentUri_PreservesActiveClassLogic()
    {
        var classValue = await RenderNavLinkAndGetAttributeAsync(
            "https://example.com/", "https://example.com/sub-site/details",
            href: "details", relativeToCurrentUri: true, attributeName: "class");

        Assert.Equal("active", classValue);
    }

    [Fact]
    public async Task ActivationChanged_InvokedWhenNavigationChangesActiveState()
    {
        var navigationManager = new TestNavigationManager();
        navigationManager.Initialize("https://example.com/", "https://example.com/");

        var renderer = new TestRenderer();
        var component = new NavLink { NavigationManager = navigationManager };
        var componentId = renderer.AssignRootComponentId(component);

        var callbackInvoked = false;
        bool callbackValue = false;

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(NavLink.AdditionalAttributes)] = new Dictionary<string, object> { ["href"] = "/page" },
            [nameof(NavLink.ActivationChanged)] = EventCallback.Factory.Create<bool>(component,
                (value) =>
                {
                    callbackInvoked = true;
                    callbackValue = value;
                })
        });

        await renderer.RenderRootComponentAsync(componentId, parameters);

        await renderer.Dispatcher.InvokeAsync(() => navigationManager.NavigateTo("/page"));

        Assert.True(callbackInvoked);
        Assert.True(callbackValue);
    }

    [Fact]
    public async Task ActivationChanged_PassesCorrectBooleanValue()
    {
        var navigationManager = new TestNavigationManager();
        navigationManager.Initialize("https://example.com/", "https://example.com/");

        var renderer = new TestRenderer();
        var component = new NavLink { NavigationManager = navigationManager };
        var componentId = renderer.AssignRootComponentId(component);

        var capturedValues = new List<bool>();

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(NavLink.AdditionalAttributes)] = new Dictionary<string, object> { ["href"] = "/target" },
            [nameof(NavLink.ActivationChanged)] = EventCallback.Factory.Create<bool>(component,
                (value) => capturedValues.Add(value))
        });

        await renderer.RenderRootComponentAsync(componentId, parameters);

        await renderer.Dispatcher.InvokeAsync(() => navigationManager.NavigateTo("/target"));
        await Task.Delay(100);

        await renderer.Dispatcher.InvokeAsync(() => navigationManager.NavigateTo("/other"));
        await Task.Delay(100);

        Assert.Equal(new[] { true, false }, capturedValues);
    }

    private async Task<object?> RenderNavLinkAndGetAttributeAsync(
        string baseUri, string currentUri, string href, bool relativeToCurrentUri, string attributeName)
    {
        var navigationManager = new TestNavigationManager();
        navigationManager.Initialize(baseUri, currentUri);

        var renderer = new TestRenderer();
        var component = new NavLink { NavigationManager = navigationManager };
        var componentId = renderer.AssignRootComponentId(component);

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(NavLink.RelativeToCurrentUri)] = relativeToCurrentUri,
            [nameof(NavLink.AdditionalAttributes)] = new Dictionary<string, object> { ["href"] = href }
        });

        await renderer.RenderRootComponentAsync(componentId, parameters);

        var batch = renderer.Batches.Single();
        return batch.ReferenceFrames.FirstOrDefault(f => f.AttributeName == attributeName).AttributeValue;
    }

    private class TestNavigationManager : NavigationManager
    {
        public new void Initialize(string baseUri, string uri) => base.Initialize(baseUri, uri);

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            // Resolve to an absolute URI so the base-relative validation in the
            // Uri setter accepts the new value.
            Uri = ToAbsoluteUri(uri).AbsoluteUri;
            NotifyLocationChanged(false);
        }
    }
}
