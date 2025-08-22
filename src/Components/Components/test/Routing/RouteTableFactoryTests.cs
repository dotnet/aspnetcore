// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing.Tree;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Routing;

public class RouteTableFactoryTests
{
    private readonly ServiceProvider _serviceProvider;

    public RouteTableFactoryTests()
    {
        _serviceProvider = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();
    }

    [Fact]
    public void CanCacheRouteTable()
    {
        // Arrange
        var routeTableFactory = new RouteTableFactory();
        var routes1 = routeTableFactory.Create(new RouteKey(GetType().Assembly, null), _serviceProvider);

        // Act
        var routes2 = routeTableFactory.Create(new RouteKey(GetType().Assembly, null), _serviceProvider);

        // Assert
        Assert.Same(routes1, routes2);
    }

    [Fact]
    public void CanCacheRouteTableWithDifferentAssembliesAndOrder()
    {
        // Arrange
        var routeTableFactory = new RouteTableFactory();
        var routes1 = routeTableFactory.Create(new RouteKey(typeof(object).Assembly, new[] { typeof(ComponentBase).Assembly, GetType().Assembly, }), _serviceProvider);

        // Act
        var routes2 = routeTableFactory.Create(new RouteKey(typeof(object).Assembly, new[] { GetType().Assembly, typeof(ComponentBase).Assembly, }), _serviceProvider);

        // Assert
        Assert.Same(routes1, routes2);
    }

    [Fact]
    public void DoesNotCacheRouteTableForDifferentAssemblies()
    {
        // Arrange
        var routeTableFactory = new RouteTableFactory();
        var routes1 = routeTableFactory.Create(new RouteKey(GetType().Assembly, null), _serviceProvider);

        // Act
        var routes2 = routeTableFactory.Create(new RouteKey(GetType().Assembly, new[] { typeof(object).Assembly }), _serviceProvider);

        // Assert
        Assert.NotSame(routes1, routes2);
    }

    [Fact]
    public void IgnoresIdenticalTypes()
    {
        // Arrange & Act
        var routeTableFactory = new RouteTableFactory();
        var routeTable = routeTableFactory.Create(new RouteKey(GetType().Assembly, new[] { GetType().Assembly }), _serviceProvider);

        var routes = GetRoutes(routeTable);

        // Assert
        Assert.Equal(routes.GroupBy(x => x.Handler).Count(), routes.Count);
    }

    [Fact]
    public void RespectsExcludeFromInteractiveRoutingAttribute()
    {
        // Arrange & Act
        var routeTableFactory = new RouteTableFactory();
        var routeTable = routeTableFactory.Create(new RouteKey(GetType().Assembly, Array.Empty<Assembly>()), _serviceProvider);

        var routes = GetRoutes(routeTable);

        // Assert
        Assert.Contains(routes, r => r.Handler == typeof(ComponentWithoutExcludeFromInteractiveRoutingAttribute));
        Assert.DoesNotContain(routes, r => r.Handler == typeof(ComponentWithExcludeFromInteractiveRoutingAttribute));
    }

    [Fact]
    public void CanDiscoverRoute()
    {
        // Arrange & Act
        var routeTable = RouteTableFactory.Create(new List<Type> { typeof(MyComponent), }, _serviceProvider);

        var routes = GetRoutes(routeTable);

        // Assert
        Assert.Equal("Test1", Assert.Single(routes).RoutePattern.RawText);
    }

    [Route("Test1")]
    private class MyComponent : ComponentBase
    {
    }

    [Fact]
    public void CanDiscoverRoutes_WithInheritance()
    {
        // Arrange & Act
        var routeTable = RouteTableFactory.Create(new List<Type> { typeof(MyComponent), typeof(MyInheritedComponent) }, _serviceProvider);

        var routes = GetRoutes(routeTable);

        // Assert
        Assert.Collection(
            routes.OrderBy(r => r.RoutePattern.RawText),
            r => Assert.Equal("Test1", r.RoutePattern.RawText),
            r => Assert.Equal("Test2", r.RoutePattern.RawText));
    }

    private List<InboundRouteEntry> GetRoutes(RouteTable routeTable)
    {
        var matchingTree = routeTable.TreeRouter.MatchingTrees.Single();
        var result = new HashSet<InboundRouteEntry>();
        GetRoutes(matchingTree.Root, result);
        return result.ToList();

        void GetRoutes(UrlMatchingNode node, HashSet<InboundRouteEntry> result)
        {
            foreach (var match in node.Matches)
            {
                result.Add(match.Entry);
            }

            foreach (var (key, child) in node.Literals)
            {
                GetRoutes(child, result);
            }

            if (node.ConstrainedParameters != null)
            {
                GetRoutes(node.ConstrainedParameters, result);
            }
            if (node.Parameters != null)
            {
                GetRoutes(node.Parameters, result);
            }

            if (node.ConstrainedCatchAlls != null)
            {
                GetRoutes(node.ConstrainedCatchAlls, result);
            }

            if (node.CatchAlls != null)
            {
                GetRoutes(node.CatchAlls, result);
            }
        }
    }

