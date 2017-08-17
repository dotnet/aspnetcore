// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.WebApiCompatShim;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace System.Web.Http
{
    public class ApiControllerActionDiscoveryTest
    {
        [Fact]
        public void GetActions_ApiControllerWithControllerSuffix_IsController()
        {
            // Arrange
            var provider = CreateProvider();

            // Act
            var context = new ActionDescriptorProviderContext();
            Invoke(provider, context);

            var results = context.Results.Cast<ControllerActionDescriptor>();

            // Assert
            var controllerType = typeof(TestControllers.ProductsController).GetTypeInfo();
            var actions = results.Where(ad => ad.ControllerTypeInfo == controllerType).ToArray();

            Assert.NotEmpty(actions);
        }

        [Fact]
        public void GetActions_ApiControllerWithoutControllerSuffix_IsNotController()
        {
            // Arrange
            var provider = CreateProvider();

            // Act
            var context = new ActionDescriptorProviderContext();
            Invoke(provider, context);

            var results = context.Results.Cast<ControllerActionDescriptor>();

            // Assert
            var controllerType = typeof(TestControllers.Blog).GetTypeInfo();
            var actions = results.Where(ad => ad.ControllerTypeInfo == controllerType);

            Assert.Equal(2, actions.Count());
        }

        [Fact]
        public void GetActions_CreatesNamedAndUnnamedAction()
        {
            // Arrange
            var provider = CreateProvider();

            // Act
            var context = new ActionDescriptorProviderContext();
            Invoke(provider, context);

            var results = context.Results.Cast<ControllerActionDescriptor>();

            // Assert
            var controllerType = typeof(TestControllers.StoreController).GetTypeInfo();
            var actions = results
                .Where(ad => ad.ControllerTypeInfo == controllerType)
                .Where(ad => ad.MethodInfo.Name == "GetAll")
                .ToArray();

            Assert.Equal(2, actions.Length);

            var action = Assert.Single(
                actions,
                a => a.RouteValues.Any(rc => rc.Key == "action" && rc.Value == "GetAll"));
            Assert.Equal(
                new string[] { "GET" },
                Assert.Single(action.ActionConstraints.OfType<HttpMethodActionConstraint>()).HttpMethods);

            action = Assert.Single(
                actions,
                a => a.RouteValues.Any(rc => rc.Key == "action" && string.IsNullOrEmpty(rc.Value)));
            Assert.Equal(
                new string[] { "GET" },
                Assert.Single(action.ActionConstraints.OfType<HttpMethodActionConstraint>()).HttpMethods);
        }

        [Fact]
        public void GetActions_CreatesNamedAndUnnamedAction_DefaultVerbIsPost()
        {
            // Arrange
            var provider = CreateProvider();

            // Act
            var context = new ActionDescriptorProviderContext();
            Invoke(provider, context);

            var results = context.Results.Cast<ControllerActionDescriptor>();

            // Assert
            var controllerType = typeof(TestControllers.StoreController).GetTypeInfo();
            var actions = results
                .Where(ad => ad.ControllerTypeInfo == controllerType)
                .Where(ad => ad.MethodInfo.Name == "Edit")
                .ToArray();

            Assert.Equal(2, actions.Length);

            var action = Assert.Single(
                actions,
                a => a.RouteValues.Any(rc => rc.Key == "action" && rc.Value == "Edit"));
            Assert.Equal(
                new string[] { "POST" },
                Assert.Single(action.ActionConstraints.OfType<HttpMethodActionConstraint>()).HttpMethods);

            action = Assert.Single(
                actions,
                a => a.RouteValues.Any(rc => rc.Key == "action" && string.IsNullOrEmpty(rc.Value)));
            Assert.Equal(
                new string[] { "POST" },
                Assert.Single(action.ActionConstraints.OfType<HttpMethodActionConstraint>()).HttpMethods);
        }

        [Fact]
        public void GetActions_CreatesNamedAndUnnamedAction_RespectsVerbAttribute()
        {
            // Arrange
            var provider = CreateProvider();

            // Act
            var context = new ActionDescriptorProviderContext();
            Invoke(provider, context);

            var results = context.Results.Cast<ControllerActionDescriptor>();

            // Assert
            var controllerType = typeof(TestControllers.StoreController).GetTypeInfo();
            var actions = results
                .Where(ad => ad.ControllerTypeInfo == controllerType)
                .Where(ad => ad.MethodInfo.Name == "Delete")
                .ToArray();

            Assert.Equal(2, actions.Length);

            var action = Assert.Single(
                actions,
                a => a.RouteValues.Any(rc => rc.Key == "action" && rc.Value == "Delete"));
            Assert.Equal(
                new string[] { "PUT" },
                Assert.Single(action.ActionConstraints.OfType<HttpMethodActionConstraint>()).HttpMethods);

            action = Assert.Single(
                actions,
                a => a.RouteValues.Any(rc => rc.Key == "action" && string.IsNullOrEmpty(rc.Value)));
            Assert.Equal(
                new string[] { "PUT" },
                Assert.Single(action.ActionConstraints.OfType<HttpMethodActionConstraint>()).HttpMethods);
        }

        // The method name is used to infer a verb, not the action name
        [Fact]
        public void GetActions_CreatesNamedAndUnnamedAction_VerbBasedOnMethodName()
        {
            // Arrange
            var provider = CreateProvider();

            // Act
            var context = new ActionDescriptorProviderContext();
            Invoke(provider, context);

            var results = context.Results.Cast<ControllerActionDescriptor>();

            // Assert
            var controllerType = typeof(TestControllers.StoreController).GetTypeInfo();
            var actions = results
                .Where(ad => ad.ControllerTypeInfo == controllerType)
                .Where(ad => ad.MethodInfo.Name == "Options")
                .ToArray();

            Assert.Equal(2, actions.Length);

            var action = Assert.Single(
                actions,
                a => a.RouteValues.Any(rc => rc.Key == "action" && rc.Value == "GetOptions"));
            Assert.Equal(
                new string[] { "OPTIONS" },
                Assert.Single(action.ActionConstraints.OfType<HttpMethodActionConstraint>()).HttpMethods);

            action = Assert.Single(
                actions,
                a => a.RouteValues.Any(rc => rc.Key == "action" && string.IsNullOrEmpty(rc.Value)));
            Assert.Equal(
                new string[] { "OPTIONS" },
                Assert.Single(action.ActionConstraints.OfType<HttpMethodActionConstraint>()).HttpMethods);
        }

        [Fact]
        public void GetActions_AllWebApiActionsAreOverloaded()
        {
            // Arrange
            var provider = CreateProvider();

            // Act
            var context = new ActionDescriptorProviderContext();
            Invoke(provider, context);

            var results = context.Results.Cast<ControllerActionDescriptor>();

            // Assert
            var controllerType = typeof(TestControllers.StoreController).GetTypeInfo();
            var actions = results
                .Where(ad => ad.ControllerTypeInfo == controllerType)
                .ToArray();

            Assert.NotEmpty(actions);
            foreach (var action in actions)
            {
                Assert.Single(action.ActionConstraints, c => c is OverloadActionConstraint);
            }
        }

        [Fact]
        public void GetActions_AllWebApiActionsAreInWebApiArea()
        {
            // Arrange
            var provider = CreateProvider();

            // Act
            var context = new ActionDescriptorProviderContext();
            Invoke(provider, context);

            var results = context.Results.Cast<ControllerActionDescriptor>();

            // Assert
            var controllerType = typeof(TestControllers.StoreController).GetTypeInfo();
            var actions = results
                .Where(ad => ad.ControllerTypeInfo == controllerType)
                .ToArray();

            Assert.NotEmpty(actions);
            foreach (var action in actions)
            {
                Assert.Single(action.RouteValues, c => c.Key == "area" && c.Value== "api");
            }
        }

        [Fact]
        public void GetActions_Parameters_SimpleTypeFromUriByDefault()
        {
            // Arrange
            var provider = CreateProvider();

            // Act
            var context = new ActionDescriptorProviderContext();
            Invoke(provider, context);

            var results = context.Results.Cast<ControllerActionDescriptor>();

            // Assert
            var controllerType = typeof(TestControllers.EmployeesController).GetTypeInfo();
            var actions = results
                .Where(ad => ad.ControllerTypeInfo == controllerType)
                .Where(ad => ad.ActionName == "Get")
                .ToArray();

            Assert.NotEmpty(actions);
            foreach (var action in actions)
            {
                var parameter = Assert.Single(action.Parameters);
                Assert.Equal((new FromUriAttribute()).BindingSource, parameter.BindingInfo.BindingSource);
                var optionalParameters = (HashSet<string>)action.Properties["OptionalParameters"];
                Assert.DoesNotContain(parameter.Name, optionalParameters);
            }
        }

        [Fact]
        public void GetActions_Parameters_ComplexTypeFromBodyByDefault()
        {
            // Arrange
            var provider = CreateProvider();

            // Act
            var context = new ActionDescriptorProviderContext();
            Invoke(provider, context);

            var results = context.Results.Cast<ControllerActionDescriptor>();

            // Assert
            var controllerType = typeof(TestControllers.EmployeesController).GetTypeInfo();
            var actions = results
                .Where(ad => ad.ControllerTypeInfo == controllerType)
                .Where(ad => ad.ActionName == "Put")
                .ToArray();

            Assert.NotEmpty(actions);
            foreach (var action in actions)
            {
                var parameter = Assert.Single(action.Parameters);
                Assert.Equal(BindingSource.Body, parameter.BindingInfo.BindingSource);
            }
        }

        [Fact]
        public void GetActions_Parameters_WithBindingSource()
        {
            // Arrange
            var provider = CreateProvider();

            // Act
            var context = new ActionDescriptorProviderContext();
            Invoke(provider, context);

            var results = context.Results.Cast<ControllerActionDescriptor>();

            // Assert
            var controllerType = typeof(TestControllers.EmployeesController).GetTypeInfo();
            var actions = results
                .Where(ad => ad.ControllerTypeInfo == controllerType)
                .Where(ad => ad.ActionName == "Post")
                .ToArray();

            Assert.NotEmpty(actions);
            foreach (var action in actions)
            {
                var parameter = Assert.Single(action.Parameters);
                Assert.Null(parameter.BindingInfo.BindingSource);
            }
        }

        [Theory]
        [InlineData(nameof(TestControllers.EventsController.GetWithId))]
        [InlineData(nameof(TestControllers.EventsController.GetWithEmployee))]
        public void GetActions_Parameters_ImplicitOptional(string name)
        {
            // Arrange
            var provider = CreateProvider();

            // Act
            var context = new ActionDescriptorProviderContext();
            Invoke(provider, context);

            var results = context.Results.Cast<ControllerActionDescriptor>();

            // Assert
            var controllerType = typeof(TestControllers.EventsController).GetTypeInfo();
            var actions = results
                .Where(ad => ad.ControllerTypeInfo == controllerType)
                .Where(ad => ad.ActionName == name)
                .ToArray();

            Assert.NotEmpty(actions);
            foreach (var action in actions)
            {
                var parameter = Assert.Single(action.Parameters);
                Assert.Equal((new FromUriAttribute()).BindingSource, parameter.BindingInfo.BindingSource);
                var optionalParameters = (HashSet<string>)action.Properties["OptionalParameters"];
                Assert.Contains(parameter.Name, optionalParameters);
            }
        }

        private ControllerActionDescriptorProvider CreateProvider()
        {
            var manager = GetApplicationManager(GetType().GetTypeInfo().Assembly.DefinedTypes.ToArray());

            var options = new MvcOptions();

            var setup = new WebApiCompatShimOptionsSetup();
            setup.Configure(options);

            var optionsAccessor = new Mock<IOptions<MvcOptions>>();
            optionsAccessor
                .SetupGet(o => o.Value)
                .Returns(options);

            var authorizationOptionsAccessor = new Mock<IOptions<AuthorizationOptions>>();
            authorizationOptionsAccessor
                .SetupGet(o => o.Value)
                .Returns(new AuthorizationOptions());

            var modelProvider = new DefaultApplicationModelProvider(optionsAccessor.Object);

            var provider = new ControllerActionDescriptorProvider(
                manager,
                new[] { modelProvider },
                optionsAccessor.Object);

            return provider;
        }

        private void Invoke(ControllerActionDescriptorProvider provider, ActionDescriptorProviderContext context)
        {
            provider.OnProvidersExecuting(context);
            provider.OnProvidersExecuted(context);
        }

        private static ApplicationPartManager GetApplicationManager(params TypeInfo[] controllerTypes)
        {
            var manager = new ApplicationPartManager();
            manager.ApplicationParts.Add(new TestPart(controllerTypes));
            manager.FeatureProviders.Add(new TestProvider());
            manager.FeatureProviders.Add(new NamespaceFilteredControllersFeatureProvider());
            return manager;
        }

        private class TestPart : ApplicationPart, IApplicationPartTypeProvider
        {
            public TestPart(IEnumerable<TypeInfo> types)
            {
                Types = types;
            }

            public override string Name => "Test";

            public IEnumerable<TypeInfo> Types { get; }
        }

        private class TestProvider : IApplicationFeatureProvider<ControllerFeature>
        {
            public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
            {
                foreach (var type in parts.OfType<IApplicationPartTypeProvider>().SelectMany(t => t.Types))
                {
                    feature.Controllers.Add(type);
                }
            }
        }

        private class NamespaceFilteredControllersFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
        {
            public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
            {
                var controllers = feature.Controllers.ToList();
                foreach (var controller in controllers)
                {
                    if (controller.Namespace != "System.Web.Http.TestControllers")
                    {
                        feature.Controllers.Remove(controller);
                    }
                }
            }
        }
    }
}

