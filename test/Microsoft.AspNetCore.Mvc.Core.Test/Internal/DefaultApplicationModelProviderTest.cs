// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class DefaultApplicationModelProviderTest
    {
        [Fact]
        public void CreateControllerModel_DerivedFromControllerClass_HasFilter()
        {
            // Arrange
            var builder = new TestApplicationModelProvider();
            var typeInfo = typeof(StoreController).GetTypeInfo();

            // Act
            var model = builder.CreateControllerModel(typeInfo);

            // Assert
            var filter = Assert.Single(model.Filters);
            Assert.IsType<ControllerActionFilter>(filter);
        }

        [Fact]
        public void OnProvidersExecuting_AddsControllerProperties()
        {
            // Arrange
            var builder = new TestApplicationModelProvider();
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
                    Assert.Equal(nameof(ModelBinderController.Unbound), property.PropertyName);
                    Assert.Null(property.BindingInfo);
                    Assert.Same(controllerModel, property.Controller);
                });
        }

        [Fact]
        public void OnProvidersExecuting_AddsBindingSources_ForActionParameters()
        {
            // Arrange
            var builder = new TestApplicationModelProvider();
            var typeInfo = typeof(ModelBinderController).GetTypeInfo();

            var context = new ApplicationModelProviderContext(new[] { typeInfo });

            // Act
            builder.OnProvidersExecuting(context);

            // Assert
            var controllerModel = Assert.Single(context.Result.Controllers);
            var action = Assert.Single(controllerModel.Actions);
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

        // This class has a filter attribute, but doesn't implement any filter interfaces,
        // so ControllerFilter is not present.
        [Fact]
        public void CreateControllerModel_ClassWithoutFilterInterfaces_HasNoControllerFilter()
        {
            // Arrange
            var builder = new TestApplicationModelProvider();
            var typeInfo = typeof(NoFiltersController).GetTypeInfo();

            // Act
            var model = builder.CreateControllerModel(typeInfo);

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
            var model = builder.CreateControllerModel(typeInfo);

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
            var model = builder.CreateControllerModel(typeInfo);

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
            var model = builder.CreateControllerModel(typeInfo);

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
            var model = builder.CreateControllerModel(typeInfo);

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
            var isValid = builder.IsAction(typeof(DerivedController).GetTypeInfo(), method);

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
            var isValid = builder.IsAction(typeof(BaseController).GetTypeInfo(), method);

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
            var isValid = builder.IsAction(typeof(DerivedController).GetTypeInfo(), method);

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
            var isValid = builder.IsAction(typeof(OperatorOverloadingController).GetTypeInfo(), method);

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
            var isValid = builder.IsAction(typeof(DerivedController).GetTypeInfo(), method);

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
            var isValid = builder.IsAction(typeof(DerivedController).GetTypeInfo(), method);

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
            var isValid = builder.IsAction(typeof(DerivedController).GetTypeInfo(), method);

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
            var isValid = builder.IsAction(typeInfo, method);

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
                var isValid = builder.IsAction(typeInfo, method);

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
            var isValid = builder.IsAction(typeInfo, method);

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
            var isValid = builder.IsAction(typeInfo, method);

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
            var isValid = builder.IsAction(typeInfo, method);

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
                var isValid = builder.IsAction(typeInfo, method);

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
                var isValid = builder.IsAction(typeInfo, method);

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
            var isValid = builder.IsAction(typeof(DerivedController).GetTypeInfo(), method);

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
        public void CreateActionModel_AttributeRouteOnAction_CreatesOneActionInforPerRouteTemplate()
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
        public void CreateActionModel_RouteOnController_CreatesOneActionInforPerRouteTemplateOnAction(Type controller)
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

        private IList<AttributeRouteModel> GetAttributeRoutes(IList<SelectorModel> selectors)
        {
            return selectors
                .Where(sm => sm.AttributeRouteModel != null)
                .Select(sm => sm.AttributeRouteModel)
                .ToList();
        }

        private class DerivedFromControllerAndExplicitIDisposableImplementationController
            : Mvc.Controller, IDisposable
        {
            void IDisposable.Dispose()
            {
                throw new NotImplementedException();
            }
        }

        private class DerivedFromControllerAndHidesBaseDisposeMethodController : Mvc.Controller
        {
            public new void Dispose()
            {
                throw new NotImplementedException();
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

        public class ModelBinderController
        {
            [FromQuery]
            public string Bound { get; set; }

            public string Unbound { get; set; }

            public IFormFile FormFile { get; set; }

            public IActionResult PostAction([FromQuery] string fromQuery, IFormFileCollection formFileCollection, string unbound) => null;
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

        private class TestApplicationModelProvider : DefaultApplicationModelProvider
        {
            public TestApplicationModelProvider()
                : this(Options.Create(new MvcOptions()))
            {
            }

            public TestApplicationModelProvider(
                IOptions<MvcOptions> options)
                : base(options)
            {
            }

            public new ControllerModel CreateControllerModel(TypeInfo typeInfo)
            {
                return base.CreateControllerModel(typeInfo);
            }

            public new ActionModel CreateActionModel(TypeInfo typeInfo, MethodInfo methodInfo)
            {
                return base.CreateActionModel(typeInfo, methodInfo);
            }

            public new PropertyModel CreatePropertyModel(PropertyInfo propertyInfo)
            {
                return base.CreatePropertyModel(propertyInfo);
            }

            public new bool IsAction(TypeInfo typeInfo, MethodInfo methodInfo)
            {
                return base.IsAction(typeInfo, methodInfo);
            }

            public new ParameterModel CreateParameterModel(ParameterInfo parameterInfo)
            {
                return base.CreateParameterModel(parameterInfo);
            }
        }
    }
}