    [Route("Test2")]
    private class MyInheritedComponent : MyComponent
    {
    }

    [Fact]
    public void CanMatchRootTemplate()
    {
        // Arrange
        var routeTable = new TestRouteTableBuilder().AddRoute("/").Build();
        var context = new RouteContext("/");

        // Act
        routeTable.Route(context);

        // Assert
        Assert.NotNull(context.Handler);
    }

    [Fact]
    public void CanMatchLiteralTemplate()
    {
        // Arrange
        var routeTable = new TestRouteTableBuilder().AddRoute("/literal").Build();
        var context = new RouteContext("/literal/");

        // Act
        routeTable.Route(context);

        // Assert
        Assert.NotNull(context.Handler);
    }

    [Fact]
    public void CanMatchTemplateWithMultipleLiterals()
    {
        // Arrange
        var routeTable = new TestRouteTableBuilder().AddRoute("/some/awesome/route/").Build();
        var context = new RouteContext("/some/awesome/route");

        // Act
        routeTable.Route(context);

        // Assert
        Assert.NotNull(context.Handler);
    }

    [Fact]
    public void RouteMatchingIsCaseInsensitive()
    {
        // Arrange
        var routeTable = new TestRouteTableBuilder().AddRoute("/some/AWESOME/route/").Build();
        var context = new RouteContext("/Some/awesome/RouTe");

        // Act
        routeTable.Route(context);

        // Assert
        Assert.NotNull(context.Handler);
    }

    [Fact]
    public void CanMatchEncodedSegments()
    {
        // Arrange
        var routeTable = new TestRouteTableBuilder().AddRoute("/some/ünicõdē/🛣/").Build();
        var context = new RouteContext("/some/%C3%BCnic%C3%B5d%C4%93/%F0%9F%9B%A3");

        // Act
        routeTable.Route(context);

        // Assert
        Assert.NotNull(context.Handler);
    }

    [Fact]
    public void DoesNotMatchIfSegmentsDontMatch()
    {
        // Arrange
        var routeTable = new TestRouteTableBuilder().AddRoute("/some/AWESOME/route/").Build();
        var context = new RouteContext("/some/brilliant/route");

        // Act
        routeTable.Route(context);

        // Assert
        Assert.Null(context.Handler);
    }

    [Theory]
    [InlineData("/{value:bool}", "/maybe")]
    [InlineData("/{value:datetime}", "/1955-01-32")]
    [InlineData("/{value:decimal}", "/hello")]
    [InlineData("/{value:double}", "/0.1.2")]
    [InlineData("/{value:float}", "/0.1.2")]
    [InlineData("/{value:guid}", "/not-a-guid")]
    [InlineData("/{value:int}", "/3.141")]
    [InlineData("/{value:long}", "/3.141")]
    public void DoesNotMatchIfConstraintDoesNotMatch(string template, string contextUrl)
    {
        // Arrange
        var routeTable = new TestRouteTableBuilder().AddRoute(template).Build();
        var context = new RouteContext(contextUrl);

        // Act
        routeTable.Route(context);

        // Assert
        Assert.Null(context.Handler);
    }

    [Theory]
    [InlineData("/some")]
    [InlineData("/some/awesome/route/with/extra/segments")]
    public void DoesNotMatchIfDifferentNumberOfSegments(string path)
    {
        // Arrange
        var routeTable = new TestRouteTableBuilder().AddRoute("/some/awesome/route/").Build();
        var context = new RouteContext(path);

        // Act
        routeTable.Route(context);

        // Assert
        Assert.Null(context.Handler);
    }

    [Theory]
    [InlineData("/value1", "value1")]
    [InlineData("/value2/", "value2")]
    [InlineData("/d%C3%A9j%C3%A0%20vu", "déjà vu")]
    [InlineData("/d%C3%A9j%C3%A0%20vu/", "déjà vu")]
    [InlineData("/d%C3%A9j%C3%A0+vu", "déjà+vu")]
    public void CanMatchParameterTemplate(string path, string expectedValue)
    {
        // Arrange
        var routeTable = new TestRouteTableBuilder().AddRoute("/{parameter}").Build();
        var context = new RouteContext(path);

        // Act
        routeTable.Route(context);

        // Assert
        Assert.NotNull(context.Handler);
        Assert.Single(context.Parameters, p => p.Key == "parameter" && (string)p.Value == expectedValue);
    }

    [Theory]
    [InlineData("/blog/value1", "value1")]
    [InlineData("/blog/value1/foo%20bar", "value1/foo bar")]
    public void CanMatchCatchAllParameterTemplate(string path, string expectedValue)
    {
        // Arrange
        var routeTable = new TestRouteTableBuilder().AddRoute("/blog/{*parameter}").Build();
        var context = new RouteContext(path);

        // Act
        routeTable.Route(context);

        // Assert
        Assert.NotNull(context.Handler);
        Assert.Single(context.Parameters, p => p.Key == "parameter" && (string)p.Value == expectedValue);
    }

