// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Microsoft.Extensions.DependencyInjection;

public class ApplicationModelConventionExtensionsTest
{
    [Fact]
    public void DefaultParameterModelConvention_AppliesToAllParametersInApp()
    {
        // Arrange
        var app = new ApplicationModel();
        var controllerType = typeof(HelloController);
        var controllerModel = new ControllerModel(controllerType.GetTypeInfo(), Array.Empty<object>());
        app.Controllers.Add(controllerModel);

        var actionModel = new ActionModel(controllerType.GetMethod(nameof(HelloController.GetInfo)), Array.Empty<object>());
        controllerModel.Actions.Add(actionModel);
        var parameterModel = new ParameterModel(
            controllerType.GetMethod(nameof(HelloController.GetInfo)).GetParameters()[0],
            Array.Empty<object>());
        actionModel.Parameters.Add(parameterModel);

        var options = new MvcOptions();
        options.Conventions.Add(new SimpleParameterConvention());

        // Act
        options.Conventions[0].Apply(app);

        // Assert
        var kvp = Assert.Single(parameterModel.Properties);
        Assert.Equal("TestProperty", kvp.Key);
        Assert.Equal("TestValue", kvp.Value);
    }

    [Fact]
    public void DefaultActionModelConvention_AppliesToAllActionsInApp()
    {
        // Arrange
        var app = new ApplicationModel();
        var controllerType1 = typeof(HelloController).GetTypeInfo();
        var actionMethod1 = controllerType1.GetMethod(nameof(HelloController.GetHello));
        var controllerModel1 = new ControllerModel(controllerType1, Array.Empty<object>())
        {
            Actions =
                {
                    new ActionModel(actionMethod1, Array.Empty<object>()),
                }
        };

        var controllerType2 = typeof(WorldController).GetTypeInfo();
        var actionMethod2 = controllerType2.GetMethod(nameof(WorldController.GetWorld));
        var controllerModel2 = new ControllerModel(controllerType2, Array.Empty<object>())
        {
            Actions =
                {
                    new ActionModel(actionMethod2, Array.Empty<object>()),
                },
        };

        app.Controllers.Add(controllerModel1);
        app.Controllers.Add(controllerModel2);

        var options = new MvcOptions();
        options.Conventions.Add(new SimpleActionConvention());

        // Act
        options.Conventions[0].Apply(app);

        // Assert
        var kvp = Assert.Single(controllerModel1.Actions[0].Properties);
        Assert.Equal("TestProperty", kvp.Key);
        Assert.Equal("TestValue", kvp.Value);

        kvp = Assert.Single(controllerModel2.Actions[0].Properties);
        Assert.Equal("TestProperty", kvp.Key);
        Assert.Equal("TestValue", kvp.Value);
    }

    [Fact]
    public void AddedParameterConvention_AppliesToAllPropertiesAndParameters()
    {
        // Arrange
        var app = new ApplicationModel();
        var controllerType1 = typeof(HelloController).GetTypeInfo();
        var parameterModel1 = new ParameterModel(
            controllerType1.GetMethod(nameof(HelloController.GetInfo)).GetParameters()[0],
            Array.Empty<object>());
        var actionMethod1 = controllerType1.GetMethod(nameof(HelloController.GetInfo));
        var property1 = controllerType1.GetProperty(nameof(HelloController.Property1));
        var controllerModel1 = new ControllerModel(controllerType1, Array.Empty<object>())
        {
            ControllerProperties =
                {
                    new PropertyModel(property1, Array.Empty<object>()),
                },
            Actions =
                {
                    new ActionModel(actionMethod1, Array.Empty<object>())
                    {
                        Parameters =
                        {
                            parameterModel1,
                        }
                    }
                }
        };

        var controllerType2 = typeof(WorldController).GetTypeInfo();
        var property2 = controllerType2.GetProperty(nameof(WorldController.Property2));
        var controllerModel2 = new ControllerModel(controllerType2, Array.Empty<object>())
        {
            ControllerProperties =
                {
                    new PropertyModel(property2, Array.Empty<object>()),
                },
        };

        app.Controllers.Add(controllerModel1);
        app.Controllers.Add(controllerModel2);

        var options = new MvcOptions();
        var convention = new SimplePropertyConvention();
        options.Conventions.Add(convention);

        // Act
        ApplicationModelConventions.ApplyConventions(app, options.Conventions);

        // Assert
        var kvp = Assert.Single(controllerModel1.ControllerProperties[0].Properties);
        Assert.Equal("TestProperty", kvp.Key);
        Assert.Equal("TestValue", kvp.Value);

        kvp = Assert.Single(controllerModel2.ControllerProperties[0].Properties);
        Assert.Equal("TestProperty", kvp.Key);
        Assert.Equal("TestValue", kvp.Value);

        kvp = Assert.Single(controllerModel1.Actions[0].Parameters[0].Properties);
        Assert.Equal("TestProperty", kvp.Key);
        Assert.Equal("TestValue", kvp.Value);
    }

