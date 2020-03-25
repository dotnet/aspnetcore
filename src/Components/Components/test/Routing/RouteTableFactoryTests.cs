// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components.Routing;
using Xunit;

namespace Microsoft.AspNetCore.Components.Test.Routing
{
    public class RouteTableFactoryTests
    {
        [Fact]
        public void CanCacheRouteTable()
        {
            // Arrange
            var routes1 = RouteTableFactory.Create(new[] { GetType().Assembly, });

            // Act
            var routes2 = RouteTableFactory.Create(new[] { GetType().Assembly, });

            // Assert
            Assert.Same(routes1, routes2);
        }

        [Fact]
        public void CanCacheRouteTableWithDifferentAssembliesAndOrder()
        {
            // Arrange
            var routes1 = RouteTableFactory.Create(new[] { typeof(object).Assembly, GetType().Assembly, });

            // Act
            var routes2 = RouteTableFactory.Create(new[] { GetType().Assembly, typeof(object).Assembly, });

            // Assert
            Assert.Same(routes1, routes2);
        }

        [Fact]
        public void DoesNotCacheRouteTableForDifferentAssemblies()
        {
            // Arrange
            var routes1 = RouteTableFactory.Create(new[] { GetType().Assembly, });

            // Act
            var routes2 = RouteTableFactory.Create(new[] { GetType().Assembly, typeof(object).Assembly, });

            // Assert
            Assert.NotSame(routes1, routes2);
        }

        [Fact]
        public void CanDiscoverRoute()
        {
            // Arrange & Act
            var routes = RouteTableFactory.Create(new[] { typeof(MyComponent), });

            // Assert
            Assert.Equal("Test1", Assert.Single(routes.Routes).Template.TemplateText);
        }

        [Route("Test1")]
        private class MyComponent : ComponentBase
        {
        }

        [Fact]
        public void CanDiscoverRoutes_WithInheritance()
        {
            // Arrange & Act
            var routes = RouteTableFactory.Create(new[] { typeof(MyComponent), typeof(MyInheritedComponent), });

            // Assert
            Assert.Collection(
                routes.Routes.OrderBy(r => r.Template.TemplateText),
                r => Assert.Equal("Test1", r.Template.TemplateText),
                r => Assert.Equal("Test2", r.Template.TemplateText));
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

        public static IEnumerable<object[]> CanMatchParameterWithConstraintCases() => new object[][]
        {
            new object[] { "/{value:bool}", "/true", true },
            new object[] { "/{value:bool}", "/false", false },
            new object[] { "/{value:datetime}", "/1955-01-30", new DateTime(1955, 1, 30) },
            new object[] { "/{value:decimal}", "/5.3", 5.3m },
            new object[] { "/{value:double}", "/0.1", 0.1d },
            new object[] { "/{value:float}", "/0.1", 0.1f },
            new object[] { "/{value:guid}", "/1FCEF085-884F-416E-B0A1-71B15F3E206B", Guid.Parse("1FCEF085-884F-416E-B0A1-71B15F3E206B") },
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
            Assert.Equal(context.Parameters, new Dictionary<string, object>
            {
                { "value", convertedValue }
            });
        }

        [Fact]
        public void CanMatchSegmentWithMultipleConstraints()
        {
            // Arrange
            var routeTable = new TestRouteTableBuilder().AddRoute("/{value:double:int}/").Build();
            var context = new RouteContext("/15");

            // Act
            routeTable.Route(context);

            // Assert
            Assert.NotNull(context.Handler);
            Assert.Equal(context.Parameters, new Dictionary<string, object>
            {
                { "value", 15 } // Final constraint's convertedValue is used
            });
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
        public void PrefersShorterRoutesOverLongerRoutes()
        {
            // Arrange & Act
            var handler = typeof(int);
            var routeTable = new TestRouteTableBuilder()
                .AddRoute("/an/awesome/path")
                .AddRoute("/an/awesome/", handler).Build();

            // Act
            Assert.Equal("an/awesome", routeTable.Routes[0].Template.TemplateText);
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
        public void ProducesAStableOrderForNonAmbiguousRoutes()
        {
            // Arrange & Act
            var handler = typeof(int);
            var routeTable = new TestRouteTableBuilder()
                .AddRoute("/an/awesome/", handler)
                .AddRoute("/a/brilliant/").Build();

            // Act
            Assert.Equal("a/brilliant", routeTable.Routes[0].Template.TemplateText);
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

            var routeTable = builder.Build();

            // Act
            Assert.Equal(17, routeTable.Routes.Length);
            for (var i = 0; i < 17; i++)
            {
                var templateText = "r" + i.ToString().PadLeft(2, '0');
                Assert.Equal(templateText, routeTable.Routes[i].Template.TemplateText);
            }
        }

        [Theory]
        [InlineData("/literal", "/Literal/")]
        [InlineData("/{parameter}", "/{parameter}/")]
        [InlineData("/literal/{parameter}", "/Literal/{something}")]
        [InlineData("/{parameter}/literal/{something}", "{param}/Literal/{else}")]
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

        [Fact]
        public void SuppliesNullForUnusedHandlerParameters()
        {
            // Arrange
            var routeTable = new TestRouteTableBuilder()
                .AddRoute("/", typeof(TestHandler1))
                .AddRoute("/products/{param1:int}", typeof(TestHandler1))
                .AddRoute("/products/{param2}/{PaRam1}", typeof(TestHandler1))
                .AddRoute("/{unrelated}", typeof(TestHandler2))
                .Build();
            var context = new RouteContext("/products/456");

            // Act
            routeTable.Route(context);

            // Assert
            Assert.Collection(routeTable.Routes,
                route =>
                {
                    Assert.Same(typeof(TestHandler1), route.Handler);
                    Assert.Equal("/", route.Template.TemplateText);
                    Assert.Equal(new[] { "param1", "param2" }, route.UnusedRouteParameterNames);
                },
                route =>
                {
                    Assert.Same(typeof(TestHandler2), route.Handler);
                    Assert.Equal("{unrelated}", route.Template.TemplateText);
                    Assert.Equal(Array.Empty<string>(), route.UnusedRouteParameterNames);
                },
                route =>
                {
                    Assert.Same(typeof(TestHandler1), route.Handler);
                    Assert.Equal("products/{param1:int}", route.Template.TemplateText);
                    Assert.Equal(new[] { "param2" }, route.UnusedRouteParameterNames);
                },
                route =>
                {
                    Assert.Same(typeof(TestHandler1), route.Handler);
                    Assert.Equal("products/{param2}/{PaRam1}", route.Template.TemplateText);
                    Assert.Equal(Array.Empty<string>(), route.UnusedRouteParameterNames);
                });
            Assert.Same(typeof(TestHandler1), context.Handler);
            Assert.Equal(new Dictionary<string, object>
            {
                { "param1", 456 },
                { "param2", null },
            }, context.Parameters);
        }

        private class TestRouteTableBuilder
        {
            IList<(string Template, Type Handler)> _routeTemplates = new List<(string, Type)>();
            Type _handler = typeof(object);

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
                    return RouteTableFactory.Create(templatesByHandler);
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
    }
}
