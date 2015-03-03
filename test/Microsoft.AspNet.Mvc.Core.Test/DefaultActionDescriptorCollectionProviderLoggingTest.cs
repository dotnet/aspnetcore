// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc.ApplicationModels;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.Framework.Logging;
using Xunit;

namespace Microsoft.AspNet.Mvc.Logging
{
    public class DefaultActionDescriptorCollectionProviderLoggingTest
    {
        [Fact]
        public void ControllerDiscovery()
        {
            // Arrange
            var sink = new TestSink();
            var loggerFactory = new TestLoggerFactory(sink);

            // Act
            var provider = GetProvider(
                loggerFactory, 
                typeof(SimpleController).GetTypeInfo(),
                typeof(BasicController).GetTypeInfo());
            provider.GetDescriptors();

            // Assert
            // 2 controllers
            Assert.Equal(2, sink.Writes.Count);

            var controllerModelValues = Assert.IsType<ControllerModelValues>(sink.Writes[0].State);
            Assert.NotNull(controllerModelValues);
            Assert.Equal("Simple", controllerModelValues.ControllerName);
            Assert.Equal(typeof(SimpleController), controllerModelValues.ControllerType);
            Assert.Single(controllerModelValues.Actions);
            Assert.Empty(controllerModelValues.AttributeRoutes);
            Assert.Empty(controllerModelValues.RouteConstraints);
            Assert.Empty(controllerModelValues.Attributes);
            Assert.Empty(controllerModelValues.Filters);

            controllerModelValues = Assert.IsType<ControllerModelValues>(sink.Writes[1].State);
            Assert.NotNull(controllerModelValues);
            Assert.Equal("Basic", controllerModelValues.ControllerName);
            Assert.Equal(typeof(BasicController), controllerModelValues.ControllerType);
            Assert.Equal(2, controllerModelValues.Actions.Count);
            Assert.Equal("GET", controllerModelValues.Actions[0].HttpMethods.FirstOrDefault());
            Assert.Equal("POST", controllerModelValues.Actions[1].HttpMethods.FirstOrDefault());
            Assert.Empty(controllerModelValues.AttributeRoutes);
            Assert.Empty(controllerModelValues.RouteConstraints);
            Assert.NotEmpty(controllerModelValues.Attributes);
            Assert.Single(controllerModelValues.Filters);
        }

        [Fact]
        public void ActionDiscovery()
        {
            // Arrange
            var sink = new TestSink();
            var loggerFactory = new TestLoggerFactory(sink);

            // Act
            CreateActionDescriptors(loggerFactory, 
                                    typeof(SimpleController).GetTypeInfo(), 
                                    typeof(BasicController).GetTypeInfo());

            // Assert
            // 2 controllers, 3 actions
            Assert.Equal(5, sink.Writes.Count);
            Assert.IsType<ControllerModelValues>(sink.Writes[0].State);
            Assert.IsType<ControllerModelValues>(sink.Writes[1].State);

            var actionDescriptorValues = Assert.IsType<ActionDescriptorValues>(sink.Writes[2].State);
            Assert.NotNull(actionDescriptorValues);
            Assert.Equal("EmptyAction", actionDescriptorValues.Name);
            Assert.Equal("Simple", actionDescriptorValues.ControllerName);
            Assert.Equal(typeof(SimpleController), actionDescriptorValues.ControllerTypeInfo);
            Assert.Null(actionDescriptorValues.AttributeRouteInfo.Name);
            Assert.Null(actionDescriptorValues.ActionConstraints);
            Assert.Empty(actionDescriptorValues.FilterDescriptors);
            Assert.Empty(actionDescriptorValues.Parameters);

            actionDescriptorValues = Assert.IsType<ActionDescriptorValues>(sink.Writes[3].State);
            Assert.NotNull(actionDescriptorValues);
            Assert.Equal("Basic", actionDescriptorValues.Name);
            Assert.Equal("Basic", actionDescriptorValues.ControllerName);
            Assert.Equal(typeof(BasicController), actionDescriptorValues.ControllerTypeInfo);
            Assert.Null(actionDescriptorValues.AttributeRouteInfo.Name);
            Assert.NotEmpty(actionDescriptorValues.ActionConstraints);
            Assert.Equal(2, actionDescriptorValues.FilterDescriptors.Count);
            Assert.Empty(actionDescriptorValues.Parameters);

            actionDescriptorValues = Assert.IsType<ActionDescriptorValues>(sink.Writes[4].State);
            Assert.NotNull(actionDescriptorValues);
            Assert.Equal("Basic", actionDescriptorValues.Name);
            Assert.Equal("Basic", actionDescriptorValues.ControllerName);
            Assert.Equal(typeof(BasicController), actionDescriptorValues.ControllerTypeInfo);
            Assert.Null(actionDescriptorValues.AttributeRouteInfo.Name);
            Assert.NotEmpty(actionDescriptorValues.ActionConstraints);
            Assert.Single(actionDescriptorValues.FilterDescriptors);
            Assert.Single(actionDescriptorValues.RouteConstraints);
            Assert.Single(actionDescriptorValues.Parameters);
        }

        private void CreateActionDescriptors(ILoggerFactory loggerFactory, params TypeInfo[] controllerTypeInfo)
        {
            var actionDescriptorProvider = GetProvider(loggerFactory, controllerTypeInfo);

            // service container does not work quite like our built in Depenency Injection container.
            var serviceContainer = new ServiceContainer();
            var list = new List<IActionDescriptorProvider>()
            {
                actionDescriptorProvider,
            };

            serviceContainer.AddService(typeof(IEnumerable<IActionDescriptorProvider>), list);

            var actionCollectionDescriptorProvider = new DefaultActionDescriptorsCollectionProvider(serviceContainer, loggerFactory);
            var descriptors = actionCollectionDescriptorProvider.ActionDescriptors;
        }

        private ControllerActionDescriptorProvider GetProvider(
            ILoggerFactory loggerFactory, params TypeInfo[] controllerTypeInfo)
        {
            var controllerTypeProvider = new FixedSetControllerTypeProvider(controllerTypeInfo);
            var modelBuilder = new DefaultControllerModelBuilder(new DefaultActionModelBuilder(null),
                                                                 loggerFactory,
                                                                 null);

            var provider = new ControllerActionDescriptorProvider(
                controllerTypeProvider,
                modelBuilder,
                new TestGlobalFilterProvider(),
                new MockMvcOptionsAccessor(),
                loggerFactory);

            return provider;
        }

        private class SimpleController
        {
            public void EmptyAction() { }
        }

        [Authorize]
        private class BasicController
        {
            [HttpGet]
            [AllowAnonymous]
            public void Basic() { }

            [HttpPost]
            [Route("/Basic")]
            public void Basic(int id) { }
        }
    }
}