    [Fact]
    public void CanMatchTemplateWithMultipleParameters()
    {
        // Arrange
        var routeTable = new TestRouteTableBuilder().AddRoute("/{some}/awesome/{route}/").Build();
        var context = new RouteContext("/an/awesome/path");

        var expectedParameters = new Dictionary<string, object>
        {
            ["some"] = "an",
            ["route"] = "path"
        };

        // Act
        routeTable.Route(context);

        // Assert
        Assert.NotNull(context.Handler);
        Assert.Equal(expectedParameters, context.Parameters);
    }

    [Fact]
    public void CanMatchTemplateWithMultipleParametersAndCatchAllParameter()
    {
        // Arrange
        var routeTable = new TestRouteTableBuilder().AddRoute("/{some}/awesome/{route}/with/{*catchAll}").Build();
        var context = new RouteContext("/an/awesome/path/with/some/catch/all/stuff");

        var expectedParameters = new Dictionary<string, object>
        {
            ["some"] = "an",
            ["route"] = "path",
            ["catchAll"] = "some/catch/all/stuff"
        };

        // Act
        routeTable.Route(context);

        // Assert
        Assert.NotNull(context.Handler);
        Assert.Equal(expectedParameters, context.Parameters);
    }

    public static IEnumerable<object[]> CanMatchParameterWithConstraintCases() => new object[][]
    {
            new object[] { "/{value:bool}", "/true", true },
            new object[] { "/{value:bool}", "/false", false },
            new object[] { "/{value:datetime}", "/1955-01-30", new DateTime(1955, 1, 30) },
            new object[] { "/{value:decimal}", "/5.3", 5.3m },
            new object[] { "/{value:double}", "/0.1", 0.1d },
            new object[] { "/{value:float}", "/0.1", 0.1f },
            new object[] { "/{value:guid}", "/1FCEF085-884F-416E-B0A1-71B15F3E206B", Guid.Parse("1FCEF085-884F-416E-B0A1-71B15F3E206B", CultureInfo.InvariantCulture) },
            new object[] { "/{value:int}", "/123", 123 },
            new object[] { "/{value:int}", "/-123", -123},
            new object[] { "/{value:long}", "/9223372036854775807", long.MaxValue },
            new object[] { "/{value:long}", $"/-9223372036854775808", long.MinValue },
    };

    [Theory]
    [MemberData(nameof(CanMatchParameterWithConstraintCases))]
    public void CanMatchParameterWithConstraint(string template, string contextUrl, object convertedValue)
    {
        // Arrange
        var routeTable = new TestRouteTableBuilder().AddRoute(template).Build();
        var context = new RouteContext(contextUrl);

        // Act
        routeTable.Route(context);

        // Assert
        if (context.Handler == null)
        {
            // Make it easier to track down failing tests when using MemberData
            throw new InvalidOperationException($"Failed to match template '{template}'.");
        }
        Assert.Equal(new Dictionary<string, object>
            {
                { "value", convertedValue }
            }, context.Parameters);
    }

    [Fact]
    public void MoreSpecificRoutesPrecedeMoreGeneralRoutes()
    {
        // Arrange

        // Routes are added in reverse precedence order
        var builder = new TestRouteTableBuilder()
            .AddRoute("/{*last}")
            .AddRoute("/{*last:int}")
            .AddRoute("/{last}")
            .AddRoute("/{last:int}")
            .AddRoute("/literal")
            .AddRoute("/literal/{*last}")
            .AddRoute("/literal/{*last:int}")
            .AddRoute("/literal/{last}")
            .AddRoute("/literal/{last:int}")
            .AddRoute("/literal/literal");

        var expectedOrder = new[]
        {
                "literal",
                "literal/literal",
                "literal/{last:int}",
                "literal/{last}",
                "literal/{*last:int}",
                "literal/{*last}",
                "{last:int}",
                "{last}",
                "{*last:int}",
                "{*last}",
            };

        // Act
        var table = builder.Build();

        // Assert
        Assert.NotNull(table);
        //var tableTemplates = table.Routes.Select(p => p.Template.TemplateText).ToArray();
        //Assert.Equal(expectedOrder, tableTemplates);
    }

