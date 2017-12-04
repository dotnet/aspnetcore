// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Internal;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection
{
    public class ApplicationModelConventionExtensionsTest
    {
        [Fact]
        public void DefaultParameterModelConvention_AppliesToAllParametersInApp()
        {
            // Arrange
            var app = new ApplicationModel();
            app.Controllers.Add(new ControllerModel(typeof(HelloController).GetTypeInfo(), new List<object>()));
            app.Controllers.Add(new ControllerModel(typeof(WorldController).GetTypeInfo(), new List<object>()));

            var options = new MvcOptions();
            options.Conventions.Add(new SimpleParameterConvention());

            // Act
            options.Conventions[0].Apply(app);

            // Assert
            foreach (var controller in app.Controllers)
            {
                foreach (var action in controller.Actions)
                {
                    foreach (var parameter in action.Parameters)
                    {
                        var kvp = Assert.Single(parameter.Properties);
                        Assert.Equal("TestProperty", kvp.Key);
                        Assert.Equal("TestValue", kvp.Value);
                    }
                }
            }
        }

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
                    var kvp = Assert.Single(action.Properties);
                    Assert.Equal("TestProperty", kvp.Key);
                    Assert.Equal("TestValue", kvp.Value);
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
                var kvp = Assert.Single(controller.Properties);
                Assert.Equal("TestProperty", kvp.Key);
                Assert.Equal("TestValue", kvp.Value);
            }
        }

        [Fact]
        public void RemoveType_RemovesAllOfType()
        {
            // Arrange
            var list = new List<IApplicationModelConvention>
            {
                new FooApplicationModelConvention(),
                new BarApplicationModelConvention(),
                new FooApplicationModelConvention()
            };

            // Act
            list.RemoveType(typeof(FooApplicationModelConvention));

            // Assert
            var convention = Assert.Single(list);
            Assert.IsType<BarApplicationModelConvention>(convention);
        }

        [Fact]
        public void ApplicationModelConventions_CopiesControllerModelCollectionOnApply()
        {
            // Arrange
            var applicationModel = new ApplicationModel();
            applicationModel.Controllers.Add(
                new ControllerModel(typeof(HelloController).GetTypeInfo(), new List<object>())
                {
                    Application = applicationModel
                });

            var controllerModelConvention = new ControllerModelCollectionModifyingConvention();
            var conventions = new List<IApplicationModelConvention>();
            conventions.Add(controllerModelConvention);

            // Act & Assert
            ApplicationModelConventions.ApplyConventions(applicationModel, conventions);
        }

        [Fact]
        public void ApplicationModelConventions_CopiesActionModelCollectionOnApply()
        {
            // Arrange
            var controllerType = typeof(HelloController).GetTypeInfo();
            var applicationModel = new ApplicationModel();
            var controllerModel = new ControllerModel(controllerType, new List<object>())
            {
                Application = applicationModel
            };
            controllerModel.Actions.Add(
                new ActionModel(controllerType.GetMethod(nameof(HelloController.GetHello)), new List<object>())
                {
                    Controller = controllerModel
                });
            applicationModel.Controllers.Add(controllerModel);

            var actionModelConvention = new ActionModelCollectionModifyingConvention();
            var conventions = new List<IApplicationModelConvention>();
            conventions.Add(actionModelConvention);

            // Act & Assert
            ApplicationModelConventions.ApplyConventions(applicationModel, conventions);
        }

        [Fact]
        public void ApplicationModelConventions_CopiesParameterModelCollectionOnApply()
        {
            // Arrange
            var controllerType = typeof(HelloController).GetTypeInfo();
            var app = new ApplicationModel();
            var controllerModel = new ControllerModel(controllerType, new List<object>())
            {
                Application = app
            };
            app.Controllers.Add(controllerModel);
            var actionModel = new ActionModel(controllerType.GetMethod(nameof(HelloController.GetInfo)), new List<object>())
            {
                Controller = controllerModel
            };
            controllerModel.Actions.Add(actionModel);
            var parameterModel = new ParameterModel(
                controllerType.GetMethod(nameof(HelloController.GetInfo)).GetParameters()[0],
                new List<object>())
            {
                Action = actionModel
            };
            actionModel.Parameters.Add(parameterModel);

            var parameterModelConvention = new ParameterModelCollectionModifyingConvention();
            var conventions = new List<IApplicationModelConvention>();
            conventions.Add(parameterModelConvention);

            // Act & Assert
            ApplicationModelConventions.ApplyConventions(app, conventions);
        }

        [Fact]
        public void GenericRemoveType_RemovesAllOfType()
        {
            // Arrange
            var list = new List<IApplicationModelConvention>
            {
                new FooApplicationModelConvention(),
                new BarApplicationModelConvention(),
                new FooApplicationModelConvention()
            };

            // Act
            list.RemoveType<FooApplicationModelConvention>();

            // Assert
            var convention = Assert.Single(list);
            Assert.IsType<BarApplicationModelConvention>(convention);
        }

        private class FooApplicationModelConvention : IApplicationModelConvention
        {
            public void Apply(ApplicationModel application)
            {
                throw new NotImplementedException();
            }
        }

        private class BarApplicationModelConvention : IApplicationModelConvention
        {
            public void Apply(ApplicationModel application)
            {
                throw new NotImplementedException();
            }
        }

        private class HelloController
        {
            public string GetHello()
            {
                return "Hello";
            }

            public string GetInfo(int id)
            {
                return "GetInfo(int id)";
            }
        }

        private class WorldController
        {
            public string GetWorld()
            {
                return "World!";
            }
        }

        private class SimpleParameterConvention : IParameterModelConvention
        {
            public void Apply(ParameterModel parameter)
            {
                parameter.Properties.Add("TestProperty", "TestValue");
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

        private class ControllerModelCollectionModifyingConvention : IControllerModelConvention
        {
            public void Apply(ControllerModel controller)
            {
                controller.Application.Controllers.Remove(controller);
            }
        }

        private class TestApplicationModelConvention : IApplicationModelConvention
        {
            public void Apply(ApplicationModel application)
            {
                application.Controllers.RemoveAt(0);
            }
        }

        private class ActionModelCollectionModifyingConvention : IActionModelConvention
        {
            public void Apply(ActionModel action)
            {
                action.Controller.Actions.Remove(action);
            }
        }

        private class ParameterModelCollectionModifyingConvention : IParameterModelConvention
        {
            public void Apply(ParameterModel parameter)
            {
                parameter.Action.Parameters.Remove(parameter);
            }
        }
    }
}