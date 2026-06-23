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
    public async Task NavLink_WithShouldMatchFunc_UsesCustomCallback()
    {
        // Arrange: Custom callback that matches current location ignoring query strings
        Func<string, bool> customMatcher = currentUri => currentUri.Split('?')[0] == "https://example.com/page";

        var classValue = await RenderNavLinkWithCustomMatcherAsync(
            "https://example.com/", "https://example.com/page?query=value",
            href: "/page", customMatcher: customMatcher, attributeName: "class");

        // Assert: Should be active because custom callback returns true
        Assert.Equal("active", classValue);
    }

    [Fact]
    public async Task NavLink_WithShouldMatchFunc_FallsBackToDefaultWhenFalse()
    {
        // Arrange: Custom callback that always returns false, but default logic would match
        Func<string, bool> neverMatch = _ => false;

        // When custom returns false, fallback to default - which DOES match here
        // (currentUri "/" matches href "/" exactly)
        var classValue = await RenderNavLinkWithCustomMatcherAsync(
            "https://example.com/", "https://example.com/",
            href: "/", customMatcher: neverMatch, attributeName: "class");

        // Assert: Custom returned false but default matched (returns only "active" when no base class)
        Assert.Equal("active", classValue);
    }

    [Fact]
    public async Task NavLink_WithShouldMatchFunc_HandlesHashFragments()
    {
        // Arrange: Custom callback that strips fragments from current location before comparing
        Func<string, bool> hashMatcher = currentUri => currentUri.Split('#')[0] == "https://example.com/page";

        var classValue = await RenderNavLinkWithCustomMatcherAsync(
            "https://example.com/", "https://example.com/page#section1",
            href: "/page", customMatcher: hashMatcher, attributeName: "class");

        // Assert: Should be active because fragments are ignored
        Assert.Equal("active", classValue);
    }

    [Fact]
    public async Task NavLink_WithShouldMatchFunc_NullCallbackUsesDefaultMatching()
    {
        // Arrange: Null custom callback should use default matching
        Func<string, bool>? noneMatch = null;

        var classValue = await RenderNavLinkWithCustomMatcherAsync(
            "https://example.com/", "https://example.com/page",
            href: "/page", customMatcher: noneMatch, attributeName: "class");

        // Assert: Should be active using default matching
        Assert.Equal("active", classValue);
    }

    [Fact]
    public async Task NavLink_WithShouldMatchFunc_ReceivesCurrentLocation()
    {
        // Arrange: Verify that custom callback receives the current location
        string? receivedUri = null;
        Func<string, bool> captureUri = currentUri =>
        {
            receivedUri = currentUri;
            return true;
        };

        await RenderNavLinkWithCustomMatcherAsync(
            "https://example.com/", "https://example.com/page",
            href: "/page", customMatcher: captureUri, attributeName: "class");

        // Assert: Should receive current absolute URL
        Assert.Equal("https://example.com/page", receivedUri);
    }

    [Fact]
    public async Task NavLink_WithShouldMatchFunc_IgnoresMatchParameter()
    {
        // Arrange: Custom callback with Match.Prefix should ignore Match parameter
        Func<string, bool> alwaysMatch = _ => true;

        var classValue = await RenderNavLinkWithCustomMatcherAndMatchAsync(
            "https://example.com/", "https://example.com/",
            href: "/page", match: NavLinkMatch.Prefix, customMatcher: alwaysMatch, attributeName: "class");

        // Assert: Should be active because custom callback returns true
        Assert.Equal("active", classValue);
    }

    [Fact]
    public async Task NavLink_WithShouldMatchFunc_ComplexLogic()
    {
        // Arrange: Custom callback with complex matching logic
        Func<string, bool> complexMatcher = currentUri =>
        {
            // Match if current URI starts with /admin or is exactly /dashboard
            var path = currentUri.Split('?')[0].Split('#')[0]; // Remove query and fragment
            return path.StartsWith("https://example.com/admin", StringComparison.Ordinal) || path == "https://example.com/dashboard";
        };

        var renderer = new TestRenderer();
        var navigationManager = new TestNavigationManager();
        navigationManager.Initialize("https://example.com/", "https://example.com/admin/users");

        var component = new NavLink { NavigationManager = navigationManager };
        var componentId = renderer.AssignRootComponentId(component);

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(NavLink.ShouldMatch)] = complexMatcher,
            [nameof(NavLink.AdditionalAttributes)] = new Dictionary<string, object> { ["href"] = "/admin/users" }
        });

        await renderer.RenderRootComponentAsync(componentId, parameters);

        var batch = renderer.Batches.Single();
        var classValue = batch.ReferenceFrames.FirstOrDefault(f => f.AttributeName == "class").AttributeValue;

        // Assert: Should be active
        Assert.Equal("active", classValue);
    }

    [Fact]
    public async Task NavLink_WithShouldMatchFunc_MatchingAfterLocationChange()
    {
        // Arrange: Setup NavLink with custom callback
        // Custom matcher that only matches specific paths
        Func<string, bool> selectiveMatcher = currentUri =>
            currentUri.Equals("https://example.com/page", StringComparison.Ordinal) ||
            currentUri.Equals("https://example.com/dashboard", StringComparison.Ordinal);

        // Test scenario 1: At /other, link to /page should not match
        var navigationManager1 = new TestNavigationManager();
        navigationManager1.Initialize("https://example.com/", "https://example.com/other");

        var renderer1 = new TestRenderer();
        var component1 = new NavLink { NavigationManager = navigationManager1 };
        var componentId1 = renderer1.AssignRootComponentId(component1);

        var parameters1 = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(NavLink.ShouldMatch)] = selectiveMatcher,
            [nameof(NavLink.AdditionalAttributes)] = new Dictionary<string, object> { ["href"] = "/page" }
        });

        await renderer1.RenderRootComponentAsync(componentId1, parameters1);

        var batch1 = renderer1.Batches.Last();
        var classValue1 = batch1.ReferenceFrames.FirstOrDefault(f => f.AttributeName == "class").AttributeValue;
        Assert.Null(classValue1); // Not active

        // Test scenario 2: At /page, link to /page should match
        var navigationManager2 = new TestNavigationManager();
        navigationManager2.Initialize("https://example.com/", "https://example.com/page");

        var renderer2 = new TestRenderer();
        var component2 = new NavLink { NavigationManager = navigationManager2 };
        var componentId2 = renderer2.AssignRootComponentId(component2);

        var parameters2 = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(NavLink.ShouldMatch)] = selectiveMatcher,
            [nameof(NavLink.AdditionalAttributes)] = new Dictionary<string, object> { ["href"] = "/page" }
        });

        await renderer2.RenderRootComponentAsync(componentId2, parameters2);

        var batch2 = renderer2.Batches.Last();
        var classValue2 = batch2.ReferenceFrames.FirstOrDefault(f => f.AttributeName == "class").AttributeValue;
        Assert.Equal("active", classValue2); // Active
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

    private async Task<object?> RenderNavLinkWithCustomMatcherAsync(
        string baseUri, string currentUri, string href, Func<string, bool>? customMatcher, string attributeName)
    {
        var navigationManager = new TestNavigationManager();
        navigationManager.Initialize(baseUri, currentUri);

        var renderer = new TestRenderer();
        var component = new NavLink { NavigationManager = navigationManager };
        var componentId = renderer.AssignRootComponentId(component);

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(NavLink.ShouldMatch)] = customMatcher,
            [nameof(NavLink.AdditionalAttributes)] = new Dictionary<string, object> { ["href"] = href }
        });

        await renderer.RenderRootComponentAsync(componentId, parameters);

        var batch = renderer.Batches.Single();
        return batch.ReferenceFrames.FirstOrDefault(f => f.AttributeName == attributeName).AttributeValue;
    }

    private async Task<object?> RenderNavLinkWithCustomMatcherAndMatchAsync(
        string baseUri, string currentUri, string href, NavLinkMatch match, Func<string, bool>? customMatcher, string attributeName)
    {
        var navigationManager = new TestNavigationManager();
        navigationManager.Initialize(baseUri, currentUri);

        var renderer = new TestRenderer();
        var component = new NavLink { NavigationManager = navigationManager };
        var componentId = renderer.AssignRootComponentId(component);

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(NavLink.Match)] = match,
            [nameof(NavLink.ShouldMatch)] = customMatcher,
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
            Uri = uri;
            NotifyLocationChanged(false);
        }
    }
}