    [Theory]
    [InlineData("/literal", null, "literal", "literal/{parameter?}", typeof(TestHandler1))]
    [InlineData("/literal/value", "value", "literal", "literal/{parameter?}", typeof(TestHandler2))]
    [InlineData("/literal", null, "literal/{parameter?}", "literal/{*parameter}", typeof(TestHandler1))]
    [InlineData("/literal/value", "value", "literal/{parameter?}", "literal/{*parameter}", typeof(TestHandler1))]
    [InlineData("/literal/value/other", "value/other", "literal /{parameter?}", "literal/{*parameter}", typeof(TestHandler2))]
    public void CorrectlyMatchesVariableLengthSegments(string path, string expectedValue, string first, string second, Type handler)
    {
        // Arrange

        // Routes are added in reverse precedence order
        var table = new TestRouteTableBuilder()
            .AddRoute(first, typeof(TestHandler1))
            .AddRoute(second, typeof(TestHandler2))
            .Build();

        var context = new RouteContext(path);

        // Act
        table.Route(context);

        // Assert
        Assert.Equal(handler, context.Handler);
        var value = expectedValue != null ? Assert.Single(context.Parameters, p => p.Key == "parameter").Value : null;
        Assert.Equal(expectedValue, value?.ToString());
    }

    [Theory(Skip = "Matching on a per segment basis in ASP.NET is not supported for catch-alls")]
    [InlineData("/values/{*values:int}", "/values/1/2/3/4/5")]
    [InlineData("/{*values:int}", "/1/2/3/4/5")]
    public void CanMatchCatchAllParametersWithConstraints(string template, string path)
    {
        // Arrange

        // Routes are added in reverse precedence order
        var table = new TestRouteTableBuilder()
            .AddRoute(template)
            .Build();

        var context = new RouteContext(path);

        // Act
        table.Route(context);

        // Assert
        Assert.True(context.Parameters.TryGetValue("values", out var values));
        Assert.Equal("1/2/3/4/5", values);
    }

    [Fact]
    public void CatchAllEmpty()
    {
        // Arrange

        // Routes are added in reverse precedence order
        var table = new TestRouteTableBuilder()
            .AddRoute("{*catchall}")
            .Build();

        var context = new RouteContext("/");

        // Act
        table.Route(context);

        // Assert
        Assert.True(context.Parameters.TryGetValue("catchall", out var values));
        Assert.Null(values);
    }

    [Fact]
    public void OptionalParameterEmpty()
    {
        // Arrange

        // Routes are added in reverse precedence order
        var table = new TestRouteTableBuilder()
            .AddRoute("{parameter?}")
            .Build();

        var context = new RouteContext("/");

        // Act
        table.Route(context);

        // Assert
        Assert.True(context.Parameters.TryGetValue("parameter", out var values));
        Assert.Null(values);
    }

    [Theory]
    [InlineData("/", 0)]
    [InlineData("/1", 1)]
    [InlineData("/1/2", 2)]
    [InlineData("/1/2/3", 3)]
    public void MultipleOptionalParameters(string path, int segments)
    {
        // Arrange

        // Routes are added in reverse precedence order
        var table = new TestRouteTableBuilder()
            .AddRoute("{param1?}/{param2?}/{param3?}")
            .Build();

        var context = new RouteContext(path);

        // Act
        table.Route(context);

        // Assert
        for (int i = 1; i <= segments; i++)
        {
            // Segments present in the path have the corresponding value.
            Assert.True(context.Parameters.TryGetValue($"param{i}", out var value));
            Assert.Equal(i.ToString(CultureInfo.InvariantCulture), value);
        }
        for (int i = segments + 1; i <= 3; i++)
        {
            // Segments omitted in the path have the default null value.
            Assert.True(context.Parameters.TryGetValue($"param{i}", out var value));
            Assert.Null(value);
        }
    }

    [Theory]
    [InlineData("/prefix/", 0)]
    [InlineData("/prefix/1", 1)]
    [InlineData("/prefix/1/2", 2)]
    [InlineData("/prefix/1/2/3", 3)]
    public void MultipleOptionalParametersWithPrefix(string path, int segments)
    {
        // Arrange

        // Routes are added in reverse precedence order
        var table = new TestRouteTableBuilder()
            .AddRoute("prefix/{param1?}/{param2?}/{param3?}")
            .Build();

        var context = new RouteContext(path);

        // Act
        table.Route(context);

        // Assert
        for (int i = 1; i <= segments; i++)
        {
            // Segments present in the path have the corresponding value.
            Assert.True(context.Parameters.TryGetValue($"param{i}", out var value));
            Assert.Equal(i.ToString(CultureInfo.InvariantCulture), value);
        }
        for (int i = segments + 1; i <= 3; i++)
        {
            // Segments omitted in the path have the default null value.
            Assert.True(context.Parameters.TryGetValue($"param{i}", out var value));
            Assert.Null(value);
        }
    }

