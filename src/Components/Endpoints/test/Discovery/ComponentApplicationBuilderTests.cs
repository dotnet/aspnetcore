// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Discovery;

public class ComponentApplicationBuilderTests
{
    [Fact]
    public void ComponentApplicationBuilder_CanAddLibrary()
    {
        // Arrange
        var builder = new ComponentApplicationBuilder();
        builder.AddLibrary(new AssemblyComponentLibraryDescriptor(
            "App1",
            CreateApp1Pages("App1"),
            CreateApp1Components("App1")));

        var app = builder.Build();

        Assert.NotNull(app);

        Assert.Collection(app.Pages,
            p => Assert.Equal(typeof(App1Test1), p.Type),
            p => Assert.Equal(typeof(App1Test2), p.Type),
            p => Assert.Equal(typeof(App1Test3), p.Type));

        Assert.Collection(app.Pages.Select(p => p.Route),
            r => Assert.Equal("/App1/Test1", r),
            r => Assert.Equal("/App1/Test2", r),
            r => Assert.Equal("/App1/Test3", r));

        Assert.Collection(app.Components,
            c => Assert.Equal(typeof(App1Test1), c.ComponentType),
            c => Assert.Equal(typeof(App1Test2), c.ComponentType),
            c => Assert.Equal(typeof(App1Test3), c.ComponentType),
            c => Assert.Equal(typeof(App1OtherComponent), c.ComponentType));
    }

    [Fact]
    public void ComponentApplicationBuilder_CanAddMultipleLibraries()
    {
        // Arrange
        var builder = new ComponentApplicationBuilder();
        builder.AddLibrary(new AssemblyComponentLibraryDescriptor(
            "App1",
            CreateApp1Pages("App1"),
            CreateApp1Components("App1")));

        builder.AddLibrary(new AssemblyComponentLibraryDescriptor(
            "App2",
            CreateApp2Pages("App2"),
            CreateApp2Components("App2")));
        var app = builder.Build();

        Assert.NotNull(app);

        Assert.Collection(
            app.Pages,
            p => Assert.Equal(typeof(App1Test1), p.Type),
            p => Assert.Equal(typeof(App1Test2), p.Type),
            p => Assert.Equal(typeof(App1Test3), p.Type),
            p => Assert.Equal(typeof(App2Test1), p.Type),
            p => Assert.Equal(typeof(App2Test2), p.Type),
            p => Assert.Equal(typeof(App2Test3), p.Type));

        Assert.Collection(
            app.Components,
            c => Assert.Equal(typeof(App1Test1), c.ComponentType),
            c => Assert.Equal(typeof(App1Test2), c.ComponentType),
            c => Assert.Equal(typeof(App1Test3), c.ComponentType),
            c => Assert.Equal(typeof(App1OtherComponent), c.ComponentType),
            c => Assert.Equal(typeof(App2Test1), c.ComponentType),
            c => Assert.Equal(typeof(App2Test2), c.ComponentType),
            c => Assert.Equal(typeof(App2Test3), c.ComponentType),
            c => Assert.Equal(typeof(App2OtherComponent), c.ComponentType));
    }

    [Fact]
    public void ComponentApplicationBuilder_CanRemoveLibrary()
    {
        // Arrange
        var builder = new ComponentApplicationBuilder();
        builder.AddLibrary(new AssemblyComponentLibraryDescriptor(
            "App1",
            CreateApp1Pages("App1"),
            CreateApp1Components("App1")));

        builder.AddLibrary(new AssemblyComponentLibraryDescriptor(
            "App2",
            CreateApp2Pages("App2"),
            CreateApp2Components("App2")));

        builder.RemoveLibrary("App1");

        var app = builder.Build();

        Assert.NotNull(app);

        Assert.Collection(
            app.Pages,
            p => Assert.Equal(typeof(App2Test1), p.Type),
            p => Assert.Equal(typeof(App2Test2), p.Type),
            p => Assert.Equal(typeof(App2Test3), p.Type));

        Assert.Collection(
            app.Components,
            c => Assert.Equal(typeof(App2Test1), c.ComponentType),
            c => Assert.Equal(typeof(App2Test2), c.ComponentType),
            c => Assert.Equal(typeof(App2Test3), c.ComponentType),
            c => Assert.Equal(typeof(App2OtherComponent), c.ComponentType));
    }

