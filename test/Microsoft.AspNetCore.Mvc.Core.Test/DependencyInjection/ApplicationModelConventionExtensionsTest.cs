// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Internal;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection
{
    public class ApplicationModelConventionExtensionsTest
    {
        [Fact]
        public void DefaultActionModelConvention_AppliesToAllActionsInApp()
        {
            // Arrange
            var app = new ApplicationModel();
            app.Controllers.Add(new ControllerModel(typeof(HelloController).GetTypeInfo(), new List<object>()));
            app.Controllers.Add(new ControllerModel(typeof(WorldController).GetTypeInfo(), new List<object>()));

            var options = new MvcOptions();
            options.Conventions.Add(new SimpleActionConvention());

            // Act
            options.Conventions[0].Apply(app);

            // Assert
            foreach (var controller in app.Controllers)
            {
                foreach (var action in controller.Actions)
                {
                    Assert.True(action.Properties.ContainsKey("TestProperty"));
                }
            }
        }

        [Fact]
        public void DefaultControllerModelConvention_AppliesToAllControllers()
        {
            // Arrange
            var options = new MvcOptions();
            var app = new ApplicationModel();
            app.Controllers.Add(new ControllerModel(typeof(HelloController).GetTypeInfo(), new List<object>()));
            app.Controllers.Add(new ControllerModel(typeof(WorldController).GetTypeInfo(), new List<object>()));
            options.Conventions.Add(new SimpleControllerConvention());

            // Act
            options.Conventions[0].Apply(app);

            // Assert
            foreach (var controller in app.Controllers)
            {
                Assert.True(controller.Properties.ContainsKey("TestProperty"));
            }
        }

        private class HelloController
        {
            public string GetHello()
            {
                return "Hello";
            }
        }

        private class WorldController
        {
            public string GetWorld()
            {
                return "World!";
            }
        }

        private class SimpleActionConvention : IActionModelConvention
        {
            public void Apply(ActionModel action)
            {
                action.Properties.Add("TestProperty", "TestValue");
            }
        }

        private class SimpleControllerConvention : IControllerModelConvention
        {
            public void Apply(ControllerModel controller)
            {
                controller.Properties.Add("TestProperty", "TestValue");
            }
        }
    }
}