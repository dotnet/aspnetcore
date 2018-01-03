// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class TreeMatcherTest
    {
        [Theory]
        [InlineData("template/5", "template/{parameter:int}")]
        [InlineData("template/5", "template/{parameter}")]
        [InlineData("template/5", "template/{*parameter:int}")]
        [InlineData("template/5", "template/{*parameter}")]
        [InlineData("template/{parameter}", "template/{parameter:alpha}")] // constraint doesn't match
        [InlineData("template/{parameter:int}", "template/{parameter}")]
        [InlineData("template/{parameter:int}", "template/{*parameter:int}")]
        [InlineData("template/{parameter:int}", "template/{*parameter}")]
        [InlineData("template/{parameter}", "template/{*parameter:int}")]
        [InlineData("template/{parameter}", "template/{*parameter}")]
        [InlineData("template/5", "template/5/{*parameter}")]
        [InlineData("template/{*parameter:int}", "template/{*parameter}")]
        public async Task MatchAsync_RespectsPrecedence(
            string firstTemplate,
            string secondTemplate)
        {
            // Arrange
            var dataSource = new DefaultDispatcherDataSource()
            {
                Endpoints =
                {
                    new RoutePatternEndpoint(firstTemplate, new { }, Test_Delegate, "Test1"),
                    new RoutePatternEndpoint(secondTemplate, new { }, Test_Delegate, "Test2"),
                },
            };

            var context = CreateMatcherContext("/template/5");
            var factory = new TreeMatcherFactory();
            var matcher = factory.CreateMatcher(dataSource, new List<EndpointSelector>());

            // Act
            await matcher.MatchAsync(context);

            // Assert
            Assert.Same(dataSource.Endpoints[0], context.Endpoint);
        }

        [Theory]
        [InlineData("template/5", "template/{parameter:int}")]
        [InlineData("template/5", "template/{parameter}")]
        [InlineData("template/5", "template/{*parameter:int}")]
        [InlineData("template/5", "template/{*parameter}")]
        [InlineData("template/{parameter:int}", "template/{parameter}")]
        [InlineData("template/{parameter:int}", "template/{*parameter:int}")]
        [InlineData("template/{parameter:int}", "template/{*parameter}")]
        [InlineData("template/{parameter}", "template/{*parameter:int}")]
        [InlineData("template/{parameter}", "template/{*parameter}")]
        [InlineData("template/5", "template/5/{*parameter}")]
        [InlineData("template/{*parameter:int}", "template/{*parameter}")]
        public async Task MatchAsync_RespectsOrderOverPrecedence(
            string firstTemplate,
            string secondTemplate)
        {
            // Arrange
            var dataSource = new DefaultDispatcherDataSource()
            {
                Endpoints =
                {
                    new RoutePatternEndpoint(firstTemplate, new { }, Test_Delegate, "Test1", new EndpointOrderMetadata(1)),
                    new RoutePatternEndpoint(secondTemplate, new { }, Test_Delegate, "Test2", new EndpointOrderMetadata(0)),
                },
            };

            var context = CreateMatcherContext("/template/5");
            var factory = new TreeMatcherFactory();
            var matcher = factory.CreateMatcher(dataSource, new List<EndpointSelector>());

            // Act
            await matcher.MatchAsync(context);

            // Assert
            Assert.Same(dataSource.Endpoints[1], context.Endpoint);
        }

        [Theory]
        [InlineData("template/{first:int}", "template/{second:int}")]
        [InlineData("template/{first}", "template/{second}")]
        [InlineData("template/{*first:int}", "template/{*second:int}")]
        [InlineData("template/{*first}", "template/{*second}")]
        public async Task MatchAsync_EnsuresStableOrdering(string firstTemplate, string secondTemplate)
        {
            // Arrange
            var dataSource = new DefaultDispatcherDataSource()
            {
                Endpoints =
                {
                    new RoutePatternEndpoint(firstTemplate, new { }, Test_Delegate, "Test1"),
                    new RoutePatternEndpoint(secondTemplate, new { }, Test_Delegate, "Test2"),
                },
            };

            var context = CreateMatcherContext("/template/5");
            var factory = new TreeMatcherFactory();
            var matcher = factory.CreateMatcher(dataSource, new List<EndpointSelector>());

            // Act
            await matcher.MatchAsync(context);

            // Assert
            Assert.Same(dataSource.Endpoints[0], context.Endpoint);
        }

        [Theory]
        [InlineData("/", 0)]
        [InlineData("/Literal1", 1)]
        [InlineData("/Literal1/Literal2", 2)]
        [InlineData("/Literal1/Literal2/Literal3", 3)]
        [InlineData("/Literal1/Literal2/Literal3/4", 4)]
        [InlineData("/Literal1/Literal2/Literal3/Literal4", 5)]
        [InlineData("/1", 6)]
        [InlineData("/1/2", 7)]
        [InlineData("/1/2/3", 8)]
        [InlineData("/1/2/3/4", 9)]
        [InlineData("/1/2/3/CatchAll4", 10)]
        [InlineData("/parameter1", 11)]
        [InlineData("/parameter1/parameter2", 12)]
        [InlineData("/parameter1/parameter2/parameter3", 13)]
        [InlineData("/parameter1/parameter2/parameter3/4", 14)]
        [InlineData("/parameter1/parameter2/parameter3/CatchAll4", 15)]
        public async Task MatchAsync_MatchesEndpointWithTheRightLength(string url, int index)
        {
            // Arrange
            var dataSource = new DefaultDispatcherDataSource()
            {
                Endpoints =
                {
                    new RoutePatternEndpoint("", Test_Delegate),
                    new RoutePatternEndpoint("Literal1", Test_Delegate),
                    new RoutePatternEndpoint("Literal1/Literal2", Test_Delegate),
                    new RoutePatternEndpoint("Literal1/Literal2/Literal3", Test_Delegate),
                    new RoutePatternEndpoint("Literal1/Literal2/Literal3/{*constrainedCatchAll:int}", Test_Delegate),
                    new RoutePatternEndpoint("Literal1/Literal2/Literal3/{*catchAll}", Test_Delegate),
                    new RoutePatternEndpoint("{constrained1:int}", Test_Delegate),
                    new RoutePatternEndpoint("{constrained1:int}/{constrained2:int}", Test_Delegate),
                    new RoutePatternEndpoint("{constrained1:int}/{constrained2:int}/{constrained3:int}", Test_Delegate),
                    new RoutePatternEndpoint("{constrained1:int}/{constrained2:int}/{constrained3:int}/{*constrainedCatchAll:int}", Test_Delegate),
                    new RoutePatternEndpoint("{constrained1:int}/{constrained2:int}/{constrained3:int}/{*catchAll}", Test_Delegate),
                    new RoutePatternEndpoint("{parameter1}", Test_Delegate),
                    new RoutePatternEndpoint("{parameter1}/{parameter2}", Test_Delegate),
                    new RoutePatternEndpoint("{parameter1}/{parameter2}/{parameter3}", Test_Delegate),
                    new RoutePatternEndpoint("{parameter1}/{parameter2}/{parameter3}/{*constrainedCatchAll:int}", Test_Delegate),
                    new RoutePatternEndpoint("{parameter1}/{parameter2}/{parameter3}/{*catchAll}", Test_Delegate),
                },
            };

            var context = CreateMatcherContext(url);
            var factory = new TreeMatcherFactory();
            var matcher = factory.CreateMatcher(dataSource, new List<EndpointSelector>());

            // Act
            await matcher.MatchAsync(context);

            // Assert
            Assert.Same(dataSource.Endpoints[index], context.Endpoint);
        }

        public static TheoryData<string, object[]> MatchesEndpointsWithDefaultsData =>
            new TheoryData<string, object[]>
            {
                { "/", new object[] { "1", "2", "3", "4" } },
                { "/a", new object[] { "a", "2", "3", "4" } },
                { "/a/b", new object[] { "a", "b", "3", "4" } },
                { "/a/b/c", new object[] { "a", "b", "c", "4" } },
                { "/a/b/c/d", new object[] { "a", "b", "c", "d" } }
            };

        [Theory]
        [MemberData(nameof(MatchesEndpointsWithDefaultsData))]
        public async Task MatchAsync_MatchesEndpointsWithDefaults(string url, object[] values)
        {
            // Arrange
            var dataSource = new DefaultDispatcherDataSource()
            {
                Endpoints =
                {
                    new RoutePatternEndpoint("{parameter1=1}/{parameter2=2}/{parameter3=3}/{parameter4=4}",
                    new { parameter1 = 1, parameter2 = 2, parameter3 = 3, parameter4 = 4 }, Test_Delegate, "Test"),
                },
            };

            var valueKeys = new[] { "parameter1", "parameter2", "parameter3", "parameter4" };
            var expectedValues = new DispatcherValueCollection();
            for (int i = 0; i < valueKeys.Length; i++)
            {
                expectedValues.Add(valueKeys[i], values[i]);
            }

            var context = CreateMatcherContext(url);
            var factory = new TreeMatcherFactory();
            var matcher = factory.CreateMatcher(dataSource, new List<EndpointSelector>());

            // Act
            await matcher.MatchAsync(context);

            // Assert
            foreach (var entry in expectedValues)
            {
                var data = Assert.Single(context.Values, v => v.Key == entry.Key);
                Assert.Equal(entry.Value, data.Value);
            }
        }

        public static TheoryData<string, object[]> MatchesConstrainedEndpointsWithDefaultsData =>
            new TheoryData<string, object[]>
            {
                { "/", new object[] { "1", "2", "3", "4" } },
                { "/10", new object[] { "10", "2", "3", "4" } },
                { "/10/11", new object[] { "10", "11", "3", "4" } },
                { "/10/11/12", new object[] { "10", "11", "12", "4" } },
                { "/10/11/12/13", new object[] { "10", "11", "12", "13" } }
            };

        [Theory]
        [MemberData(nameof(MatchesConstrainedEndpointsWithDefaultsData))]
        public async Task MatchAsync_MatchesConstrainedEndpointsWithDefaults(string url, object[] values)
        {
            // Arrange
            var dataSource = new DefaultDispatcherDataSource()
            {
                Endpoints =
                {
                    new RoutePatternEndpoint("{parameter1:int=1}/{parameter2:int=2}/{parameter3:int=3}/{parameter4:int=4}",
                    new { parameter1 = 1, parameter2 = 2, parameter3 = 3, parameter4 = 4 }, Test_Delegate, "Test"),
                },
            };

            var valueKeys = new[] { "parameter1", "parameter2", "parameter3", "parameter4" };
            var expectedValues = new DispatcherValueCollection();
            for (int i = 0; i < valueKeys.Length; i++)
            {
                expectedValues.Add(valueKeys[i], values[i]);
            }

            var context = CreateMatcherContext(url);
            var factory = new TreeMatcherFactory();
            var matcher = factory.CreateMatcher(dataSource, new List<EndpointSelector>());

            // Act
            await matcher.MatchAsync(context);

            // Assert
            foreach (var entry in expectedValues)
            {
                var data = Assert.Single(context.Values, v => v.Key == entry.Key);
                Assert.Equal(entry.Value, data.Value);
            }
        }

        [Fact]
        public async Task MatchAsync_MatchesCatchAllEndpointsWithDefaults()
        {
            // Arrange
            var dataSource = new DefaultDispatcherDataSource()
            {
                Endpoints =
                {
                    new RoutePatternEndpoint("{parameter1=1}/{parameter2=2}/{parameter3=3}/{*parameter4=4}",
                    new { parameter1 = 1, parameter2 = 2, parameter3 = 3, parameter4 = 4 }, Test_Delegate, "Test"),
                },
            };

            var url = "/a/b/c";
            var values = new[] { "a", "b", "c", "4" };

            var valueKeys = new[] { "parameter1", "parameter2", "parameter3", "parameter4" };
            var expectedValues = new DispatcherValueCollection();
            for (int i = 0; i < valueKeys.Length; i++)
            {
                expectedValues.Add(valueKeys[i], values[i]);
            }

            var context = CreateMatcherContext(url);
            var factory = new TreeMatcherFactory();
            var matcher = factory.CreateMatcher(dataSource, new List<EndpointSelector>());

            // Act
            await matcher.MatchAsync(context);

            // Assert
            foreach (var entry in expectedValues)
            {
                var data = Assert.Single(context.Values, v => v.Key == entry.Key);
                Assert.Equal(entry.Value, data.Value);
            }
        }

        [Fact]
        public async Task MatchAsync_DoesNotMatchEndpointsWithIntermediateDefaultValues()
        {
            // Arrange
            var url = "/a/b";
            var dataSource = new DefaultDispatcherDataSource()
            {
                Endpoints =
                {
                    new RoutePatternEndpoint("a/b/{parameter3=3}/d",
                    new { parameter3 = 3}, Test_Delegate, "Test"),
                },
            };

            var context = CreateMatcherContext(url);
            var factory = new TreeMatcherFactory();
            var matcher = factory.CreateMatcher(dataSource, new List<EndpointSelector>());

            // Act
            await matcher.MatchAsync(context);

            // Assert
            Assert.Null(context.Endpoint);
        }

        [Theory]
        [InlineData("a/{b=3}/c/{d?}/e/{*f}", "/a")]
        [InlineData("a/{b=3}/c/{d?}/e/{*f}", "/a/b")]
        [InlineData("a/{b=3}/c/{d?}/e/{*f}", "/a/b/c")]
        [InlineData("a/{b=3}/c/{d?}/e/{*f}", "/a/b/c/d")]
        public async Task MatchAsync_DoesNotMatchEndpointsWithMultipleIntermediateDefaultOrOptionalValues(string template, string url)
        {
            // Arrange
            var dataSource = new DefaultDispatcherDataSource()
            {
                Endpoints =
                {
                    new RoutePatternEndpoint(template,
                    new { b = 3}, Test_Delegate, "Test"),
                },
            };

            var context = CreateMatcherContext(url);
            var factory = new TreeMatcherFactory();
            var matcher = factory.CreateMatcher(dataSource, new List<EndpointSelector>());

            // Act
            await matcher.MatchAsync(context);

            // Assert
            Assert.Null(context.Endpoint);
        }

        [Theory]
        [InlineData("a/{b=3}/c/{d?}/e/{*f}", "/a/b/c/d/e")]
        [InlineData("a/{b=3}/c/{d?}/e/{*f}", "/a/b/c/d/e/f")]
        public async Task MatchAsync_MatchRoutesWithMultipleIntermediateDefaultOrOptionalValues_WhenAllIntermediateValuesAreProvided(string template, string url)
        {
            // Arrange
            var dataSource = new DefaultDispatcherDataSource()
            {
                Endpoints =
                {
                    new RoutePatternEndpoint(template,
                    new { b = 3}, Test_Delegate, "Test"),
                },
            };

            var context = CreateMatcherContext(url);
            var factory = new TreeMatcherFactory();
            var matcher = factory.CreateMatcher(dataSource, new List<EndpointSelector>());

            // Act
            await matcher.MatchAsync(context);

            // Assert
            Assert.NotNull(context.Endpoint);
        }

        [Fact]
        public void MatchAsync_DoesNotMatchShorterUrl()
        {
            // Arrange
            var dataSource = new DefaultDispatcherDataSource()
            {
                Endpoints =
                {
                    new RoutePatternEndpoint("Literal1/Literal2/Literal3",
                    new object(), Test_Delegate, "Test"),
                },
            };

            var routes = new[] {
                "Literal1/Literal2/Literal3",
            };

            var context = CreateMatcherContext("/Literal1");
            var factory = new TreeMatcherFactory();
            var matcher = factory.CreateMatcher(dataSource, new List<EndpointSelector>());

            // Assert
            Assert.Null(context.Endpoint);
        }

        [Theory]
        [InlineData("///")]
        [InlineData("/a//")]
        [InlineData("/a/b//")]
        [InlineData("//b//")]
        [InlineData("///c")]
        [InlineData("///c/")]
        public async Task MatchAsync_MultipleOptionalParameters_WithEmptyIntermediateSegmentsDoesNotMatch(string url)
        {
            // Arrange
            var dataSource = new DefaultDispatcherDataSource()
            {
                Endpoints =
                {
                    new RoutePatternEndpoint("{controller?}/{action?}/{id?}",
                    new object(), Test_Delegate, "Test"),
                },
            };
            var context = CreateMatcherContext(url);
            var factory = new TreeMatcherFactory();
            var matcher = factory.CreateMatcher(dataSource, new List<EndpointSelector>());

            // Act
            await matcher.MatchAsync(context);

            // Assert
            Assert.Null(context.Endpoint);
        }

        [Theory]
        [InlineData("")]
        [InlineData("/")]
        [InlineData("/a")]
        [InlineData("/a/")]
        [InlineData("/a/b")]
        [InlineData("/a/b/")]
        [InlineData("/a/b/c")]
        [InlineData("/a/b/c/")]
        public async Task MatchAsync_MultipleOptionalParameters_WithIncrementalOptionalValues(string url)
        {
            // Arrange
            var dataSource = new DefaultDispatcherDataSource()
            {
                Endpoints =
                {
                    new RoutePatternEndpoint("{controller?}/{action?}/{id?}", new {}, Test_Delegate, "Test"),
                },
            };

            var context = CreateMatcherContext(url);
            var factory = new TreeMatcherFactory();
            var matcher = factory.CreateMatcher(dataSource, new List<EndpointSelector>());

            // Act
            await matcher.MatchAsync(context);

            // Assert
            Assert.NotNull(context.Endpoint);
        }

        [Theory]
        [InlineData("///")]
        [InlineData("////")]
        [InlineData("/a//")]
        [InlineData("/a///")]
        [InlineData("//b/")]
        [InlineData("//b//")]
        [InlineData("///c")]
        [InlineData("///c/")]
        public async Task MatchAsync_MultipleParameters_WithEmptyValuesDoesNotMatch(string url)
        {
            // Arrange
            var dataSource = new DefaultDispatcherDataSource()
            {
                Endpoints =
                {
                    new RoutePatternEndpoint("{controller?}/{action?}/{id?}",
                    new object(), Test_Delegate, "Test"),
                },
            };

            var context = CreateMatcherContext(url);
            var factory = new TreeMatcherFactory();
            var matcher = factory.CreateMatcher(dataSource, new List<EndpointSelector>());

            // Act
            await matcher.MatchAsync(context);

            // Assert
            Assert.Null(context.Endpoint);
        }

        [Theory]
        [InlineData("/a/b/c//")]
        [InlineData("/a/b/c/////")]
        public async Task MatchAsync_CatchAllParameters_WithEmptyValuesAtTheEnd(string url)
        {
            // Arrange
            var dataSource = new DefaultDispatcherDataSource()
            {
                Endpoints =
                {
                    new RoutePatternEndpoint("{controller}/{action}/{*id}",
                    new object(), Test_Delegate, "Test"),
                },
            };

            var context = CreateMatcherContext(url);
            var factory = new TreeMatcherFactory();
            var matcher = factory.CreateMatcher(dataSource, new List<EndpointSelector>());

            // Act
            await matcher.MatchAsync(context);

            // Assert
            Assert.Same(dataSource.Endpoints[0], context.Endpoint);
        }

        [Theory]
        [InlineData("/a/b//")]
        [InlineData("/a/b///c")]
        public async Task MatchAsync_CatchAllParameters_WithEmptyValues(string url)
        {
            // Arrange
            var dataSource = new DefaultDispatcherDataSource()
            {
                Endpoints =
                {
                    new RoutePatternEndpoint("{controller}/{action}/{*id}",
                    new object(), Test_Delegate, "Test"),
                },
            };

            var context = CreateMatcherContext(url);
            var factory = new TreeMatcherFactory();
            var matcher = factory.CreateMatcher(dataSource, new List<EndpointSelector>());

            // Act
            await matcher.MatchAsync(context);

            // Assert
            Assert.Null(context.Endpoint);
        }

        [Theory]
        [InlineData("{*path}", "/a", "a")]
        [InlineData("{*path}", "/a/b/c", "a/b/c")]
        [InlineData("a/{*path}", "/a/b", "b")]
        [InlineData("a/{*path}", "/a/b/c/d", "b/c/d")]
        [InlineData("a/{*path:regex(10/20/30)}", "/a/10/20/30", "10/20/30")]
        public async Task MatchAsync_MatchesWildCard_ForLargerPathSegments(
            string template,
            string requestPath,
            string expectedResult)
        {
            // Arrange
            var dataSource = new DefaultDispatcherDataSource()
            {
                Endpoints =
                {
                    new RoutePatternEndpoint(template,
                    new object(), Test_Delegate, "Test"),
                },
            };

            var context = CreateMatcherContext(requestPath);
            var factory = new TreeMatcherFactory();
            var matcher = factory.CreateMatcher(dataSource, new List<EndpointSelector>());

            // Act
            await matcher.MatchAsync(context);

            // Assert
            Assert.Same(dataSource.Endpoints[0], context.Endpoint);
            Assert.Equal(expectedResult, context.Values["path"]);
        }

        [Theory]
        [InlineData("a/{*path}", "/a")]
        [InlineData("a/{*path}", "/a/")]
        public async Task MatchAsync_MatchesCatchAll_NullValue(
            string template,
            string requestPath)
        {
            // Arrange
            var dataSource = new DefaultDispatcherDataSource()
            {
                Endpoints =
                {
                    new RoutePatternEndpoint(template,
                    new object(), Test_Delegate, "Test"),
                },
            };

            var context = CreateMatcherContext(requestPath);
            var factory = new TreeMatcherFactory();
            var matcher = factory.CreateMatcher(dataSource, new List<EndpointSelector>());

            // Act
            await matcher.MatchAsync(context);

            // Assert
            Assert.Same(dataSource.Endpoints[0], context.Endpoint);
            Assert.Null(context.Values["path"]);
        }

        [Theory]
        [InlineData("a/{*path=default}", "/a")]
        [InlineData("a/{*path=default}", "/a/")]
        public async Task MatchAsync_MatchesCatchAll_UsesDefaultValue(
            string template,
            string requestPath)
        {
            // Arrange
            var dataSource = new DefaultDispatcherDataSource()
            {
                Endpoints =
                {
                    new RoutePatternEndpoint(template,
                    new object(), Test_Delegate, "Test"),
                },
            };

            var factory = new TreeMatcherFactory();
            var matcher = factory.CreateMatcher(dataSource, new List<EndpointSelector>());

            var context = CreateMatcherContext(requestPath);

            // Act
            await matcher.MatchAsync(context);

            // Assert
            Assert.Same(dataSource.Endpoints[0], context.Endpoint);
            Assert.Equal("default", context.Values["path"]);
        }

        [Theory]
        [InlineData("template/{parameter:int}", "/template/5", true)]
        [InlineData("template/{parameter:int?}", "/template/5", true)]
        [InlineData("template/{parameter:int?}", "/template", true)]
        [InlineData("template/{parameter:int?}", "/template/qwer", false)]
        public async Task MatchAsync_WithOptionalConstraint(
            string template,
            string request,
            bool expectedResult)
        {
            // Arrange
            var dataSource = new DefaultDispatcherDataSource()
            {
                Endpoints =
                {
                    new RoutePatternEndpoint(template,
                    new object(), Test_Delegate, "Test"),
                },
            };

            var factory = new TreeMatcherFactory();
            var matcher = factory.CreateMatcher(dataSource, new List<EndpointSelector>());
            var context = CreateMatcherContext(request);

            // Act
            await matcher.MatchAsync(context);

            // Assert
            if (expectedResult)
            {
                Assert.NotNull(context.Endpoint);
            }
            else
            {
                Assert.Null(context.Endpoint);
            }
        }

        [Theory]
        [InlineData("moo/{p1}.{p2?}", "/moo/foo.bar", "foo", "bar", null)]
        [InlineData("moo/{p1?}", "/moo/foo", "foo", null, null)]
        [InlineData("moo/{p1?}", "/moo", null, null, null)]
        [InlineData("moo/{p1}.{p2?}", "/moo/foo", "foo", null, null)]
        [InlineData("moo/{p1}.{p2?}", "/moo/foo..bar", "foo.", "bar", null)]
        [InlineData("moo/{p1}.{p2?}", "/moo/foo.moo.bar", "foo.moo", "bar", null)]
        [InlineData("moo/{p1}.{p2}", "/moo/foo.bar", "foo", "bar", null)]
        [InlineData("moo/foo.{p1}.{p2?}", "/moo/foo.moo.bar", "moo", "bar", null)]
        [InlineData("moo/foo.{p1}.{p2?}", "/moo/foo.moo", "moo", null, null)]
        [InlineData("moo/.{p2?}", "/moo/.foo", null, "foo", null)]
        [InlineData("moo/{p1}.{p2?}", "/moo/....", "..", ".", null)]
        [InlineData("moo/{p1}.{p2?}", "/moo/.bar", ".bar", null, null)]
        [InlineData("moo/{p1}.{p2}.{p3?}", "/moo/foo.moo.bar", "foo", "moo", "bar")]
        [InlineData("moo/{p1}.{p2}.{p3?}", "/moo/foo.moo", "foo", "moo", null)]
        [InlineData("moo/{p1}.{p2}.{p3}.{p4?}", "/moo/foo.moo.bar", "foo", "moo", "bar")]
        [InlineData("{p1}.{p2?}/{p3}", "/foo.moo/bar", "foo", "moo", "bar")]
        [InlineData("{p1}.{p2?}/{p3}", "/foo/bar", "foo", null, "bar")]
        [InlineData("{p1}.{p2?}/{p3}", "/.foo/bar", ".foo", null, "bar")]
        public async Task MatchAsync_WithOptionalCompositeParameter_Valid(
            string template,
            string request,
            string p1,
            string p2,
            string p3)
        {
            // Arrange
            var dataSource = new DefaultDispatcherDataSource()
            {
                Endpoints =
                {
                    new RoutePatternEndpoint(template,
                    new object(), Test_Delegate, "Test"),
                },
            };

            var factory = new TreeMatcherFactory();
            var matcher = factory.CreateMatcher(dataSource, new List<EndpointSelector>());
            var context = CreateMatcherContext(request);

            // Act
            await matcher.MatchAsync(context);

            // Assert
            Assert.NotNull(context.Endpoint);
            if (p1 != null)
            {
                Assert.Equal(p1, context.Values["p1"]);
            }
            if (p2 != null)
            {
                Assert.Equal(p2, context.Values["p2"]);
            }
            if (p3 != null)
            {
                Assert.Equal(p3, context.Values["p3"]);
            }
        }

        [Theory]
        [InlineData("moo/{p1}.{p2?}", "/moo/foo.")]
        [InlineData("moo/{p1}.{p2?}", "/moo/.")]
        [InlineData("moo/{p1}.{p2}", "/foo.")]
        [InlineData("moo/{p1}.{p2}", "/foo")]
        [InlineData("moo/{p1}.{p2}.{p3?}", "/moo/foo.moo.")]
        [InlineData("moo/foo.{p2}.{p3?}", "/moo/bar.foo.moo")]
        [InlineData("moo/foo.{p2}.{p3?}", "/moo/kungfoo.moo.bar")]
        [InlineData("moo/foo.{p2}.{p3?}", "/moo/kungfoo.moo")]
        [InlineData("moo/{p1}.{p2}.{p3?}", "/moo/foo")]
        [InlineData("{p1}.{p2?}/{p3}", "/foo./bar")]
        [InlineData("moo/.{p2?}", "/moo/.")]
        [InlineData("{p1}.{p2}/{p3}", "/.foo/bar")]
        public async Task MatchAsync_WithOptionalCompositeParameter_Invalid(
            string template,
            string request)
        {
            // Arrange
            var dataSource = new DefaultDispatcherDataSource()
            {
                Endpoints =
                {
                    new RoutePatternEndpoint(template,
                    new object(), Test_Delegate, "Test"),
                },
            };

            var factory = new TreeMatcherFactory();
            var matcher = factory.CreateMatcher(dataSource, new List<EndpointSelector>());
            var context = CreateMatcherContext(request);

            // Act
            await matcher.MatchAsync(context);

            // Assert
            Assert.Null(context.Endpoint);
        }

        [Fact]
        public async Task ChangeTokenTriggers_CreateCache()
        {
            // Arrange 1
            var factory = new TreeMatcherFactory();
            var dataSource = new TestDataSource(new List<Endpoint>() { new RoutePatternEndpoint("test/{p1}", new object(), Test_Delegate, "Test"), });
            var matcher = factory.CreateMatcher(dataSource, new List<EndpointSelector>());
            var context = CreateMatcherContext("/test/parameter1");

            // Act 1
            await matcher.MatchAsync(context);

            // Assert 1
            Assert.Equal("Test", context.Endpoint.DisplayName);
            context.Values.TryGetValue("p1", out var value);
            Assert.Equal("parameter1", value);

            // Arrange 2
            dataSource.UpdateEndpoints(new List<Endpoint>() { new RoutePatternEndpoint("test2/{p2}", new object(), Test_Delegate, "Test2"), });
            context = CreateMatcherContext("/test2/parameter2");

            // Act 2
            await matcher.MatchAsync(context);

            // Assert 2
            Assert.Equal("Test2", context.Endpoint.DisplayName);
            Assert.False(context.Values.TryGetValue("p1", out value));
            context.Values.TryGetValue("p2", out value);
            Assert.Equal("parameter2", value);
        }

        private static MatcherContext CreateMatcherContext(string requestPath)
        {
            var request = new Mock<HttpRequest>(MockBehavior.Strict);
            request.SetupGet(r => r.Path).Returns(new PathString(requestPath));

            var context = new Mock<HttpContext>(MockBehavior.Strict);
            context.Setup(m => m.RequestServices.GetService(typeof(ILoggerFactory)))
                .Returns(NullLoggerFactory.Instance);
            context.Setup(m => m.RequestServices.GetService(typeof(IConstraintFactory)))
                .Returns(CreateConstraintFactory);
            context.SetupGet(c => c.Request).Returns(request.Object);

            return new MatcherContext(context.Object);
        }

        private static DefaultConstraintFactory CreateConstraintFactory()
        {
            var options = new DispatcherOptions();
            var optionsMock = new Mock<IOptions<DispatcherOptions>>();
            optionsMock.SetupGet(o => o.Value).Returns(options);

            return new DefaultConstraintFactory(optionsMock.Object);
        }

        private static Task Test_Delegate(HttpContext httpContext)
        {
            return Task.CompletedTask;
        }

        public class TestDataSource : DispatcherDataSource, IAddressCollectionProvider, IEndpointCollectionProvider
        {
            private IList<Address> _addresses;
            private IList<Endpoint> _endpoints;
            private TestChangeToken _changeToken;
            private readonly object _lock = new object();

            public TestDataSource(IList<Endpoint> endpoints)
            {
                _addresses = new List<Address>();
                _endpoints = endpoints;
                _changeToken = new TestChangeToken();
            }

            public void UpdateEndpoints(IList<Endpoint> endpoints)
            {
                lock (_lock)
                {
                    _endpoints = endpoints;
                    // Trigger change token here
                    var previousToken = _changeToken;
                    _changeToken = new TestChangeToken();
                    previousToken.OnChange();
                }
            }

            public override IChangeToken ChangeToken
            {
                get
                {
                    lock (_lock)
                    {
                        return _changeToken;
                    }
                }
            }

            IReadOnlyList<Address> IAddressCollectionProvider.Addresses => GetAddresses();

            IReadOnlyList<Endpoint> IEndpointCollectionProvider.Endpoints => GetEndpoints();

            protected override IReadOnlyList<Address> GetAddresses() => _addresses.ToList();

            protected override IReadOnlyList<Endpoint> GetEndpoints() => _endpoints.ToList();

            public IList<Endpoint> Endpoints => _endpoints;

            private class TestChangeToken : IChangeToken
            {
                private CancellationTokenSource _cts = new CancellationTokenSource();

                public bool HasChanged => true;

                public bool ActiveChangeCallbacks => true;

                public IDisposable RegisterChangeCallback(Action<object> callback, object state) => _cts.Token.Register(callback, state);

                public void OnChange() => _cts.Cancel();
            }
        }
    }
}
