// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNet.Mvc.ApplicationModels
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
            var model = Assert.Single(context.Result.Controllers);
            Assert.Equal(2, model.ControllerProperties.Count);
            Assert.Equal("Bound", model.ControllerProperties[0].PropertyName);
            Assert.Equal(BindingSource.Query, model.ControllerProperties[0].BindingInfo.BindingSource);
            Assert.NotNull(model.ControllerProperties[0].Controller);
            var attribute = Assert.Single(model.ControllerProperties[0].Attributes);
            Assert.IsType<FromQueryAttribute>(attribute);
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
            Assert.Equal(2, model.AttributeRoutes.Count);
            Assert.Equal(2, model.Attributes.Count);

            var route = Assert.Single(model.AttributeRoutes, r => r.Template == "A");
            Assert.Contains(route.Attribute, model.Attributes);

            route = Assert.Single(model.AttributeRoutes, r => r.Template == "B");
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
            Assert.Equal(2, model.AttributeRoutes.Count);
            Assert.Equal(2, model.Attributes.Count);

            var route = Assert.Single(model.AttributeRoutes, r => r.Template == "C");
            Assert.Contains(route.Attribute, model.Attributes);

            route = Assert.Single(model.AttributeRoutes, r => r.Template == "D");
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
        public void BuildActionModels_ConventionallyRoutedAction_WithoutHttpConstraints()
        {
            // Arrange
            var builder = new TestApplicationModelProvider();
            var typeInfo = typeof(ConventionallyRoutedController).GetTypeInfo();
            var actionName = nameof(ConventionallyRoutedController.Edit);

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.AsType().GetMethod(actionName));

            // Assert
            var action = Assert.Single(actions);
            Assert.Equal("Edit", action.ActionName);
            Assert.Empty(action.HttpMethods);
            Assert.Null(action.AttributeRouteModel);
            Assert.Empty(action.Attributes);
        }

        [Fact]
        public void BuildActionModels_ConventionallyRoutedAction_WithHttpConstraints()
        {
            // Arrange
            var builder = new TestApplicationModelProvider();
            var typeInfo = typeof(ConventionallyRoutedController).GetTypeInfo();
            var actionName = nameof(ConventionallyRoutedController.Update);

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.AsType().GetMethod(actionName));

            // Assert
            var action = Assert.Single(actions);
            Assert.Contains("PUT", action.HttpMethods);
            Assert.Contains("PATCH", action.HttpMethods);

            Assert.Equal("Update", action.ActionName);
            Assert.Null(action.AttributeRouteModel);
            Assert.IsType<CustomHttpMethodsAttribute>(Assert.Single(action.Attributes));
        }

        [Fact]
        public void BuildActionModels_ConventionallyRoutedActionWithHttpConstraints_AndInvalidRouteTemplateProvider()
        {
            // Arrange
            var builder = new TestApplicationModelProvider();
            var typeInfo = typeof(ConventionallyRoutedController).GetTypeInfo();
            var actionName = nameof(ConventionallyRoutedController.Delete);

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.AsType().GetMethod(actionName));

            // Assert
            var action = Assert.Single(actions);
            Assert.Contains("DELETE", action.HttpMethods);
            Assert.Contains("HEAD", action.HttpMethods);

            Assert.Equal("Delete", action.ActionName);
            Assert.Null(action.AttributeRouteModel);
            Assert.Single(action.Attributes.OfType<HttpDeleteAttribute>());
            Assert.Single(action.Attributes.OfType<HttpHeadAttribute>());
        }

        [Fact]
        public void BuildActionModels_ConventionallyRoutedAction_WithMultipleHttpConstraints()
        {
            // Arrange
            var builder = new TestApplicationModelProvider();
            var typeInfo = typeof(ConventionallyRoutedController).GetTypeInfo();
            var actionName = nameof(ConventionallyRoutedController.Details);

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.AsType().GetMethod(actionName));

            // Assert
            var action = Assert.Single(actions);
            Assert.Contains("GET", action.HttpMethods);
            Assert.Contains("POST", action.HttpMethods);
            Assert.Contains("HEAD", action.HttpMethods);
            Assert.Equal("Details", action.ActionName);
            Assert.Null(action.AttributeRouteModel);
        }

        [Fact]
        public void BuildActionModels_ConventionallyRoutedAction_WithMultipleOverlappingHttpConstraints()
        {
            // Arrange
            var builder = new TestApplicationModelProvider();
            var typeInfo = typeof(ConventionallyRoutedController).GetTypeInfo();
            var actionName = nameof(ConventionallyRoutedController.List);

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.AsType().GetMethod(actionName));

            // Assert
            var action = Assert.Single(actions);
            Assert.Contains("GET", action.HttpMethods);
            Assert.Contains("PUT", action.HttpMethods);
            Assert.Contains("POST", action.HttpMethods);
            Assert.Equal("List", action.ActionName);
            Assert.Null(action.AttributeRouteModel);
        }

        [Fact]
        public void BuildActionModels_AttributeRouteOnAction()
        {
            // Arrange
            var builder = new TestApplicationModelProvider();
            var typeInfo = typeof(NoRouteAttributeOnControllerController).GetTypeInfo();
            var actionName = nameof(NoRouteAttributeOnControllerController.Edit);

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.AsType().GetMethod(actionName));

            // Assert
            var action = Assert.Single(actions);

            Assert.Equal("Edit", action.ActionName);

            var httpMethod = Assert.Single(action.HttpMethods);
            Assert.Equal("HEAD", httpMethod);

            Assert.NotNull(action.AttributeRouteModel);
            Assert.Equal("Change", action.AttributeRouteModel.Template);

            Assert.IsType<HttpHeadAttribute>(Assert.Single(action.Attributes));
        }

        [Fact]
        public void BuildActionModels_AttributeRouteOnAction_RouteAttribute()
        {
            // Arrange
            var builder = new TestApplicationModelProvider();
            var typeInfo = typeof(NoRouteAttributeOnControllerController).GetTypeInfo();
            var actionName = nameof(NoRouteAttributeOnControllerController.Update);

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.AsType().GetMethod(actionName));

            // Assert
            var action = Assert.Single(actions);

            Assert.Equal("Update", action.ActionName);

            Assert.Empty(action.HttpMethods);

            Assert.NotNull(action.AttributeRouteModel);
            Assert.Equal("Update", action.AttributeRouteModel.Template);

            Assert.IsType<RouteAttribute>(Assert.Single(action.Attributes));
        }

        [Fact]
        public void BuildActionModels_AttributeRouteOnAction_AcceptVerbsAttributeWithTemplate()
        {
            // Arrange
            var builder = new TestApplicationModelProvider();
            var typeInfo = typeof(NoRouteAttributeOnControllerController).GetTypeInfo();
            var actionName = nameof(NoRouteAttributeOnControllerController.List);

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.AsType().GetMethod(actionName));

            // Assert
            var action = Assert.Single(actions);

            Assert.Equal("List", action.ActionName);

            Assert.Equal(new[] { "GET", "HEAD" }, action.HttpMethods.OrderBy(m => m, StringComparer.Ordinal));

            Assert.NotNull(action.AttributeRouteModel);
            Assert.Equal("ListAll", action.AttributeRouteModel.Template);

            Assert.IsType<AcceptVerbsAttribute>(Assert.Single(action.Attributes));
        }

        [Fact]
        public void BuildActionModels_AttributeRouteOnAction_CreatesOneActionInforPerRouteTemplate()
        {
            // Arrange
            var builder = new TestApplicationModelProvider();
            var typeInfo = typeof(NoRouteAttributeOnControllerController).GetTypeInfo();
            var actionName = nameof(NoRouteAttributeOnControllerController.Index);

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.AsType().GetMethod(actionName));

            // Assert
            Assert.Equal(2, actions.Count());

            foreach (var action in actions)
            {
                Assert.Equal("Index", action.ActionName);
                Assert.NotNull(action.AttributeRouteModel);
            }

            var list = Assert.Single(actions, ai => ai.AttributeRouteModel.Template.Equals("List"));
            var listMethod = Assert.Single(list.HttpMethods);
            Assert.Equal("POST", listMethod);
            Assert.IsType<HttpPostAttribute>(Assert.Single(list.Attributes));

            var all = Assert.Single(actions, ai => ai.AttributeRouteModel.Template.Equals("All"));
            var allMethod = Assert.Single(all.HttpMethods);
            Assert.Equal("GET", allMethod);
            Assert.IsType<HttpGetAttribute>(Assert.Single(all.Attributes));
        }

        [Fact]
        public void BuildActionModels_NoRouteOnController_AllowsConventionallyRoutedActions_OnTheSameController()
        {
            // Arrange
            var builder = new TestApplicationModelProvider();
            var typeInfo = typeof(NoRouteAttributeOnControllerController).GetTypeInfo();
            var actionName = nameof(NoRouteAttributeOnControllerController.Remove);

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.AsType().GetMethod(actionName));

            // Assert
            var action = Assert.Single(actions);

            Assert.Equal("Remove", action.ActionName);

            Assert.Empty(action.HttpMethods);

            Assert.Null(action.AttributeRouteModel);

            Assert.Empty(action.Attributes);
        }

        [Theory]
        [InlineData(typeof(SingleRouteAttributeController))]
        [InlineData(typeof(MultipleRouteAttributeController))]
        public void BuildActionModels_RouteAttributeOnController_CreatesAttributeRoute_ForNonAttributedActions(Type controller)
        {
            // Arrange
            var builder = new TestApplicationModelProvider();
            var typeInfo = controller.GetTypeInfo();

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.AsType().GetMethod("Delete"));

            // Assert
            var action = Assert.Single(actions);

            Assert.Equal("Delete", action.ActionName);

            Assert.Empty(action.HttpMethods);

            Assert.Null(action.AttributeRouteModel);

            Assert.Empty(action.Attributes);
        }

        [Theory]
        [InlineData(typeof(SingleRouteAttributeController))]
        [InlineData(typeof(MultipleRouteAttributeController))]
        public void BuildActionModels_RouteOnController_CreatesOneActionInforPerRouteTemplateOnAction(Type controller)
        {
            // Arrange
            var builder = new TestApplicationModelProvider();
            var typeInfo = controller.GetTypeInfo();

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.AsType().GetMethod("Index"));

            // Assert
            Assert.Equal(2, actions.Count());

            foreach (var action in actions)
            {
                Assert.Equal("Index", action.ActionName);

                var httpMethod = Assert.Single(action.HttpMethods);
                Assert.Equal("GET", httpMethod);

                Assert.NotNull(action.AttributeRouteModel.Template);

                Assert.IsType<HttpGetAttribute>(Assert.Single(action.Attributes));
            }

            Assert.Single(actions, ai => ai.AttributeRouteModel.Template.Equals("List"));
            Assert.Single(actions, ai => ai.AttributeRouteModel.Template.Equals("All"));
        }

        [Fact]
        public void BuildActionModels_MixedHttpVerbsAndRoutes_EmptyVerbWithRoute()
        {
            // Arrange
            var builder = new TestApplicationModelProvider();
            var typeInfo = typeof(MixedHttpVerbsAndRouteAttributeController).GetTypeInfo();
            var actionName = nameof(MixedHttpVerbsAndRouteAttributeController.VerbAndRoute);

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.AsType().GetMethod(actionName));

            // Assert
            var action = Assert.Single(actions);
            Assert.Equal<string>(new string[] { "GET" }, action.HttpMethods);
            Assert.Equal("Products", action.AttributeRouteModel.Template);
        }

        [Fact]
        public void BuildActionModels_MixedHttpVerbsAndRoutes_MultipleEmptyVerbsWithMultipleRoutes()
        {
            // Arrange
            var builder = new TestApplicationModelProvider();
            var typeInfo = typeof(MixedHttpVerbsAndRouteAttributeController).GetTypeInfo();
            var actionName = nameof(MixedHttpVerbsAndRouteAttributeController.MultipleVerbsAndRoutes);

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.AsType().GetMethod(actionName));

            // Assert
            Assert.Equal(2, actions.Count());

            // OrderBy is used because the order of the results may very depending on the platform / client.
            var action = Assert.Single(actions, a => a.AttributeRouteModel.Template == "Products");
            Assert.Equal(new[] { "GET", "POST" }, action.HttpMethods.OrderBy(key => key, StringComparer.Ordinal));

            action = Assert.Single(actions, a => a.AttributeRouteModel.Template == "v2/Products");
            Assert.Equal(new[] { "GET", "POST" }, action.HttpMethods.OrderBy(key => key, StringComparer.Ordinal));
        }

        [Fact]
        public void BuildActionModels_MixedHttpVerbsAndRoutes_MultipleEmptyAndNonEmptyVerbsWithMultipleRoutes()
        {
            // Arrange
            var builder = new TestApplicationModelProvider();
            var typeInfo = typeof(MixedHttpVerbsAndRouteAttributeController).GetTypeInfo();
            var actionName = nameof(MixedHttpVerbsAndRouteAttributeController.MultipleVerbsWithAnyWithoutTemplateAndRoutes);

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.AsType().GetMethod(actionName));

            // Assert
            Assert.Equal(3, actions.Count());

            var action = Assert.Single(actions, a => a.AttributeRouteModel.Template == "Products");
            Assert.Equal<string>(new string[] { "GET" }, action.HttpMethods);

            action = Assert.Single(actions, a => a.AttributeRouteModel.Template == "v2/Products");
            Assert.Equal<string>(new string[] { "GET" }, action.HttpMethods);

            action = Assert.Single(actions, a => a.AttributeRouteModel.Template == "Products/Buy");
            Assert.Equal<string>(new string[] { "POST" }, action.HttpMethods);
        }

        [Fact]
        public void BuildActionModels_MixedHttpVerbsAndRoutes_MultipleEmptyAndNonEmptyVerbs()
        {
            // Arrange
            var builder = new TestApplicationModelProvider();
            var typeInfo = typeof(MixedHttpVerbsAndRouteAttributeController).GetTypeInfo();
            var actionName = nameof(MixedHttpVerbsAndRouteAttributeController.Invalid);

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.AsType().GetMethod(actionName));

            // Assert
            Assert.Equal(2, actions.Count());

            var action = Assert.Single(actions, a => a.AttributeRouteModel?.Template == "Products");
            Assert.Equal<string>(new string[] { "POST" }, action.HttpMethods);

            action = Assert.Single(actions, a => a.AttributeRouteModel?.Template == null);
            Assert.Equal<string>(new string[] { "GET" }, action.HttpMethods);
        }

        [Fact]
        public void BuildActionModels_InheritedAttributeRoutes()
        {
            // Arrange
            var builder = new TestApplicationModelProvider();
            var typeInfo = typeof(DerivedClassInheritsAttributeRoutesController).GetTypeInfo();
            var actionName = nameof(DerivedClassInheritsAttributeRoutesController.Edit);

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.AsType().GetMethod(actionName));

            // Assert
            Assert.Equal(2, actions.Count());

            var action = Assert.Single(actions, a => a.AttributeRouteModel?.Template == "A");
            Assert.Equal(1, action.Attributes.Count);
            Assert.Contains(action.AttributeRouteModel.Attribute, action.Attributes);

            action = Assert.Single(actions, a => a.AttributeRouteModel?.Template == "B");
            Assert.Equal(1, action.Attributes.Count);
            Assert.Contains(action.AttributeRouteModel.Attribute, action.Attributes);
        }

        [Fact]
        public void BuildActionModels_InheritedAttributeRoutesOverridden()
        {
            // Arrange
            var builder = new TestApplicationModelProvider();
            var typeInfo = typeof(DerivedClassOverridesAttributeRoutesController).GetTypeInfo();
            var actionName = nameof(DerivedClassOverridesAttributeRoutesController.Edit);

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.AsType().GetMethod(actionName));

            // Assert
            Assert.Equal(2, actions.Count());

            var action = Assert.Single(actions, a => a.AttributeRouteModel?.Template == "C");
            Assert.Equal(1, action.Attributes.Count);
            Assert.Contains(action.AttributeRouteModel.Attribute, action.Attributes);

            action = Assert.Single(actions, a => a.AttributeRouteModel?.Template == "D");
            Assert.Equal(1, action.Attributes.Count);
            Assert.Contains(action.AttributeRouteModel.Attribute, action.Attributes);
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

            public IEnumerable<string> HttpMethods
            {
                get
                {
                    return _methods;
                }
            }
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
            public void OnAuthorization(AuthorizationContext context)
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
                : this(new TestOptionsManager<MvcOptions>())
            {
            }

            public TestApplicationModelProvider(
                IOptions<MvcOptions> options)
                : base(options)
            {
                Options = options.Value;
            }

            public MvcOptions Options { get; }

            public new IEnumerable<ControllerModel> BuildControllerModels(TypeInfo typeInfo)
            {
                return base.BuildControllerModels(typeInfo);
            }

            public new ControllerModel CreateControllerModel(TypeInfo typeInfo)
            {
                return base.CreateControllerModel(typeInfo);
            }

            public new PropertyModel CreatePropertyModel(PropertyInfo propertyInfo)
            {
                return base.CreatePropertyModel(propertyInfo);
            }

            public new IEnumerable<ActionModel> BuildActionModels(TypeInfo typeInfo, MethodInfo methodInfo)
            {
                return base.BuildActionModels(typeInfo, methodInfo);
            }

            public new ActionModel CreateActionModel(MethodInfo methodInfo, IReadOnlyList<object> attributes)
            {
                return base.CreateActionModel(methodInfo, attributes);
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