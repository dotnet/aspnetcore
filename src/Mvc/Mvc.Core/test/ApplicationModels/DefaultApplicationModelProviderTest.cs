// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

public class DefaultApplicationModelProviderTest
{
    private readonly TestApplicationModelProvider Provider = new TestApplicationModelProvider();

    [Fact]
    public void OnProvidersExecuting_AddsGlobalFilters()
    {
        // Arrange
        var options = new MvcOptions()
        {
            Filters =
                {
                    new MyFilterAttribute(),
                },
        };

        var builder = new TestApplicationModelProvider(options, TestModelMetadataProvider.CreateDefaultProvider());
        var context = new ApplicationModelProviderContext(Array.Empty<TypeInfo>());

        // Act
        builder.OnProvidersExecuting(context);

        // Assert
        Assert.Equal(options.Filters.ToArray(), context.Result.Filters);
    }

    [Fact]
    public void OnProvidersExecuting_IncludesAllControllers()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();

        var context = new ApplicationModelProviderContext(new[] { typeof(ModelBinderController).GetTypeInfo(), typeof(ConventionallyRoutedController).GetTypeInfo() });

        // Act
        builder.OnProvidersExecuting(context);

        // Assert
        Assert.Collection(
            context.Result.Controllers.OrderBy(c => c.ControllerType.Name),
            c => Assert.Equal(typeof(ConventionallyRoutedController).GetTypeInfo(), c.ControllerType),
            c => Assert.Equal(typeof(ModelBinderController).GetTypeInfo(), c.ControllerType));
    }

    [Fact]
    public void OnProvidersExecuting_AddsControllerProperties()
    {
        // Arrange
        var builder = new TestApplicationModelProvider(
            new MvcOptions(),
            TestModelMetadataProvider.CreateDefaultProvider());
        var typeInfo = typeof(ModelBinderController).GetTypeInfo();

        var context = new ApplicationModelProviderContext(new[] { typeInfo });

        // Act
        builder.OnProvidersExecuting(context);

        // Assert
        var controllerModel = Assert.Single(context.Result.Controllers);
        Assert.Collection(
            controllerModel.ControllerProperties.OrderBy(p => p.PropertyName),
            property =>
            {
                Assert.Equal(nameof(ModelBinderController.Bound), property.PropertyName);
                Assert.Equal(BindingSource.Query, property.BindingInfo.BindingSource);
                Assert.Same(controllerModel, property.Controller);

                var attribute = Assert.Single(property.Attributes);
                Assert.IsType<FromQueryAttribute>(attribute);
            },
            property =>
            {
                Assert.Equal(nameof(ModelBinderController.FormFile), property.PropertyName);
                Assert.Equal(BindingSource.FormFile, property.BindingInfo.BindingSource);
                Assert.Same(controllerModel, property.Controller);

                Assert.Empty(property.Attributes);
            },
            property =>
            {
                Assert.Equal(nameof(ModelBinderController.Service), property.PropertyName);
                Assert.Equal(BindingSource.Services, property.BindingInfo.BindingSource);
                Assert.Same(controllerModel, property.Controller);

                var attribute = Assert.Single(property.Attributes);
                Assert.IsType<FromServicesAttribute>(attribute);
            },
            property =>
            {
                Assert.Equal(nameof(ModelBinderController.Unbound), property.PropertyName);
                Assert.Null(property.BindingInfo);
                Assert.Same(controllerModel, property.Controller);
            });
    }

    [Fact]
    public void OnProvidersExecuting_ReadsBindingSourceForPropertiesFromModelMetadata()
    {
        // Arrange
        var detailsProvider = new BindingSourceMetadataProvider(typeof(string), BindingSource.Special);
        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider(new[] { detailsProvider });
        var typeInfo = typeof(ModelBinderController).GetTypeInfo();
        var provider = new TestApplicationModelProvider(new MvcOptions(), modelMetadataProvider);

        var context = new ApplicationModelProviderContext(new[] { typeInfo });

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        var controllerModel = Assert.Single(context.Result.Controllers);
        Assert.Collection(
            controllerModel.ControllerProperties.OrderBy(p => p.PropertyName),
            property =>
            {
                Assert.Equal(nameof(ModelBinderController.Bound), property.PropertyName);
                Assert.Equal(BindingSource.Query, property.BindingInfo.BindingSource);
                Assert.Same(controllerModel, property.Controller);

                var attribute = Assert.Single(property.Attributes);
                Assert.IsType<FromQueryAttribute>(attribute);
            },
            property =>
            {
                Assert.Equal(nameof(ModelBinderController.FormFile), property.PropertyName);
                Assert.Equal(BindingSource.FormFile, property.BindingInfo.BindingSource);
                Assert.Same(controllerModel, property.Controller);

                Assert.Empty(property.Attributes);
            },
            property =>
            {
                Assert.Equal(nameof(ModelBinderController.Service), property.PropertyName);
                Assert.Equal(BindingSource.Services, property.BindingInfo.BindingSource);
                Assert.Same(controllerModel, property.Controller);

                var attribute = Assert.Single(property.Attributes);
                Assert.IsType<FromServicesAttribute>(attribute);
            },
            property =>
            {
                Assert.Equal(nameof(ModelBinderController.Unbound), property.PropertyName);
                Assert.Equal(BindingSource.Special, property.BindingInfo.BindingSource);
                Assert.Same(controllerModel, property.Controller);
            });
    }

    [Fact]
    public void OnProvidersExecuting_AddsBindingSources_ForActionParameters()
    {
        // Arrange
        var builder = new TestApplicationModelProvider(
            new MvcOptions(),
            TestModelMetadataProvider.CreateDefaultProvider());
        var typeInfo = typeof(ModelBinderController).GetTypeInfo();

        var context = new ApplicationModelProviderContext(new[] { typeInfo });

        // Act
        builder.OnProvidersExecuting(context);

        // Assert
        var controllerModel = Assert.Single(context.Result.Controllers);
        var action = Assert.Single(controllerModel.Actions, a => a.ActionMethod.Name == nameof(ModelBinderController.PostAction));
        Assert.Collection(
            action.Parameters,
            parameter =>
            {
                Assert.Equal("fromQuery", parameter.ParameterName);
                Assert.Equal(BindingSource.Query, parameter.BindingInfo.BindingSource);
                Assert.Same(action, parameter.Action);

                var attribute = Assert.Single(parameter.Attributes);
                Assert.IsType<FromQueryAttribute>(attribute);
            },
            parameter =>
            {
                Assert.Equal("formFileCollection", parameter.ParameterName);
                Assert.Equal(BindingSource.FormFile, parameter.BindingInfo.BindingSource);
                Assert.Same(action, parameter.Action);

                Assert.Empty(parameter.Attributes);
            },
            parameter =>
            {
                Assert.Equal("unbound", parameter.ParameterName);
                Assert.Null(parameter.BindingInfo);
                Assert.Same(action, parameter.Action);
            });
    }

    [Fact]
    public void OnProvidersExecuting_InfersFormFileSourceForTypesAssignableFromIEnumerableOfFormFiles()
    {
        // Arrange
        var builder = new TestApplicationModelProvider(
            new MvcOptions(),
            TestModelMetadataProvider.CreateDefaultProvider());
        var typeInfo = typeof(ModelBinderController).GetTypeInfo();

        var context = new ApplicationModelProviderContext(new[] { typeInfo });

        // Act
        builder.OnProvidersExecuting(context);

        // Assert
        var controllerModel = Assert.Single(context.Result.Controllers);
        var action = Assert.Single(controllerModel.Actions, a => a.ActionMethod.Name == nameof(ModelBinderController.FormFilesSequences));
        Assert.Collection(
            action.Parameters,
            parameter =>
            {
                Assert.Equal("formFileEnumerable", parameter.ParameterName);
                Assert.Equal(BindingSource.FormFile, parameter.BindingInfo.BindingSource);
            },
            parameter =>
            {
                Assert.Equal("formFileCollection", parameter.ParameterName);
                Assert.Equal(BindingSource.FormFile, parameter.BindingInfo.BindingSource);
            },
            parameter =>
            {
                Assert.Equal("formFileIList", parameter.ParameterName);
                Assert.Equal(BindingSource.FormFile, parameter.BindingInfo.BindingSource);
            },
            parameter =>
            {
                Assert.Equal("formFileList", parameter.ParameterName);
                Assert.Equal(BindingSource.FormFile, parameter.BindingInfo.BindingSource);
            },
            parameter =>
            {
                Assert.Equal("formFileArray", parameter.ParameterName);
                Assert.Equal(BindingSource.FormFile, parameter.BindingInfo.BindingSource);
            });
    }

    [Fact]
    public void OnProvidersExecuting_AddsBindingSources_ForActionParameters_ReadFromModelMetadata()
    {
        // Arrange
        var options = new MvcOptions();
        var detailsProvider = new BindingSourceMetadataProvider(typeof(Guid), BindingSource.Special);
        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider(new[] { detailsProvider });

        var provider = new TestApplicationModelProvider(options, modelMetadataProvider);
        var typeInfo = typeof(ModelBinderController).GetTypeInfo();

        var context = new ApplicationModelProviderContext(new[] { typeInfo });

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        var controllerModel = Assert.Single(context.Result.Controllers);
        var action = Assert.Single(controllerModel.Actions, a => a.ActionName == nameof(ModelBinderController.PostAction1));
        Assert.Collection(
            action.Parameters,
            parameter =>
            {
                Assert.Equal("guid", parameter.ParameterName);
                Assert.Equal(BindingSource.Special, parameter.BindingInfo.BindingSource);
            });
    }

    [Fact]
    public void OnProvidersExecuting_UsesBindingSourceSpecifiedOnParameter()
    {
        // Arrange
        var options = new MvcOptions();
        var detailsProvider = new BindingSourceMetadataProvider(typeof(Guid), BindingSource.Special);
        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider(new[] { detailsProvider });

        var provider = new TestApplicationModelProvider(options, modelMetadataProvider);
        var typeInfo = typeof(ModelBinderController).GetTypeInfo();

        var context = new ApplicationModelProviderContext(new[] { typeInfo });

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        var controllerModel = Assert.Single(context.Result.Controllers);
        var action = Assert.Single(controllerModel.Actions, a => a.ActionName == nameof(ModelBinderController.PostAction2));
        Assert.Collection(
            action.Parameters,
            parameter =>
            {
                Assert.Equal("fromQuery", parameter.ParameterName);
                Assert.Equal(BindingSource.Query, parameter.BindingInfo.BindingSource);
            });
    }

    [Fact]
    public void OnProvidersExecuting_RemovesAsyncSuffix_WhenOptionIsSet()
    {
        // Arrange
        var options = new MvcOptions();
        var provider = new TestApplicationModelProvider(options, new EmptyModelMetadataProvider());
        var typeInfo = typeof(AsyncActionController).GetTypeInfo();
        var methodInfo = typeInfo.GetMethod(nameof(AsyncActionController.GetPersonAsync));

        var context = new ApplicationModelProviderContext(new[] { typeInfo });

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        var controllerModel = Assert.Single(context.Result.Controllers);
        var action = Assert.Single(controllerModel.Actions, a => a.ActionMethod == methodInfo);
        Assert.Equal("GetPerson", action.ActionName);
    }

    [Fact]
    public void OnProvidersExecuting_DoesNotRemoveAsyncSuffix_WhenOptionIsDisabled()
    {
        // Arrange
        var options = new MvcOptions { SuppressAsyncSuffixInActionNames = false };
        var provider = new TestApplicationModelProvider(options, new EmptyModelMetadataProvider());
        var typeInfo = typeof(AsyncActionController).GetTypeInfo();
        var methodInfo = typeInfo.GetMethod(nameof(AsyncActionController.GetPersonAsync));

        var context = new ApplicationModelProviderContext(new[] { typeInfo });

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        var controllerModel = Assert.Single(context.Result.Controllers);
        var action = Assert.Single(controllerModel.Actions, a => a.ActionMethod == methodInfo);
        Assert.Equal(nameof(AsyncActionController.GetPersonAsync), action.ActionName);
    }

    [Fact]
    public void OnProvidersExecuting_DoesNotRemoveAsyncSuffix_WhenActionNameIsSpecifiedUsingActionNameAttribute()
    {
        // Arrange
        var options = new MvcOptions();
        var provider = new TestApplicationModelProvider(options, new EmptyModelMetadataProvider());
        var typeInfo = typeof(AsyncActionController).GetTypeInfo();
        var methodInfo = typeInfo.GetMethod(nameof(AsyncActionController.GetAddressAsync));

        var context = new ApplicationModelProviderContext(new[] { typeInfo });

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        var controllerModel = Assert.Single(context.Result.Controllers);
        var action = Assert.Single(controllerModel.Actions, a => a.ActionMethod == methodInfo);
        Assert.Equal("GetRealAddressAsync", action.ActionName);
    }

    [Fact]
    public void CreateControllerModel_DerivedFromControllerClass_HasFilter()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = typeof(StoreController).GetTypeInfo();

        // Act
        var model = DefaultApplicationModelProvider.CreateControllerModel(typeInfo);

        // Assert
        var filter = Assert.Single(model.Filters);
        Assert.IsType<ControllerActionFilter>(filter);
    }

    // This class has a filter attribute, but doesn't implement any filter interfaces,
    // so ControllerFilter is not present.
    [Fact]
    public void CreateControllerModel_ClassWithoutFilterInterfaces_HasNoControllerFilter()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = typeof(NoFiltersController).GetTypeInfo();

        // Act
        var model = DefaultApplicationModelProvider.CreateControllerModel(typeInfo);

        // Assert
        var filter = Assert.Single(model.Filters);
        Assert.IsType<MyFilterAttribute>(filter);
    }

    [Fact]
    public void CreateControllerModel_ClassWithFilterInterfaces_HasFilter()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = typeof(SomeFiltersController).GetTypeInfo();

        // Act
        var model = DefaultApplicationModelProvider.CreateControllerModel(typeInfo);

        // Assert
        Assert.Single(model.Filters, f => f is ControllerActionFilter);
        Assert.Single(model.Filters, f => f is ControllerResultFilter);
    }

    [Fact]
    public void CreateControllerModel_ClassWithFilterInterfaces_UnsupportedType()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = typeof(UnsupportedFiltersController).GetTypeInfo();

        // Act
        var model = DefaultApplicationModelProvider.CreateControllerModel(typeInfo);

        // Assert
        Assert.Empty(model.Filters);
    }

    [Fact]
    public void CreateControllerModel_ClassWithInheritedRoutes()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = typeof(DerivedClassInheritingRoutesController).GetTypeInfo();

        // Act
        var model = DefaultApplicationModelProvider.CreateControllerModel(typeInfo);

        // Assert
        var attributeRoutes = GetAttributeRoutes(model.Selectors);
        Assert.Equal(2, attributeRoutes.Count);
        Assert.Equal(2, model.Attributes.Count);

        var route = Assert.Single(attributeRoutes, r => r.Template == "A");
        Assert.Contains(route.Attribute, model.Attributes);

        route = Assert.Single(attributeRoutes, r => r.Template == "B");
        Assert.Contains(route.Attribute, model.Attributes);
    }

    [Fact]
    public void CreateControllerModel_ClassWithHiddenInheritedRoutes()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = typeof(DerivedClassHidingRoutesController).GetTypeInfo();

        // Act
        var model = DefaultApplicationModelProvider.CreateControllerModel(typeInfo);

        // Assert
        var attributeRoutes = GetAttributeRoutes(model.Selectors);
        Assert.Equal(2, attributeRoutes.Count);
        Assert.Equal(2, model.Attributes.Count);

        var route = Assert.Single(attributeRoutes, r => r.Template == "C");
        Assert.Contains(route.Attribute, model.Attributes);

        route = Assert.Single(attributeRoutes, r => r.Template == "D");
        Assert.Contains(route.Attribute, model.Attributes);
    }

    [Theory]
    [InlineData("GetFromDerived", true)]
    [InlineData("NewMethod", true)] // "NewMethod" is a public method declared with keyword "new".
    [InlineData("GetFromBase", true)]
    public void IsAction_WithInheritedMethods(string methodName, bool expected)
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var method = typeof(DerivedController).GetMethod(methodName);
        Assert.NotNull(method);

        // Act
        var isValid = DefaultApplicationModelProvider.IsAction(typeof(DerivedController).GetTypeInfo(), method);

        // Assert
        Assert.Equal(expected, isValid);
    }

    [Fact]
    public void IsAction_OverridenMethodControllerClass()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var method = typeof(BaseController).GetMethod(nameof(BaseController.Redirect));
        Assert.NotNull(method);

        // Act
        var isValid = DefaultApplicationModelProvider.IsAction(typeof(BaseController).GetTypeInfo(), method);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsAction_PrivateMethod_FromUserDefinedController()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var method = typeof(DerivedController).GetMethod(
            "PrivateMethod",
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(method);

        // Act
        var isValid = DefaultApplicationModelProvider.IsAction(typeof(DerivedController).GetTypeInfo(), method);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsAction_OperatorOverloadingMethod_FromOperatorOverloadingController()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var method = typeof(OperatorOverloadingController).GetMethod("op_Addition");
        Assert.NotNull(method);
        Assert.True(method.IsSpecialName);

        // Act
        var isValid = DefaultApplicationModelProvider.IsAction(typeof(OperatorOverloadingController).GetTypeInfo(), method);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsAction_GenericMethod_FromUserDefinedController()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var method = typeof(DerivedController).GetMethod("GenericMethod");
        Assert.NotNull(method);

        // Act
        var isValid = DefaultApplicationModelProvider.IsAction(typeof(DerivedController).GetTypeInfo(), method);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsAction_OverridenNonActionMethod()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var method = typeof(DerivedController).GetMethod("OverridenNonActionMethod");
        Assert.NotNull(method);

        // Act
        var isValid = DefaultApplicationModelProvider.IsAction(typeof(DerivedController).GetTypeInfo(), method);

        // Assert
        Assert.False(isValid);
    }

    [Theory]
    [InlineData("Equals")]
    [InlineData("GetHashCode")]
    [InlineData("MemberwiseClone")]
    [InlineData("ToString")]
    public void IsAction_OverriddenMethodsFromObjectClass(string methodName)
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var method = typeof(DerivedController).GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(method);

        // Act
        var isValid = DefaultApplicationModelProvider.IsAction(typeof(DerivedController).GetTypeInfo(), method);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsAction_DerivedControllerIDisposableDisposeMethod()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = typeof(DerivedController).GetTypeInfo();
        var methodInfo =
            typeInfo.GetRuntimeInterfaceMap(typeof(IDisposable)).TargetMethods[0];
        var method = typeInfo.AsType().GetMethods().SingleOrDefault(m => (m == methodInfo));
        Assert.NotNull(method);

        // Act
        var isValid = DefaultApplicationModelProvider.IsAction(typeInfo, method);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsAction_DerivedControllerDisposeMethod()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = typeof(DerivedController).GetTypeInfo();
        var methodInfo =
            typeInfo.GetRuntimeInterfaceMap(typeof(IDisposable)).TargetMethods[0];
        var methods = typeInfo.AsType().GetMethods().Where(m => m.Name.Equals("Dispose") && m != methodInfo);

        Assert.NotEmpty(methods);

        foreach (var method in methods)
        {
            // Act
            var isValid = DefaultApplicationModelProvider.IsAction(typeInfo, method);

            // Assert
            Assert.True(isValid);
        }
    }

    [Fact]
    public void IsAction_OverriddenDisposeMethod()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = typeof(DerivedOverriddenDisposeController).GetTypeInfo();
        var method = typeInfo.GetDeclaredMethods("Dispose").SingleOrDefault();
        Assert.NotNull(method);

        // Act
        var isValid = DefaultApplicationModelProvider.IsAction(typeInfo, method);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsAction_NewDisposeMethod()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = typeof(DerivedNewDisposeController).GetTypeInfo();
        var method = typeInfo.GetDeclaredMethods("Dispose").SingleOrDefault();
        Assert.NotNull(method);

        // Act
        var isValid = DefaultApplicationModelProvider.IsAction(typeInfo, method);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsAction_PocoControllerIDisposableDisposeMethod()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = typeof(IDisposablePocoController).GetTypeInfo();
        var methodInfo =
            typeInfo.GetRuntimeInterfaceMap(typeof(IDisposable)).TargetMethods[0];
        var method = typeInfo.AsType().GetMethods().SingleOrDefault(m => (m == methodInfo));
        Assert.NotNull(method);

        // Act
        var isValid = DefaultApplicationModelProvider.IsAction(typeInfo, method);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsAction_PocoControllerDisposeMethod()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = typeof(IDisposablePocoController).GetTypeInfo();
        var methodInfo =
            typeInfo.GetRuntimeInterfaceMap(typeof(IDisposable)).TargetMethods[0];
        var methods = typeInfo.AsType().GetMethods().Where(m => m.Name.Equals("Dispose") && m != methodInfo);

        Assert.NotEmpty(methods);

        foreach (var method in methods)
        {
            // Act
            var isValid = DefaultApplicationModelProvider.IsAction(typeInfo, method);

            // Assert
            Assert.True(isValid);
        }
    }

    [Fact]
    public void IsAction_SimplePocoControllerDisposeMethod()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = typeof(SimplePocoController).GetTypeInfo();
        var methods = typeInfo.AsType().GetMethods().Where(m => m.Name.Equals("Dispose"));

        Assert.NotEmpty(methods);

        foreach (var method in methods)
        {
            // Act
            var isValid = DefaultApplicationModelProvider.IsAction(typeInfo, method);

            // Assert
            Assert.True(isValid);
        }
    }

    [Theory]
    [InlineData("StaticMethod")]
    [InlineData("ProtectedStaticMethod")]
    [InlineData("PrivateStaticMethod")]
    public void IsAction_StaticMethods(string methodName)
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var method = typeof(DerivedController).GetMethod(
            methodName,
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(method);

        // Act
        var isValid = DefaultApplicationModelProvider.IsAction(typeof(DerivedController).GetTypeInfo(), method);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void CreateActionModel_ConventionallyRoutedAction_WithoutHttpConstraints()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = typeof(ConventionallyRoutedController).GetTypeInfo();
        var actionName = nameof(ConventionallyRoutedController.Edit);

        // Act
        var action = builder.CreateActionModel(typeInfo, typeInfo.AsType().GetMethod(actionName));

        // Assert
        Assert.NotNull(action);
        Assert.Equal(actionName, action.ActionName);
        Assert.Empty(action.Attributes);
        Assert.Single(action.Selectors);
        Assert.Empty(action.Selectors[0].ActionConstraints.OfType<HttpMethodActionConstraint>());
        Assert.Empty(GetAttributeRoutes(action.Selectors));
    }

    [Fact]
    public void CreateActionModel_ConventionallyRoutedAction_WithHttpConstraints()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = typeof(ConventionallyRoutedController).GetTypeInfo();
        var actionName = nameof(ConventionallyRoutedController.Update);

        // Act
        var action = builder.CreateActionModel(typeInfo, typeInfo.AsType().GetMethod(actionName));

        // Assert
        Assert.NotNull(action);
        Assert.Single(action.Selectors);
        var methodConstraint = Assert.Single(
            action.Selectors[0].ActionConstraints.OfType<HttpMethodActionConstraint>());
        Assert.Contains("PUT", methodConstraint.HttpMethods);
        Assert.Contains("PATCH", methodConstraint.HttpMethods);

        Assert.Equal(actionName, action.ActionName);
        Assert.Empty(GetAttributeRoutes(action.Selectors));
        Assert.IsType<CustomHttpMethodsAttribute>(Assert.Single(action.Attributes));
    }

    [Fact]
    public void CreateActionModel_ConventionallyRoutedActionWithHttpConstraints_AndInvalidRouteTemplateProvider()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = typeof(ConventionallyRoutedController).GetTypeInfo();
        var actionName = nameof(ConventionallyRoutedController.Delete);

        // Act
        var action = builder.CreateActionModel(typeInfo, typeInfo.AsType().GetMethod(actionName));

        // Assert
        Assert.NotNull(action);
        Assert.Single(action.Selectors);
        var methodConstraint = Assert.Single(
            action.Selectors[0].ActionConstraints.OfType<HttpMethodActionConstraint>());
        Assert.Contains("DELETE", methodConstraint.HttpMethods);
        Assert.Contains("HEAD", methodConstraint.HttpMethods);

        Assert.Equal(actionName, action.ActionName);
        Assert.Empty(GetAttributeRoutes(action.Selectors));
        Assert.Single(action.Attributes.OfType<HttpDeleteAttribute>());
        Assert.Single(action.Attributes.OfType<HttpHeadAttribute>());
    }

    [Fact]
    public void CreateActionModel_ConventionallyRoutedAction_WithMultipleHttpConstraints()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = typeof(ConventionallyRoutedController).GetTypeInfo();
        var actionName = nameof(ConventionallyRoutedController.Details);

        // Act
        var action = builder.CreateActionModel(typeInfo, typeInfo.AsType().GetMethod(actionName));

        // Assert
        Assert.NotNull(action);
        Assert.Single(action.Selectors);
        var methodConstraint = Assert.Single(
            action.Selectors[0].ActionConstraints.OfType<HttpMethodActionConstraint>());
        Assert.Contains("GET", methodConstraint.HttpMethods);
        Assert.Contains("POST", methodConstraint.HttpMethods);
        Assert.Contains("HEAD", methodConstraint.HttpMethods);
        Assert.Equal(actionName, action.ActionName);
        Assert.Empty(GetAttributeRoutes(action.Selectors));
    }

    [Fact]
    public void CreateActionModel_ConventionallyRoutedAction_WithMultipleOverlappingHttpConstraints()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = typeof(ConventionallyRoutedController).GetTypeInfo();
        var actionName = nameof(ConventionallyRoutedController.List);

        // Act
        var action = builder.CreateActionModel(typeInfo, typeInfo.AsType().GetMethod(actionName));

        // Assert
        Assert.NotNull(action);
        Assert.Single(action.Selectors);
        var methodConstraint = Assert.Single(
            action.Selectors[0].ActionConstraints.OfType<HttpMethodActionConstraint>());
        Assert.Contains("GET", methodConstraint.HttpMethods);
        Assert.Contains("PUT", methodConstraint.HttpMethods);
        Assert.Contains("POST", methodConstraint.HttpMethods);
        Assert.Equal(actionName, action.ActionName);
        Assert.Empty(GetAttributeRoutes(action.Selectors));
    }

    [Fact]
    public void CreateActionModel_AttributeRouteOnAction()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = typeof(NoRouteAttributeOnControllerController).GetTypeInfo();
        var actionName = nameof(NoRouteAttributeOnControllerController.Edit);

        // Act
        var action = builder.CreateActionModel(typeInfo, typeInfo.AsType().GetMethod(actionName));

        // Assert
        Assert.NotNull(action);
        Assert.Single(action.Selectors);
        var methodConstraint = Assert.Single(
            action.Selectors[0].ActionConstraints.OfType<HttpMethodActionConstraint>());

        Assert.Equal(actionName, action.ActionName);

        var httpMethod = Assert.Single(methodConstraint.HttpMethods);
        Assert.Equal("HEAD", httpMethod);

        var attributeRoute = Assert.Single(GetAttributeRoutes(action.Selectors));
        Assert.Equal("Change", attributeRoute.Template);

        Assert.IsType<HttpHeadAttribute>(Assert.Single(action.Attributes));
    }

    [Fact]
    public void CreateActionModel_AttributeRouteOnAction_RouteAttribute()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = typeof(NoRouteAttributeOnControllerController).GetTypeInfo();
        var actionName = nameof(NoRouteAttributeOnControllerController.Update);

        // Act
        var action = builder.CreateActionModel(typeInfo, typeInfo.AsType().GetMethod(actionName));

        // Assert
        Assert.NotNull(action);
        Assert.Single(action.Selectors);
        Assert.Empty(action.Selectors[0].ActionConstraints);

        Assert.Equal(actionName, action.ActionName);

        var attributeRoute = Assert.Single(GetAttributeRoutes(action.Selectors));
        Assert.Equal("Update", attributeRoute.Template);

        Assert.IsType<RouteAttribute>(Assert.Single(action.Attributes));
    }

    [Fact]
    public void CreateActionModel_AttributeRouteOnAction_AcceptVerbsAttributeWithTemplate()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = typeof(NoRouteAttributeOnControllerController).GetTypeInfo();
        var actionName = nameof(NoRouteAttributeOnControllerController.List);

        // Act
        var action = builder.CreateActionModel(typeInfo, typeInfo.AsType().GetMethod(actionName));

        // Assert
        Assert.NotNull(action);
        Assert.Single(action.Selectors);
        var methodConstraint = Assert.Single(
            action.Selectors[0].ActionConstraints.OfType<HttpMethodActionConstraint>());

        Assert.Equal(actionName, action.ActionName);

        Assert.Equal(
            new[] { "GET", "HEAD" },
            methodConstraint.HttpMethods.OrderBy(m => m, StringComparer.Ordinal));

        var attributeRoute = Assert.Single(GetAttributeRoutes(action.Selectors));
        Assert.Equal("ListAll", attributeRoute.Template);

        Assert.IsType<AcceptVerbsAttribute>(Assert.Single(action.Attributes));
    }

    [Fact]
    public void CreateActionModel_AttributeRouteOnAction_CreatesOneActionInfoPerRouteTemplate()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = typeof(NoRouteAttributeOnControllerController).GetTypeInfo();
        var actionName = nameof(NoRouteAttributeOnControllerController.Index);

        // Act
        var action = builder.CreateActionModel(typeInfo, typeInfo.AsType().GetMethod(actionName));

        // Assert
        Assert.NotNull(action);
        Assert.Equal(actionName, action.ActionName);
        Assert.NotNull(action.Attributes);
        Assert.Equal(2, action.Attributes.Count);
        Assert.Single(action.Attributes.OfType<HttpGetAttribute>());
        Assert.Single(action.Attributes.OfType<HttpPostAttribute>());
        Assert.Equal(2, action.Selectors.Count);

        foreach (var actionSelectorModel in action.Selectors)
        {
            Assert.NotNull(actionSelectorModel.AttributeRouteModel);
        }

        var selectorModel = Assert.Single(action.Selectors, ai => ai.AttributeRouteModel?.Template == "List");
        var methodConstraint = Assert.Single(selectorModel.ActionConstraints.OfType<HttpMethodActionConstraint>());
        var listMethod = Assert.Single(methodConstraint.HttpMethods);
        Assert.Equal("POST", listMethod);

        var all = Assert.Single(action.Selectors, ai => ai.AttributeRouteModel?.Template == "All");
        methodConstraint = Assert.Single(all.ActionConstraints.OfType<HttpMethodActionConstraint>());
        var allMethod = Assert.Single(methodConstraint.HttpMethods);
        Assert.Equal("GET", allMethod);
    }

    [Fact]
    public void CreateActionModel_NoRouteOnController_AllowsConventionallyRoutedActions_OnTheSameController()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = typeof(NoRouteAttributeOnControllerController).GetTypeInfo();
        var actionName = nameof(NoRouteAttributeOnControllerController.Remove);

        // Act
        var action = builder.CreateActionModel(typeInfo, typeInfo.AsType().GetMethod(actionName));

        // Assert
        Assert.NotNull(action);

        Assert.Equal(actionName, action.ActionName);
        Assert.Empty(action.Attributes);
        Assert.Single(action.Selectors);
        Assert.Empty(action.Selectors[0].ActionConstraints);
        Assert.Null(action.Selectors[0].AttributeRouteModel);
    }

    [Theory]
    [InlineData(typeof(SingleRouteAttributeController))]
    [InlineData(typeof(MultipleRouteAttributeController))]
    public void CreateActionModel_RouteAttributeOnController_CreatesAttributeRoute_ForNonAttributedActions(Type controller)
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = controller.GetTypeInfo();

        // Act
        var action = builder.CreateActionModel(typeInfo, typeInfo.AsType().GetMethod("Delete"));

        // Assert
        Assert.NotNull(action);

        Assert.Equal("Delete", action.ActionName);

        Assert.Single(action.Selectors);
        Assert.Empty(action.Selectors[0].ActionConstraints);
        Assert.Empty(GetAttributeRoutes(action.Selectors));
        Assert.Empty(action.Attributes);
    }

    [Theory]
    [InlineData(typeof(SingleRouteAttributeController))]
    [InlineData(typeof(MultipleRouteAttributeController))]
    public void CreateActionModel_RouteOnController_CreatesOneActionInfoPerRouteTemplateOnAction(Type controller)
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = controller.GetTypeInfo();

        // Act
        var action = builder.CreateActionModel(typeInfo, typeInfo.AsType().GetMethod("Index"));

        // Assert
        Assert.NotNull(action.Attributes);
        Assert.Equal(2, action.Attributes.Count);
        Assert.Equal(2, action.Selectors.Count);
        Assert.Equal("Index", action.ActionName);

        foreach (var selectorModel in action.Selectors)
        {
            var methodConstraint = Assert.Single(selectorModel.ActionConstraints.OfType<HttpMethodActionConstraint>());
            var httpMethod = Assert.Single(methodConstraint.HttpMethods);
            Assert.Equal("GET", httpMethod);

            Assert.NotNull(selectorModel.AttributeRouteModel.Template);
        }

        Assert.Single(action.Selectors, ai => ai.AttributeRouteModel.Template.Equals("List"));
        Assert.Single(action.Selectors, ai => ai.AttributeRouteModel.Template.Equals("All"));
    }

    [Fact]
    public void CreateActionModel_MixedHttpVerbsAndRoutes_EmptyVerbWithRoute()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = typeof(MixedHttpVerbsAndRouteAttributeController).GetTypeInfo();
        var actionName = nameof(MixedHttpVerbsAndRouteAttributeController.VerbAndRoute);

        // Act
        var action = builder.CreateActionModel(typeInfo, typeInfo.AsType().GetMethod(actionName));

        // Assert
        Assert.NotNull(action);
        Assert.Single(action.Selectors);
        var methodConstraint = Assert.Single(
            action.Selectors[0].ActionConstraints.OfType<HttpMethodActionConstraint>());
        Assert.Equal<string>(new string[] { "GET" }, methodConstraint.HttpMethods);
        var attributeRoute = Assert.Single(GetAttributeRoutes(action.Selectors));
        Assert.Equal("Products", attributeRoute.Template);
    }

    [Fact]
    public void CreateActionModel_MixedHttpVerbsAndRoutes_MultipleEmptyVerbsWithMultipleRoutes()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = typeof(MixedHttpVerbsAndRouteAttributeController).GetTypeInfo();
        var actionName = nameof(MixedHttpVerbsAndRouteAttributeController.MultipleVerbsAndRoutes);

        // Act
        var actions = builder.CreateActionModel(typeInfo, typeInfo.AsType().GetMethod(actionName));

        // Assert
        Assert.Equal(2, actions.Selectors.Count);

        // OrderBy is used because the order of the results may very depending on the platform / client.
        var selectorModel = Assert.Single(actions.Selectors, a => a.AttributeRouteModel.Template == "Products");
        var methodConstraint = Assert.Single(selectorModel.ActionConstraints.OfType<HttpMethodActionConstraint>());
        Assert.Equal(new[] { "GET", "POST" }, methodConstraint.HttpMethods.OrderBy(key => key, StringComparer.Ordinal));

        selectorModel = Assert.Single(actions.Selectors, a => a.AttributeRouteModel.Template == "v2/Products");
        methodConstraint = Assert.Single(selectorModel.ActionConstraints.OfType<HttpMethodActionConstraint>());
        Assert.Equal(new[] { "GET", "POST" }, methodConstraint.HttpMethods.OrderBy(key => key, StringComparer.Ordinal));
    }

    [Fact]
    public void CreateActionModel_MixedHttpVerbsAndRoutes_MultipleEmptyAndNonEmptyVerbsWithMultipleRoutes()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = typeof(MixedHttpVerbsAndRouteAttributeController).GetTypeInfo();
        var actionName = nameof(MixedHttpVerbsAndRouteAttributeController.MultipleVerbsWithAnyWithoutTemplateAndRoutes);

        // Act
        var action = builder.CreateActionModel(typeInfo, typeInfo.AsType().GetMethod(actionName));

        // Assert
        Assert.Equal(3, action.Selectors.Count);

        var selectorModel = Assert.Single(action.Selectors, s => s.AttributeRouteModel.Template == "Products");
        var methodConstraint = Assert.Single(selectorModel.ActionConstraints.OfType<HttpMethodActionConstraint>());
        Assert.Equal<string>(new string[] { "GET" }, methodConstraint.HttpMethods);

        selectorModel = Assert.Single(action.Selectors, s => s.AttributeRouteModel.Template == "v2/Products");
        methodConstraint = Assert.Single(selectorModel.ActionConstraints.OfType<HttpMethodActionConstraint>());
        Assert.Equal<string>(new string[] { "GET" }, methodConstraint.HttpMethods);

        selectorModel = Assert.Single(action.Selectors, s => s.AttributeRouteModel.Template == "Products/Buy");
        methodConstraint = Assert.Single(selectorModel.ActionConstraints.OfType<HttpMethodActionConstraint>());
        Assert.Equal<string>(new string[] { "POST" }, methodConstraint.HttpMethods);
    }

    [Fact]
    public void CreateActionModel_MixedHttpVerbsAndRoutes_WithRouteOnController()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = typeof(RouteAttributeOnController).GetTypeInfo();
        var actionName = nameof(RouteAttributeOnController.Get);

        // Act
        var action = builder.CreateActionModel(typeInfo, typeInfo.AsType().GetMethod(actionName));

        // Assert
        Assert.Equal(2, action.Selectors.Count);

        var selectorModel = Assert.Single(action.Selectors, s => s.AttributeRouteModel == null);
        var methodConstraint = Assert.Single(selectorModel.ActionConstraints.OfType<HttpMethodActionConstraint>());
        Assert.Equal(new string[] { "GET" }, methodConstraint.HttpMethods);

        selectorModel = Assert.Single(action.Selectors, s => s.AttributeRouteModel?.Template == "id/{id?}");
        methodConstraint = Assert.Single(selectorModel.ActionConstraints.OfType<HttpMethodActionConstraint>());
        Assert.Equal(new string[] { "GET" }, methodConstraint.HttpMethods);
    }

    [Fact]
    public void CreateActionModel_MixedHttpVerbsAndRoutes_MultipleEmptyAndNonEmptyVerbs()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = typeof(MixedHttpVerbsAndRouteAttributeController).GetTypeInfo();
        var actionName = nameof(MixedHttpVerbsAndRouteAttributeController.Invalid);

        // Act
        var action = builder.CreateActionModel(typeInfo, typeInfo.AsType().GetMethod(actionName));

        // Assert
        Assert.NotNull(action);
        Assert.Equal(2, action.Selectors.Count);

        var selectorModel = Assert.Single(action.Selectors, s => s.AttributeRouteModel?.Template == "Products");
        var methodConstraint = Assert.Single(selectorModel.ActionConstraints.OfType<HttpMethodActionConstraint>());
        Assert.Equal<string>(new string[] { "POST" }, methodConstraint.HttpMethods);

        selectorModel = Assert.Single(action.Selectors, s => s.AttributeRouteModel?.Template == null);
        methodConstraint = Assert.Single(selectorModel.ActionConstraints.OfType<HttpMethodActionConstraint>());
        Assert.Equal<string>(new string[] { "GET" }, methodConstraint.HttpMethods);
    }

    [Fact]
    public void CreateActionModel_SplitsConstraintsBasedOnRoute()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = typeof(MultipleRouteProviderOnActionController).GetTypeInfo();
        var methodInfo = typeInfo.GetMethod(nameof(MultipleRouteProviderOnActionController.Edit));

        // Act
        var actionModel = builder.CreateActionModel(typeInfo, methodInfo);

        // Assert
        Assert.Equal(3, actionModel.Attributes.Count);
        Assert.Equal(2, actionModel.Attributes.OfType<RouteAndConstraintAttribute>().Count());
        Assert.Single(actionModel.Attributes.OfType<ConstraintAttribute>());
        Assert.Equal(2, actionModel.Selectors.Count);

        var selectorModel = Assert.Single(
            actionModel.Selectors.Where(sm => sm.AttributeRouteModel?.Template == "R1"));

        Assert.Equal(2, selectorModel.ActionConstraints.Count);
        Assert.Single(selectorModel.ActionConstraints.OfType<RouteAndConstraintAttribute>());
        Assert.Single(selectorModel.ActionConstraints.OfType<ConstraintAttribute>());

        selectorModel = Assert.Single(
            actionModel.Selectors.Where(sm => sm.AttributeRouteModel?.Template == "R2"));

        Assert.Equal(2, selectorModel.ActionConstraints.Count);
        Assert.Single(selectorModel.ActionConstraints.OfType<RouteAndConstraintAttribute>());
        Assert.Single(selectorModel.ActionConstraints.OfType<ConstraintAttribute>());
    }

    [Fact]
    public void CreateActionModel_InheritedAttributeRoutes()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = typeof(DerivedClassInheritsAttributeRoutesController).GetTypeInfo();
        var actionName = nameof(DerivedClassInheritsAttributeRoutesController.Edit);

        // Act
        var actions = builder.CreateActionModel(typeInfo, typeInfo.AsType().GetMethod(actionName));

        // Assert
        Assert.Equal(2, actions.Attributes.Count);
        Assert.Equal(2, actions.Selectors.Count);

        var selectorModel = Assert.Single(actions.Selectors, a => a.AttributeRouteModel?.Template == "A");
        Assert.Contains(selectorModel.AttributeRouteModel.Attribute, actions.Attributes);

        selectorModel = Assert.Single(actions.Selectors, a => a.AttributeRouteModel?.Template == "B");
        Assert.Contains(selectorModel.AttributeRouteModel.Attribute, actions.Attributes);
    }

    [Fact]
    public void CreateActionModel_InheritedAttributeRoutesOverridden()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = typeof(DerivedClassOverridesAttributeRoutesController).GetTypeInfo();
        var actionName = nameof(DerivedClassOverridesAttributeRoutesController.Edit);

        // Act
        var action = builder.CreateActionModel(typeInfo, typeInfo.AsType().GetMethod(actionName));

        // Assert
        Assert.Equal(4, action.Attributes.Count);
        Assert.Equal(2, action.Selectors.Count);

        var selectorModel = Assert.Single(action.Selectors, a => a.AttributeRouteModel?.Template == "C");
        Assert.Contains(selectorModel.AttributeRouteModel.Attribute, action.Attributes);

        selectorModel = Assert.Single(action.Selectors, a => a.AttributeRouteModel?.Template == "D");
        Assert.Contains(selectorModel.AttributeRouteModel.Attribute, action.Attributes);
    }

    [Fact]
    public void ControllerDispose_ExplicitlyImplemented_IDisposableMethods_AreTreatedAs_NonActions()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = typeof(DerivedFromControllerAndExplicitIDisposableImplementationController).GetTypeInfo();
        var context = new ApplicationModelProviderContext(new[] { typeInfo });

        // Act
        builder.OnProvidersExecuting(context);

        // Assert
        var model = Assert.Single(context.Result.Controllers);
        Assert.Empty(model.Actions);
    }

    [Fact]
    public void ControllerDispose_MethodsNamedDispose_AreTreatedAsActions()
    {
        // Arrange
        var builder = new TestApplicationModelProvider();
        var typeInfo = typeof(DerivedFromControllerAndHidesBaseDisposeMethodController).GetTypeInfo();
        var context = new ApplicationModelProviderContext(new[] { typeInfo });

        // Act
        builder.OnProvidersExecuting(context);

        // Assert
        var model = Assert.Single(context.Result.Controllers);
        var action = Assert.Single(model.Actions);

        // Make sure that the Dispose method is from the derived controller and not the base 'Controller' type
        Assert.Equal(typeInfo, action.ActionMethod.DeclaringType.GetTypeInfo());
    }

    [BindProperties]
    public class BindPropertyController
    {
        public string Property { get; set; }

        [ModelBinder(typeof(ComplexObjectModelBinder))]
        public string BinderType { get; set; }

        [FromRoute]
        public string BinderSource { get; set; }
    }

    [Fact]
    public void CreatePropertyModel_AddsBindingInfoToProperty_IfDeclaringTypeHasBindPropertiesAttribute()
    {
        // Arrange
        var propertyInfo = typeof(BindPropertyController).GetProperty(nameof(BindPropertyController.Property));

        // Act
        var property = Provider.CreatePropertyModel(propertyInfo);

        // Assert
        var bindingInfo = property.BindingInfo;
        Assert.NotNull(bindingInfo);
        Assert.Null(bindingInfo.BinderModelName);
        Assert.Null(bindingInfo.BinderType);
        Assert.Null(bindingInfo.BindingSource);
        Assert.Null(bindingInfo.PropertyFilterProvider);
        Assert.NotNull(bindingInfo.RequestPredicate);
    }

    [Fact]
    public void CreatePropertyModel_DoesNotSetBindingInfo_IfPropertySpecifiesBinderType()
    {
        // Arrange
        var propertyInfo = typeof(BindPropertyController).GetProperty(nameof(BindPropertyController.BinderType));

        // Act
        var property = Provider.CreatePropertyModel(propertyInfo);

        // Assert
        var bindingInfo = property.BindingInfo;
        Assert.Same(typeof(ComplexObjectModelBinder), bindingInfo.BinderType);
    }

    [Fact]
    public void CreatePropertyModel_DoesNotSetBindingInfo_IfPropertySpecifiesBinderSource()
    {
        // Arrange
        var propertyInfo = typeof(BindPropertyController).GetProperty(nameof(BindPropertyController.BinderSource));

        // Act
        var property = Provider.CreatePropertyModel(propertyInfo);

        // Assert
        var bindingInfo = property.BindingInfo;
        Assert.Null(bindingInfo.BinderType);
        Assert.Same(BindingSource.Path, property.BindingInfo.BindingSource);
    }

    public class DerivedFromBindPropertyController : BindPropertyController
    {
        public string DerivedProperty { get; set; }
    }

    [Fact]
    public void CreatePropertyModel_AppliesBindPropertyAttributeDeclaredOnBaseType()
    {
        // Arrange
        var propertyInfo = typeof(DerivedFromBindPropertyController).GetProperty(
            nameof(DerivedFromBindPropertyController.DerivedProperty));

        // Act
        var property = Provider.CreatePropertyModel(propertyInfo);

        // Assert
        Assert.NotNull(property.BindingInfo);
    }

    [BindProperties]
    public class UserController : ControllerBase
    {
        public string DerivedProperty { get; set; }
    }

    [Fact]
    public void CreatePropertyModel_DoesNotApplyBindingInfoToPropertiesOnBaseType()
    {
        // This test ensures that applying BindPropertyAttribute on a user defined type does not cause properties on
        // Controller \ ControllerBase to be treated as model bound.
        // Arrange
        var derivedPropertyInfo = typeof(UserController).GetProperty(nameof(UserController.DerivedProperty));
        var basePropertyInfo = typeof(UserController).GetProperty(nameof(ControllerBase.ControllerContext));

        // Act
        var derivedProperty = Provider.CreatePropertyModel(derivedPropertyInfo);
        var baseProperty = Provider.CreatePropertyModel(basePropertyInfo);

        // Assert
        Assert.NotNull(derivedProperty.BindingInfo);
        Assert.Null(baseProperty.BindingInfo);
    }

    private IList<AttributeRouteModel> GetAttributeRoutes(IList<SelectorModel> selectors)
    {
        return selectors
            .Where(sm => sm.AttributeRouteModel != null)
            .Select(sm => sm.AttributeRouteModel)
            .ToList();
    }

    private class DerivedFromControllerAndExplicitIDisposableImplementationController
        : ViewFeaturesController, IDisposable
    {
        void IDisposable.Dispose()
        {
            throw new NotImplementedException();
        }
    }

    private class DerivedFromControllerAndHidesBaseDisposeMethodController : ViewFeaturesController
    {
        public new void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    private class ViewFeaturesController : ControllerBase, IDisposable
    {
        public virtual void Dispose()
        {
        }
    }

    private class BaseClassWithAttributeRoutesController
    {
        [Route("A")]
        [Route("B")]
        public virtual void Edit()
        {
        }
    }

    private class DerivedClassInheritsAttributeRoutesController : BaseClassWithAttributeRoutesController
    {
        public override void Edit()
        {
        }
    }

    private class DerivedClassOverridesAttributeRoutesController : BaseClassWithAttributeRoutesController
    {
        [Route("C")]
        [Route("D")]
        public override void Edit()
        {
        }
    }

    private class Controller : IDisposable
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        [NonAction]
        public virtual IActionResult Redirect(string url)
        {
            return null;
        }
    }

    private class BaseController : Controller
    {
        public void GetFromBase() // Valid action method.
        {
        }

        [NonAction]
        public virtual void OverridenNonActionMethod()
        {
        }

        [NonAction]
        public virtual void NewMethod()
        {
        }

        public override IActionResult Redirect(string url)
        {
            return base.Redirect(url + "#RedirectOverride");
        }
    }

    private class DerivedController : BaseController
    {
        public void GetFromDerived() // Valid action method.
        {
        }

        [HttpGet]
        public override void OverridenNonActionMethod()
        {
        }

        public new void NewMethod() // Valid action method.
        {
        }

        public void GenericMethod<T>()
        {
        }

        private void PrivateMethod()
        {
        }

        public static void StaticMethod()
        {
        }

        protected static void ProtectedStaticMethod()
        {
        }

        private static void PrivateStaticMethod()
        {
        }

        public string Dispose(string s)
        {
            return s;
        }

        public new void Dispose()
        {
        }
    }

    private class IDisposablePocoController : IDisposable
    {
        public string Index()
        {
            return "Hello world";
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
        public string Dispose(string s)
        {
            return s;
        }
    }

    private class BaseClass : IDisposable
    {
        public virtual void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
        }
    }
    private class DerivedOverriddenDisposeController : BaseClass
    {
        public override void Dispose()
        {
            base.Dispose();
        }
    }

    private class DerivedNewDisposeController : BaseClass
    {
        public new void Dispose()
        {
            base.Dispose();
        }
    }

    private class SimplePocoController
    {
        public string Index()
        {
            return "Hello world";
        }

        public void Dispose()
        {
        }

        public void Dispose(string s)
        {
        }
    }

    private class OperatorOverloadingController : Controller
    {
        public static OperatorOverloadingController operator +(
            OperatorOverloadingController c1,
            OperatorOverloadingController c2)
        {
            return new OperatorOverloadingController();
        }
    }

    private class NoRouteAttributeOnControllerController : Controller
    {
        [HttpGet("All")]
        [HttpPost("List")]
        public void Index() { }

        [HttpHead("Change")]
        public void Edit() { }

        public void Remove() { }

        [Route("Update")]
        public void Update() { }

        [AcceptVerbs("GET", "HEAD", Route = "ListAll")]
        public void List() { }
    }

    [Route("Products")]
    private class SingleRouteAttributeController : Controller
    {
        [HttpGet("All")]
        [HttpGet("List")]
        public void Index() { }

        public void Delete() { }
    }

    [Route("Products")]
    [Route("Items")]
    private class MultipleRouteAttributeController : Controller
    {
        [HttpGet("All")]
        [HttpGet("List")]
        public void Index() { }

        public void Delete() { }
    }

    private class MixedHttpVerbsAndRouteAttributeController : Controller
    {
        // Should produce a single action constrained to GET
        [HttpGet]
        [Route("Products")]
        public void VerbAndRoute() { }

        // Should produce two actions constrained to GET,POST
        [HttpGet]
        [HttpPost]
        [Route("Products")]
        [Route("v2/Products")]
        public void MultipleVerbsAndRoutes() { }

        // Produces:
        //
        // Products - GET
        // v2/Products - GET
        // Products/Buy - POST
        [HttpGet]
        [Route("Products")]
        [Route("v2/Products")]
        [HttpPost("Products/Buy")]
        public void MultipleVerbsWithAnyWithoutTemplateAndRoutes() { }

        // Produces:
        //
        // (no route) - GET
        // Products - POST
        //
        // This is invalid, and will throw during the ADP construction phase.
        [HttpGet]
        [HttpPost("Products")]
        public void Invalid() { }
    }

    [Route("api/[controller]")]
    private class RouteAttributeOnController : Controller
    {
        [HttpGet]
        [HttpGet("id/{id?}")]
        public object Get(short? id)
        {
            return null;
        }

        [HttpDelete("{id}")]
        public object Delete(int id)
        {
            return null;
        }
    }

    // Here the constraints on the methods are acting as an IActionHttpMethodProvider and
    // not as an IRouteTemplateProvider given that there is no RouteAttribute
    // on the controller and the template for all the constraints on a method is null.
    private class ConventionallyRoutedController : Controller
    {
        public void Edit() { }

        [CustomHttpMethods("PUT", "PATCH")]
        public void Update() { }

        [HttpHead]
        [HttpDelete]
        public void Delete() { }

        [HttpPost]
        [HttpGet]
        [HttpHead]
        public void Details() { }

        [HttpGet]
        [HttpPut]
        [AcceptVerbs("GET", "POST")]
        public void List() { }
    }

    private class CustomHttpMethodsAttribute : Attribute, IActionHttpMethodProvider
    {
        private readonly string[] _methods;

        public CustomHttpMethodsAttribute(params string[] methods)
        {
            _methods = methods;
        }

        public IEnumerable<string> HttpMethods => _methods;
    }

    [Route("A")]
    [Route("B")]
    private class BaseClassWithRoutesController
    {
    }

    private class DerivedClassInheritingRoutesController : BaseClassWithRoutesController
    {
    }

    [Route("C")]
    [Route("D")]
    private class DerivedClassHidingRoutesController : BaseClassWithRoutesController
    {
    }

    private class StoreController : Controller, IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context)
        {
            throw new NotImplementedException();
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            throw new NotImplementedException();
        }
    }

    private class MyFilterAttribute : Attribute, IFilterMetadata
    {
    }

    [MyFilter]
    public class NoFiltersController
    {
    }

    public interface ITestService
    { }

    public class ModelBinderController
    {
        [FromQuery]
        public string Bound { get; set; }

        public string Unbound { get; set; }

        [FromServices]
        public ITestService Service { get; set; }

        public IFormFile FormFile { get; set; }

        public IActionResult PostAction([FromQuery] string fromQuery, IFormFileCollection formFileCollection, string unbound) => null;

        public IActionResult FormFilesSequences(
            IEnumerable<IFormFile> formFileEnumerable,
            ICollection<IFormFile> formFileCollection,
            IList<IFormFile> formFileIList,
            List<IFormFile> formFileList,
            IFormFile[] formFileArray) => null;

        public IActionResult PostAction1(Guid guid) => null;

        public IActionResult PostAction2([FromQuery] Guid fromQuery) => null;
    }

    public class SomeFiltersController : IAsyncActionFilter, IResultFilter
    {
        public Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            return null;
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {
        }

        public void OnResultExecuting(ResultExecutingContext context)
        {
        }
    }

    private class UnsupportedFiltersController : IExceptionFilter, IAuthorizationFilter, IAsyncResourceFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            throw new NotImplementedException();
        }

        public void OnException(ExceptionContext context)
        {
            throw new NotImplementedException();
        }

        public Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            throw new NotImplementedException();
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    private class RouteAndConstraintAttribute : Attribute, IActionConstraintMetadata, IRouteTemplateProvider
    {
        public RouteAndConstraintAttribute(string template)
        {
            Template = template;
        }

        public string Name { get; set; }

        public int? Order { get; set; }

        public string Template { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    private class ConstraintAttribute : Attribute, IActionConstraintMetadata
    {
    }

    private class MultipleRouteProviderOnActionController
    {
        [Constraint]
        [RouteAndConstraint("R1")]
        [RouteAndConstraint("R2")]
        public void Edit() { }
    }

    private class AsyncActionController : Controller
    {
        public Task<IActionResult> GetPersonAsync() => null;

        [ActionName("GetRealAddressAsync")]
        public Task<IActionResult> GetAddressAsync() => null;
    }

    private class TestApplicationModelProvider : DefaultApplicationModelProvider
    {
        public TestApplicationModelProvider()
            : this(new MvcOptions(), new EmptyModelMetadataProvider())
        {
        }

        public TestApplicationModelProvider(
            MvcOptions options,
            IModelMetadataProvider modelMetadataProvider)
            : base(Options.Create(options), modelMetadataProvider)
        {
        }
    }
}