    [Theory]
    [InlineData("/{parameter?}/{*catchAll}", "/", null, null)]
    [InlineData("/{parameter?}/{*catchAll}", "/parameter", "parameter", null)]
    [InlineData("/{parameter?}/{*catchAll}", "/value/1", "value", "1")]
    [InlineData("/{parameter?}/{*catchAll}", "/value/1/2/3/4/5", "value", "1/2/3/4/5")]
    [InlineData("prefix/{parameter?}/{*catchAll}", "/prefix/", null, null)]
    [InlineData("prefix/{parameter?}/{*catchAll}", "/prefix/parameter", "parameter", null)]
    [InlineData("prefix/{parameter?}/{*catchAll}", "/prefix/value/1", "value", "1")]
    [InlineData("prefix/{parameter?}/{*catchAll}", "/prefix/value/1/2/3/4/5", "value", "1/2/3/4/5")]
    public void OptionalParameterPlusCatchAllRoute(string template, string path, string parameterValue, string catchAllValue)
    {
        // Arrange

        // Routes are added in reverse precedence order
        var table = new TestRouteTableBuilder()
            .AddRoute(template)
            .Build();

        var context = new RouteContext(path);

        // Act
        table.Route(context);

        // Assert
        Assert.True(context.Parameters.TryGetValue("parameter", out var parameter));
        Assert.True(context.Parameters.TryGetValue("catchAll", out var catchAll));
        Assert.Equal(parameterValue, parameter);
        Assert.Equal(catchAllValue, catchAll);
    }

    [Fact]
    public void CanMatchCatchAllParametersWithConstraints_NotMatchingRoute()
    {
        // Arrange

        // Routes are added in reverse precedence order
        var table = new TestRouteTableBuilder()
            .AddRoute("/values/{*values:int}")
            .Build();

        var context = new RouteContext("/values/1/2/3/4/5/A");

        // Act
        table.Route(context);

        // Assert
        Assert.Null(context.Handler);
    }

    [Fact]
    public void CanMatchOptionalParameterWithoutConstraints()
    {
        // Arrange
        var template = "/optional/{value?}";
        var contextUrl = "/optional/";
        string convertedValue = null;

        var routeTable = new TestRouteTableBuilder().AddRoute(template).Build();
        var context = new RouteContext(contextUrl);

        // Act
        routeTable.Route(context);

        // Assert
        if (context.Handler == null)
        {
            // Make it easier to track down failing tests when using MemberData
            throw new InvalidOperationException($"Failed to match template '{template}'.");
        }
        Assert.Equal(new Dictionary<string, object>
            {
                { "value", convertedValue }
            }, context.Parameters);
    }

    public static IEnumerable<object[]> CanMatchOptionalParameterWithConstraintCases() => new object[][]
{
            new object[] { "/optional/{value:bool?}", "/optional/", null },
            new object[] { "/optional/{value:datetime?}", "/optional/", null },
            new object[] { "/optional/{value:decimal?}", "/optional/", null },
};

    [Theory]
    [MemberData(nameof(CanMatchOptionalParameterWithConstraintCases))]
    public void CanMatchOptionalParameterWithConstraint(string template, string contextUrl, object convertedValue)
    {
        // Arrange
        var routeTable = new TestRouteTableBuilder().AddRoute(template).Build();
        var context = new RouteContext(contextUrl);

        // Act
        routeTable.Route(context);

        // Assert
        if (context.Handler == null)
        {
            // Make it easier to track down failing tests when using MemberData
            throw new InvalidOperationException($"Failed to match template '{template}'.");
        }
        Assert.Equal(new Dictionary<string, object>
            {
                { "value", convertedValue }
            }, context.Parameters);
    }

    [Fact]
    public void CanMatchMultipleOptionalParameterWithConstraint()
    {
        // Arrange
        var template = "/optional/{value:datetime?}/{value2:datetime?}";
        var contextUrl = "/optional/";
        object convertedValue = null;

        var routeTable = new TestRouteTableBuilder().AddRoute(template).Build();
        var context = new RouteContext(contextUrl);

        // Act
        routeTable.Route(context);

        // Assert
        if (context.Handler == null)
        {
            // Make it easier to track down failing tests when using MemberData
            throw new InvalidOperationException($"Failed to match template '{template}'.");
        }
        Assert.Equal(new Dictionary<string, object>
            {
                { "value", convertedValue },
                { "value2", convertedValue }
            }, context.Parameters);
    }

    public static IEnumerable<object[]> CanMatchSegmentWithMultipleConstraintsCases() => new object[][]
{
            new object[] { "/{value:double:int}/", "/15", 15 },
            new object[] { "/{value:double:int?}/", "/", null },
};

    [Theory]
    [MemberData(nameof(CanMatchSegmentWithMultipleConstraintsCases))]
    public void CanMatchSegmentWithMultipleConstraints(string template, string contextUrl, object convertedValue)
    {
        // Arrange
        var routeTable = new TestRouteTableBuilder().AddRoute(template).Build();
        var context = new RouteContext(contextUrl);

        // Act
        routeTable.Route(context);

        // Assert
        Assert.Equal(new Dictionary<string, object>
            {
                { "value", convertedValue }
            }, context.Parameters);
    }

