// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    public class MvcEndpointDataSourceTests
    {
        [Fact]
        public void Endpoints_AccessParameters_InitializedFromProvider()
        {
            // Arrange
            var routeValue = "Value";
            var requiredValues = new Dictionary<string, string>
            {
                ["Name"] = routeValue
            };
            var displayName = "DisplayName!";
            var order = 1;
            var template = "/Template!";
            var filterDescriptor = new FilterDescriptor(new ControllerActionFilter(), 1);

            var mockDescriptorProvider = new Mock<IActionDescriptorCollectionProvider>();
            mockDescriptorProvider.Setup(m => m.ActionDescriptors).Returns(new ActionDescriptorCollection(new List<ActionDescriptor>
            {
                new ActionDescriptor
                {
                    RouteValues = requiredValues,
                    DisplayName = displayName,
                    AttributeRouteInfo = new AttributeRouteInfo
                    {
                        Order = order,
                        Template = template
                    },
                    FilterDescriptors = new List<FilterDescriptor>
                    {
                        filterDescriptor
                    }
                }
            }, 0));

            var dataSource = CreateMvcEndpointDataSource(mockDescriptorProvider.Object);

            // Act
            var endpoints = dataSource.Endpoints;

            // Assert
            var endpoint = Assert.Single(endpoints);
            var matcherEndpoint = Assert.IsType<RouteEndpoint>(endpoint);

            var endpointValue = matcherEndpoint.RoutePattern.RequiredValues["Name"];
            Assert.Equal(routeValue, endpointValue);

            Assert.Equal(displayName, matcherEndpoint.DisplayName);
            Assert.Equal(order, matcherEndpoint.Order);
            Assert.Equal("/Template!", matcherEndpoint.RoutePattern.RawText);
        }

        [Fact]
        public async Task Endpoints_InvokeReturnedEndpoint_ActionInvokerProviderCalled()
        {
            // Arrange
            var endpointFeature = new EndpointSelectorContext
            {
                RouteValues = new RouteValueDictionary()
            };

            var featureCollection = new FeatureCollection();
            featureCollection.Set<IEndpointFeature>(endpointFeature);
            featureCollection.Set<IRouteValuesFeature>(endpointFeature);
            featureCollection.Set<IRoutingFeature>(endpointFeature);

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(m => m.Features).Returns(featureCollection);

            var descriptorProviderMock = new Mock<IActionDescriptorCollectionProvider>();
            descriptorProviderMock.Setup(m => m.ActionDescriptors).Returns(new ActionDescriptorCollection(new List<ActionDescriptor>
            {
                new ActionDescriptor
                {
                    AttributeRouteInfo = new AttributeRouteInfo
                    {
                        Template = string.Empty
                    },
                    FilterDescriptors = new List<FilterDescriptor>()
                }
            }, 0));

            var actionInvokerCalled = false;
            var actionInvokerMock = new Mock<IActionInvoker>();
            actionInvokerMock.Setup(m => m.InvokeAsync()).Returns(() =>
            {
                actionInvokerCalled = true;
                return Task.CompletedTask;
            });

            var actionInvokerProviderMock = new Mock<IActionInvokerFactory>();
            actionInvokerProviderMock.Setup(m => m.CreateInvoker(It.IsAny<ActionContext>())).Returns(actionInvokerMock.Object);

            var dataSource = CreateMvcEndpointDataSource(
                descriptorProviderMock.Object,
                new MvcEndpointInvokerFactory(actionInvokerProviderMock.Object));

            // Act
            var endpoints = dataSource.Endpoints;

            // Assert
            var endpoint = Assert.Single(endpoints);
            var matcherEndpoint = Assert.IsType<RouteEndpoint>(endpoint);

            await matcherEndpoint.RequestDelegate(httpContextMock.Object);

            Assert.True(actionInvokerCalled);
        }

        [Fact]
        public void Endpoints_SingleAction_ConventionalRoute_ContainsParameterWithNullRequiredRouteValue()
        {
            // Arrange
            var actionDescriptorCollection = GetActionDescriptorCollection(
                new { controller = "TestController", action = "TestAction", page = (string)null });
            var dataSource = CreateMvcEndpointDataSource(actionDescriptorCollection);
            dataSource.ConventionalEndpointInfos.Add(CreateEndpointInfo(
                string.Empty,
                "{controller}/{action}/{page}",
                new RouteValueDictionary(new { action = "TestAction" })));

            // Act
            var endpoints = dataSource.Endpoints;

            // Assert
            Assert.Empty(endpoints);
        }

        [Fact]
        public void Endpoints_SingleAction_AttributeRoute_ContainsParameterWithNullRequiredRouteValue()
        {
            // Arrange
            var actionDescriptorCollection = GetActionDescriptorCollection(
                "{controller}/{action}/{page}",
                new { controller = "TestController", action = "TestAction", page = (string)null });
            var dataSource = CreateMvcEndpointDataSource(actionDescriptorCollection);

            // Act
            var endpoints = dataSource.Endpoints;

            // Assert
            Assert.Collection(endpoints.Cast<RouteEndpoint>(),
                (e) =>
                {
                    Assert.Equal("{controller}/{action}/{page}", e.RoutePattern.RawText);
                    Assert.Equal("TestController", e.RoutePattern.RequiredValues["controller"]);
                    Assert.Equal("TestAction", e.RoutePattern.RequiredValues["action"]);
                    Assert.False(e.RoutePattern.RequiredValues.ContainsKey("page"));
                });
        }

        [Fact]
        public void Endpoints_CalledMultipleTimes_ReturnsSameInstance()
        {
            // Arrange
            var actionDescriptorCollectionProviderMock = new Mock<IActionDescriptorCollectionProvider>();
            actionDescriptorCollectionProviderMock
                .Setup(m => m.ActionDescriptors)
                .Returns(new ActionDescriptorCollection(new[]
                {
                    CreateActionDescriptor(new { controller = "TestController", action = "TestAction" })
                }, version: 0));

            var dataSource = CreateMvcEndpointDataSource(actionDescriptorCollectionProviderMock.Object);
            dataSource.ConventionalEndpointInfos.Add(CreateEndpointInfo(
                string.Empty,
                "{controller}/{action}",
                new RouteValueDictionary(new { action = "TestAction" })));

            // Act
            var endpoints1 = dataSource.Endpoints;
            var endpoints2 = dataSource.Endpoints;

            // Assert
            Assert.Collection(endpoints1,
                (e) => Assert.Equal("{controller}/{action}", Assert.IsType<RouteEndpoint>(e).RoutePattern.RawText));
            Assert.Same(endpoints1, endpoints2);

            actionDescriptorCollectionProviderMock.VerifyGet(m => m.ActionDescriptors, Times.Once);
        }

        [Fact]
        public void Endpoints_ChangeTokenTriggered_EndpointsRecreated()
        {
            // Arrange
            var actionDescriptorCollectionProviderMock = new Mock<ActionDescriptorCollectionProvider>();
            actionDescriptorCollectionProviderMock
                .Setup(m => m.ActionDescriptors)
                .Returns(new ActionDescriptorCollection(new[]
                {
                    CreateActionDescriptor(new { controller = "TestController", action = "TestAction" })
                }, version: 0));

            CancellationTokenSource cts = null;
            actionDescriptorCollectionProviderMock
                .Setup(m => m.GetChangeToken())
                .Returns(() =>
                {
                    cts = new CancellationTokenSource();
                    var changeToken = new CancellationChangeToken(cts.Token);

                    return changeToken;
                });


            var dataSource = CreateMvcEndpointDataSource(actionDescriptorCollectionProviderMock.Object);
            dataSource.ConventionalEndpointInfos.Add(CreateEndpointInfo(
                string.Empty,
                "{controller}/{action}",
                new RouteValueDictionary(new { action = "TestAction" })));

            // Act
            var endpoints = dataSource.Endpoints;

            Assert.Collection(endpoints,
                (e) =>
                {
                    var routePattern = Assert.IsType<RouteEndpoint>(e).RoutePattern;
                    Assert.Equal("{controller}/{action}", routePattern.RawText);
                    Assert.Equal("TestController", routePattern.RequiredValues["controller"]);
                    Assert.Equal("TestAction", routePattern.RequiredValues["action"]);
                });

            actionDescriptorCollectionProviderMock
                .Setup(m => m.ActionDescriptors)
                .Returns(new ActionDescriptorCollection(new[]
                {
                    CreateActionDescriptor(new { controller = "NewTestController", action = "NewTestAction" })
                }, version: 1));

            cts.Cancel();

            // Assert
            var newEndpoints = dataSource.Endpoints;

            Assert.NotSame(endpoints, newEndpoints);
            Assert.Collection(newEndpoints,
                (e) =>
                {
                    var routePattern = Assert.IsType<RouteEndpoint>(e).RoutePattern;
                    Assert.Equal("{controller}/{action}", routePattern.RawText);
                    Assert.Equal("NewTestController", routePattern.RequiredValues["controller"]);
                    Assert.Equal("NewTestAction", routePattern.RequiredValues["action"]);
                });
        }

        [Fact]
        public void Endpoints_MultipleActions_WithActionConstraint()
        {
            // Arrange
            var actionDescriptorCollection = GetActionDescriptorCollection(
                new { controller = "TestController", action = "TestAction" },
                new { controller = "TestController", action = "TestAction1" },
                new { controller = "TestController", action = "TestAction2" });
            var dataSource = CreateMvcEndpointDataSource(actionDescriptorCollection);
            dataSource.ConventionalEndpointInfos.Add(CreateEndpointInfo(
                string.Empty,
                "{controller}/{action}",
                constraints: new RouteValueDictionary(new { action = "(TestAction1|TestAction2)" })));

            // Act
            var endpoints = dataSource.Endpoints;

            // Assert
            Assert.Collection(endpoints.Cast<RouteEndpoint>(),
                (e) =>
                {
                    Assert.Equal("{controller}/{action}", e.RoutePattern.RawText);
                    Assert.Equal("TestController", e.RoutePattern.RequiredValues["controller"]);
                    Assert.Equal("TestAction1", e.RoutePattern.RequiredValues["action"]);
                },
                (e) =>
                {
                    Assert.Equal("{controller}/{action}", e.RoutePattern.RawText);
                    Assert.Equal("TestController", e.RoutePattern.RequiredValues["controller"]);
                    Assert.Equal("TestAction2", e.RoutePattern.RequiredValues["action"]);
                });
        }

        [Fact]
        public void Endpoints_ConventionalRoute_WithEmptyRouteName_CreatesMetadataWithEmptyRouteName()
        {
            // Arrange
            var actionDescriptorCollection = GetActionDescriptorCollection(
                new { controller = "Home", action = "Index" });
            var dataSource = CreateMvcEndpointDataSource(actionDescriptorCollection);
            dataSource.ConventionalEndpointInfos.Add(
                CreateEndpointInfo(string.Empty, "named/{controller}/{action}/{id?}"));

            // Act
            var endpoints = dataSource.Endpoints;

            // Assert
            var endpoint = Assert.Single(endpoints);
            var matcherEndpoint = Assert.IsType<RouteEndpoint>(endpoint);
            var routeNameMetadata = matcherEndpoint.Metadata.GetMetadata<IRouteNameMetadata>();
            Assert.NotNull(routeNameMetadata);
            Assert.Equal(string.Empty, routeNameMetadata.RouteName);
        }

        [Fact]
        public void Endpoints_CanCreateMultipleEndpoints_WithSameRouteName()
        {
            // Arrange
            var actionDescriptorCollection = GetActionDescriptorCollection(
                new { controller = "Home", action = "Index" },
                new { controller = "Products", action = "Details" });
            var dataSource = CreateMvcEndpointDataSource(actionDescriptorCollection);
            dataSource.ConventionalEndpointInfos.Add(
                CreateEndpointInfo("namedRoute", "named/{controller}/{action}/{id?}"));

            // Act
            var endpoints = dataSource.Endpoints;

            // Assert
            Assert.Collection(
                endpoints,
                (ep) =>
                {
                    var matcherEndpoint = Assert.IsType<RouteEndpoint>(ep);
                    var routeNameMetadata = matcherEndpoint.Metadata.GetMetadata<IRouteNameMetadata>();
                    Assert.NotNull(routeNameMetadata);
                    Assert.Equal("namedRoute", routeNameMetadata.RouteName);
                    Assert.Equal("named/{controller}/{action}/{id?}", matcherEndpoint.RoutePattern.RawText);
                },
                (ep) =>
                {
                    var matcherEndpoint = Assert.IsType<RouteEndpoint>(ep);
                    var routeNameMetadata = matcherEndpoint.Metadata.GetMetadata<IRouteNameMetadata>();
                    Assert.NotNull(routeNameMetadata);
                    Assert.Equal("namedRoute", routeNameMetadata.RouteName);
                    Assert.Equal("named/{controller}/{action}/{id?}", matcherEndpoint.RoutePattern.RawText);
                });
        }

        [Fact]
        public void Endpoints_ConventionalRoutes_StaticallyDefinedOrder_IsMaintained()
        {
            // Arrange
            var actionDescriptorCollection = GetActionDescriptorCollection(
                new { controller = "Home", action = "Index" },
                new { controller = "Products", action = "Details" });
            var dataSource = CreateMvcEndpointDataSource(actionDescriptorCollection);
            dataSource.ConventionalEndpointInfos.Add(CreateEndpointInfo(
                name: string.Empty,
                template: "{controller}/{action}/{id?}"));
            dataSource.ConventionalEndpointInfos.Add(CreateEndpointInfo(
                name: "namedRoute",
                "named/{controller}/{action}/{id?}"));

            // Act
            var endpoints = dataSource.Endpoints;

            // Assert
            Assert.Collection(
                endpoints,
                (ep) =>
                {
                    var matcherEndpoint = Assert.IsType<RouteEndpoint>(ep);
                    Assert.Equal("{controller}/{action}/{id?}", matcherEndpoint.RoutePattern.RawText);
                    Assert.Equal("Index", matcherEndpoint.RoutePattern.RequiredValues["action"]);
                    Assert.Equal("Home", matcherEndpoint.RoutePattern.RequiredValues["controller"]);
                    Assert.Equal(1, matcherEndpoint.Order);
                },
                (ep) =>
                {
                    var matcherEndpoint = Assert.IsType<RouteEndpoint>(ep);
                    Assert.Equal("named/{controller}/{action}/{id?}", matcherEndpoint.RoutePattern.RawText);
                    Assert.Equal("Index", matcherEndpoint.RoutePattern.RequiredValues["action"]);
                    Assert.Equal("Home", matcherEndpoint.RoutePattern.RequiredValues["controller"]);
                    Assert.Equal(2, matcherEndpoint.Order);
                },
                (ep) =>
                {
                    var matcherEndpoint = Assert.IsType<RouteEndpoint>(ep);
                    Assert.Equal("{controller}/{action}/{id?}", matcherEndpoint.RoutePattern.RawText);
                    Assert.Equal("Details", matcherEndpoint.RoutePattern.RequiredValues["action"]);
                    Assert.Equal("Products", matcherEndpoint.RoutePattern.RequiredValues["controller"]);
                    Assert.Equal(1, matcherEndpoint.Order);
                },
                (ep) =>
                {
                    var matcherEndpoint = Assert.IsType<RouteEndpoint>(ep);
                    Assert.Equal("named/{controller}/{action}/{id?}", matcherEndpoint.RoutePattern.RawText);
                    Assert.Equal("Details", matcherEndpoint.RoutePattern.RequiredValues["action"]);
                    Assert.Equal("Products", matcherEndpoint.RoutePattern.RequiredValues["controller"]);
                    Assert.Equal(2, matcherEndpoint.Order);
                });
        }

        [Fact]
        public void RequiredValue_WithNoCorresponding_TemplateParameter_DoesNotProduceEndpoint()
        {
            // Arrange
            var requiredValues = new RouteValueDictionary(new { area = "admin", controller = "home", action = "index" });
            var actionDescriptorCollection = GetActionDescriptorCollection(requiredValues);
            var dataSource = CreateMvcEndpointDataSource(actionDescriptorCollection);
            dataSource.ConventionalEndpointInfos.Add(CreateEndpointInfo(string.Empty, "{controller}/{action}"));

            // Act
            var endpoints = dataSource.Endpoints;

            // Assert
            Assert.Empty(endpoints);
        }

        // area, controller, action and page are special, but not hardcoded. Actions can define custom required
        // route values. This has been used successfully for localization, versioning and similar schemes. We should
        // be able to replace custom route values too.
        [Fact]
        public void NonReservedRequiredValue_WithNoCorresponding_TemplateParameter_DoesNotProduceEndpoint()
        {
            // Arrange
            var action1 = new RouteValueDictionary(new { controller = "home", action = "index", locale = "en-NZ" });
            var action2 = new RouteValueDictionary(new { controller = "home", action = "about", locale = "en-CA" });
            var action3 = new RouteValueDictionary(new { controller = "home", action = "index", locale = (string)null });

            var actionDescriptorCollection = GetActionDescriptorCollection(action1, action2, action3);
            var dataSource = CreateMvcEndpointDataSource(actionDescriptorCollection);

            // Adding a localized route a non-localized route
            dataSource.ConventionalEndpointInfos.Add(CreateEndpointInfo(string.Empty, "{locale}/{controller}/{action}"));
            dataSource.ConventionalEndpointInfos.Add(CreateEndpointInfo(string.Empty, "{controller}/{action}"));

            // Act
            var endpoints = dataSource.Endpoints;

            // Assert
            Assert.Collection(
                endpoints.Cast<RouteEndpoint>(),
                e =>
                {
                    Assert.Equal("{locale}/{controller}/{action}", e.RoutePattern.RawText);
                    Assert.Equal("home", e.RoutePattern.RequiredValues["controller"]);
                    Assert.Equal("index", e.RoutePattern.RequiredValues["action"]);
                    Assert.Equal("en-NZ", e.RoutePattern.RequiredValues["locale"]);
                },
                e =>
                {
                    Assert.Equal("{locale}/{controller}/{action}", e.RoutePattern.RawText);
                    Assert.Equal("home", e.RoutePattern.RequiredValues["controller"]);
                    Assert.Equal("about", e.RoutePattern.RequiredValues["action"]);
                    Assert.Equal("en-CA", e.RoutePattern.RequiredValues["locale"]);
                },
                e =>
                {
                    Assert.Equal("{controller}/{action}", e.RoutePattern.RawText);
                    Assert.Equal("home", e.RoutePattern.RequiredValues["controller"]);
                    Assert.Equal("index", e.RoutePattern.RequiredValues["action"]);
                });
        }

        [Fact]
        public void TemplateParameter_WithNoDefaultOrRequiredValue_DoesNotProduceEndpoint()
        {
            // Arrange
            var requiredValues = new RouteValueDictionary(new { controller = "home", action = "index", area = (string)null });
            var actionDescriptorCollection = GetActionDescriptorCollection(requiredValues);
            var dataSource = CreateMvcEndpointDataSource(actionDescriptorCollection);
            dataSource.ConventionalEndpointInfos.Add(CreateEndpointInfo(string.Empty, "{area}/{controller}/{action}"));

            // Act
            var endpoints = dataSource.Endpoints;

            // Assert
            Assert.Empty(endpoints);
        }

        [Fact]
        public void TemplateParameter_WithDefaultValue_AndNullRequiredValue_DoesNotProduceEndpoint()
        {
            // Arrange
            var requiredValues = new RouteValueDictionary(new { area = (string)null, controller = "home", action = "index" });
            var actionDescriptorCollection = GetActionDescriptorCollection(requiredValues);
            var dataSource = CreateMvcEndpointDataSource(actionDescriptorCollection);
            dataSource.ConventionalEndpointInfos.Add(CreateEndpointInfo(string.Empty, "{area=admin}/{controller}/{action}"));

            // Act
            var endpoints = dataSource.Endpoints;

            // Assert
            Assert.Empty(endpoints);
        }

        [Fact]
        public void TemplateParameter_WithNullRequiredValue_DoesNotProduceEndpoint()
        {
            // Arrange
            var requiredValues = new RouteValueDictionary(new { area = (string)null, controller = "home", action = "index" });
            var actionDescriptorCollection = GetActionDescriptorCollection(requiredValues);
            var dataSource = CreateMvcEndpointDataSource(actionDescriptorCollection);
            dataSource.ConventionalEndpointInfos.Add(CreateEndpointInfo(string.Empty, "{area}/{controller}/{action}"));

            // Act
            var endpoints = dataSource.Endpoints;

            // Assert
            Assert.Empty(endpoints);
        }

        [Fact]
        public void NoDefaultValues_RequiredValues_UsedToCreateDefaultValues()
        {
            // Arrange
            var requiredValues = new RouteValueDictionary(new { controller = "Foo", action = "Bar" });
            var actionDescriptorCollection = GetActionDescriptorCollection(requiredValues: requiredValues);
            var dataSource = CreateMvcEndpointDataSource(actionDescriptorCollection);
            dataSource.ConventionalEndpointInfos.Add(CreateEndpointInfo(string.Empty, "{controller}/{action}"));

            // Act
            var endpoints = dataSource.Endpoints;

            // Assert
            var endpoint = Assert.Single(endpoints);
            var matcherEndpoint = Assert.IsType<RouteEndpoint>(endpoint);
            Assert.Equal("{controller}/{action}", matcherEndpoint.RoutePattern.RawText);
            AssertIsSubset(requiredValues, matcherEndpoint.RoutePattern.RequiredValues);
        }

        [Fact]
        public void RequiredValues_NotPresent_InDefaultValuesOrParameter_EndpointNotCreated()
        {
            // Arrange
            var requiredValues = new RouteValueDictionary(
                new { controller = "Foo", action = "Bar", subarea = "test" });
            var expectedDefaults = requiredValues;
            var actionDescriptorCollection = GetActionDescriptorCollection(requiredValues: requiredValues);
            var dataSource = CreateMvcEndpointDataSource(actionDescriptorCollection);
            dataSource.ConventionalEndpointInfos.Add(
                CreateEndpointInfo(string.Empty, "{controller=Home}/{action=Index}"));

            // Act
            var endpoints = dataSource.Endpoints;

            // Assert
            Assert.Empty(endpoints);
        }

        [Fact]
        public void RequiredValues_DoesNotMatchParameterDefaults_Included()
        {
            // Arrange
            var action = new RouteValueDictionary(
                new { controller = "Foo", action = "Baz", }); // Doesn't match default
            var expectedDefaults = new RouteValueDictionary(
                new { controller = "Foo", action = "Baz", });
            var actionDescriptorCollection = GetActionDescriptorCollection(action);
            var dataSource = CreateMvcEndpointDataSource(actionDescriptorCollection);
            dataSource.ConventionalEndpointInfos.Add(
                CreateEndpointInfo(
                    string.Empty,
                    "{controller}/{action}/{id?}",
                    defaults: new RouteValueDictionary(new { controller = "Foo", action = "Bar" })));

            // Act
            var endpoints = dataSource.Endpoints;

            // Assert
            var endpoint = Assert.Single(endpoints);
            var matcherEndpoint = Assert.IsType<RouteEndpoint>(endpoint);
            Assert.Equal("{controller}/{action}/{id?}", matcherEndpoint.RoutePattern.RawText);
            Assert.Equal("Foo", matcherEndpoint.RoutePattern.RequiredValues["controller"]);
            Assert.Equal("Baz", matcherEndpoint.RoutePattern.RequiredValues["action"]);
            Assert.Equal("Foo", matcherEndpoint.RoutePattern.Defaults["controller"]);
            Assert.False(matcherEndpoint.RoutePattern.Defaults.ContainsKey("action"));
        }

        [Fact]
        public void RequiredValues_DoesNotMatchNonParameterDefaults_FilteredOut()
        {
            // Arrange
            var action1 = new RouteValueDictionary(
                new { controller = "Foo", action = "Bar", });
            var action2 = new RouteValueDictionary(
                new { controller = "Foo", action = "Baz", }); // Doesn't match default
            var expectedDefaults = new RouteValueDictionary(
                new { controller = "Foo", action = "Bar", });
            var actionDescriptorCollection = GetActionDescriptorCollection(action1, action2);
            var dataSource = CreateMvcEndpointDataSource(actionDescriptorCollection);
            dataSource.ConventionalEndpointInfos.Add(
                CreateEndpointInfo(
                    string.Empty,
                    "Blog/{*slug}",
                    defaults: new RouteValueDictionary(new { controller = "Foo", action = "Bar" })));

            // Act
            var endpoints = dataSource.Endpoints;

            // Assert
            var endpoint = Assert.Single(endpoints);
            var matcherEndpoint = Assert.IsType<RouteEndpoint>(endpoint);
            Assert.Equal("Blog/{*slug}", matcherEndpoint.RoutePattern.RawText);
            AssertIsSubset(expectedDefaults, matcherEndpoint.RoutePattern.Defaults);
        }

        [Fact]
        public void Endpoints_ConventionalRoutes_DefaultValuesAndCatchAll_EndpointInfoDefaultsNotModified()
        {
            // Arrange
            var actionDescriptorCollection = GetActionDescriptorCollection(
                new { controller = "TestController", action = "TestAction" });
            var dataSource = CreateMvcEndpointDataSource(actionDescriptorCollection);

            var endpointInfo = CreateEndpointInfo(
                name: string.Empty,
                defaults: new RouteValueDictionary(),
                template: "{controller=TestController}/{action=TestAction}/{id=17}/{**catchAll}");
            dataSource.ConventionalEndpointInfos.Add(endpointInfo);

            // Act
            var endpoints = dataSource.Endpoints;

            // Assert
            Assert.Empty(endpointInfo.Defaults);
        }

        [Fact]
        public void Endpoints_AttributeRoutes_DefaultDifferentCaseFromRouteValue_UseDefaultCase()
        {
            // Arrange
            var actionDescriptorCollection = GetActionDescriptorCollection(
                "{controller}/{action=TESTACTION}/{id?}",
                new { controller = "TestController", action = "TestAction" });
            var dataSource = CreateMvcEndpointDataSource(actionDescriptorCollection);

            // Act
            var endpoints = dataSource.Endpoints;

            // Assert
            Assert.Collection(
                endpoints,
                (ep) =>
                {
                    var matcherEndpoint = Assert.IsType<RouteEndpoint>(ep);
                    Assert.Equal("{controller}/{action=TESTACTION}/{id?}", matcherEndpoint.RoutePattern.RawText);
                    Assert.Equal("TESTACTION", matcherEndpoint.RoutePattern.Defaults["action"]);
                    Assert.Equal(0, matcherEndpoint.Order);

                    Assert.Equal("TestAction", matcherEndpoint.RoutePattern.RequiredValues["action"]);
                });
        }

        private MvcEndpointDataSource CreateMvcEndpointDataSource(
            IActionDescriptorCollectionProvider actionDescriptorCollectionProvider = null,
            MvcEndpointInvokerFactory mvcEndpointInvokerFactory = null)
        {
            if (actionDescriptorCollectionProvider == null)
            {
                actionDescriptorCollectionProvider = new DefaultActionDescriptorCollectionProvider(
                    Array.Empty<IActionDescriptorProvider>(),
                    Array.Empty<IActionDescriptorChangeProvider>());
            }

            var services = new ServiceCollection();
            services.AddSingleton(actionDescriptorCollectionProvider);

            var routeOptionsSetup = new MvcCoreRouteOptionsSetup();
            services.Configure<RouteOptions>(routeOptionsSetup.Configure);
            services.AddRouting(options =>
            {
                options.ConstraintMap["upper-case"] = typeof(UpperCaseParameterTransform);
            });

            var serviceProvider = services.BuildServiceProvider();

            var dataSource = new MvcEndpointDataSource(
                actionDescriptorCollectionProvider,
                mvcEndpointInvokerFactory ?? new MvcEndpointInvokerFactory(new ActionInvokerFactory(Array.Empty<IActionInvokerProvider>())),
                serviceProvider.GetRequiredService<ParameterPolicyFactory>(),
                serviceProvider.GetRequiredService<RoutePatternTransformer>());

            var defaultEndpointConventionBuilder = new DefaultEndpointConventionBuilder();
            dataSource.AttributeRoutingConventionResolvers.Add((actionDescriptor) =>
            {
                return defaultEndpointConventionBuilder;
            });

            return dataSource;
        }

        private class UpperCaseParameterTransform : IOutboundParameterTransformer
        {
            public string TransformOutbound(object value)
            {
                return value?.ToString().ToUpperInvariant();
            }
        }

        private MvcEndpointInfo CreateEndpointInfo(
            string name,
            string template,
            RouteValueDictionary defaults = null,
            IDictionary<string, object> constraints = null,
            RouteValueDictionary dataTokens = null,
            IServiceProvider serviceProvider = null)
        {
            if (serviceProvider == null)
            {
                var services = new ServiceCollection();
                services.AddRouting();
                services.AddSingleton(typeof(UpperCaseParameterTransform), new UpperCaseParameterTransform());

                var routeOptionsSetup = new MvcCoreRouteOptionsSetup();
                services.Configure<RouteOptions>(routeOptionsSetup.Configure);
                services.Configure<RouteOptions>(options =>
                {
                    options.ConstraintMap["upper-case"] = typeof(UpperCaseParameterTransform);
                });

                serviceProvider = services.BuildServiceProvider();
            }

            var parameterPolicyFactory = serviceProvider.GetRequiredService<ParameterPolicyFactory>();
            return new MvcEndpointInfo(name, template, defaults, constraints, dataTokens, parameterPolicyFactory);
        }

        private IActionDescriptorCollectionProvider GetActionDescriptorCollection(params object[] requiredValues)
        {
            return GetActionDescriptorCollection(attributeRouteTemplate: null, requiredValues);
        }

        private IActionDescriptorCollectionProvider GetActionDescriptorCollection(string attributeRouteTemplate, params object[] requiredValues)
        {
            var actionDescriptors = new List<ActionDescriptor>();
            foreach (var requiredValue in requiredValues)
            {
                actionDescriptors.Add(CreateActionDescriptor(requiredValue, attributeRouteTemplate));
            }

            return GetActionDescriptorCollection(actionDescriptors.ToArray());
        }

        private IActionDescriptorCollectionProvider GetActionDescriptorCollection(params ActionDescriptor[] actionDescriptors)
        {
            var actionDescriptorCollectionProviderMock = new Mock<IActionDescriptorCollectionProvider>();
            actionDescriptorCollectionProviderMock
                .Setup(m => m.ActionDescriptors)
                .Returns(new ActionDescriptorCollection(actionDescriptors, version: 0));
            return actionDescriptorCollectionProviderMock.Object;
        }

        private ActionDescriptor CreateActionDescriptor(
            object requiredValues,
            string attributeRouteTemplate = null,
            IList<object> metadata = null)
        {
            var actionDescriptor = new ActionDescriptor();
            var routeValues = new RouteValueDictionary(requiredValues);
            foreach (var kvp in routeValues)
            {
                actionDescriptor.RouteValues[kvp.Key] = kvp.Value?.ToString();
            }
            if (!string.IsNullOrEmpty(attributeRouteTemplate))
            {
                actionDescriptor.AttributeRouteInfo = new AttributeRouteInfo
                {
                    Name = attributeRouteTemplate,
                    Template = attributeRouteTemplate
                };
            }
            actionDescriptor.EndpointMetadata = metadata;
            return actionDescriptor;
        }

        private void AssertIsSubset(
            IReadOnlyDictionary<string, object> subset,
            IReadOnlyDictionary<string, object> fullSet)
        {
            foreach (var subsetPair in subset)
            {
                var isPresent = fullSet.TryGetValue(subsetPair.Key, out var fullSetPairValue);
                Assert.True(isPresent);
                Assert.Equal(subsetPair.Value, fullSetPairValue);
            }
        }

        private void AssertMatchingSuppressed(Endpoint endpoint, bool suppressed)
        {
            var isEndpointSuppressed = endpoint.Metadata.GetMetadata<ISuppressMatchingMetadata>()?.SuppressMatching ?? false;
            Assert.Equal(suppressed, isEndpointSuppressed);
        }
    }
}