    [Fact]
    public void ComponentApplicationBuilder_CanCombineBuilders()
    {
        // Arrange
        var builder = new ComponentApplicationBuilder();
        builder.AddLibrary(new AssemblyComponentLibraryDescriptor(
            "App1",
            CreateApp1Pages("App1"),
            CreateApp1Components("App1")));

        var builder2 = new ComponentApplicationBuilder();
        builder2.AddLibrary(new AssemblyComponentLibraryDescriptor(
            "App2",
            CreateApp2Pages("App2"),
            CreateApp2Components("App2")));
        builder.Combine(builder2);
        var app = builder.Build();

        Assert.NotNull(app);

        Assert.Collection(
            app.Pages,
            p => Assert.Equal(typeof(App1Test1), p.Type),
            p => Assert.Equal(typeof(App1Test2), p.Type),
            p => Assert.Equal(typeof(App1Test3), p.Type),
            p => Assert.Equal(typeof(App2Test1), p.Type),
            p => Assert.Equal(typeof(App2Test2), p.Type),
            p => Assert.Equal(typeof(App2Test3), p.Type));

        Assert.Collection(
            app.Components,
            c => Assert.Equal(typeof(App1Test1), c.ComponentType),
            c => Assert.Equal(typeof(App1Test2), c.ComponentType),
            c => Assert.Equal(typeof(App1Test3), c.ComponentType),
            c => Assert.Equal(typeof(App1OtherComponent), c.ComponentType),
            c => Assert.Equal(typeof(App2Test1), c.ComponentType),
            c => Assert.Equal(typeof(App2Test2), c.ComponentType),
            c => Assert.Equal(typeof(App2Test3), c.ComponentType),
            c => Assert.Equal(typeof(App2OtherComponent), c.ComponentType));
    }

    [Fact]
    public void ComponentApplicationBuilder_CombiningDoesNotDuplicateSharedDependencies()
    {
        // Arrange
        var builder = new ComponentApplicationBuilder();
        builder.AddLibrary(new AssemblyComponentLibraryDescriptor(
            "App1",
            CreateApp1Pages("App1"),
            CreateApp1Components("App1")));

        builder.AddLibrary(new AssemblyComponentLibraryDescriptor(
            "Shared",
            CreateSharedPages("Shared"),
            CreateSharedComponents("Shared")));

        var builder2 = new ComponentApplicationBuilder();
        builder2.AddLibrary(new AssemblyComponentLibraryDescriptor(
            "App2",
            CreateApp2Pages("App2"),
            CreateApp2Components("App2")));

        builder2.AddLibrary(new AssemblyComponentLibraryDescriptor(
            "Shared",
            CreateSharedPages("Shared"),
            CreateSharedComponents("Shared")));

        builder.Combine(builder2);
        var app = builder.Build();

        Assert.NotNull(app);

        Assert.Collection(
            app.Pages,
            p => Assert.Equal(typeof(App1Test1), p.Type),
            p => Assert.Equal(typeof(App1Test2), p.Type),
            p => Assert.Equal(typeof(App1Test3), p.Type),
            p => Assert.Equal(typeof(SharedTest1), p.Type),
            p => Assert.Equal(typeof(SharedTest2), p.Type),
            p => Assert.Equal(typeof(SharedTest3), p.Type),
            p => Assert.Equal(typeof(App2Test1), p.Type),
            p => Assert.Equal(typeof(App2Test2), p.Type),
            p => Assert.Equal(typeof(App2Test3), p.Type));

        Assert.Collection(
            app.Components,
            c => Assert.Equal(typeof(App1Test1), c.ComponentType),
            c => Assert.Equal(typeof(App1Test2), c.ComponentType),
            c => Assert.Equal(typeof(App1Test3), c.ComponentType),
            c => Assert.Equal(typeof(App1OtherComponent), c.ComponentType),
            c => Assert.Equal(typeof(SharedTest1), c.ComponentType),
            c => Assert.Equal(typeof(SharedTest2), c.ComponentType),
            c => Assert.Equal(typeof(SharedTest3), c.ComponentType),
            c => Assert.Equal(typeof(SharedOtherComponent), c.ComponentType),
            c => Assert.Equal(typeof(App2Test1), c.ComponentType),
            c => Assert.Equal(typeof(App2Test2), c.ComponentType),
            c => Assert.Equal(typeof(App2Test3), c.ComponentType),
            c => Assert.Equal(typeof(App2OtherComponent), c.ComponentType));
    }

    [Fact]
    public void ComponentApplicationBuilder_CanExcludeOtherBuilders()
    {
        // Arrange
        var builder = new ComponentApplicationBuilder();
        builder.AddLibrary(new AssemblyComponentLibraryDescriptor(
            "App1",
            CreateApp1Pages("App1"),
            CreateApp1Components("App1")));

        builder.AddLibrary(new AssemblyComponentLibraryDescriptor(
            "App2",
            CreateApp2Pages("App2"),
            CreateApp2Components("App2")));

        builder.AddLibrary(new AssemblyComponentLibraryDescriptor(
            "Shared",
            CreateSharedPages("Shared"),
            CreateSharedComponents("Shared")));

        var builder2 = new ComponentApplicationBuilder();
        builder2.AddLibrary(new AssemblyComponentLibraryDescriptor(
            "App2",
            CreateApp2Pages("App2"),
            CreateApp2Components("App2")));

        builder2.AddLibrary(new AssemblyComponentLibraryDescriptor(
            "Shared",
            CreateSharedPages("Shared"),
            CreateSharedComponents("Shared")));

        builder.Exclude(builder2);
        var app = builder.Build();

        Assert.NotNull(app);

        Assert.Collection(
            app.Pages,
            p => Assert.Equal(typeof(App1Test1), p.Type),
            p => Assert.Equal(typeof(App1Test2), p.Type),
            p => Assert.Equal(typeof(App1Test3), p.Type));

        Assert.Collection(
            app.Components,
            c => Assert.Equal(typeof(App1Test1), c.ComponentType),
            c => Assert.Equal(typeof(App1Test2), c.ComponentType),
            c => Assert.Equal(typeof(App1Test3), c.ComponentType),
            c => Assert.Equal(typeof(App1OtherComponent), c.ComponentType));
    }