    [Fact]
    public void PrefersLiteralTemplateOverTemplateWithParameters()
    {
        // Arrange
        var routeTable = new TestRouteTableBuilder()
            .AddRoute("/an/awesome/path", typeof(TestHandler1))
            .AddRoute("/{some}/awesome/{route}/", typeof(TestHandler2))
            .Build();
        var context = new RouteContext("/an/awesome/path");

        // Act
        routeTable.Route(context);

        // Assert
        Assert.NotNull(context.Handler);
        Assert.Null(context.Parameters);
    }

    [Fact]
    public void PrefersLiteralTemplateOverTemplateWithOptionalParameters()
    {
        // Arrange
        var routeTable = new TestRouteTableBuilder()
            .AddRoute("/users/1", typeof(TestHandler1))
            .AddRoute("/users/{id?}", typeof(TestHandler2))
            .Build();
        var context = new RouteContext("/users/1");

        // Act
        routeTable.Route(context);

        // Assert
        Assert.NotNull(context.Handler);
        Assert.Null(context.Parameters);
    }

    [Fact]
    public void ThrowsForOptionalParametersAndNonOptionalParameters()
    {
        // Arrange, act & assert
        Assert.Throws<InvalidOperationException>(() => new TestRouteTableBuilder()
            .AddRoute("/users/{id}", typeof(TestHandler1))
            .AddRoute("/users/{id?}", typeof(TestHandler2))
            .Build());
    }

    [Theory]
    [InlineData("{*catchall}/literal")]
    [InlineData("{*catchall}/{parameter}")]
    [InlineData("{*catchall}/{parameter?}")]
    [InlineData("{*catchall}/{*other}")]
    [InlineData("prefix/{*catchall}/literal")]
    [InlineData("prefix/{*catchall}/{parameter}")]
    [InlineData("prefix/{*catchall}/{parameter?}")]
    [InlineData("prefix/{*catchall}/{*other}")]
    public void ThrowsWhenCatchAllIsNotTheLastSegment(string template)
    {
        // Arrange, act & assert
        Assert.Throws<RoutePatternException>(() => new TestRouteTableBuilder()
            .AddRoute(template)
            .Build());
    }

    [Theory(Skip = "This is allowed in ASP.NET Core routing.")]
    [InlineData("{optional?}/literal")]
    [InlineData("{optional?}/{parameter}")]
    [InlineData("{optional?}/{parameter:int}")]
    [InlineData("prefix/{optional?}/literal")]
    [InlineData("prefix/{optional?}/{parameter}")]
    [InlineData("prefix/{optional?}/{parameter:int}")]
    public void ThrowsForOptionalParametersFollowedByNonOptionalParameters(string template)
    {
        // Arrange, act & assert
        Assert.Throws<InvalidOperationException>(() => new TestRouteTableBuilder()
            .AddRoute(template)
            .Build());
    }

    [Theory]
    [InlineData("{parameter}", "{parameter?}")]
    [InlineData("{parameter:int}", "{parameter:bool?}")]
    public void ThrowsForAmbiguousRoutes(string first, string second)
    {
        // Arrange, act & assert
        var exception = Assert.Throws<InvalidOperationException>(() => new TestRouteTableBuilder()
            .AddRoute(first, typeof(TestHandler1))
            .AddRoute(second, typeof(TestHandler2))
            .Build());

        exception.Message.Contains("The following routes are ambiguous");
    }

    // It's important the precedence is inverted here to also validate that
    // the precedence is correct in these cases
    [Theory]
    [InlineData("{optional?}", "/")]
    [InlineData("{optional?}", "literal")]
    [InlineData("{optional?}", "{optional:int?}")]
    [InlineData("{*catchAll:int}", "{optional?}")]
    [InlineData("{*catchAll}", "{optional?}")]
    [InlineData("literal/{optional?}", "/")]
    [InlineData("literal/{optional?}", "literal")]
    [InlineData("literal/{optional?}", "literal/{optional:int?}")]
    [InlineData("literal/{*catchAll:int}", "literal/{optional?}")]
    [InlineData("literal/{*catchAll}", "literal/{optional?}")]
    [InlineData("{param}/{optional?}", "/")]
    [InlineData("{param}/{optional?}", "{param}")]
    [InlineData("{param}/{optional?}", "{param}/{optional:int?}")]
    [InlineData("{param}/{*catchAll:int}", "{param}/{optional?}")]
    [InlineData("{param}/{*catchAll}", "{param}/{optional?}")]
    [InlineData("{param1?}/{param2?}/{param3?}/{optional?}", "/")]
    [InlineData("{param1?}/{param2?}/{param3?}/{optional?}", "{param1?}/{param2?}/{param3?}/{optional:int?}")]
    [InlineData("{param1?}/{param2?}/{param3?}/{optional?}", "{param1?}/{param2?}/{param3:int?}/{optional?}")]
    [InlineData("{param1?}/{param2?}/{param3:int?}/{optional?}", "{param1?}/{param2?}")]
    [InlineData("{param1?}/{param2?}/{param3?}/{*catchAll:int}", "{param1?}/{param2?}/{param3?}/{optional?}")]
    [InlineData("{param1?}/{param2?}/{param3?}/{*catchAll}", "{param1?}/{param2?}/{param3?}/{optional?}")]
    public void DoesNotThrowForNonAmbiguousRoutes(string first, string second)
    {
        // Arrange
        var builder = new TestRouteTableBuilder()
            .AddRoute(first, typeof(TestHandler1))
            .AddRoute(second, typeof(TestHandler2));

        //var expectedOrder = new[] { second, first };

        // Act
        var table = builder.Build();

        // Assert
        //var tableTemplates = table .Routes.Select(p => p.Template.TemplateText).ToArray();
        //Assert.Equal(expectedOrder, tableTemplates);
    }

