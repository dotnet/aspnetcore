// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Moq;

namespace Microsoft.AspNetCore.Routing.Matching;

// Many of these are integration tests that exercise the system end to end,
// so we're reusing the services here.
public class DfaMatcherTest
{
    private RouteEndpoint CreateEndpoint(string template, int order, object defaults = null, object requiredValues = null, object policies = null)
    {
        return EndpointFactory.CreateRouteEndpoint(template, defaults, policies, requiredValues, order, displayName: template);
    }

    private DataSourceDependentMatcher CreateDfaMatcher(
        EndpointDataSource dataSource,
        MatcherPolicy[] policies = null,
        EndpointSelector endpointSelector = null,
        ILoggerFactory loggerFactory = null)
    {
        var serviceCollection = new ServiceCollection()
            .AddLogging()
            .AddOptions()
            .AddRouting(options =>
            {
                options.ConstraintMap["slugify"] = typeof(SlugifyParameterTransformer);
            });

        if (policies != null)
        {
            for (var i = 0; i < policies.Length; i++)
            {
                serviceCollection.AddSingleton<MatcherPolicy>(policies[i]);
            }
        }

        if (endpointSelector != null)
        {
            serviceCollection.AddSingleton<EndpointSelector>(endpointSelector);
        }

        if (loggerFactory != null)
        {
            serviceCollection.AddSingleton<ILoggerFactory>(loggerFactory);
        }

        var services = serviceCollection.BuildServiceProvider();

        var factory = services.GetRequiredService<MatcherFactory>();
        return Assert.IsType<DataSourceDependentMatcher>(factory.CreateMatcher(dataSource));
    }

    [Fact]
    public async Task MatchAsync_ValidRouteConstraint_EndpointMatched()
    {
        // Arrange
        var endpointDataSource = new DefaultEndpointDataSource(new List<Endpoint>
            {
                CreateEndpoint("/{p:int}", 0)
            });

        var matcher = CreateDfaMatcher(endpointDataSource);

        var httpContext = CreateContext();
        httpContext.Request.Path = "/1";

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        Assert.NotNull(httpContext.GetEndpoint());
    }

    [Fact]
    public async Task MatchAsync_InvalidRouteConstraint_NoEndpointMatched()
    {
        // Arrange
        var endpointDataSource = new DefaultEndpointDataSource(new List<Endpoint>
            {
                CreateEndpoint("/{p:int}", 0)
            });

        var matcher = CreateDfaMatcher(endpointDataSource);

        var httpContext = CreateContext();
        httpContext.Request.Path = "/One";

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        Assert.Null(httpContext.GetEndpoint());
    }

    [Theory]
    [InlineData("{a}.{b}.{c}/{d}", "/.git/index")]
    [InlineData("{a}-{b}-{c}/c.aspx", "/-hello/c.aspx")]
    [InlineData("-{b}-{c}", "/-hello")]
    [InlineData("--{b}-{c}", "/-hello")]
    [InlineData("-{b}--{c}", "/-hello")]
    [InlineData("{b}-{c}", "/-hello")]
    [InlineData("-{b}--{c}", "/--hello")]
    [InlineData(".{b}-{c}", "/-hello")]
    public async Task MatchAsync_ComplexSegmentEndpointAndPathStartingWithLiteral_NoEndpointMatched(string endpoint, string path)
    {
        // Arrange
        var endpointDataSource = new DefaultEndpointDataSource(new List<Endpoint>
            {
                CreateEndpoint(endpoint, 0)
            });

        var matcher = CreateDfaMatcher(endpointDataSource);

        var httpContext = CreateContext();
        httpContext.Request.Path = path;

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        Assert.Null(httpContext.GetEndpoint());
    }

    [Fact]
    public async Task MatchAsync_RequireValuesAndDefaultValues_EndpointMatched()
    {
        // Arrange
        var endpoint = CreateEndpoint(
            "{controller=Home}/{action=Index}/{id?}",
            0,
            requiredValues: new { controller = "Home", action = "Index", area = (string)null, page = (string)null });

        var dataSource = new DefaultEndpointDataSource(new List<Endpoint>
            {
                endpoint
            });

        var matcher = CreateDfaMatcher(dataSource);

        var httpContext = CreateContext();
        httpContext.Request.Path = "/";

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        Assert.Same(endpoint, httpContext.GetEndpoint());

        Assert.Collection(
            httpContext.Request.RouteValues.OrderBy(kvp => kvp.Key),
            (kvp) =>
            {
                Assert.Equal("action", kvp.Key);
                Assert.Equal("Index", kvp.Value);
            },
            (kvp) =>
            {
                Assert.Equal("controller", kvp.Key);
                Assert.Equal("Home", kvp.Value);
            });
    }

