// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Routing;

public class NavLinkTest
{
    [Fact]
    public async Task NavLink_WithRelativeToCurrentUri_ResolvesHrefRelativeToCurrentPath()
    {
        var testNavigationManager = new TestNavigationManager();
        testNavigationManager.Initialize("https://example.com/", "https://example.com/sub-site/page-a");

        var renderer = new TestRenderer();
        var component = new NavLink();
        
        var componentId = renderer.AssignRootComponentId(component);
        SetNavigationManager(component, testNavigationManager);
        
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(NavLink.RelativeToCurrentUri)] = true,
            [nameof(NavLink.AdditionalAttributes)] = new Dictionary<string, object>
            {
                ["href"] = "details"
            }
        });
        
        await renderer.RenderRootComponentAsync(componentId, parameters);

        var batch = renderer.Batches.Single();
        var hrefFrame = batch.ReferenceFrames.First(f => f.AttributeName == "href");
        Assert.Equal("https://example.com/sub-site/details", hrefFrame.AttributeValue);
    }

    [Fact]
    public async Task NavLink_WithRelativeToCurrentUri_HandlesNestedPaths()
    {
        var testNavigationManager = new TestNavigationManager();
        testNavigationManager.Initialize("https://example.com/", "https://example.com/a/b/c/page");

        var renderer = new TestRenderer();
        var component = new NavLink();
        
        var componentId = renderer.AssignRootComponentId(component);
        SetNavigationManager(component, testNavigationManager);
        
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(NavLink.RelativeToCurrentUri)] = true,
            [nameof(NavLink.AdditionalAttributes)] = new Dictionary<string, object>
            {
                ["href"] = "sibling"
            }
        });
        
        await renderer.RenderRootComponentAsync(componentId, parameters);

        var batch = renderer.Batches.Single();
        var hrefFrame = batch.ReferenceFrames.First(f => f.AttributeName == "href");
        Assert.Equal("https://example.com/a/b/c/sibling", hrefFrame.AttributeValue);
    }

    [Fact]
    public async Task NavLink_WithRelativeToCurrentUri_HandlesQueryAndFragment()
    {
        var testNavigationManager = new TestNavigationManager();
        testNavigationManager.Initialize("https://example.com/", "https://example.com/folder/page?query=value#hash");

        var renderer = new TestRenderer();
        var component = new NavLink();
        
        var componentId = renderer.AssignRootComponentId(component);
        SetNavigationManager(component, testNavigationManager);
        
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(NavLink.RelativeToCurrentUri)] = true,
            [nameof(NavLink.AdditionalAttributes)] = new Dictionary<string, object>
            {
                ["href"] = "other"
            }
        });
        
        await renderer.RenderRootComponentAsync(componentId, parameters);

        var batch = renderer.Batches.Single();
        var hrefFrame = batch.ReferenceFrames.First(f => f.AttributeName == "href");
        Assert.Equal("https://example.com/folder/other", hrefFrame.AttributeValue);
    }

    [Fact]
    public async Task NavLink_WithRelativeToCurrentUriFalse_DoesNotResolve()
    {
        var testNavigationManager = new TestNavigationManager();
        testNavigationManager.Initialize("https://example.com/", "https://example.com/folder/page");

        var renderer = new TestRenderer();
        var component = new NavLink();
        
        var componentId = renderer.AssignRootComponentId(component);
        SetNavigationManager(component, testNavigationManager);
        
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(NavLink.RelativeToCurrentUri)] = false,
            [nameof(NavLink.AdditionalAttributes)] = new Dictionary<string, object>
            {
                ["href"] = "relative"
            }
        });
        
        await renderer.RenderRootComponentAsync(componentId, parameters);

        var batch = renderer.Batches.Single();
        var hrefFrame = batch.ReferenceFrames.First(f => f.AttributeName == "href");
        // Should keep the original href as-is (from AdditionalAttributes)
        Assert.Equal("relative", hrefFrame.AttributeValue);
    }

    [Fact]
    public async Task NavLink_WithRelativeToCurrentUri_AtRootLevel()
    {
        var testNavigationManager = new TestNavigationManager();
        testNavigationManager.Initialize("https://example.com/", "https://example.com/page");

        var renderer = new TestRenderer();
        var component = new NavLink();
        
        var componentId = renderer.AssignRootComponentId(component);
        SetNavigationManager(component, testNavigationManager);
        
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(NavLink.RelativeToCurrentUri)] = true,
            [nameof(NavLink.AdditionalAttributes)] = new Dictionary<string, object>
            {
                ["href"] = "other"
            }
        });
        
        await renderer.RenderRootComponentAsync(componentId, parameters);

        var batch = renderer.Batches.Single();
        var hrefFrame = batch.ReferenceFrames.First(f => f.AttributeName == "href");
        Assert.Equal("https://example.com/other", hrefFrame.AttributeValue);
    }

    [Fact]
    public async Task NavLink_WithRelativeToCurrentUri_PreservesActiveClassLogic()
    {
        var testNavigationManager = new TestNavigationManager();
        testNavigationManager.Initialize("https://example.com/", "https://example.com/sub-site/details");

        var renderer = new TestRenderer();
        var component = new NavLink();
        
        var componentId = renderer.AssignRootComponentId(component);
        SetNavigationManager(component, testNavigationManager);
        
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(NavLink.RelativeToCurrentUri)] = true,
            [nameof(NavLink.AdditionalAttributes)] = new Dictionary<string, object>
            {
                ["href"] = "details"
            }
        });
        
        // Initially on details page - link should be active
        await renderer.RenderRootComponentAsync(componentId, parameters);
        var batch = renderer.Batches.Single();
        var classFrame = batch.ReferenceFrames.FirstOrDefault(f => f.AttributeName == "class");
        Assert.Equal("active", classFrame.AttributeValue);
    }

    [Fact]
    public async Task NavLink_WithPathRelative_WorksWithDeeplyNestedBaseUri()
    {
        // App hosted at https://example.com/org/project/app/
        var testNavigationManager = new TestNavigationManager();
        testNavigationManager.Initialize("https://example.com/org/project/app/", "https://example.com/org/project/app/admin/users");

        var renderer = new TestRenderer();
        var component = new NavLink();
        
        var componentId = renderer.AssignRootComponentId(component);
        SetNavigationManager(component, testNavigationManager);
        
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(NavLink.PathRelative)] = true,
            [nameof(NavLink.AdditionalAttributes)] = new Dictionary<string, object>
            {
                ["href"] = "roles"
            }
        });
        
        await renderer.RenderRootComponentAsync(componentId, parameters);

        var batch = renderer.Batches.Single();
        var hrefFrame = batch.ReferenceFrames.First(f => f.AttributeName == "href");
        Assert.Equal("https://example.com/org/project/app/admin/roles", hrefFrame.AttributeValue);
    }

    private void SetNavigationManager(NavLink component, NavigationManager navigationManager)
    {
        var navManagerProperty = typeof(NavLink).GetProperty("NavigationManager",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        navManagerProperty!.SetValue(component, navigationManager);
    }

    private class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager()
        {
        }

        public new void Initialize(string baseUri, string uri)
        {
            base.Initialize(baseUri, uri);
        }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            Uri = uri;
            NotifyLocationChanged(false);
        }
    }
}