    [Fact]
    public void ThrowsForLiteralWithQuestionMark()
    {
        // Arrange, act & assert
        Assert.Throws<RoutePatternException>(() => new TestRouteTableBuilder()
            .AddRoute("literal?")
            .Build());
    }

    [Fact]
    public void PrefersLiteralTemplateOverParameterizedTemplates()
    {
        // Arrange
        var routeTable = new TestRouteTableBuilder()
            .AddRoute("/users/1/friends", typeof(TestHandler1))
            .AddRoute("/users/{id}/{location}", typeof(TestHandler2))
            .AddRoute("/users/1/{location}", typeof(TestHandler2))
            .Build();
        var context = new RouteContext("/users/1/friends");

        // Act
        routeTable.Route(context);

        // Assert
        Assert.NotNull(context.Handler);
        Assert.Equal(typeof(TestHandler1), context.Handler);
        Assert.Null(context.Parameters);
    }

    [Fact]
    public void PrefersShorterRoutesOverLongerRoutes()
    {
        // Arrange & Act
        var handler = typeof(int);
        var routeTable = new TestRouteTableBuilder()
            .AddRoute("/an/awesome/path")
            .AddRoute("/an/awesome/", handler).Build();

        // Act
        //Assert.Equal("an/awesome", routeTable.Routes[0].Template.TemplateText);
    }

    [Fact]
    public void PrefersMoreConstraintsOverFewer()
    {
        // Arrange
        var routeTable = new TestRouteTableBuilder()
            .AddRoute("/products/{id}")
            .AddRoute("/products/{id:int}").Build();
        var context = new RouteContext("/products/456");

        // Act
        routeTable.Route(context);

        // Assert
        Assert.NotNull(context.Handler);
        Assert.Equal(context.Parameters, new Dictionary<string, object>
            {
                { "id", 456 }
            });
    }

    [Fact]
    public void PrefersRoutesThatMatchMoreSegments()
    {
        // Arrange
        var routeTable = new TestRouteTableBuilder()
            .AddRoute("/{anythingGoes}", typeof(TestHandler1))
            .AddRoute("/users/{id?}", typeof(TestHandler2))
            .Build();
        var context = new RouteContext("/users/1");

        // Act
        routeTable.Route(context);

        // Assert
        Assert.NotNull(context.Handler);
        Assert.Equal(typeof(TestHandler2), context.Handler);
        Assert.NotNull(context.Parameters);
    }

    [Fact]
    public void ProducesAStableOrderForNonAmbiguousRoutes()
    {
        // Arrange & Act
        var handler = typeof(int);
        var routeTable = new TestRouteTableBuilder()
            .AddRoute("/an/awesome/", handler)
            .AddRoute("/a/brilliant/").Build();

        // Act
        //Assert.Equal("a/brilliant", routeTable.Routes[0].Template.TemplateText);
    }

    [Fact]
    public void DoesNotThrowIfStableSortComparesRouteWithItself()
    {
        // Test for https://github.com/dotnet/aspnetcore/issues/13313
        // Arrange & Act
        var builder = new TestRouteTableBuilder();
        builder.AddRoute("r16");
        builder.AddRoute("r05");
        builder.AddRoute("r09");
        builder.AddRoute("r00");
        builder.AddRoute("r13");
        builder.AddRoute("r02");
        builder.AddRoute("r03");
        builder.AddRoute("r10");
        builder.AddRoute("r15");
        builder.AddRoute("r14");
        builder.AddRoute("r12");
        builder.AddRoute("r07");
        builder.AddRoute("r11");
        builder.AddRoute("r08");
        builder.AddRoute("r06");
        builder.AddRoute("r04");
        builder.AddRoute("r01");

        // Act
        var routeTable = builder.Build();

        // Assert
        Assert.NotNull(routeTable);
        var matchingTree = Assert.Single(routeTable.TreeRouter.MatchingTrees);

        //Assert.Equal(17, routeTable.Routes.Length);
        //for (var i = 0; i < 17; i++)
        //{
        //    var templateText = "r" + i.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0');
        //    Assert.Equal(templateText, routeTable.Routes[i].Template.TemplateText);
        //}
    }