    private IReadOnlyList<ComponentBuilder> CreateApp1Components(string assemblyName)
    {
        return new[]
        {
            new ComponentBuilder
            {
                AssemblyName = assemblyName,
                ComponentType = typeof(App1Test1),
            },
            new ComponentBuilder
            {
                AssemblyName = assemblyName,
                ComponentType = typeof(App1Test2),
            },
            new ComponentBuilder
            {
                AssemblyName = assemblyName,
                ComponentType = typeof(App1Test3),
            },
            new ComponentBuilder
            {
                AssemblyName = assemblyName,
                ComponentType = typeof(App1OtherComponent),
            }
        };
    }

    private IReadOnlyList<PageComponentBuilder> CreateApp1Pages(string assemblyName)
    {
        return new[]
        {
            new PageComponentBuilder
            {
                AssemblyName = assemblyName,
                PageType = typeof(App1Test1),
                RouteTemplates = new List<string> { "/App1/Test1" }
            },
            new PageComponentBuilder
            {
                AssemblyName = assemblyName,
                PageType = typeof(App1Test2),
                RouteTemplates = new List<string> { "/App1/Test2" }
            },
            new PageComponentBuilder
            {
                AssemblyName = assemblyName,
                PageType = typeof(App1Test3),
                RouteTemplates = new List<string> { "/App1/Test3" }
            }
        };
    }

    private IReadOnlyList<ComponentBuilder> CreateApp2Components(string assemblyName)
    {
        return new[]
        {
            new ComponentBuilder
            {
                AssemblyName = assemblyName,
                ComponentType = typeof(App2Test1),
            },
            new ComponentBuilder
            {
                AssemblyName = assemblyName,
                ComponentType = typeof(App2Test2),
            },
            new ComponentBuilder
            {
                AssemblyName = assemblyName,
                ComponentType = typeof(App2Test3),
            },
            new ComponentBuilder
            {
                AssemblyName = assemblyName,
                ComponentType = typeof(App2OtherComponent),
            }
        };
    }

    private IReadOnlyList<PageComponentBuilder> CreateApp2Pages(string assemblyName)
    {
        return new[]
        {
            new PageComponentBuilder
            {
                AssemblyName = assemblyName,
                PageType = typeof(App2Test1),
                RouteTemplates = new List<string> { "/App2/Test1" }
            },
            new PageComponentBuilder
            {
                AssemblyName = assemblyName,
                PageType = typeof(App2Test2),
                RouteTemplates = new List<string> { "/App2/Test2" }
            },
            new PageComponentBuilder
            {
                AssemblyName = assemblyName,
                PageType = typeof(App2Test3),
                RouteTemplates = new List<string> { "/App2/Test3" }
            }
        };
    }

    private IReadOnlyList<ComponentBuilder> CreateSharedComponents(string assemblyName)
    {
        return new[]
        {
            new ComponentBuilder
            {
                AssemblyName = assemblyName,
                ComponentType = typeof(SharedTest1),
            },
            new ComponentBuilder
            {
                AssemblyName = assemblyName,
                ComponentType = typeof(SharedTest2),
            },
            new ComponentBuilder
            {
                AssemblyName = assemblyName,
                ComponentType = typeof(SharedTest3),
            },
            new ComponentBuilder
            {
                AssemblyName = assemblyName,
                ComponentType = typeof(SharedOtherComponent),
            }
        };
    }

    private IReadOnlyList<PageComponentBuilder> CreateSharedPages(string assemblyName)
    {
        return new[]
        {
            new PageComponentBuilder
            {
                AssemblyName = assemblyName,
                PageType = typeof(SharedTest1),
                RouteTemplates = new List<string> { "/Shared/Test1" }
            },
            new PageComponentBuilder
            {
                AssemblyName = assemblyName,
                PageType = typeof(SharedTest2),
                RouteTemplates = new List<string> { "/Shared/Test2" }
            },
            new PageComponentBuilder
            {
                AssemblyName = assemblyName,
                PageType = typeof(SharedTest3),
                RouteTemplates = new List<string> { "/Shared/Test3" }
            },
        };
    }

    class App1Test1 : ComponentBase { }

    class App1Test2 : ComponentBase { }

    class App1Test3 : ComponentBase { }

    class App1OtherComponent : ComponentBase { }

    class App2Test1 : ComponentBase { }

    class App2Test2 : ComponentBase { }

    class App2Test3 : ComponentBase { }

    class App2OtherComponent : ComponentBase { }

    class SharedTest1 : ComponentBase { }

    class SharedTest2 : ComponentBase { }

    class SharedTest3 : ComponentBase { }

    class SharedOtherComponent : ComponentBase { }
}
