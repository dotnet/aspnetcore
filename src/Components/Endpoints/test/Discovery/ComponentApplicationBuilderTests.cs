// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Infrastructure;

namespace Microsoft.AspNetCore.Components.Discovery;

#nullable enable

public class ComponentApplicationBuilderTests
{
    [Fact]
    public void ComponentApplicationBuilder_CanAddLibrary()
    {
        var builder = new ComponentApplicationBuilder();
        builder.AddLibrary("App1", CreateApp1());

        var app = builder.Build();

        Assert.Collection(
            app.Pages,
            page => Assert.Equal(typeof(App1Test1), page.Type),
            page => Assert.Equal(typeof(App1Test2), page.Type),
            page => Assert.Equal(typeof(App1Test3), page.Type));
        Assert.Equal(
            ["/App1/Test1", "/App1/Test2", "/App1/Test3"],
            app.Pages.Select(static page => page.Route));
        Assert.Equal(
            [typeof(App1Test1), typeof(App1Test2), typeof(App1Test3), typeof(App1OtherComponent)],
            app.Components.Select(static component => component.Type));
    }

    [Fact]
    public void ComponentApplicationBuilder_CanAddMultipleLibraries()
    {
        var builder = new ComponentApplicationBuilder();
        builder.AddLibrary("App1", CreateApp1());
        builder.AddLibrary("App2", CreateApp2());

        var app = builder.Build();

        Assert.Equal(6, app.Pages.Count);
        Assert.Equal(8, app.Components.Count);
    }

    [Fact]
    public void ComponentApplicationBuilder_CanRemoveLibrary()
    {
        var builder = new ComponentApplicationBuilder();
        builder.AddLibrary("App1", CreateApp1());
        builder.AddLibrary("App2", CreateApp2());

        builder.RemoveLibrary("App1");
        var app = builder.Build();

        Assert.All(app.Pages, static page => Assert.StartsWith("/App2/", page.Route));
        Assert.Equal(4, app.Components.Count);
    }

    [Fact]
    public void ComponentApplicationBuilder_CombiningDoesNotDuplicateSharedDependencies()
    {
        var builder = new ComponentApplicationBuilder();
        builder.AddLibrary("App1", CreateApp1());
        builder.AddLibrary("Shared", CreateShared());
        var other = new ComponentApplicationBuilder();
        other.AddLibrary("App2", CreateApp2());
        other.AddLibrary("Shared", CreateShared());

        builder.Combine(other);
        var app = builder.Build();

        Assert.Equal(9, app.Pages.Count);
        Assert.Equal(12, app.Components.Count);
    }

    [Fact]
    public void ComponentApplicationBuilder_CanExcludeOtherBuilders()
    {
        var builder = new ComponentApplicationBuilder();
        builder.AddLibrary("App1", CreateApp1());
        builder.AddLibrary("App2", CreateApp2());
        builder.AddLibrary("Shared", CreateShared());
        var excluded = new ComponentApplicationBuilder();
        excluded.AddLibrary("App2", CreateApp2());
        excluded.AddLibrary("Shared", CreateShared());

        builder.Exclude(excluded);
        var app = builder.Build();

        Assert.Equal(3, app.Pages.Count);
        Assert.Equal(4, app.Components.Count);
        Assert.All(app.Components, static component => Assert.StartsWith("App1", component.Type.Name));
    }

    [Fact]
    public void ComponentApplicationBuilder_PreservesNormalizedEndpointMetadata()
    {
        var marker = new MarkerAttribute();
        var builder = new ComponentApplicationBuilder();
        builder.AddLibrary(
            "App",
            [Describe(typeof(App1Test1), "/route", marker)]);

        var page = Assert.Single(builder.Build().Pages);

        Assert.DoesNotContain(page.Metadata, static item => item is RouteAttribute);
        Assert.Same(marker, Assert.Single(page.Metadata));
    }

    private static IReadOnlyList<ComponentTypeInfo> CreateApp1()
        =>
        [
            Describe(typeof(App1Test1), "/App1/Test1"),
            Describe(typeof(App1Test2), "/App1/Test2"),
            Describe(typeof(App1Test3), "/App1/Test3"),
            Describe(typeof(App1OtherComponent)),
        ];

    private static IReadOnlyList<ComponentTypeInfo> CreateApp2()
        =>
        [
            Describe(typeof(App2Test1), "/App2/Test1"),
            Describe(typeof(App2Test2), "/App2/Test2"),
            Describe(typeof(App2Test3), "/App2/Test3"),
            Describe(typeof(App2OtherComponent)),
        ];

    private static IReadOnlyList<ComponentTypeInfo> CreateShared()
        =>
        [
            Describe(typeof(SharedTest1), "/Shared/Test1"),
            Describe(typeof(SharedTest2), "/Shared/Test2"),
            Describe(typeof(SharedTest3), "/Shared/Test3"),
            Describe(typeof(SharedOtherComponent)),
        ];

    private static ComponentTypeInfo Describe(Type type, string? route = null, params object[] metadata)
    {
        IReadOnlyList<object> normalizedMetadata = route is null
            ? metadata
            : [new RouteAttribute(route), .. metadata];
        return new ComponentTypeInfo(new ComponentDescriptor
        {
            Type = type,
            Metadata = normalizedMetadata,
        });
    }

    private sealed class MarkerAttribute : Attribute;

    private sealed class App1Test1 : ComponentBase;
    private sealed class App1Test2 : ComponentBase;
    private sealed class App1Test3 : ComponentBase;
    private sealed class App1OtherComponent : ComponentBase;
    private sealed class App2Test1 : ComponentBase;
    private sealed class App2Test2 : ComponentBase;
    private sealed class App2Test3 : ComponentBase;
    private sealed class App2OtherComponent : ComponentBase;
    private sealed class SharedTest1 : ComponentBase;
    private sealed class SharedTest2 : ComponentBase;
    private sealed class SharedTest3 : ComponentBase;
    private sealed class SharedOtherComponent : ComponentBase;
}
