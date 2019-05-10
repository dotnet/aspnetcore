// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Matching
{
    // Many of these are integration tests that exercise the system end to end,
    // so we're reusing the services here.
    public class DfaMatcherTest
    {
        private RouteEndpoint CreateEndpoint(string template, int order, object defaults = null, object requiredValues = null, object policies = null)
        {
            return EndpointFactory.CreateRouteEndpoint(template, defaults, policies, requiredValues, order, displayName: template);
        }

        private Matcher CreateDfaMatcher(
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
                    Assert.Equal(DfaMatcher.EventIds.CandidatesNotFound, log.EventId);
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
                    Assert.Equal(DfaMatcher.EventIds.CandidatesFound, log.EventId);
                    Assert.Equal("1 candidate(s) found for the request path '/One'", log.Message);
                },
                (log) =>
                {
                    Assert.Equal(DfaMatcher.EventIds.CandidateRejectedByConstraint, log.EventId);
                    Assert.Equal("Endpoint '/{p:int}' with route pattern '/{p:int}' was rejected by constraint 'p':'Microsoft.AspNetCore.Routing.Constraints.IntRouteConstraint' with value 'One' for the request path '/One'", log.Message);
                },
                (log) =>
                {
                    Assert.Equal(DfaMatcher.EventIds.CandidateNotValid, log.EventId);
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
                    Assert.Equal(DfaMatcher.EventIds.CandidatesFound, log.EventId);
                    Assert.Equal("1 candidate(s) found for the request path '/One'", log.Message);
                },
                (log) =>
                {
                    Assert.Equal(DfaMatcher.EventIds.CandidateRejectedByComplexSegment, log.EventId);
                    Assert.Equal("Endpoint '/x-{id}-y' with route pattern '/x-{id}-y' was rejected by complex segment 'x-{id}-y' for the request path '/One'", log.Message);
                },
                (log) =>
                {
                    Assert.Equal(DfaMatcher.EventIds.CandidateNotValid, log.EventId);
                    Assert.Equal("Endpoint '/x-{id}-y' with route pattern '/x-{id}-y' is not valid for the request path '/One'", log.Message);
                });
        }

        [Fact]
        public async Task MatchAsync_MultipleCandidates_Logging()
        {
            // Arrange
            var endpointDataSource = new DefaultEndpointDataSource(new List<Endpoint>
            {
                CreateEndpoint("/One", 0),
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
                    Assert.Equal(DfaMatcher.EventIds.CandidatesFound, log.EventId);
                    Assert.Equal("3 candidate(s) found for the request path '/One'", log.Message);
                },
                (log) =>
                {
                    Assert.Equal(DfaMatcher.EventIds.CandidateValid, log.EventId);
                    Assert.Equal("Endpoint '/One' with route pattern '/One' is valid for the request path '/One'", log.Message);
                },
                (log) =>
                {
                    Assert.Equal(DfaMatcher.EventIds.CandidateRejectedByConstraint, log.EventId);
                    Assert.Equal("Endpoint '/{p:int}' with route pattern '/{p:int}' was rejected by constraint 'p':'Microsoft.AspNetCore.Routing.Constraints.IntRouteConstraint' with value 'One' for the request path '/One'", log.Message);
                },
                (log) =>
                {
                    Assert.Equal(DfaMatcher.EventIds.CandidateNotValid, log.EventId);
                    Assert.Equal("Endpoint '/{p:int}' with route pattern '/{p:int}' is not valid for the request path '/One'", log.Message);
                },
                (log) =>
                {
                    Assert.Equal(DfaMatcher.EventIds.CandidateRejectedByComplexSegment, log.EventId);
                    Assert.Equal("Endpoint '/x-{id}-y' with route pattern '/x-{id}-y' was rejected by complex segment 'x-{id}-y' for the request path '/One'", log.Message);
                },
                (log) =>
                {
                    Assert.Equal(DfaMatcher.EventIds.CandidateNotValid, log.EventId);
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
}