// These need to be public top-level classes to test discovery end-to-end. Don't reuse
// these outside of this test.
namespace System.Web.Http.TestControllers
{
    public class ProductsController : ApiController
    {
        public IActionResult GetAll()
        {
            return null;
        }
    }

    // Not a controller, because there's no controller suffix
    public class Blog : ApiController
    {
        public IActionResult GetBlogPosts()
        {
            return null;
        }
    }

    public class StoreController : ApiController
    {
        public IActionResult GetAll()
        {
            return null;
        }

        public IActionResult Edit(int id)
        {
            return null;
        }

        [HttpPut]
        public IActionResult Delete(int id)
        {
            return null;
        }

        [ActionName("GetOptions")]
        public IActionResult Options()
        {
            return null;
        }
    }

    public class EmployeesController : ApiController
    {
        public IActionResult Get(int id)
        {
            return null;
        }

        public IActionResult Put(Employee employee)
        {
            return null;
        }

        public IActionResult Post([ModelBinder] Employee employee)
        {
            return null;
        }
    }

    public class Employee
    {
    }

    public class EventsController : ApiController
    {
        public IActionResult GetWithId(int id = 0)
        {
            return null;
        }

        public IActionResult GetWithEmployee([FromUri] Employee e = null)
        {
            return null;
        }
    }
}
