// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

public class InferParameterBindingInfoConventionTest
{
    [Fact]
    public void Apply_DoesNotInferBindingSourceForParametersWithBindingInfo()
    {
        // Arrange
        var actionName = nameof(ParameterWithBindingInfo.Action);
        var convention = GetConvention();
        var action = GetActionModel(typeof(ParameterWithBindingInfo), actionName);

        // Act
        convention.Apply(action);

        // Assert
        var parameterModel = Assert.Single(action.Parameters);
        Assert.NotNull(parameterModel.BindingInfo);
        Assert.Same(BindingSource.Custom, parameterModel.BindingInfo.BindingSource);
    }

    [Fact]
    public void Apply_DoesNotInferBindingSourceFor_ComplexType_WithPropertiesWithBindingSource()
    {
        // Arrange
        var actionName = nameof(ParameterBindingController.CompositeComplexTypeModel);
        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var convention = GetConvention(modelMetadataProvider);
        var action = GetActionModel(typeof(ParameterBindingController), actionName);

        // Act
        convention.Apply(action);

        // Assert
        var parameterModel = Assert.Single(action.Parameters);
        Assert.NotNull(parameterModel.BindingInfo);
        Assert.Null(parameterModel.BindingInfo.BindingSource);
    }

    [Fact]
    public void InferParameterBindingSources_Throws_IfMultipleParametersAreInferredAsBodyBound()
    {
        // Arrange
        var actionName = nameof(MultipleFromBodyController.MultipleInferred);
        var expected =
$@"Action '{typeof(MultipleFromBodyController).FullName}.{actionName} ({typeof(MultipleFromBodyController).Assembly.GetName().Name})' " +
"has more than one parameter that was specified or inferred as bound from request body. Only one parameter per action may be bound from body. Inspect the following parameters, and use 'FromQueryAttribute' to specify bound from query, 'FromRouteAttribute' to specify bound from route, and 'FromBodyAttribute' for parameters to be bound from body:" +
Environment.NewLine + "TestModel a" +
Environment.NewLine + "Car b";

        var convention = GetConvention();
        var action = GetActionModel(typeof(MultipleFromBodyController), actionName);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => convention.InferParameterBindingSources(action));
        Assert.Equal(expected, ex.Message);
    }

    [Fact]
    public void InferParameterBindingSources_Throws_IfMultipleParametersAreInferredOrSpecifiedAsBodyBound()
    {
        // Arrange
        var actionName = nameof(MultipleFromBodyController.InferredAndSpecified);
        var expected =
$@"Action '{typeof(MultipleFromBodyController).FullName}.{actionName} ({typeof(MultipleFromBodyController).Assembly.GetName().Name})' " +
"has more than one parameter that was specified or inferred as bound from request body. Only one parameter per action may be bound from body. Inspect the following parameters, and use 'FromQueryAttribute' to specify bound from query, 'FromRouteAttribute' to specify bound from route, and 'FromBodyAttribute' for parameters to be bound from body:" +
Environment.NewLine + "TestModel a" +
Environment.NewLine + "int b";

        var convention = GetConvention();
        var action = GetActionModel(typeof(MultipleFromBodyController), actionName);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => convention.InferParameterBindingSources(action));
        Assert.Equal(expected, ex.Message);
    }

    [Fact]
    public void InferParameterBindingSources_Throws_IfMultipleParametersAreFromBody()
    {
        // Arrange
        var actionName = nameof(MultipleFromBodyController.MultipleSpecified);
        var expected =
$@"Action '{typeof(MultipleFromBodyController).FullName}.{actionName} ({typeof(MultipleFromBodyController).Assembly.GetName().Name})' " +
"has more than one parameter that was specified or inferred as bound from request body. Only one parameter per action may be bound from body. Inspect the following parameters, and use 'FromQueryAttribute' to specify bound from query, 'FromRouteAttribute' to specify bound from route, and 'FromBodyAttribute' for parameters to be bound from body:" +
Environment.NewLine + "decimal a" +
Environment.NewLine + "int b";

        var convention = GetConvention();
        var action = GetActionModel(typeof(MultipleFromBodyController), actionName);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => convention.InferParameterBindingSources(action));
        Assert.Equal(expected, ex.Message);
    }

    [Fact]
    public void InferParameterBindingSources_InfersSources()
    {
        // Arrange
        var actionName = nameof(ParameterBindingController.ComplexTypeModelWithCancellationToken);
        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var convention = GetConvention(modelMetadataProvider);
        var action = GetActionModel(typeof(ParameterBindingController), actionName, modelMetadataProvider);

        // Act
        convention.InferParameterBindingSources(action);

        // Assert
        Assert.Collection(
            action.Parameters,
            parameter =>
            {
                Assert.Equal("model", parameter.Name);

                var bindingInfo = parameter.BindingInfo;
                Assert.NotNull(bindingInfo);
                Assert.Equal(EmptyBodyBehavior.Default, bindingInfo.EmptyBodyBehavior);
                Assert.Same(BindingSource.Body, bindingInfo.BindingSource);
            },
            parameter =>
            {
                Assert.Equal("cancellationToken", parameter.Name);

                var bindingInfo = parameter.BindingInfo;
                Assert.NotNull(bindingInfo);
                Assert.Equal(BindingSource.Special, bindingInfo.BindingSource);
            });
    }

    [Fact]
    public void InferParameterBindingSources_InfersSourcesFromRequiredComplexType()
    {
        // Arrange
        var actionName = nameof(ParameterBindingController.RequiredComplexType);
        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var convention = GetConvention(modelMetadataProvider);
        var action = GetActionModel(typeof(ParameterBindingController), actionName, modelMetadataProvider);

        // Act
        convention.InferParameterBindingSources(action);

        // Assert
        Assert.Collection(
            action.Parameters,
            parameter =>
            {
                Assert.Equal("model", parameter.Name);

                var bindingInfo = parameter.BindingInfo;
                Assert.NotNull(bindingInfo);
                Assert.Equal(EmptyBodyBehavior.Default, bindingInfo.EmptyBodyBehavior);
                Assert.Same(BindingSource.Body, bindingInfo.BindingSource);
            });
    }

    [Fact]
    public void InferParameterBindingSources_InfersSourcesFromNullableComplexType()
    {
        // Arrange
        var actionName = nameof(ParameterBindingController.NullableComplexType);
        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var convention = GetConvention(modelMetadataProvider);
        var action = GetActionModel(typeof(ParameterBindingController), actionName, modelMetadataProvider);

        // Act
        convention.InferParameterBindingSources(action);

        // Assert
        Assert.Collection(
            action.Parameters,
            parameter =>
            {
                Assert.Equal("model", parameter.Name);

                var bindingInfo = parameter.BindingInfo;
                Assert.NotNull(bindingInfo);
                Assert.Equal(EmptyBodyBehavior.Allow, bindingInfo.EmptyBodyBehavior);
                Assert.Same(BindingSource.Body, bindingInfo.BindingSource);
            });
    }

    [Fact]
    public void InferParameterBindingSources_InfersSourcesFromComplexTypeWithDefaultValue()
    {
        // Arrange
        var actionName = nameof(ParameterBindingController.ComplexTypeWithDefaultValue);
        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var convention = GetConvention(modelMetadataProvider);
        var action = GetActionModel(typeof(ParameterBindingController), actionName, modelMetadataProvider);

        // Act
        convention.InferParameterBindingSources(action);

        // Assert
        Assert.Collection(
            action.Parameters,
            parameter =>
            {
                Assert.Equal("model", parameter.Name);

                var bindingInfo = parameter.BindingInfo;
                Assert.NotNull(bindingInfo);
                Assert.Equal(EmptyBodyBehavior.Allow, bindingInfo.EmptyBodyBehavior);
                Assert.Same(BindingSource.Body, bindingInfo.BindingSource);
            });
    }

    [Fact]
    public void Apply_PreservesBindingInfo_WhenInferringFor_ParameterWithModelBinder_AndExplicitName()
    {
        // Arrange
        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var actionName = nameof(ModelBinderOnParameterController.ModelBinderAttributeWithExplicitModelName);
        var convention = GetConvention();
        var action = GetActionModel(typeof(ModelBinderOnParameterController), actionName, modelMetadataProvider);

        // Act
        convention.Apply(action);

        // Assert
        var parameter = Assert.Single(action.Parameters);

        var bindingInfo = parameter.BindingInfo;
        Assert.NotNull(bindingInfo);
        Assert.Same(BindingSource.Query, bindingInfo.BindingSource);
        Assert.Equal("top", bindingInfo.BinderModelName);
    }

    [Fact]
    public void Apply_PreservesBindingInfo_WhenInferringFor_ParameterWithModelBinderType()
    {
        // Arrange
        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var actionName = nameof(ModelBinderOnParameterController.ModelBinderType);
        var convention = GetConvention();
        var action = GetActionModel(typeof(ModelBinderOnParameterController), actionName, modelMetadataProvider);

        // Act
        convention.Apply(action);

        // Assert
        var parameter = Assert.Single(action.Parameters);

        var bindingInfo = parameter.BindingInfo;
        Assert.NotNull(bindingInfo);
        Assert.Same(BindingSource.Custom, bindingInfo.BindingSource);
        Assert.Null(bindingInfo.BinderModelName);
    }

    [Fact]
    public void OnProvidersExecuting_PreservesBindingInfo_WhenInferringFor_ParameterWithModelBinderType_AndExplicitModelName()
    {
        // Arrange
        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var actionName = nameof(ModelBinderOnParameterController.ModelBinderTypeWithExplicitModelName);
        var convention = GetConvention();
        var action = GetActionModel(typeof(ModelBinderOnParameterController), actionName, modelMetadataProvider);

        // Act
        convention.Apply(action);

        // Assert
        var parameter = Assert.Single(action.Parameters);

        var bindingInfo = parameter.BindingInfo;
        Assert.NotNull(bindingInfo);
        Assert.Same(BindingSource.Custom, bindingInfo.BindingSource);
        Assert.Equal("foo", bindingInfo.BinderModelName);
    }

    [Fact]
    public void InferBindingSourceForParameter_ReturnsPath_IfParameterNameExistsInRouteAsSimpleToken()
    {
        // Arrange
        var actionName = nameof(ParameterBindingController.SimpleRouteToken);
        var parameter = GetParameterModel(typeof(ParameterBindingController), actionName);
        var convention = GetConvention();

        // Act
        var result = convention.InferBindingSourceForParameter(parameter);

        // Assert
        Assert.Same(BindingSource.Path, result);
    }

    [Fact]
    public void InferBindingSourceForParameter_ReturnsPath_IfParameterNameExistsInRouteAsOptionalToken()
    {
        // Arrange
        var actionName = nameof(ParameterBindingController.OptionalRouteToken);
        var parameter = GetParameterModel(typeof(ParameterBindingController), actionName);
        var convention = GetConvention();

        // Act
        var result = convention.InferBindingSourceForParameter(parameter);

        // Assert
        Assert.Same(BindingSource.Path, result);
    }

    [Fact]
    public void InferBindingSourceForParameter_ReturnsPath_IfParameterNameExistsInRouteAsConstrainedToken()
    {
        // Arrange
        var actionName = nameof(ParameterBindingController.ConstrainedRouteToken);
        var parameter = GetParameterModel(typeof(ParameterBindingController), actionName);
        var convention = GetConvention();

        // Act
        var result = convention.InferBindingSourceForParameter(parameter);

        // Assert
        Assert.Same(BindingSource.Path, result);
    }

    [Fact]
    public void InferBindingSourceForParameter_ReturnsBody_ForComplexTypeParameterThatAppearsInRoute()
    {
        // Arrange
        var actionName = nameof(ParameterBindingController.ComplexTypeInRoute);
        var parameter = GetParameterModel(typeof(ParameterBindingController), actionName);
        var convention = GetConvention();

        // Act
        var result = convention.InferBindingSourceForParameter(parameter);

        // Assert
        Assert.Same(BindingSource.Body, result);
    }

    [Fact]
    public void InferBindingSourceForParameter_ReturnsPath_IfParameterAppearsInAnyRoutes_MulitpleRoutes()
    {
        // Arrange
        var actionName = nameof(ParameterBindingController.ParameterInMultipleRoutes);
        var parameter = GetParameterModel(typeof(ParameterBindingController), actionName);
        var convention = GetConvention();

        // Act
        var result = convention.InferBindingSourceForParameter(parameter);

        // Assert
        Assert.Same(BindingSource.Path, result);
    }

    [Fact]
    public void InferBindingSourceForParameter_ReturnsPath_IfParameterAppearsInAnyRoute()
    {
        // Arrange
        var actionName = nameof(ParameterBindingController.ParameterNotInAllRoutes);
        var parameter = GetParameterModel(typeof(ParameterBindingController), actionName);
        var convention = GetConvention();

        // Act
        var result = convention.InferBindingSourceForParameter(parameter);

        // Assert
        Assert.Same(BindingSource.Path, result);
    }

    [Fact]
    public void InferBindingSourceForParameter_ReturnsPath_IfParameterAppearsInControllerRoute()
    {
        // Arrange
        var actionName = nameof(ParameterInController.ActionWithoutRoute);
        var parameter = GetParameterModel(typeof(ParameterInController), actionName);
        var convention = GetConvention();

        // Act
        var result = convention.InferBindingSourceForParameter(parameter);

        // Assert
        Assert.Same(BindingSource.Path, result);
    }

    [Fact]
    public void InferBindingSourceForParameter_ReturnsPath_IfParameterAppearsInControllerRoute_AndActionHasRoute()
    {
        // Arrange
        var actionName = nameof(ParameterInController.ActionWithRoute);
        var parameter = GetParameterModel(typeof(ParameterInController), actionName);
        var convention = GetConvention();

        // Act
        var result = convention.InferBindingSourceForParameter(parameter);

        // Assert
        Assert.Same(BindingSource.Path, result);
    }

    [Fact]
    public void InferBindingSourceForParameter_ReturnsPath_IfParameterAppearsInAllActionRoutes()
    {
        // Arrange
        var actionName = nameof(ParameterInController.MultipleRoute);
        var parameter = GetParameterModel(typeof(ParameterInController), actionName);
        var convention = GetConvention();

        // Act
        var result = convention.InferBindingSourceForParameter(parameter);

        // Assert
        Assert.Same(BindingSource.Path, result);
    }

    [Fact]
    public void InferBindingSourceForParameter_DoesNotReturnPath_IfActionRouteOverridesControllerRoute()
    {
        // Arrange
        var actionName = nameof(ParameterInController.AbsoluteRoute);
        var parameter = GetParameterModel(typeof(ParameterInController), actionName);
        var convention = GetConvention();

        // Act
        var result = convention.InferBindingSourceForParameter(parameter);

        // Assert
        Assert.Same(BindingSource.Query, result);
    }

    [Fact]
    public void InferBindingSourceForParameter_ReturnsPath_IfParameterPresentInNonOverriddenControllerRoute()
    {
        // Arrange
        var actionName = nameof(ParameterInController.MultipleRouteWithOverride);
        var parameter = GetParameterModel(typeof(ParameterInController), actionName);
        var convention = GetConvention();

        // Act
        var result = convention.InferBindingSourceForParameter(parameter);

        // Assert
        Assert.Same(BindingSource.Path, result);
    }

    [Fact]
    public void InferBindingSourceForParameter_ReturnsPath_IfParameterExistsInRoute_OnControllersWithoutSelectors()
    {
        // Arrange
        var actionName = nameof(ParameterBindingNoRoutesOnController.SimpleRoute);
        var parameter = GetParameterModel(typeof(ParameterBindingNoRoutesOnController), actionName);
        var convention = GetConvention();

        // Act
        var result = convention.InferBindingSourceForParameter(parameter);

        // Assert
        Assert.Same(BindingSource.Path, result);
    }

    [Fact]
    public void InferBindingSourceForParameter_ReturnsPath_IfParameterExistsInAllRoutes_OnControllersWithoutSelectors()
    {
        // Arrange
        var actionName = nameof(ParameterBindingNoRoutesOnController.ParameterInMultipleRoutes);
        var parameter = GetParameterModel(typeof(ParameterBindingNoRoutesOnController), actionName);
        var convention = GetConvention();

        // Act
        var result = convention.InferBindingSourceForParameter(parameter);

        // Assert
        Assert.Same(BindingSource.Path, result);
    }

    [Fact]
    public void InferBindingSourceForParameter_DoesNotReturnPath_IfNeitherActionNorControllerHasTemplate()
    {
        // Arrange
        var actionName = nameof(ParameterBindingNoRoutesOnController.NoRouteTemplate);
        var parameter = GetParameterModel(typeof(ParameterBindingNoRoutesOnController), actionName);
        var convention = GetConvention();

        // Act
        var result = convention.InferBindingSourceForParameter(parameter);

        // Assert
        Assert.Same(BindingSource.Query, result);
    }

    [Fact]
    public void InferBindingSourceForParameter_ReturnsBodyForComplexTypes()
    {
        // Arrange
        var actionName = nameof(ParameterBindingController.ComplexTypeModel);
        var parameter = GetParameterModel(typeof(ParameterBindingController), actionName);
        var convention = GetConvention();

        // Act
        var result = convention.InferBindingSourceForParameter(parameter);

        // Assert
        Assert.Same(BindingSource.Body, result);
    }

    [Fact]
    public void InferParameterBindingSources_SetsCorrectBindingSourceForComplexTypesWithCancellationToken()
    {
        // Arrange
        var actionName = nameof(ParameterBindingController.ComplexTypeModelWithCancellationToken);

        // Use the default set of ModelMetadataProviders so we get metadata details for CancellationToken.
        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var context = GetContext(typeof(ParameterBindingController), modelMetadataProvider);
        var controllerModel = Assert.Single(context.Result.Controllers);
        var actionModel = Assert.Single(controllerModel.Actions, m => m.ActionName == actionName);

        var convention = GetConvention();

        // Act
        convention.InferParameterBindingSources(actionModel);

        // Assert
        var model = GetParameterModel<TestModel>(actionModel);
        Assert.Same(BindingSource.Body, model.BindingInfo.BindingSource);

        var cancellationToken = GetParameterModel<CancellationToken>(actionModel);
        Assert.Same(BindingSource.Special, cancellationToken.BindingInfo.BindingSource);
    }

    [Fact]
    public void InferBindingSourceForParameter_ReturnsQueryForSimpleTypes()
    {
        // Arrange
        var actionName = nameof(ParameterBindingController.SimpleTypeModel);
        var parameter = GetParameterModel(typeof(ParameterBindingController), actionName);
        var convention = GetConvention();

        // Act
        var result = convention.InferBindingSourceForParameter(parameter);

        // Assert
        Assert.Same(BindingSource.Query, result);
    }

    [Fact]
    public void InferBindingSourceForParameter_ReturnsBodyForCollectionOfSimpleTypes()
    {
        // Arrange
        var actionName = nameof(ParameterBindingController.CollectionOfSimpleTypes);
        var parameter = GetParameterModel(typeof(ParameterBindingController), actionName);
        var convention = GetConvention();

        // Act
        var result = convention.InferBindingSourceForParameter(parameter);

        // Assert
        Assert.Same(BindingSource.Body, result);
    }

    [Fact]
    public void InferBindingSourceForParameter_ReturnsBodyForIEnumerableOfSimpleTypes()
    {
        // Arrange
        var actionName = nameof(ParameterBindingController.IEnumerableOfSimpleTypes);
        var parameter = GetParameterModel(typeof(ParameterBindingController), actionName);
        var convention = GetConvention();

        // Act
        var result = convention.InferBindingSourceForParameter(parameter);

        // Assert
        Assert.Same(BindingSource.Body, result);
    }

    [Fact]
    public void InferBindingSourceForParameter_ReturnsBodyForCollectionOfComplexTypes()
    {
        // Arrange
        var actionName = nameof(ParameterBindingController.CollectionOfComplexTypes);
        var parameter = GetParameterModel(typeof(ParameterBindingController), actionName);
        var convention = GetConvention();

        // Act
        var result = convention.InferBindingSourceForParameter(parameter);

        // Assert
        Assert.Same(BindingSource.Body, result);
    }

    [Fact]
    public void InferBindingSourceForParameter_ReturnsBodyForIEnumerableOfComplexTypes()
    {
        // Arrange
        var actionName = nameof(ParameterBindingController.IEnumerableOfComplexTypes);
        var parameter = GetParameterModel(typeof(ParameterBindingController), actionName);
        var convention = GetConvention();

        // Act
        var result = convention.InferBindingSourceForParameter(parameter);

        // Assert
        Assert.Same(BindingSource.Body, result);
    }

    [Fact]
    public void InferBindingSourceForParameter_ReturnsServicesForComplexTypesRegisteredInDI()
    {
        // Arrange
        var actionName = nameof(ParameterBindingController.ServiceParameter);
        var parameter = GetParameterModel(typeof(ParameterBindingController), actionName);
        // Using any built-in type defined in the Test action
        var serviceProvider = Mock.Of<IServiceProviderIsService>(s => s.IsService(typeof(IApplicationModelProvider)) == true);
        var convention = GetConvention(serviceProviderIsService: serviceProvider);

        // Act
        var result = convention.InferBindingSourceForParameter(parameter);

        // Assert
        Assert.True(convention.IsInferForServiceParametersEnabled);
        Assert.Same(BindingSource.Services, result);
    }

    [Fact]
    public void InferBindingSourceForParameter_ReturnsServicesForIEnumerableOfComplexTypesRegisteredInDI()
    {
        // Arrange
        var actionName = nameof(ParameterBindingController.IEnumerableServiceParameter);
        var parameter = GetParameterModel(typeof(ParameterBindingController), actionName);
        // Using any built-in type defined in the Test action
        var serviceProvider = Mock.Of<IServiceProviderIsService>(s => s.IsService(typeof(IApplicationModelProvider)) == true);
        var convention = GetConvention(serviceProviderIsService: serviceProvider);

        // Act
        var result = convention.InferBindingSourceForParameter(parameter);

        // Assert
        Assert.True(convention.IsInferForServiceParametersEnabled);
        Assert.Same(BindingSource.Services, result);
    }

    [Fact]
    public void PreservesBindingSourceInference_ForFromQueryParameter_WithDefaultName()
    {
        // Arrange
        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var actionName = nameof(ParameterBindingController.FromQuery);
        var convention = GetConvention();
        var action = GetActionModel(typeof(ParameterBindingController), actionName, modelMetadataProvider);

        // Act
        convention.Apply(action);

        // Assert
        var parameter = Assert.Single(action.Parameters);

        var bindingInfo = parameter.BindingInfo;
        Assert.NotNull(bindingInfo);
        Assert.Same(BindingSource.Query, bindingInfo.BindingSource);
        Assert.Null(bindingInfo.BinderModelName);
    }

    [Fact]
    public void PreservesBindingSourceInference_ForFromQueryParameter_WithCustomName()
    {
        // Arrange
        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var actionName = nameof(ParameterBindingController.FromQueryWithCustomName);
        var convention = GetConvention();
        var action = GetActionModel(typeof(ParameterBindingController), actionName, modelMetadataProvider);

        // Act
        convention.Apply(action);

        // Assert
        var parameter = Assert.Single(action.Parameters);

        var bindingInfo = parameter.BindingInfo;
        Assert.NotNull(bindingInfo);
        Assert.Same(BindingSource.Query, bindingInfo.BindingSource);
        Assert.Equal("top", bindingInfo.BinderModelName);
    }

    [Fact]
    public void PreservesBindingSourceInference_ForFromQueryParameterOnComplexType_WithDefaultName()
    {
        // Arrange
        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var actionName = nameof(ParameterBindingController.FromQueryOnComplexType);
        var convention = GetConvention();
        var action = GetActionModel(typeof(ParameterBindingController), actionName, modelMetadataProvider);

        // Act
        convention.Apply(action);

        // Assert
        var parameter = Assert.Single(action.Parameters);

        var bindingInfo = parameter.BindingInfo;
        Assert.NotNull(bindingInfo);
        Assert.Same(BindingSource.Query, bindingInfo.BindingSource);
    }

    [Fact]
    public void PreservesBindingSourceInference_ForFromQueryParameterOnComplexType_WithCustomName()
    {
        // Arrange
        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var actionName = nameof(ParameterBindingController.FromQueryOnComplexTypeWithCustomName);
        var convention = GetConvention();
        var action = GetActionModel(typeof(ParameterBindingController), actionName, modelMetadataProvider);

        // Act
        convention.Apply(action);

        // Assert
        var parameter = Assert.Single(action.Parameters);

        var bindingInfo = parameter.BindingInfo;
        Assert.NotNull(bindingInfo);
        Assert.Same(BindingSource.Query, bindingInfo.BindingSource);
        Assert.Equal("gps", bindingInfo.BinderModelName);
    }

    [Fact]
    public void PreservesBindingSourceInference_ForFromQueryParameterOnCollectionType()
    {
        // Arrange
        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var actionName = nameof(ParameterBindingController.FromQueryOnCollectionType);
        var convention = GetConvention();
        var action = GetActionModel(typeof(ParameterBindingController), actionName, modelMetadataProvider);

        // Act
        convention.Apply(action);

        // Assert
        var parameter = Assert.Single(action.Parameters);

        var bindingInfo = parameter.BindingInfo;
        Assert.NotNull(bindingInfo);
        Assert.Same(BindingSource.Query, bindingInfo.BindingSource);
        Assert.Null(bindingInfo.BinderModelName);
    }

    [Fact]
    public void PreservesBindingSourceInference_ForFromQueryOnArrayType()
    {
        // Arrange
        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var actionName = nameof(ParameterBindingController.FromQueryOnArrayType);
        var convention = GetConvention();
        var action = GetActionModel(typeof(ParameterBindingController), actionName, modelMetadataProvider);

        // Act
        convention.Apply(action);

        // Assert
        var parameter = Assert.Single(action.Parameters);

        var bindingInfo = parameter.BindingInfo;
        Assert.NotNull(bindingInfo);
        Assert.Same(BindingSource.Query, bindingInfo.BindingSource);
        Assert.Null(bindingInfo.BinderModelName);
    }

    [Fact]
    public void PreservesBindingSourceInference_FromQueryOnArrayTypeWithCustomName()
    {
        // Arrange
        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var actionName = nameof(ParameterBindingController.FromQueryOnArrayTypeWithCustomName);
        var convention = GetConvention();
        var action = GetActionModel(typeof(ParameterBindingController), actionName, modelMetadataProvider);

        // Act
        convention.Apply(action);

        // Assert
        var parameter = Assert.Single(action.Parameters);

        var bindingInfo = parameter.BindingInfo;
        Assert.NotNull(bindingInfo);
        Assert.Same(BindingSource.Query, bindingInfo.BindingSource);
        Assert.Equal("ids", bindingInfo.BinderModelName);
    }

    [Fact]
    public void PreservesBindingSourceInference_ForFromRouteParameter_WithDefaultName()
    {
        // Arrange
        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var actionName = nameof(ParameterBindingController.FromRoute);
        var convention = GetConvention();
        var action = GetActionModel(typeof(ParameterBindingController), actionName, modelMetadataProvider);

        // Act
        convention.Apply(action);

        // Assert
        var parameter = Assert.Single(action.Parameters);

        var bindingInfo = parameter.BindingInfo;
        Assert.NotNull(bindingInfo);
        Assert.Same(BindingSource.Path, bindingInfo.BindingSource);
        Assert.Null(bindingInfo.BinderModelName);
    }

    [Fact]
    public void PreservesBindingSourceInference_ForFromRouteParameter_WithCustomName()
    {
        // Arrange
        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var actionName = nameof(ParameterBindingController.FromRouteWithCustomName);
        var convention = GetConvention();
        var action = GetActionModel(typeof(ParameterBindingController), actionName, modelMetadataProvider);

        // Act
        convention.Apply(action);

        // Assert
        var parameter = Assert.Single(action.Parameters);

        var bindingInfo = parameter.BindingInfo;
        Assert.NotNull(bindingInfo);
        Assert.Same(BindingSource.Path, bindingInfo.BindingSource);
        Assert.Equal("top", bindingInfo.BinderModelName);
    }

    [Fact]
    public void PreservesBindingSourceInference_ForFromRouteParameterOnComplexType_WithDefaultName()
    {
        // Arrange
        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var actionName = nameof(ParameterBindingController.FromRouteOnComplexType);
        var convention = GetConvention();
        var action = GetActionModel(typeof(ParameterBindingController), actionName, modelMetadataProvider);

        // Act
        convention.Apply(action);

        // Assert
        var parameter = Assert.Single(action.Parameters);

        var bindingInfo = parameter.BindingInfo;
        Assert.NotNull(bindingInfo);
        Assert.Same(BindingSource.Path, bindingInfo.BindingSource);
    }

    [Fact]
    public void PreservesBindingSourceInference_ForFromRouteParameterOnComplexType_WithCustomName()
    {
        // Arrange
        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var actionName = nameof(ParameterBindingController.FromRouteOnComplexTypeWithCustomName);
        var convention = GetConvention();
        var action = GetActionModel(typeof(ParameterBindingController), actionName, modelMetadataProvider);

        // Act
        convention.Apply(action);

        // Assert
        var parameter = Assert.Single(action.Parameters);

        var bindingInfo = parameter.BindingInfo;
        Assert.NotNull(bindingInfo);
        Assert.Same(BindingSource.Path, bindingInfo.BindingSource);
        Assert.Equal("gps", bindingInfo.BinderModelName);
    }

    [Fact]
    public void PreservesBindingSourceInference_ForParameterWithRequestPredicateAndPropertyFilterProvider()
    {
        // Arrange
        var expectedPredicate = CustomRequestPredicateAndPropertyFilterProviderAttribute.RequestPredicateStatic;
        var expectedPropertyFilter = CustomRequestPredicateAndPropertyFilterProviderAttribute.PropertyFilterStatic;
        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var actionName = nameof(ParameterBindingController.ParameterWithRequestPredicateProvider);
        var action = GetActionModel(typeof(ParameterBindingController), actionName, modelMetadataProvider);
        var convention = GetConvention();

        // Act
        convention.Apply(action);

        // Assert
        var parameter = Assert.Single(action.Parameters);

        var bindingInfo = parameter.BindingInfo;
        Assert.NotNull(bindingInfo);
        Assert.Same(BindingSource.Query, bindingInfo.BindingSource);
        Assert.Same(expectedPredicate, bindingInfo.RequestPredicate);
        Assert.Same(expectedPropertyFilter, bindingInfo.PropertyFilterProvider.PropertyFilter);
        Assert.Null(bindingInfo.BinderModelName);
    }

    private static InferParameterBindingInfoConvention GetConvention(
        IModelMetadataProvider modelMetadataProvider = null,
        IServiceProviderIsService serviceProviderIsService = null)
    {
        modelMetadataProvider = modelMetadataProvider ?? new EmptyModelMetadataProvider();
        serviceProviderIsService = serviceProviderIsService ?? Mock.Of<IServiceProviderIsService>(s => s.IsService(It.IsAny<Type>()) == false);
        return new InferParameterBindingInfoConvention(modelMetadataProvider, serviceProviderIsService);
    }

    private static ApplicationModelProviderContext GetContext(
        Type type,
        IModelMetadataProvider modelMetadataProvider = null)
    {
        var context = new ApplicationModelProviderContext(new[] { type.GetTypeInfo() });
        var mvcOptions = Options.Create(new MvcOptions());
        modelMetadataProvider = modelMetadataProvider ?? new EmptyModelMetadataProvider();
        var convention = new DefaultApplicationModelProvider(mvcOptions, modelMetadataProvider);
        convention.OnProvidersExecuting(context);

        return context;
    }

    private static ActionModel GetActionModel(
        Type controllerType,
        string actionName,
        IModelMetadataProvider modelMetadataProvider = null)
    {
        var context = GetContext(controllerType, modelMetadataProvider);
        var controller = Assert.Single(context.Result.Controllers);
        return Assert.Single(controller.Actions, m => m.ActionName == actionName);
    }

    private static ParameterModel GetParameterModel(Type controllerType, string actionName)
    {
        var action = GetActionModel(controllerType, actionName);
        return Assert.Single(action.Parameters);
    }

    private static ParameterModel GetParameterModel<T>(ActionModel action)
    {
        return Assert.Single(action.Parameters.Where(x => typeof(T).IsAssignableFrom(x.ParameterType)));
    }

    [ApiController]
    [Route("[controller]/[action]")]
    private class ParameterBindingController
    {
        [HttpGet("{parameter}")]
        public IActionResult ActionWithBoundParameter([FromBody] object parameter) => null;

        [HttpGet("{id}")]
        public IActionResult SimpleRouteToken(int id) => null;

        [HttpPost("optional/{id?}")]
        public IActionResult OptionalRouteToken(int id) => null;

        [HttpDelete("delete-by-status/{status:int?}")]
        public IActionResult ConstrainedRouteToken(int status) => null;

        [HttpPut("/absolute-route/{status:int}")]
        public IActionResult ComplexTypeInRoute(object status) => null;

        [HttpPost("multiple/{id}")]
        [HttpPut("multiple/{id}")]
        public IActionResult ParameterInMultipleRoutes(int id) => null;

        [HttpPatch("patchroute")]
        [HttpPost("multiple/{id}")]
        [HttpPut("multiple/{id}")]
        public IActionResult ParameterNotInAllRoutes(int id) => null;

        [HttpPut("put-action/{id}")]
        public IActionResult ComplexTypeModel(TestModel model) => null;

        [HttpPut("put-action/{id}")]
        public IActionResult CompositeComplexTypeModel(CompositeTestModel model) => null;

        [HttpPut("put-action/{id}")]
        public IActionResult SimpleTypeModel(ConvertibleFromString model) => null;

        [HttpPost("form-file")]
        public IActionResult FormFileParameter(IFormFile formFile) => null;

        [HttpPost("form-file-collection")]
        public IActionResult FormFileCollectionParameter(IFormFileCollection formFiles) => null;

        [HttpPost("form-file-sequence")]
        public IActionResult FormFileSequenceParameter(IFormFile[] formFiles) => null;

        [HttpPost]
        public IActionResult FromFormParameter([FromForm] string parameter) => null;

        [HttpPost]
        [Consumes("application/json")]
        public IActionResult ActionWithConsumesAttribute([FromForm] string parameter) => null;

        [HttpPut("cancellation")]
        public IActionResult ComplexTypeModelWithCancellationToken(TestModel model, CancellationToken cancellationToken) => null;

#nullable enable
        [HttpPut("parameter-notnull")]
        public IActionResult RequiredComplexType(TestModel model) => new OkResult();

        [HttpPut("parameter-null")]
        public IActionResult NullableComplexType(TestModel? model) => new OkResult();
#nullable restore

        [HttpPut("parameter-with-default-value")]
        public IActionResult ComplexTypeWithDefaultValue(TestModel model = null) => null;

        [HttpGet("parameter-with-model-binder-attribute")]
        public IActionResult ModelBinderAttribute([ModelBinder(Name = "top")] int value) => null;

        [HttpGet("parameter-with-fromquery")]
        public IActionResult FromQuery([FromQuery] int value) => null;

        [HttpGet("parameter-with-fromquery-and-customname")]
        public IActionResult FromQueryWithCustomName([FromQuery(Name = "top")] int value) => null;

        [HttpGet("parameter-with-fromquery-on-complextype")]
        public IActionResult FromQueryOnComplexType([FromQuery] GpsCoordinates gpsCoordinates) => null;

        [HttpGet("parameter-with-fromquery-on-complextype-and-customname")]
        public IActionResult FromQueryOnComplexTypeWithCustomName([FromQuery(Name = "gps")] GpsCoordinates gpsCoordinates) => null;

        [HttpGet("parameter-with-fromquery-on-collection-type")]
        public IActionResult FromQueryOnCollectionType([FromQuery] ICollection<int> value) => null;

        [HttpGet("parameter-with-fromquery-on-array-type")]
        public IActionResult FromQueryOnArrayType([FromQuery] int[] value) => null;

        [HttpGet("parameter-with-fromquery-on-array-type-customname")]
        public IActionResult FromQueryOnArrayTypeWithCustomName([FromQuery(Name = "ids")] int[] value) => null;

        [HttpGet("parameter-with-fromroute")]
        public IActionResult FromRoute([FromRoute] int value) => null;

        [HttpGet("parameter-with-fromroute-and-customname")]
        public IActionResult FromRouteWithCustomName([FromRoute(Name = "top")] int value) => null;

        [HttpGet("parameter-with-fromroute-on-complextype")]
        public IActionResult FromRouteOnComplexType([FromRoute] GpsCoordinates gpsCoordinates) => null;

        [HttpGet("parameter-with-fromroute-on-complextype-and-customname")]
        public IActionResult FromRouteOnComplexTypeWithCustomName([FromRoute(Name = "gps")] GpsCoordinates gpsCoordinates) => null;

        [HttpGet]
        public IActionResult ParameterWithRequestPredicateProvider([CustomRequestPredicateAndPropertyFilterProvider] int value) => null;

        public IActionResult FromFormFormFileParameters([FromForm] IFormFile p1, [FromForm] IFormFile[] p2, [FromForm] IFormFileCollection p3) => null;

        public IActionResult FormFileParameters(IFormFile p1, IFormFile[] p2, IFormFileCollection p3) => null;

        public IActionResult CollectionOfSimpleTypes(IList<int> parameter) => null;

        public IActionResult IEnumerableOfSimpleTypes(IEnumerable<int> parameter) => null;

        public IActionResult CollectionOfComplexTypes(IList<TestModel> parameter) => null;

        public IActionResult IEnumerableOfComplexTypes(IEnumerable<TestModel> parameter) => null;

        public IActionResult ServiceParameter(IApplicationModelProvider parameter) => null;

        public IActionResult IEnumerableServiceParameter(IEnumerable<IApplicationModelProvider> parameter) => null;
    }

    [ApiController]
    [Route("[controller]/[action]")]
    private class ModelBinderOnParameterController
    {
        [HttpGet]
        public IActionResult ModelBinderAttributeWithExplicitModelName([ModelBinder(Name = "top")] int value) => null;

        [HttpGet]
        public IActionResult ModelBinderType([ModelBinder(typeof(TestModelBinder))] string name) => null;

        [HttpGet]
        public IActionResult ModelBinderTypeWithExplicitModelName([ModelBinder(typeof(TestModelBinder), Name = "foo")] string name) => null;
    }

    [ApiController]
    [Route("/route1/[controller]/[action]/{id}")]
    [Route("/route2/[controller]/[action]/{id?}")]
    private class ParameterInController
    {
        [HttpGet]
        public IActionResult ActionWithoutRoute(int id) => null;

        [HttpGet("stuff/{status}")]
        public IActionResult ActionWithRoute(int id) => null;

        [HttpGet("/absolute-route")]
        public IActionResult AbsoluteRoute(int id) => null;

        [HttpPut]
        [HttpPost("stuff/{status}")]
        public IActionResult MultipleRoute(int id) => null;

        [HttpPut]
        [HttpPost("~/stuff/{status}")]
        public IActionResult MultipleRouteWithOverride(int id) => null;
    }

    [ApiController]
    private class ParameterBindingNoRoutesOnController
    {
        [HttpGet("{parameter}")]
        public IActionResult SimpleRoute(int parameter) => null;

        [HttpGet]
        public IActionResult NoRouteTemplate(int id) => null;

        [HttpPost("multiple/{id}")]
        [HttpPut("multiple/{id}")]
        public IActionResult ParameterInMultipleRoutes(int id) => null;
    }

    private class TestModel { }

    private class CompositeTestModel
    {
        [FromQuery]
        public int Id { get; set; }

        public TestModel TestModel { get; set; }
    }

    [TypeConverter(typeof(ConvertibleFromStringConverter))]
    private class ConvertibleFromString { }

    private class ConvertibleFromStringConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            => sourceType == typeof(string);
    }

    private class CustomRequestPredicateAndPropertyFilterProviderAttribute : Attribute, IRequestPredicateProvider, IPropertyFilterProvider
    {
        public static Func<ActionContext, bool> RequestPredicateStatic => (c) => true;
        public static Func<ModelMetadata, bool> PropertyFilterStatic => (c) => true;

        public Func<ActionContext, bool> RequestPredicate => RequestPredicateStatic;

        public Func<ModelMetadata, bool> PropertyFilter => PropertyFilterStatic;
    }

    private class GpsCoordinates
    {
        public long Latitude { get; set; }
        public long Longitude { get; set; }
    }

    private class TestModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            throw new NotImplementedException();
        }
    }

    private class ControllerWithBoundProperty
    {
        [FromQuery]
        public TestModel TestProperty { get; set; }

        [FromForm]
        public IList<IFormFile> Files { get; set; }

        public IActionResult SomeAction([FromQuery] TestModel test) => null;
    }

    private class ControllerWithBoundCollectionProperty
    {
        [FromQuery]
        public List<int> TestProperty { get; set; }

        public IActionResult SomeAction([FromQuery] List<int> test) => null;
    }

    private class Car { }

    private class MultipleFromBodyController
    {
        public IActionResult MultipleInferred(TestModel a, Car b) => null;

        public IActionResult InferredAndSpecified(TestModel a, [FromBody] int b) => null;

        public IActionResult MultipleSpecified([FromBody] decimal a, [FromBody] int b) => null;
    }

    private class ParameterWithBindingInfo
    {
        [HttpGet("test")]
        public IActionResult Action([ModelBinder(typeof(ComplexObjectModelBinder))] Car car) => null;
    }
}