    [Fact]
    public void DefaultControllerModelConvention_AppliesToAllControllers()
    {
        // Arrange
        var options = new MvcOptions();
        var app = new ApplicationModel();
        app.Controllers.Add(new ControllerModel(typeof(HelloController).GetTypeInfo(), Array.Empty<object>()));
        app.Controllers.Add(new ControllerModel(typeof(WorldController).GetTypeInfo(), Array.Empty<object>()));
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
            new ControllerModel(typeof(HelloController).GetTypeInfo(), Array.Empty<object>())
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
    public void ApplicationModelConventions_CopiesControllerModelCollectionOnApply_WhenRegisteredAsAnAttribute()
    {
        // Arrange
        var controllerModelConvention = new ControllerModelCollectionModifyingConvention();
        var applicationModel = new ApplicationModel();
        applicationModel.Controllers.Add(
            new ControllerModel(typeof(HelloController).GetTypeInfo(), new[] { controllerModelConvention })
            {
                Application = applicationModel
            });

        var conventions = new List<IApplicationModelConvention>();

        // Act & Assert
        ApplicationModelConventions.ApplyConventions(applicationModel, conventions);
    }

    [Fact]
    public void ApplicationModelConventions_CopiesActionModelCollectionOnApply()
    {
        // Arrange
        var controllerType = typeof(HelloController).GetTypeInfo();
        var applicationModel = new ApplicationModel();
        var controllerModel = new ControllerModel(controllerType, Array.Empty<object>())
        {
            Application = applicationModel
        };
        controllerModel.Actions.Add(
            new ActionModel(controllerType.GetMethod(nameof(HelloController.GetHello)), Array.Empty<object>())
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
    public void ApplicationModelConventions_CopiesPropertyModelCollectionOnApply()
    {
        // Arrange
        var controllerType = typeof(HelloController).GetTypeInfo();
        var applicationModel = new ApplicationModel();
        var controllerModel = new ControllerModel(controllerType, Array.Empty<object>())
        {
            Application = applicationModel
        };
        controllerModel.ControllerProperties.Add(
            new PropertyModel(controllerType.GetProperty(nameof(HelloController.Property1)), Array.Empty<object>())
            {
                Controller = controllerModel
            });
        applicationModel.Controllers.Add(controllerModel);

        var propertyModelConvention = new ParameterModelBaseConvention();
        var conventions = new List<IApplicationModelConvention>();
        conventions.Add(propertyModelConvention);

        // Act & Assert
        ApplicationModelConventions.ApplyConventions(applicationModel, conventions);
    }

    [Fact]
    public void ApplicationModelConventions_CopiesPropertyModelCollectionOnApply_WhenAppliedViaAttributes()
    {
        // Arrange
        var propertyModelConvention = new ParameterModelBaseConvention();
        var controllerType = typeof(HelloController).GetTypeInfo();
        var applicationModel = new ApplicationModel();
        var controllerModel = new ControllerModel(controllerType, Array.Empty<object>())
        {
            Application = applicationModel
        };
        controllerModel.ControllerProperties.Add(
            new PropertyModel(controllerType.GetProperty(nameof(HelloController.Property1)), new[] { propertyModelConvention })
            {
                Controller = controllerModel
            });
        applicationModel.Controllers.Add(controllerModel);

        var conventions = new List<IApplicationModelConvention>();

        // Act & Assert
        ApplicationModelConventions.ApplyConventions(applicationModel, conventions);
    }

    [Fact]
    public void ApplicationModelConventions_CopiesParameterModelCollectionOnApply()
    {
        // Arrange
        var controllerType = typeof(HelloController).GetTypeInfo();
        var app = new ApplicationModel();
        var controllerModel = new ControllerModel(controllerType, Array.Empty<object>())
        {
            Application = app
        };
        app.Controllers.Add(controllerModel);
        var actionModel = new ActionModel(controllerType.GetMethod(nameof(HelloController.GetInfo)), Array.Empty<object>())
        {
            Controller = controllerModel
        };
        controllerModel.Actions.Add(actionModel);
        var parameterModel = new ParameterModel(
            controllerType.GetMethod(nameof(HelloController.GetInfo)).GetParameters()[0],
            Array.Empty<object>())
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
    public void ApplicationModelConventions_CopiesParameterModelCollectionOnApply_WhenRegisteredViaAttribute()
    {
        // Arrange
        var parameterModelConvention = new ParameterModelCollectionModifyingConvention();
        var controllerType = typeof(HelloController).GetTypeInfo();
        var app = new ApplicationModel();
        var controllerModel = new ControllerModel(controllerType, Array.Empty<object>())
        {
            Application = app
        };
        app.Controllers.Add(controllerModel);
        var actionModel = new ActionModel(controllerType.GetMethod(nameof(HelloController.GetInfo)), Array.Empty<object>())
        {
            Controller = controllerModel
        };
        controllerModel.Actions.Add(actionModel);
        var parameterModel = new ParameterModel(
            controllerType.GetMethod(nameof(HelloController.GetInfo)).GetParameters()[0],
            new[] { parameterModelConvention })
        {
            Action = actionModel
        };
        actionModel.Parameters.Add(parameterModel);

        var conventions = new List<IApplicationModelConvention>();

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
        public string Property1 { get; set; }

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
        public string Property2 { get; set; }

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

    private class SimplePropertyConvention : IParameterModelBaseConvention
    {
        public void Apply(ParameterModelBase action)
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

    private class ParameterModelBaseConvention : IParameterModelBaseConvention
    {
        public void Apply(ParameterModelBase modelBase)
        {
            var property = (PropertyModel)modelBase;
            property.Controller.ControllerProperties.Remove(property);
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