    [Fact]
    public async Task MatchAsync_RequireValuesAndDifferentPath_NoEndpointMatched()
    {
        // Arrange
        var endpoint = CreateEndpoint(
            "{controller}/{action}",
            0,
            requiredValues: new { controller = "Home", action = "Index", area = (string)null, page = (string)null });

        var dataSource = new DefaultEndpointDataSource(new List<Endpoint>
            {
                endpoint
            });

        var matcher = CreateDfaMatcher(dataSource);

        var httpContext = CreateContext();
        httpContext.Request.Path = "/Login/Index";

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        Assert.Null(httpContext.GetEndpoint());
    }

    [Fact]
    public async Task MatchAsync_RequireValuesAndOptionalParameter_EndpointMatched()
    {
        // Arrange
        var endpoint = CreateEndpoint(
            "{controller}/{action}/{id?}",
            0,
            requiredValues: new { controller = "Home", action = "Index", area = (string)null, page = (string)null });

        var dataSource = new DefaultEndpointDataSource(new List<Endpoint>
            {
                endpoint
            });

        var matcher = CreateDfaMatcher(dataSource);

        var httpContext = CreateContext();
        httpContext.Request.Path = "/Home/Index/123";

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        Assert.Same(endpoint, httpContext.GetEndpoint());

        Assert.Collection(
            httpContext.Request.RouteValues.OrderBy(kvp => kvp.Key),
            (kvp) =>
            {
                Assert.Equal("action", kvp.Key);
                Assert.Equal("Index", kvp.Value);
            },
            (kvp) =>
            {
                Assert.Equal("controller", kvp.Key);
                Assert.Equal("Home", kvp.Value);
            },
            (kvp) =>
            {
                Assert.Equal("id", kvp.Key);
                Assert.Equal("123", kvp.Value);
            });
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/TestController")]
    [InlineData("/TestController/TestAction")]
    [InlineData("/TestController/TestAction/17")]
    [InlineData("/TestController/TestAction/17/catchAll")]
    public async Task MatchAsync_ShortenedPattern_EndpointMatched(string path)
    {
        // Arrange
        var endpoint = CreateEndpoint(
            "{controller=TestController}/{action=TestAction}/{id=17}/{**catchAll}",
            0,
            requiredValues: new { controller = "TestController", action = "TestAction", area = (string)null, page = (string)null });

        var dataSource = new DefaultEndpointDataSource(new List<Endpoint>
            {
                endpoint
            });

        var matcher = CreateDfaMatcher(dataSource);

        var httpContext = CreateContext();
        httpContext.Request.Path = path;

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        Assert.Same(endpoint, httpContext.GetEndpoint());

        Assert.Equal("TestAction", httpContext.Request.RouteValues["action"]);
        Assert.Equal("TestController", httpContext.Request.RouteValues["controller"]);
        Assert.Equal("17", httpContext.Request.RouteValues["id"]);
    }

    [Fact]
    public async Task MatchAsync_MultipleEndpointsWithDifferentRequiredValues_EndpointMatched()
    {
        // Arrange
        var endpoint1 = CreateEndpoint(
            "{controller}/{action}/{id?}",
            0,
            requiredValues: new { controller = "Home", action = "Index", area = (string)null, page = (string)null });
        var endpoint2 = CreateEndpoint(
            "{controller}/{action}/{id?}",
            0,
            requiredValues: new { controller = "Login", action = "Index", area = (string)null, page = (string)null });

        var dataSource = new DefaultEndpointDataSource(new List<Endpoint>
            {
                endpoint1,
                endpoint2
            });

        var matcher = CreateDfaMatcher(dataSource);

        var httpContext = CreateContext();
        httpContext.Request.Path = "/Home/Index/123";

        // Act 1
        await matcher.MatchAsync(httpContext);

        // Assert 1
        Assert.Same(endpoint1, httpContext.GetEndpoint());

        httpContext.Request.Path = "/Login/Index/123";

        // Act 2
        await matcher.MatchAsync(httpContext);

        // Assert 2
        Assert.Same(endpoint2, httpContext.GetEndpoint());
    }

    [Fact]
    public async Task MatchAsync_ParameterTransformer_EndpointMatched()
    {
        // Arrange
        var endpoint = CreateEndpoint(
            "ConventionalTransformerRoute/{controller:slugify}/{action=Index}/{param:slugify?}",
            0,
            requiredValues: new { controller = "ConventionalTransformer", action = "Index", area = (string)null, page = (string)null });

        var dataSource = new DefaultEndpointDataSource(new List<Endpoint>
            {
                endpoint
            });

        var matcher = CreateDfaMatcher(dataSource);

        var httpContext = CreateContext();
        httpContext.Request.Path = "/ConventionalTransformerRoute/conventional-transformer/Index";

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        Assert.Same(endpoint, httpContext.GetEndpoint());

        Assert.Collection(
            httpContext.Request.RouteValues.OrderBy(kvp => kvp.Key),
            (kvp) =>
            {
                Assert.Equal("action", kvp.Key);
                Assert.Equal("Index", kvp.Value);
            },
            (kvp) =>
            {
                Assert.Equal("controller", kvp.Key);
                Assert.Equal("ConventionalTransformer", kvp.Value);
            });
    }

    [Fact]
    public void MatchAsync_ConstrainedParameter_EndpointMatched()
    {
        // Arrange
        var endpoint1 = CreateEndpoint("a/c", 0);
        var endpoint2 = CreateEndpoint("{param:length(2)}/b/c", 0);

        var dataSource = new DefaultEndpointDataSource(new List<Endpoint>
            {
                endpoint1,
                endpoint2
            });

        var matcher = (DfaMatcher)CreateDfaMatcher(dataSource).CurrentMatcher;
        var buffer = new PathSegment[3];
        var (context, path, count) = CreateMatchingContext("/aa/b/c", buffer);

        // Act
        var set = matcher.FindCandidateSet(context, path, buffer.AsSpan().Slice(0, count));

        // Assert
        // We expect endpoint2 to match here since we trimmed the branch for the parameter based on `a` not meeting
        // the constraints.
        var candidate = Assert.Single(set.candidates);
        Assert.Same(endpoint2, candidate.Endpoint);
    }

    [Fact]
    public void MatchAsync_ConstrainedParameter_EndpointNotMatched()
    {
        // Arrange
        var endpoint1 = CreateEndpoint("a/c", 0);
        var endpoint2 = CreateEndpoint("{param:length(2)}/b/c", 0);

        var dataSource = new DefaultEndpointDataSource(new List<Endpoint>
            {
                endpoint1,
                endpoint2
            });

        var matcher = (DfaMatcher)CreateDfaMatcher(dataSource).CurrentMatcher;
        var buffer = new PathSegment[3];
        var (context, path, count) = CreateMatchingContext("/a/b/c", buffer);

        // Act
        var set = matcher.FindCandidateSet(context, path, buffer.AsSpan().Slice(0, count));

        // Assert
        // We expect no candidates here, since the path on the tree (a -> b -> c = ({param:length(2)}/b/c)) for not meeting the length(2) constraint.
        Assert.Empty(set.candidates);
    }

    [Fact]
    public void MatchAsync_ConstrainedParameter_EndpointMatched_WhenExplicitRouteExists()
    {
        // Arrange
        // Note that there is now an explicit branch created by the first endpoint, however endpoint 2 will
        // be filtered out of the candidates list because it didn't meet the constraint.
        var endpoint1 = CreateEndpoint("a/b/c", 0);
        var endpoint2 = CreateEndpoint("{param:length(2)}/b/c", 0);

        var dataSource = new DefaultEndpointDataSource(new List<Endpoint>
            {
                endpoint1,
                endpoint2
            });

        var matcher = (DfaMatcher)CreateDfaMatcher(dataSource).CurrentMatcher;
        var buffer = new PathSegment[3];
        var (context, path, count) = CreateMatchingContext("/a/b/c", buffer);

        // Act
        var set = matcher.FindCandidateSet(context, path, buffer.AsSpan().Slice(0, count));

        // Assert
        // We expect only one candidate, since the path on the tree (a -> b -> c = ({param:length(2)}/b/c)) does not meet the length(2) constraint.
        var candidate = Assert.Single(set.candidates);
        Assert.Same(endpoint1, candidate.Endpoint);
    }

    [Fact]
    public void MatchAsync_ConstrainedParameter_EndpointMatchedWithTwoCandidates_WhenLiteralMeetsConstraint()
    {
        // Arrange
        // Note that the literal now meets the constraint, so there will be an explicit branch and two candidates
        var endpoint1 = CreateEndpoint("aa/b/c", 0);
        var endpoint2 = CreateEndpoint("{param:length(2)}/b/c", 0);
        var endpoints = new List<Endpoint>
            {
                endpoint2,
                endpoint1,
            };
        var dataSource = new DefaultEndpointDataSource(endpoints);

        var matcher = (DfaMatcher)CreateDfaMatcher(dataSource).CurrentMatcher;
        var buffer = new PathSegment[3];
        var (context, path, count) = CreateMatchingContext("/aa/b/c", buffer);

        // Act
        var set = matcher.FindCandidateSet(context, path, buffer.AsSpan().Slice(0, count));

        // Assert
        // We expect 2 candidates, since the path on the tree (aa -> b -> c = ({param:length(2)}/b/c)) meets the length(2) constraint.
        Assert.Equal(endpoints.ToArray(), set.candidates.Select(e => e.Endpoint).OrderBy(e => ((RouteEndpoint)e).RoutePattern.RawText).ToArray());
    }

    [Fact]
    public void MatchAsync_ConstrainedParameter_MiddleSegment_EndpointMatched()
    {
        // Arrange
        var endpoint1 = CreateEndpoint("a/b/c", 0);
        var endpoint2 = CreateEndpoint("a/{param:length(2)}/c", 0);

        var dataSource = new DefaultEndpointDataSource(new List<Endpoint>
            {
                endpoint1,
                endpoint2
            });

        var matcher = (DfaMatcher)CreateDfaMatcher(dataSource).CurrentMatcher;
        var buffer = new PathSegment[3];
        var (context, path, count) = CreateMatchingContext("/a/bb/c", buffer);

        // Act
        var set = matcher.FindCandidateSet(context, path, buffer.AsSpan().Slice(0, count));

        // Assert
        // We expect endpoint2 to match here since we trimmed the branch (a -> b -> c = (a/{param:length(2)}/c)) for the parameter based on `b` not meeting the length(2) constraint.
        var candidate = Assert.Single(set.candidates);
        Assert.Same(endpoint2, candidate.Endpoint);
    }

    [Fact]
    public void MatchAsync_ConstrainedParameter_MiddleSegment_EndpointNotMatched()
    {
        // Arrange
        var endpoint1 = CreateEndpoint("a/b/d", 0);
        var endpoint2 = CreateEndpoint("a/{param:length(2)}/c", 0);

        var dataSource = new DefaultEndpointDataSource(new List<Endpoint>
            {
                endpoint1,
                endpoint2
            });

        var matcher = (DfaMatcher)CreateDfaMatcher(dataSource).CurrentMatcher;
        var buffer = new PathSegment[3];
        var (context, path, count) = CreateMatchingContext("/a/b/c", buffer);

        // Act
        var set = matcher.FindCandidateSet(context, path, buffer.AsSpan().Slice(0, count));

        // Assert
        // We expect no candidates here since we trimmed the branch (a -> b -> c = (a/{param:length(2)}/c)) for the parameter based on `b` not meeting the length(2) constraint.
        Assert.Empty(set.candidates);
    }

    [Fact]
    public void MatchAsync_ConstrainedParameter_MiddleSegment_EndpointMatched_WhenExplicitRouteExists()
    {
        // Arrange
        // Note that there is now an explicit branch created by the first endpoint, however endpoint 2 will
        // be filtered out of the candidates list because it didn't meet the constraint.
        var endpoint1 = CreateEndpoint("a/b/c", 0);
        var endpoint2 = CreateEndpoint("a/{param:length(2)}/c", 0);

        var dataSource = new DefaultEndpointDataSource(new List<Endpoint>
            {
                endpoint1,
                endpoint2
            });

        var matcher = (DfaMatcher)CreateDfaMatcher(dataSource).CurrentMatcher;
        var buffer = new PathSegment[3];
        var (context, path, count) = CreateMatchingContext("/a/b/c", buffer);

        // Act
        var set = matcher.FindCandidateSet(context, path, buffer.AsSpan().Slice(0, count));

        // Assert
        // We expect only one candidate, since the path on the tree (a -> b -> c = (a/{param:length(2)}/c)) does not meet the length(2) constraint.
        var candidate = Assert.Single(set.candidates);
        Assert.Same(endpoint1, candidate.Endpoint);
    }

    [Fact]
    public void MatchAsync_ConstrainedParameter_MiddleSegment_EndpointMatchedWithTwoCandidates_WhenLiteralMeetsConstraint()
    {
        // Arrange
        // Note that the literal now meets the constraint, so there will be an explicit branch and two candidates
        var endpoint1 = CreateEndpoint("a/bb/c", 0);
        var endpoint2 = CreateEndpoint("a/{param:length(2)}/c", 0);
        var endpoints = new List<Endpoint>
            {
                endpoint2,
                endpoint1,
            };
        var dataSource = new DefaultEndpointDataSource(endpoints);

        var matcher = (DfaMatcher)CreateDfaMatcher(dataSource).CurrentMatcher;
        var buffer = new PathSegment[3];
        var (context, path, count) = CreateMatchingContext("/a/bb/c", buffer);

        // Act
        var set = matcher.FindCandidateSet(context, path, buffer.AsSpan().Slice(0, count));

        // Assert
        // We expect 2 candidates, since the path on the tree (aa -> b -> c = ({param:length(2)}/b/c)) meets the length(2) constraint.
        Assert.Equal(endpoints.ToArray(), set.candidates.Select(e => e.Endpoint).OrderBy(e => ((RouteEndpoint)e).RoutePattern.RawText).ToArray());
    }

    private (HttpContext context, string path, int count) CreateMatchingContext(string requestPath, PathSegment[] buffer)
    {
        var context = CreateContext();
        context.Request.Path = requestPath;

        // First tokenize the path into series of segments.
        var count = FastPathTokenizer.Tokenize(requestPath, buffer);
        return (context, requestPath, count);
    }

    [Fact]
    public async Task MatchAsync_DifferentDefaultCase_RouteValueUsesDefaultCase()
    {
        // Arrange
        var endpoint = CreateEndpoint(
            "{controller}/{action=TESTACTION}/{id?}",
            0,
            requiredValues: new { controller = "TestController", action = "TestAction" });

        var dataSource = new DefaultEndpointDataSource(new List<Endpoint>
            {
                endpoint
            });

        var matcher = CreateDfaMatcher(dataSource);

        var httpContext = CreateContext();
        httpContext.Request.Path = "/TestController";

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        Assert.Same(endpoint, httpContext.GetEndpoint());

        Assert.Collection(
            httpContext.Request.RouteValues.OrderBy(kvp => kvp.Key),
            (kvp) =>
            {
                Assert.Equal("action", kvp.Key);
                Assert.Equal("TESTACTION", kvp.Value);
            },
            (kvp) =>
            {
                Assert.Equal("controller", kvp.Key);
                Assert.Equal("TestController", kvp.Value);
            });
    }

    [Fact]
    public async Task MatchAsync_DuplicateTemplatesAndDifferentOrder_LowerOrderEndpointMatched()
    {
        // Arrange
        var higherOrderEndpoint = CreateEndpoint("/Teams", 1);
        var lowerOrderEndpoint = CreateEndpoint("/Teams", 0);

        var endpointDataSource = new DefaultEndpointDataSource(new List<Endpoint>
            {
                higherOrderEndpoint,
                lowerOrderEndpoint
            });

        var matcher = CreateDfaMatcher(endpointDataSource);

        var httpContext = CreateContext();
        httpContext.Request.Path = "/Teams";

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        Assert.Equal(lowerOrderEndpoint, httpContext.GetEndpoint());
    }

    [Fact]
    public async Task MatchAsync_MultipleMatches_EndpointSelectorCalled()
    {
        // Arrange
        var endpoint1 = CreateEndpoint("/Teams", 0);
        var endpoint2 = CreateEndpoint("/Teams", 1);

        var endpointSelector = new Mock<EndpointSelector>();
        endpointSelector
            .Setup(s => s.SelectAsync(It.IsAny<HttpContext>(), It.IsAny<CandidateSet>()))
            .Callback<HttpContext, CandidateSet>((c, cs) =>
            {
                Assert.Equal(2, cs.Count);

                Assert.Same(endpoint1, cs[0].Endpoint);
                Assert.True(cs.IsValidCandidate(0));
                Assert.Equal(0, cs[0].Score);
                Assert.Null(cs[0].Values);

                Assert.Same(endpoint2, cs[1].Endpoint);
                Assert.True(cs.IsValidCandidate(1));
                Assert.Equal(1, cs[1].Score);
                Assert.Null(cs[1].Values);

                c.SetEndpoint(endpoint2);
            })
            .Returns(Task.CompletedTask);

        var endpointDataSource = new DefaultEndpointDataSource(new List<Endpoint>
            {
                endpoint1,
                endpoint2
            });

        var matcher = CreateDfaMatcher(endpointDataSource, endpointSelector: endpointSelector.Object);

        var httpContext = CreateContext();
        httpContext.Request.Path = "/Teams";

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        Assert.Equal(endpoint2, httpContext.GetEndpoint());
    }

    [Fact]
    public async Task MatchAsync_MultipleMatches_EndpointSelectorCalled_AllocatesDictionaryForRouteParameter()
    {
        // Arrange
        var endpoint1 = CreateEndpoint("/Teams/{x?}", 0);
        var endpoint2 = CreateEndpoint("/Teams/{x?}", 1);

        var endpointSelector = new Mock<EndpointSelector>();
        endpointSelector
            .Setup(s => s.SelectAsync(It.IsAny<HttpContext>(), It.IsAny<CandidateSet>()))
            .Callback<HttpContext, CandidateSet>((c, cs) =>
            {
                Assert.Equal(2, cs.Count);

                Assert.Same(endpoint1, cs[0].Endpoint);
                Assert.True(cs.IsValidCandidate(0));
                Assert.Equal(0, cs[0].Score);
                Assert.Empty(cs[0].Values);

                Assert.Same(endpoint2, cs[1].Endpoint);
                Assert.True(cs.IsValidCandidate(1));
                Assert.Equal(1, cs[1].Score);
                Assert.Empty(cs[1].Values);

                c.SetEndpoint(endpoint2);
            })
            .Returns(Task.CompletedTask);

        var endpointDataSource = new DefaultEndpointDataSource(new List<Endpoint>
            {
                endpoint1,
                endpoint2
            });

        var matcher = CreateDfaMatcher(endpointDataSource, endpointSelector: endpointSelector.Object);

        var httpContext = CreateContext();
        httpContext.Request.Path = "/Teams";

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        Assert.Equal(endpoint2, httpContext.GetEndpoint());
    }

    [Fact]
    public async Task MatchAsync_MultipleMatches_EndpointSelectorCalled_AllocatesDictionaryForRouteConstraint()
    {
        // Arrange
        var constraint = new OptionalRouteConstraint(new IntRouteConstraint());
        var endpoint1 = CreateEndpoint("/Teams", 0, policies: new { x = constraint, });
        var endpoint2 = CreateEndpoint("/Teams", 1, policies: new { x = constraint, });

        var endpointSelector = new Mock<EndpointSelector>();
        endpointSelector
            .Setup(s => s.SelectAsync(It.IsAny<HttpContext>(), It.IsAny<CandidateSet>()))
            .Callback<HttpContext, CandidateSet>((c, cs) =>
            {
                Assert.Equal(2, cs.Count);

                Assert.Same(endpoint1, cs[0].Endpoint);
                Assert.True(cs.IsValidCandidate(0));
                Assert.Equal(0, cs[0].Score);
                Assert.Empty(cs[0].Values);

                Assert.Same(endpoint2, cs[1].Endpoint);
                Assert.True(cs.IsValidCandidate(1));
                Assert.Equal(1, cs[1].Score);
                Assert.Empty(cs[1].Values);

                c.SetEndpoint(endpoint2);
            })
            .Returns(Task.CompletedTask);

        var endpointDataSource = new DefaultEndpointDataSource(new List<Endpoint>
            {
                endpoint1,
                endpoint2
            });

        var matcher = CreateDfaMatcher(endpointDataSource, endpointSelector: endpointSelector.Object);

        var httpContext = CreateContext();
        httpContext.Request.Path = "/Teams";

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        Assert.Equal(endpoint2, httpContext.GetEndpoint());
    }

    [Fact]
    public async Task MatchAsync_NoCandidates_Logging()
    {
        // Arrange
        var endpointDataSource = new DefaultEndpointDataSource(new List<Endpoint>
            {
                CreateEndpoint("/{p:int}", 0)
            });

        var sink = new TestSink();
        var matcher = CreateDfaMatcher(endpointDataSource, loggerFactory: new TestLoggerFactory(sink, enabled: true));

        var httpContext = CreateContext();
        httpContext.Request.Path = "/";

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        Assert.Null(httpContext.GetEndpoint());

        Assert.Collection(
            sink.Writes,
            (log) =>
            {
                Assert.Equal(1000, log.EventId);
                Assert.Equal("No candidates found for the request path '/'", log.Message);
            });
    }

    [Fact]
    public async Task MatchAsync_ConstraintRejectsEndpoint_Logging()
    {
        // Arrange
        var endpointDataSource = new DefaultEndpointDataSource(new List<Endpoint>
            {
                CreateEndpoint("/{p:int}", 0)
            });

        var sink = new TestSink();
        var matcher = CreateDfaMatcher(endpointDataSource, loggerFactory: new TestLoggerFactory(sink, enabled: true));

        var httpContext = CreateContext();
        httpContext.Request.Path = "/One";

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        Assert.Null(httpContext.GetEndpoint());

        Assert.Collection(
            sink.Writes,
            (log) =>
            {
                Assert.Equal(1001, log.EventId);
                Assert.Equal("1 candidate(s) found for the request path '/One'", log.Message);
            },
            (log) =>
            {
                Assert.Equal(1003, log.EventId);
                Assert.Equal("Endpoint '/{p:int}' with route pattern '/{p:int}' was rejected by constraint 'p':'Microsoft.AspNetCore.Routing.Constraints.IntRouteConstraint' with value 'One' for the request path '/One'", log.Message);
            },
            (log) =>
            {
                Assert.Equal(1004, log.EventId);
                Assert.Equal("Endpoint '/{p:int}' with route pattern '/{p:int}' is not valid for the request path '/One'", log.Message);
            });
    }

    [Fact]
    public async Task MatchAsync_ComplexSegmentRejectsEndpoint_Logging()
    {
        // Arrange
        var endpointDataSource = new DefaultEndpointDataSource(new List<Endpoint>
            {
                CreateEndpoint("/x-{id}-y", 0)
            });

        var sink = new TestSink();
        var matcher = CreateDfaMatcher(endpointDataSource, loggerFactory: new TestLoggerFactory(sink, enabled: true));

        var httpContext = CreateContext();
        httpContext.Request.Path = "/One";

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        Assert.Null(httpContext.GetEndpoint());

        Assert.Collection(
            sink.Writes,
            (log) =>
            {
                Assert.Equal(1001, log.EventId);
                Assert.Equal("1 candidate(s) found for the request path '/One'", log.Message);
            },
            (log) =>
            {
                Assert.Equal(1002, log.EventId);
                Assert.Equal("Endpoint '/x-{id}-y' with route pattern '/x-{id}-y' was rejected by complex segment 'x-{id}-y' for the request path '/One'", log.Message);
            },
            (log) =>
            {
                Assert.Equal(1004, log.EventId);
                Assert.Equal("Endpoint '/x-{id}-y' with route pattern '/x-{id}-y' is not valid for the request path '/One'", log.Message);
            });
    }

    [Fact]
    public async Task MatchAsync_MultipleCandidates_Logging()
    {
        // Arrange
        var endpointDataSource = new DefaultEndpointDataSource(new List<Endpoint>
            {
                CreateEndpoint("/{one}", 0),
                CreateEndpoint("/{p:int}", 1),
                CreateEndpoint("/x-{id}-y", 2),
            });

        var sink = new TestSink();
        var matcher = CreateDfaMatcher(endpointDataSource, loggerFactory: new TestLoggerFactory(sink, enabled: true));

        var httpContext = CreateContext();
        httpContext.Request.Path = "/One";

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        Assert.Same(endpointDataSource.Endpoints[0], httpContext.GetEndpoint());

        Assert.Collection(
            sink.Writes,
            (log) =>
            {
                Assert.Equal(1001, log.EventId);
                Assert.Equal("3 candidate(s) found for the request path '/One'", log.Message);
            },
            (log) =>
            {
                Assert.Equal(1005, log.EventId);
                Assert.Equal("Endpoint '/{one}' with route pattern '/{one}' is valid for the request path '/One'", log.Message);
            },
            (log) =>
            {
                Assert.Equal(1003, log.EventId);
                Assert.Equal("Endpoint '/{p:int}' with route pattern '/{p:int}' was rejected by constraint 'p':'Microsoft.AspNetCore.Routing.Constraints.IntRouteConstraint' with value 'One' for the request path '/One'", log.Message);
            },
            (log) =>
            {
                Assert.Equal(1004, log.EventId);
                Assert.Equal("Endpoint '/{p:int}' with route pattern '/{p:int}' is not valid for the request path '/One'", log.Message);
            },
            (log) =>
            {
                Assert.Equal(1002, log.EventId);
                Assert.Equal("Endpoint '/x-{id}-y' with route pattern '/x-{id}-y' was rejected by complex segment 'x-{id}-y' for the request path '/One'", log.Message);
            },
            (log) =>
            {
                Assert.Equal(1004, log.EventId);
                Assert.Equal("Endpoint '/x-{id}-y' with route pattern '/x-{id}-y' is not valid for the request path '/One'", log.Message);
            });
    }

    [Fact]
    public async Task MatchAsync_RunsApplicableEndpointSelectorPolicies()
    {
        // Arrange
        var dataSource = new DefaultEndpointDataSource(new List<Endpoint>
            {
                CreateEndpoint("/test/{id:alpha}", 0),
                CreateEndpoint("/test/{id:int}", 0),
                CreateEndpoint("/test/{id}", 0),
            });

        var policy = new Mock<MatcherPolicy>();
        policy
            .As<IEndpointSelectorPolicy>()
            .Setup(p => p.AppliesToEndpoints(It.IsAny<IReadOnlyList<Endpoint>>())).Returns(true);
        policy
            .As<IEndpointSelectorPolicy>()
            .Setup(p => p.ApplyAsync(It.IsAny<HttpContext>(), It.IsAny<CandidateSet>()))
            .Returns<HttpContext, CandidateSet>((c, cs) =>
            {
                cs.SetValidity(1, false);
                return Task.CompletedTask;
            });

        var matcher = CreateDfaMatcher(dataSource, policies: new[] { policy.Object, });

        var httpContext = CreateContext();
        httpContext.Request.Path = "/test/17";

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        Assert.Same(dataSource.Endpoints[2], httpContext.GetEndpoint());
    }

    [Fact]
    public async Task MatchAsync_SkipsNonApplicableEndpointSelectorPolicies()
    {
        // Arrange
        var dataSource = new DefaultEndpointDataSource(new List<Endpoint>
            {
                CreateEndpoint("/test/{id:alpha}", 0),
                CreateEndpoint("/test/{id:int}", 0),
                CreateEndpoint("/test/{id}", 0),
            });

        var policy = new Mock<MatcherPolicy>();
        policy
            .As<IEndpointSelectorPolicy>()
            .Setup(p => p.AppliesToEndpoints(It.IsAny<IReadOnlyList<Endpoint>>())).Returns(false);
        policy
            .As<IEndpointSelectorPolicy>()
            .Setup(p => p.ApplyAsync(It.IsAny<HttpContext>(), It.IsAny<CandidateSet>()))
            .Returns<HttpContext, CandidateSet>((c, cs) =>
            {
                throw null; // Won't be called.
            });

        var matcher = CreateDfaMatcher(dataSource, policies: new[] { policy.Object, });

        var httpContext = CreateContext();
        httpContext.Request.Path = "/test/17";

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        Assert.Same(dataSource.Endpoints[1], httpContext.GetEndpoint());
    }

    [Fact]
    public async Task MatchAsync_RunsEndpointSelectorPolicies_CanShortCircuit()
    {
        // Arrange
        var dataSource = new DefaultEndpointDataSource(new List<Endpoint>
            {
                CreateEndpoint("/test/{id:alpha}", 0),
                CreateEndpoint("/test/{id:int}", 0),
                CreateEndpoint("/test/{id}", 0),
            });

        var policy1 = new Mock<MatcherPolicy>();
        policy1
            .As<IEndpointSelectorPolicy>()
            .Setup(p => p.AppliesToEndpoints(It.IsAny<IReadOnlyList<Endpoint>>())).Returns(true);
        policy1
            .As<IEndpointSelectorPolicy>()
            .Setup(p => p.ApplyAsync(It.IsAny<HttpContext>(), It.IsAny<CandidateSet>()))
            .Returns<HttpContext, CandidateSet>((c, cs) =>
            {
                c.SetEndpoint(cs[0].Endpoint);
                return Task.CompletedTask;
            });

        // This should never run, it's after policy1 which short circuits
        var policy2 = new Mock<MatcherPolicy>();
        policy2
            .SetupGet(p => p.Order)
            .Returns(1000);
        policy2
            .As<IEndpointSelectorPolicy>()
            .Setup(p => p.AppliesToEndpoints(It.IsAny<IReadOnlyList<Endpoint>>())).Returns(true);
        policy2
            .As<IEndpointSelectorPolicy>()
            .Setup(p => p.ApplyAsync(It.IsAny<HttpContext>(), It.IsAny<CandidateSet>()))
            .Throws(new InvalidOperationException());

        var matcher = CreateDfaMatcher(dataSource, policies: new[] { policy1.Object, policy2.Object, });

        var httpContext = CreateContext();
        httpContext.Request.Path = "/test/17";

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        Assert.Same(dataSource.Endpoints[0], httpContext.GetEndpoint());
    }

    private HttpContext CreateContext()
    {
        return new DefaultHttpContext();
    }
}
