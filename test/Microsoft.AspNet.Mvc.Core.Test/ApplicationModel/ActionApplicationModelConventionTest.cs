// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.Framework.Internal;
using Xunit;

namespace Microsoft.AspNet.Mvc.ApplicationModels
{
    public class ActionApplicationModelConventionTest
    {
        [Fact]
        public void DefaultActionModelConvention_AppliesToAllActionsInApp()
        {
            // Arrange
            var options = new MvcOptions();
            var app = new ApplicationModel();
            app.Controllers.Add(new ControllerModel(typeof(HelloController).GetTypeInfo(), new List<object>()));
            app.Controllers.Add(new ControllerModel(typeof(WorldController).GetTypeInfo(), new List<object>()));
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

        private class HelloController : Controller
        {
            public string GetHello()
            {
                return "Hello";
            }
        }

        private class WorldController : Controller
        {
            public string GetWorld()
            {
                return "World!";
            }
        }

        private class SimpleActionConvention : IActionModelConvention
        {
            public void Apply([NotNull] ActionModel action)
            {
                action.Properties.Add("TestProperty", "TestValue");
            }
        }
    }
}