    [Theory]
    [InlineData("/literal", "/Literal/")]
    [InlineData("/{parameter}", "/{parameter}/")]
    [InlineData("/{parameter}part", "/part{parameter}")]
    [InlineData("/literal/{parameter}", "/Literal/{something}")]
    [InlineData("/{parameter}/literal/{something}", "{param}/Literal/{else}")]
    [InlineData("/{parameter}part/literal/part{something}", "{param}Part/Literal/part{else}")]
    public void DetectsAmbiguousRoutes(string left, string right)
    {
        // Arrange
        var expectedMessage = $@"The following routes are ambiguous:
'{left.Trim('/')}' in '{typeof(object).FullName}'
'{right.Trim('/')}' in '{typeof(object).FullName}'
";
        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => new TestRouteTableBuilder()
            .AddRoute(left)
            .AddRoute(right).Build());

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Theory]
    [InlineData("/literal/{parameter}", "/Literal/")]
    [InlineData("/literal/literal2/{parameter}", "/literal/literal3/{parameter}")]
    [InlineData("/literal/part{parameter}part", "/literal/part{parameter}")]
    [InlineData("/{parameter}", "/{{parameter}}")]
    public void DetectsAmbiguousRoutesNoFalsePositives(string left, string right)
    {
        // Act

        new TestRouteTableBuilder()
            .AddRoute(left)
            .AddRoute(right).Build();

        // Assertion is that it doesn't throw
    }

    [Fact]
    public void SuppliesNullForUnusedHandlerParameters()
    {
        // Arrange
        var routeTable = new TestRouteTableBuilder()
            .AddRoute("/{unrelated}", typeof(TestHandler2))
            .AddRoute("/products/{param2}/{PaRam1}", typeof(TestHandler1))
            .AddRoute("/products/{param1:int}", typeof(TestHandler1))
            .AddRoute("/", typeof(TestHandler1))
            .Build();
        var context = new RouteContext("/products/456");

        // Act
        routeTable.Route(context);

        // Assert
        //Assert.Collection(
        //    routeTable.Routes,
        //    route =>
        //    {
        //        Assert.Same(typeof(TestHandler1), route.Handler);
        //        Assert.Equal("/", route.Template.TemplateText);
        //        Assert.Equal(new[] { "PaRam1", "param2" }, route.UnusedRouteParameterNames.OrderBy(id => id).ToArray());
        //    },
        //    route =>
        //    {
        //        Assert.Same(typeof(TestHandler1), route.Handler);
        //        Assert.Equal("products/{param1:int}", route.Template.TemplateText);
        //        Assert.Equal(new[] { "param2" }, route.UnusedRouteParameterNames.OrderBy(id => id).ToArray());
        //    },
        //    route =>
        //    {
        //        Assert.Same(typeof(TestHandler1), route.Handler);
        //        Assert.Equal("products/{param2}/{PaRam1}", route.Template.TemplateText);
        //        Assert.Null(route.UnusedRouteParameterNames);
        //    },
        //    route =>
        //    {
        //        Assert.Same(typeof(TestHandler2), route.Handler);
        //        Assert.Equal("{unrelated}", route.Template.TemplateText);
        //        Assert.Null(route.UnusedRouteParameterNames);
        //    });

        Assert.Same(typeof(TestHandler1), context.Handler);
        Assert.Equal(new Dictionary<string, object>
            {
                { "param1", 456 },
                { "param2", null },
            }, context.Parameters);
    }

    private class TestRouteTableBuilder
    {
        private readonly ServiceProvider _serviceProvider;

        public TestRouteTableBuilder()
        {
            _serviceProvider = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider();
        }
        readonly IList<(string Template, Type Handler)> _routeTemplates = new List<(string, Type)>();
        readonly Type _handler = typeof(object);

        public TestRouteTableBuilder AddRoute(string template, Type handler = null)
        {
            _routeTemplates.Add((template, handler ?? _handler));
            return this;
        }

        public RouteTable Build()
        {
            try
            {
                var templatesByHandler = _routeTemplates
                    .GroupBy(rt => rt.Handler)
                    .ToDictionary(group => group.Key, group => group.Select(g => g.Template).ToArray());
                return RouteTableFactory.Create(templatesByHandler, _serviceProvider);
            }
            catch (InvalidOperationException ex) when (ex.InnerException is InvalidOperationException)
            {
                // ToArray() will wrap our exception in its own.
                throw ex.InnerException;
            }
        }
    }

    class TestHandler1 { }
    class TestHandler2 { }

    [Route("/ComponentWithoutExcludeFromInteractiveRoutingAttribute")]
    public class ComponentWithoutExcludeFromInteractiveRoutingAttribute : ComponentBase { }

    [Route("/ComponentWithExcludeFromInteractiveRoutingAttribute")]
    [ExcludeFromInteractiveRouting]
    public class ComponentWithExcludeFromInteractiveRoutingAttribute : ComponentBase { }
}
