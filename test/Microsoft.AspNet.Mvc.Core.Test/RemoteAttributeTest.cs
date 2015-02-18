// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class RemoteAttributeTest
    {
        private static readonly IModelMetadataProvider _metadataProvider = new EmptyModelMetadataProvider();
        private static readonly ModelMetadata _metadata = _metadataProvider.GetMetadataForProperty(
            typeof(string),
            "Length");

        public static TheoryData<string> SomeNames
        {
            get
            {
                return new TheoryData<string>
                {
                    string.Empty,
                    "Action",
                    "In a controller",
                    "  slightly\t odd\t whitespace\t\r\n",
                };
            }
        }

        // Null or empty property names are invalid. (Those containing just whitespace are legal.)
        public static TheoryData<string> NullOrEmptyNames
        {
            get
            {
                return new TheoryData<string>
                {
                    null,
                    string.Empty,
                };
            }
        }

        [Fact]
        public void IsValidAlwaysReturnsTrue()
        {
            // Act & Assert
            Assert.True(new RemoteAttribute("RouteName", "ParameterName").IsValid(null));
            Assert.True(new RemoteAttribute("ActionName", "ControllerName", "ParameterName").IsValid(null));
        }

        [Fact]
        public void Constructor_WithNullAction_IgnoresArgument()
        {
            // Arrange & Act
            var attribute = new TestableRemoteAttribute(action: null, controller: "AController");

            // Assert
            var keyValuePair = Assert.Single(attribute.RouteData);
            Assert.Equal(keyValuePair.Key, "controller");
        }

        [Fact]
        public void Constructor_WithNullController_IgnoresArgument()
        {
            // Arrange & Act
            var attribute = new TestableRemoteAttribute("AnAction", controller: null);

            // Assert
            var keyValuePair = Assert.Single(attribute.RouteData);
            Assert.Equal(keyValuePair.Key, "action");
            Assert.Null(attribute.RouteName);
        }

        [Theory]
        [InlineData(null)]
        [MemberData(nameof(SomeNames))]
        public void Constructor_WithRouteName_UpdatesProperty(string routeName)
        {
            // Arrange & Act
            var attribute = new TestableRemoteAttribute(routeName);

            // Assert
            Assert.Empty(attribute.RouteData);
            Assert.Equal(routeName, attribute.RouteName);
        }

        [Theory]
        [MemberData(nameof(SomeNames))]
        public void Constructor_WithActionController_UpdatesActionRouteData(string action)
        {
            // Arrange & Act
            var attribute = new TestableRemoteAttribute(action, "AController");

            // Assert
            Assert.Equal(2, attribute.RouteData.Count);
            Assert.Contains("controller", attribute.RouteData.Keys);
            var resultName = Assert.Single(
                    attribute.RouteData,
                    keyValuePair => string.Equals(keyValuePair.Key, "action", StringComparison.Ordinal))
                .Value;
            Assert.Equal(action, resultName);
            Assert.Null(attribute.RouteName);
        }

        [Theory]
        [MemberData(nameof(SomeNames))]
        public void Constructor_WithActionController_UpdatesControllerRouteData(string controller)
        {
            // Arrange & Act
            var attribute = new TestableRemoteAttribute("AnAction", controller);

            // Assert
            Assert.Equal(2, attribute.RouteData.Count);
            Assert.Contains("action", attribute.RouteData.Keys);
            var resultName = Assert.Single(
                    attribute.RouteData,
                    keyValuePair => string.Equals(keyValuePair.Key, "controller", StringComparison.Ordinal))
                .Value;
            Assert.Equal(controller, resultName);
            Assert.Null(attribute.RouteName);
        }

        [Theory]
        [InlineData(null)]
        [MemberData(nameof(SomeNames))]
        public void Constructor_WithActionControllerAreaName_UpdatesAreaRouteData(string areaName)
        {
            // Arrange & Act
            var attribute = new TestableRemoteAttribute("AnAction", "AController", areaName: areaName);

            // Assert
            Assert.Equal(3, attribute.RouteData.Count);
            Assert.Contains("action", attribute.RouteData.Keys);
            Assert.Contains("controller", attribute.RouteData.Keys);
            var resultName = Assert.Single(
                    attribute.RouteData,
                    keyValuePair => string.Equals(keyValuePair.Key, "area", StringComparison.Ordinal))
                .Value;
            Assert.Equal(areaName, resultName);
            Assert.Null(attribute.RouteName);
        }

        [Theory]
        [MemberData(nameof(NullOrEmptyNames))]
        public void FormatAdditionalFieldsForClientValidation_WithInvalidPropertyName_Throws(string property)
        {
            // Arrange
            var attribute = new RemoteAttribute(routeName: "default");
            var expected = "Value cannot be null or empty." + Environment.NewLine + "Parameter name: property";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(
                "property",
                () => attribute.FormatAdditionalFieldsForClientValidation(property));
            Assert.Equal(expected, exception.Message);
        }

        [Theory]
        [MemberData(nameof(NullOrEmptyNames))]
        public void FormatPropertyForClientValidation_WithInvalidPropertyName_Throws(string property)
        {
            // Arrange
            var expected = "Value cannot be null or empty." + Environment.NewLine + "Parameter name: property";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(
                "property",
                () => RemoteAttribute.FormatPropertyForClientValidation(property));
            Assert.Equal(expected, exception.Message);
        }

        [Fact]
        public void GetClientValidationRules_WithBadRouteName_Throws()
        {
            // Arrange
            var attribute = new RemoteAttribute("nonexistentRoute");
            var context = GetValidationContextWithArea(currentArea: null);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => attribute.GetClientValidationRules(context));
            Assert.Equal("No URL for remote validation could be found.", exception.Message);
        }

        [Fact]
        public void GetClientValidationRules_WithActionController_NoController_Throws()
        {
            // Arrange
            var attribute = new RemoteAttribute("Action", "Controller");
            var context = GetValidationContextWithNoController();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => attribute.GetClientValidationRules(context));
            Assert.Equal("No URL for remote validation could be found.", exception.Message);
        }

        [Fact]
        public void GetClientValidationRules_WithRoute_CallsUrlHelperWithExpectedValues()
        {
            // Arrange
            var routeName = "RouteName";
            var attribute = new RemoteAttribute(routeName);
            var url = "/my/URL";
            var urlHelper = new MockUrlHelper(url, routeName);
            var context = GetValidationContext(urlHelper);

            // Act & Assert
            var rule = Assert.Single(attribute.GetClientValidationRules(context));
            Assert.Equal("remote", rule.ValidationType);
            Assert.Equal("'Length' is invalid.", rule.ErrorMessage);

            Assert.Equal(2, rule.ValidationParameters.Count);
            Assert.Equal("*.Length", rule.ValidationParameters["additionalfields"]);
            Assert.Equal(url, rule.ValidationParameters["url"]);

            var routeDictionary = Assert.IsType<RouteValueDictionary>(urlHelper.RouteValues);
            Assert.Empty(routeDictionary);
        }

        [Fact]
        public void GetClientValidationRules_WithActionController_CallsUrlHelperWithExpectedValues()
        {
            // Arrange
            var attribute = new RemoteAttribute("Action", "Controller");
            var url = "/Controller/Action";
            var urlHelper = new MockUrlHelper(url, routeName: null);
            var context = GetValidationContext(urlHelper);

            // Act & Assert
            var rule = Assert.Single(attribute.GetClientValidationRules(context));
            Assert.Equal("remote", rule.ValidationType);
            Assert.Equal("'Length' is invalid.", rule.ErrorMessage);

            Assert.Equal(2, rule.ValidationParameters.Count);
            Assert.Equal("*.Length", rule.ValidationParameters["additionalfields"]);
            Assert.Equal(url, rule.ValidationParameters["url"]);

            var routeDictionary = Assert.IsType<RouteValueDictionary>(urlHelper.RouteValues);
            Assert.Equal(2, routeDictionary.Count);
            Assert.Equal("Action", routeDictionary["action"] as string);
            Assert.Equal("Controller", routeDictionary["controller"] as string);
        }

        [Fact]
        public void GetClientValidationRules_WithActionController_PropertiesSet_CallsUrlHelperWithExpectedValues()
        {
            // Arrange
            var attribute = new RemoteAttribute("Action", "Controller")
            {
                HttpMethod = "POST",
                AdditionalFields = "Password,ConfirmPassword",
            };
            var url = "/Controller/Action";
            var urlHelper = new MockUrlHelper(url, routeName: null);
            var context = GetValidationContext(urlHelper);

            // Act & Assert
            var rule = Assert.Single(attribute.GetClientValidationRules(context));
            Assert.Equal("remote", rule.ValidationType);
            Assert.Equal("'Length' is invalid.", rule.ErrorMessage);

            Assert.Equal(3, rule.ValidationParameters.Count);
            Assert.Equal("*.Length,*.Password,*.ConfirmPassword", rule.ValidationParameters["additionalfields"]);
            Assert.Equal("POST", rule.ValidationParameters["type"]);
            Assert.Equal(url, rule.ValidationParameters["url"]);

            var routeDictionary = Assert.IsType<RouteValueDictionary>(urlHelper.RouteValues);
            Assert.Equal(2, routeDictionary.Count);
            Assert.Equal("Action", routeDictionary["action"] as string);
            Assert.Equal("Controller", routeDictionary["controller"] as string);
        }

        [Fact]
        public void GetClientValidationRules_WithActionControllerArea_CallsUrlHelperWithExpectedValues()
        {
            // Arrange
            var attribute = new RemoteAttribute("Action", "Controller", "Test")
            {
                HttpMethod = "POST",
            };
            var url = "/Test/Controller/Action";
            var urlHelper = new MockUrlHelper(url, routeName: null);
            var context = GetValidationContext(urlHelper);

            // Act & Assert
            var rule = Assert.Single(attribute.GetClientValidationRules(context));
            Assert.Equal("remote", rule.ValidationType);
            Assert.Equal("'Length' is invalid.", rule.ErrorMessage);

            Assert.Equal(3, rule.ValidationParameters.Count);
            Assert.Equal("*.Length", rule.ValidationParameters["additionalfields"]);
            Assert.Equal("POST", rule.ValidationParameters["type"]);
            Assert.Equal(url, rule.ValidationParameters["url"]);

            var routeDictionary = Assert.IsType<RouteValueDictionary>(urlHelper.RouteValues);
            Assert.Equal(3, routeDictionary.Count);
            Assert.Equal("Action", routeDictionary["action"] as string);
            Assert.Equal("Controller", routeDictionary["controller"] as string);
            Assert.Equal("Test", routeDictionary["area"] as string);
        }

        // Root area is current in this case.
        [Fact]
        public void GetClientValidationRules_WithActionController_FindsControllerInCurrentArea()
        {
            // Arrange
            var attribute = new RemoteAttribute("Action", "Controller");
            var context = GetValidationContextWithArea(currentArea: null);

            // Act & Assert
            var rule = Assert.Single(attribute.GetClientValidationRules(context));
            Assert.Equal("remote", rule.ValidationType);
            Assert.Equal("'Length' is invalid.", rule.ErrorMessage);

            Assert.Equal(2, rule.ValidationParameters.Count);
            Assert.Equal("*.Length", rule.ValidationParameters["additionalfields"]);
            Assert.Equal("/Controller/Action", rule.ValidationParameters["url"]);
        }

        // Test area is current in this case.
        [Fact]
        public void GetClientValidationRules_WithActionControllerInArea_FindsControllerInCurrentArea()
        {
            // Arrange
            var attribute = new RemoteAttribute("Action", "Controller");
            var context = GetValidationContextWithArea(currentArea: "Test");

            // Act & Assert
            var rule = Assert.Single(attribute.GetClientValidationRules(context));
            Assert.Equal("remote", rule.ValidationType);
            Assert.Equal("'Length' is invalid.", rule.ErrorMessage);

            Assert.Equal(2, rule.ValidationParameters.Count);
            Assert.Equal("*.Length", rule.ValidationParameters["additionalfields"]);
            Assert.Equal("/Test/Controller/Action", rule.ValidationParameters["url"]);
        }

        // Explicit reference to the (current) root area.
        [Theory]
        [MemberData(nameof(NullOrEmptyNames))]
        public void GetClientValidationRules_WithActionControllerArea_FindsControllerInRootArea(string areaName)
        {
            // Arrange
            var attribute = new RemoteAttribute("Action", "Controller", areaName);
            var context = GetValidationContextWithArea(currentArea: null);

            // Act & Assert
            var rule = Assert.Single(attribute.GetClientValidationRules(context));
            Assert.Equal("remote", rule.ValidationType);
            Assert.Equal("'Length' is invalid.", rule.ErrorMessage);

            Assert.Equal(2, rule.ValidationParameters.Count);
            Assert.Equal("*.Length", rule.ValidationParameters["additionalfields"]);
            Assert.Equal("/Controller/Action", rule.ValidationParameters["url"]);
        }

        // Test area is current in this case.
        [Theory]
        [MemberData(nameof(NullOrEmptyNames))]
        public void GetClientValidationRules_WithActionControllerAreaInArea_FindsControllerInRootArea(string areaName)
        {
            // Arrange
            var attribute = new RemoteAttribute("Action", "Controller", areaName);
            var context = GetValidationContextWithArea(currentArea: "Test");

            // Act & Assert
            var rule = Assert.Single(attribute.GetClientValidationRules(context));
            Assert.Equal("remote", rule.ValidationType);
            Assert.Equal("'Length' is invalid.", rule.ErrorMessage);

            Assert.Equal(2, rule.ValidationParameters.Count);
            Assert.Equal("*.Length", rule.ValidationParameters["additionalfields"]);
            Assert.Equal("/Controller/Action", rule.ValidationParameters["url"]);
        }

        // Root area is current in this case.
        [Fact]
        public void GetClientValidationRules_WithActionControllerArea_FindsControllerInNamedArea()
        {
            // Arrange
            var attribute = new RemoteAttribute("Action", "Controller", "Test");
            var context = GetValidationContextWithArea(currentArea: null);

            // Act & Assert
            var rule = Assert.Single(attribute.GetClientValidationRules(context));
            Assert.Equal("remote", rule.ValidationType);
            Assert.Equal("'Length' is invalid.", rule.ErrorMessage);

            Assert.Equal(2, rule.ValidationParameters.Count);
            Assert.Equal("*.Length", rule.ValidationParameters["additionalfields"]);
            Assert.Equal("/Test/Controller/Action", rule.ValidationParameters["url"]);
        }

        // Explicit reference to the current (Test) area.
        [Fact]
        public void GetClientValidationRules_WithActionControllerAreaInArea_FindsControllerInNamedArea()
        {
            // Arrange
            var attribute = new RemoteAttribute("Action", "Controller", "Test");
            var context = GetValidationContextWithArea(currentArea: "Test");

            // Act & Assert
            var rule = Assert.Single(attribute.GetClientValidationRules(context));
            Assert.Equal("remote", rule.ValidationType);
            Assert.Equal("'Length' is invalid.", rule.ErrorMessage);

            Assert.Equal(2, rule.ValidationParameters.Count);
            Assert.Equal("*.Length", rule.ValidationParameters["additionalfields"]);
            Assert.Equal("/Test/Controller/Action", rule.ValidationParameters["url"]);
        }

        // Test area is current in this case.
        [Fact]
        public void GetClientValidationRules_WithActionControllerAreaInArea_FindsControllerInDifferentArea()
        {
            // Arrange
            var attribute = new RemoteAttribute("Action", "Controller", "AnotherArea");
            var context = GetValidationContextWithArea(currentArea: "Test");

            // Act & Assert
            var rule = Assert.Single(attribute.GetClientValidationRules(context));
            Assert.Equal("remote", rule.ValidationType);
            Assert.Equal("'Length' is invalid.", rule.ErrorMessage);

            Assert.Equal(2, rule.ValidationParameters.Count);
            Assert.Equal("*.Length", rule.ValidationParameters["additionalfields"]);
            Assert.Equal("/AnotherArea/Controller/Action", rule.ValidationParameters["url"]);
        }

        private static ClientModelValidationContext GetValidationContext(IUrlHelper urlHelper)
        {
            var serviceCollection = GetServiceCollection();
            serviceCollection.AddInstance<IUrlHelper>(urlHelper);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            return new ClientModelValidationContext(_metadata, _metadataProvider, serviceProvider);
        }

        private static ClientModelValidationContext GetValidationContextWithArea(string currentArea)
        {
            var serviceCollection = GetServiceCollection();
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var routeCollection = GetRouteCollectionWithArea(serviceProvider);
            var routeData = new RouteData
            {
                Routers =
                {
                    routeCollection,
                },
                Values =
                {
                    { "action", "Index" },
                    { "controller", "Home" },
                },
            };
            if (!string.IsNullOrEmpty(currentArea))
            {
                routeData.Values["area"] = currentArea;
            }

            var contextAccessor = GetContextAccessor(serviceProvider, routeData);
            var actionSelector = new Mock<IActionSelector>(MockBehavior.Strict);
            var urlHelper = new UrlHelper(contextAccessor, actionSelector.Object);
            serviceCollection.AddInstance<IUrlHelper>(urlHelper);
            serviceProvider = serviceCollection.BuildServiceProvider();

            return new ClientModelValidationContext(_metadata, _metadataProvider, serviceProvider);
        }

        private static ClientModelValidationContext GetValidationContextWithNoController()
        {
            var serviceCollection = GetServiceCollection();
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var routeCollection = GetRouteCollectionWithNoController(serviceProvider);
            var routeData = new RouteData
            {
                Routers =
                {
                    routeCollection,
                },
            };

            var contextAccessor = GetContextAccessor(serviceProvider, routeData);
            var actionSelector = new Mock<IActionSelector>(MockBehavior.Strict);
            var urlHelper = new UrlHelper(contextAccessor, actionSelector.Object);
            serviceCollection.AddInstance<IUrlHelper>(urlHelper);
            serviceProvider = serviceCollection.BuildServiceProvider();

            return new ClientModelValidationContext(_metadata, _metadataProvider, serviceProvider);
        }

        private static IRouter GetRouteCollectionWithArea(IServiceProvider serviceProvider)
        {
            var builder = GetRouteBuilder(serviceProvider, isBound: true);

            // Setting IsBound to true makes order more important than usual. First try the route that requires the
            // area value. Skip usual "area:exists" constraint because that isn't relevant for link generation and it
            // complicates the setup significantly.
            builder.MapRoute("areaRoute", "{area}/{controller}/{action}");
            builder.MapRoute("default", "{controller}/{action}", new { controller = "Home", action = "Index" });

            return builder.Build();
        }

        private static IRouter GetRouteCollectionWithNoController(IServiceProvider serviceProvider)
        {
            var builder = GetRouteBuilder(serviceProvider, isBound: false);
            builder.MapRoute("default", "static/route");

            return builder.Build();
        }

        private static RouteBuilder GetRouteBuilder(IServiceProvider serviceProvider, bool isBound)
        {
            var builder = new RouteBuilder
            {
                ServiceProvider = serviceProvider,
            };

            var handler = new Mock<IRouter>(MockBehavior.Strict);
            handler
                .Setup(router => router.GetVirtualPath(It.IsAny<VirtualPathContext>()))
                .Callback<VirtualPathContext>(context => context.IsBound = isBound)
                .Returns((string)null);
            builder.DefaultHandler = handler.Object;

            return builder;
        }

        private static IScopedInstance<ActionContext> GetContextAccessor(
            IServiceProvider serviceProvider,
            RouteData routeData = null)
        {
            // Set IServiceProvider properties because TemplateRoute gets services (e.g. an ILoggerFactory instance)
            // through the HttpContext.
            var httpContext = new DefaultHttpContext
            {
                ApplicationServices = serviceProvider,
                RequestServices = serviceProvider,
            };

            if (routeData == null)
            {
                routeData = new RouteData
                {
                    Routers = { Mock.Of<IRouter>(), },
                };
            }

            var actionContext = new ActionContext(httpContext, routeData, new ActionDescriptor());
            var contextAccessor = new Mock<IScopedInstance<ActionContext>>();
            contextAccessor
                .SetupGet(accessor => accessor.Value)
                .Returns(actionContext);

            return contextAccessor.Object;
        }

        private static ServiceCollection GetServiceCollection()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddInstance<ILoggerFactory>(new NullLoggerFactory());

            var routeOptions = new RouteOptions();
            var accessor = new Mock<IOptions<RouteOptions>>();
            accessor
                .SetupGet(options => options.Options)
                .Returns(routeOptions);

            serviceCollection.AddInstance<IOptions<RouteOptions>>(accessor.Object);

            serviceCollection.AddInstance<IInlineConstraintResolver>(
                new DefaultInlineConstraintResolver(accessor.Object));

            return serviceCollection;
        }

        private class MockUrlHelper : IUrlHelper
        {
            private readonly string _routeName;
            private readonly string _url;

            public MockUrlHelper(string url, string routeName)
            {
                _routeName = routeName;
                _url = url;
            }

            public object RouteValues { get; private set; }

            public string Action(UrlActionContext actionContext)
            {
                throw new NotImplementedException();
            }

            public string Content(string contentPath)
            {
                throw new NotImplementedException();
            }

            public bool IsLocalUrl(string url)
            {
                throw new NotImplementedException();
            }

            public string Link(string routeName, object values)
            {
                throw new NotImplementedException();
            }

            public string RouteUrl(UrlRouteContext routeContext)
            {
                Assert.Equal(_routeName, routeContext.RouteName);
                Assert.Null(routeContext.Protocol);
                Assert.Null(routeContext.Host);
                Assert.Null(routeContext.Fragment);

                RouteValues = routeContext.Values;

                return _url;
            }
        }

        private class TestableRemoteAttribute : RemoteAttribute
        {
            public TestableRemoteAttribute(string routeName)
                : base(routeName)
            {
            }

            public TestableRemoteAttribute(string action, string controller)
                : base(action, controller)
            {
            }

            public TestableRemoteAttribute(string action, string controller, string areaName)
                : base(action, controller, areaName)
            {
            }

            public new string RouteName
            {
                get
                {
                    return base.RouteName;
                }
            }

            public new RouteValueDictionary RouteData
            {
                get
                {
                    return base.RouteData;
                }
            }
        }
    }
}
