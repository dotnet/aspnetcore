// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Authorization;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ApplicationModels
{
    public class DefaultActionModelBuilderTest
    {
        [Theory]
        [InlineData("GetFromDerived", true)]
        [InlineData("NewMethod", true)] // "NewMethod" is a public method declared with keyword "new".
        [InlineData("GetFromBase", true)]
        public void IsAction_WithInheritedMethods(string methodName, bool expected)
        {
            // Arrange
            var builder = CreateTestAccessibleActionModelBuilder();
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
            var builder = CreateTestAccessibleActionModelBuilder();
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
            var builder = CreateTestAccessibleActionModelBuilder();
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
            var builder = CreateTestAccessibleActionModelBuilder();
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
            var builder = CreateTestAccessibleActionModelBuilder();
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
            var builder = CreateTestAccessibleActionModelBuilder();
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
            var builder = CreateTestAccessibleActionModelBuilder();
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
            var builder = CreateTestAccessibleActionModelBuilder();
            var typeInfo = typeof(DerivedController).GetTypeInfo();
            var methodInfo =
                typeInfo.GetRuntimeInterfaceMap(typeof(IDisposable)).TargetMethods[0];
            var method = typeInfo.GetMethods().Where(m => (m == methodInfo)).SingleOrDefault();
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
            var builder = CreateTestAccessibleActionModelBuilder();
            var typeInfo = typeof(DerivedController).GetTypeInfo();
            var methodInfo =
                typeInfo.GetRuntimeInterfaceMap(typeof(IDisposable)).TargetMethods[0];
            var methods = typeInfo.GetMethods().Where(m => m.Name.Equals("Dispose") && m != methodInfo);

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
            var builder = CreateTestAccessibleActionModelBuilder();
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
            var builder = CreateTestAccessibleActionModelBuilder();
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
            var builder = CreateTestAccessibleActionModelBuilder();
            var typeInfo = typeof(IDisposablePocoController).GetTypeInfo();
            var methodInfo =
                typeInfo.GetRuntimeInterfaceMap(typeof(IDisposable)).TargetMethods[0];
            var method = typeInfo.GetMethods().Where(m => (m == methodInfo)).SingleOrDefault();
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
            var builder = CreateTestAccessibleActionModelBuilder();
            var typeInfo = typeof(IDisposablePocoController).GetTypeInfo();
            var methodInfo =
                typeInfo.GetRuntimeInterfaceMap(typeof(IDisposable)).TargetMethods[0];
            var methods = typeInfo.GetMethods().Where(m => m.Name.Equals("Dispose") && m != methodInfo);

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
            var builder = CreateTestAccessibleActionModelBuilder();
            var typeInfo = typeof(SimplePocoController).GetTypeInfo();
            var methods = typeInfo.GetMethods().Where(m => m.Name.Equals("Dispose"));

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
            var builder = CreateTestAccessibleActionModelBuilder();
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
        public void GetActions_ConventionallyRoutedAction_WithoutHttpConstraints()
        {
            // Arrange
            var builder = CreateTestDefaultActionModelBuilder();
            var typeInfo = typeof(ConventionallyRoutedController).GetTypeInfo();
            var actionName = nameof(ConventionallyRoutedController.Edit);

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.GetMethod(actionName));

            // Assert
            var action = Assert.Single(actions);
            Assert.Equal("Edit", action.ActionName);
            Assert.Empty(action.HttpMethods);
            Assert.Null(action.AttributeRouteModel);
            Assert.Empty(action.Attributes);
        }

        [Fact]
        public void GetActions_ConventionallyRoutedAction_WithHttpConstraints()
        {
            // Arrange
            var builder = CreateTestDefaultActionModelBuilder();
            var typeInfo = typeof(ConventionallyRoutedController).GetTypeInfo();
            var actionName = nameof(ConventionallyRoutedController.Update);

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.GetMethod(actionName));

            // Assert
            var action = Assert.Single(actions);
            Assert.Contains("PUT", action.HttpMethods);
            Assert.Contains("PATCH", action.HttpMethods);

            Assert.Equal("Update", action.ActionName);
            Assert.Null(action.AttributeRouteModel);
            Assert.IsType<CustomHttpMethodsAttribute>(Assert.Single(action.Attributes));
        }

        [Fact]
        public void GetActions_BaseAuthorizeFiltersAreStillValidWhenOverriden()
        {
            // Arrange
            var options = new AuthorizationOptions();
            options.AddPolicy("Base", policy => policy.RequireClaim("Basic").RequireClaim("Basic2"));
            options.AddPolicy("Derived", policy => policy.RequireClaim("Derived"));
            var builder = CreateTestDefaultActionModelBuilder(options);
            var typeInfo = typeof(DerivedController).GetTypeInfo();
            var actionName = nameof(DerivedController.Authorize);

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.GetMethod(actionName));

            // Assert
            var action = Assert.Single(actions);
            Assert.Equal("Authorize", action.ActionName);
            Assert.Null(action.AttributeRouteModel);
            var authorizeFilters = action.Filters.OfType<AuthorizeFilter>();
            Assert.Single(authorizeFilters);
            Assert.Equal(3, authorizeFilters.First().Policy.Requirements.Count);
        }

        [Fact]
        public void GetActions_ConventionallyRoutedActionWithHttpConstraints_AndInvalidRouteTemplateProvider()
        {
            // Arrange
            var builder = CreateTestDefaultActionModelBuilder();
            var typeInfo = typeof(ConventionallyRoutedController).GetTypeInfo();
            var actionName = nameof(ConventionallyRoutedController.Delete);

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.GetMethod(actionName));

            // Assert
            var action = Assert.Single(actions);
            Assert.Equal("Delete", action.ActionName);

            var httpMethod = Assert.Single(action.HttpMethods);
            Assert.Equal("DELETE", httpMethod);
            Assert.Null(action.AttributeRouteModel);

            Assert.IsType<HttpDeleteAttribute>(Assert.Single(action.Attributes));
        }

        [Fact]
        public void GetActions_ConventionallyRoutedAction_WithMultipleHttpConstraints()
        {
            // Arrange
            var builder = CreateTestDefaultActionModelBuilder();
            var typeInfo = typeof(ConventionallyRoutedController).GetTypeInfo();
            var actionName = nameof(ConventionallyRoutedController.Details);

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.GetMethod(actionName));

            // Assert
            var action = Assert.Single(actions);
            Assert.Contains("GET", action.HttpMethods);
            Assert.Contains("POST", action.HttpMethods);
            Assert.Equal("Details", action.ActionName);
            Assert.Null(action.AttributeRouteModel);
        }

        [Fact]
        public void GetActions_ConventionallyRoutedAction_WithMultipleOverlappingHttpConstraints()
        {
            // Arrange
            var builder = CreateTestDefaultActionModelBuilder();
            var typeInfo = typeof(ConventionallyRoutedController).GetTypeInfo();
            var actionName = nameof(ConventionallyRoutedController.List);

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.GetMethod(actionName));

            // Assert
            var action = Assert.Single(actions);
            Assert.Contains("GET", action.HttpMethods);
            Assert.Contains("PUT", action.HttpMethods);
            Assert.Contains("POST", action.HttpMethods);
            Assert.Equal("List", action.ActionName);
            Assert.Null(action.AttributeRouteModel);
        }

        [Fact]
        public void GetActions_AttributeRouteOnAction()
        {
            // Arrange
            var builder = CreateTestDefaultActionModelBuilder();
            var typeInfo = typeof(NoRouteAttributeOnControllerController).GetTypeInfo();
            var actionName = nameof(NoRouteAttributeOnControllerController.Edit);

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.GetMethod(actionName));

            // Assert
            var action = Assert.Single(actions);

            Assert.Equal("Edit", action.ActionName);

            var httpMethod = Assert.Single(action.HttpMethods);
            Assert.Equal("POST", httpMethod);

            Assert.NotNull(action.AttributeRouteModel);
            Assert.Equal("Change", action.AttributeRouteModel.Template);

            Assert.IsType<HttpPostAttribute>(Assert.Single(action.Attributes));
        }

        [Fact]
        public void GetActions_AttributeRouteOnAction_RouteAttribute()
        {
            // Arrange
            var builder = CreateTestDefaultActionModelBuilder();
            var typeInfo = typeof(NoRouteAttributeOnControllerController).GetTypeInfo();
            var actionName = nameof(NoRouteAttributeOnControllerController.Update);

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.GetMethod(actionName));

            // Assert
            var action = Assert.Single(actions);

            Assert.Equal("Update", action.ActionName);

            Assert.Empty(action.HttpMethods);

            Assert.NotNull(action.AttributeRouteModel);
            Assert.Equal("Update", action.AttributeRouteModel.Template);

            Assert.IsType<RouteAttribute>(Assert.Single(action.Attributes));
        }

        [Fact]
        public void GetActions_AttributeRouteOnAction_AcceptVerbsAttributeWithTemplate()
        {
            // Arrange
            var builder = CreateTestDefaultActionModelBuilder();
            var typeInfo = typeof(NoRouteAttributeOnControllerController).GetTypeInfo();
            var actionName = nameof(NoRouteAttributeOnControllerController.List);

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.GetMethod(actionName));

            // Assert
            var action = Assert.Single(actions);

            Assert.Equal("List", action.ActionName);

            Assert.Equal(new[] { "GET", "HEAD" }, action.HttpMethods.OrderBy(m => m, StringComparer.Ordinal));

            Assert.NotNull(action.AttributeRouteModel);
            Assert.Equal("ListAll", action.AttributeRouteModel.Template);

            Assert.IsType<AcceptVerbsAttribute>(Assert.Single(action.Attributes));
        }

        [Fact]
        public void GetActions_AttributeRouteOnAction_CreatesOneActionInforPerRouteTemplate()
        {
            // Arrange
            var builder = CreateTestDefaultActionModelBuilder();
            var typeInfo = typeof(NoRouteAttributeOnControllerController).GetTypeInfo();
            var actionName = nameof(NoRouteAttributeOnControllerController.Index);

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.GetMethod(actionName));

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
        public void GetActions_NoRouteOnController_AllowsConventionallyRoutedActions_OnTheSameController()
        {
            // Arrange
            var builder = CreateTestDefaultActionModelBuilder();
            var typeInfo = typeof(NoRouteAttributeOnControllerController).GetTypeInfo();
            var actionName = nameof(NoRouteAttributeOnControllerController.Remove);

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.GetMethod(actionName));

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
        public void GetActions_RouteAttributeOnController_CreatesAttributeRoute_ForNonAttributedActions(Type controller)
        {
            // Arrange
            var builder = CreateTestDefaultActionModelBuilder();
            var typeInfo = controller.GetTypeInfo();

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.GetMethod("Delete"));

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
        public void GetActions_RouteOnController_CreatesOneActionInforPerRouteTemplateOnAction(Type controller)
        {
            // Arrange
            var builder = CreateTestDefaultActionModelBuilder();
            var typeInfo = controller.GetTypeInfo();

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.GetMethod("Index"));

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
        public void GetActions_MixedHttpVerbsAndRoutes_EmptyVerbWithRoute()
        {
            // Arrange
            var builder = CreateTestDefaultActionModelBuilder();
            var typeInfo = typeof(MixedHttpVerbsAndRouteAttributeController).GetTypeInfo();
            var actionName = nameof(MixedHttpVerbsAndRouteAttributeController.VerbAndRoute);

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.GetMethod(actionName));

            // Assert
            var action = Assert.Single(actions);
            Assert.Equal<string>(new string[] { "GET" }, action.HttpMethods);
            Assert.Equal("Products", action.AttributeRouteModel.Template);
        }

        [Fact]
        public void GetActions_MixedHttpVerbsAndRoutes_MultipleEmptyVerbsWithMultipleRoutes()
        {
            // Arrange
            var builder = CreateTestDefaultActionModelBuilder();
            var typeInfo = typeof(MixedHttpVerbsAndRouteAttributeController).GetTypeInfo();
            var actionName = nameof(MixedHttpVerbsAndRouteAttributeController.MultipleVerbsAndRoutes);

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.GetMethod(actionName));

            // Assert
            Assert.Equal(2, actions.Count());

            var action = Assert.Single(actions, a => a.AttributeRouteModel.Template == "Products");
            Assert.Equal<string>(new string[] { "GET", "POST" }, action.HttpMethods);

            action = Assert.Single(actions, a => a.AttributeRouteModel.Template == "v2/Products");
            Assert.Equal<string>(new string[] { "GET", "POST" }, action.HttpMethods);
        }

        [Fact]
        public void GetActions_MixedHttpVerbsAndRoutes_MultipleEmptyAndNonEmptyVerbsWithMultipleRoutes()
        {
            // Arrange
            var builder = CreateTestDefaultActionModelBuilder();
            var typeInfo = typeof(MixedHttpVerbsAndRouteAttributeController).GetTypeInfo();
            var actionName = nameof(MixedHttpVerbsAndRouteAttributeController.MultipleVerbsWithAnyWithoutTemplateAndRoutes);

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.GetMethod(actionName));

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
        public void GetActions_MixedHttpVerbsAndRoutes_MultipleEmptyAndNonEmptyVerbs()
        {
            // Arrange
            var builder = CreateTestDefaultActionModelBuilder();
            var typeInfo = typeof(MixedHttpVerbsAndRouteAttributeController).GetTypeInfo();
            var actionName = nameof(MixedHttpVerbsAndRouteAttributeController.Invalid);

            // Act
            var actions = builder.BuildActionModels(typeInfo, typeInfo.GetMethod(actionName));

            // Assert
            Assert.Equal(2, actions.Count());

            var action = Assert.Single(actions, a => a.AttributeRouteModel?.Template == "Products");
            Assert.Equal<string>(new string[] { "POST" }, action.HttpMethods);

            action = Assert.Single(actions, a => a.AttributeRouteModel?.Template == null);
            Assert.Equal<string>(new string[] { "GET" }, action.HttpMethods);
        }

        private static DefaultActionModelBuilder CreateTestDefaultActionModelBuilder(
            AuthorizationOptions authOptions = null)
        {
            var options = new Mock<IOptions<AuthorizationOptions>>();
            options.Setup(o => o.Options).Returns(authOptions ?? new AuthorizationOptions());
            return new DefaultActionModelBuilder(options.Object);
        }

        private static AccessibleActionModelBuilder CreateTestAccessibleActionModelBuilder()
        {
            var options = new Mock<IOptions<AuthorizationOptions>>();
            options.Setup(o => o.Options).Returns(new AuthorizationOptions());
            return new AccessibleActionModelBuilder(options.Object);
        }

        private class AccessibleActionModelBuilder : DefaultActionModelBuilder
        {
            public AccessibleActionModelBuilder(IOptions<AuthorizationOptions> options) : base(options) { }

            public new bool IsAction([NotNull] TypeInfo typeInfo, [NotNull]MethodInfo methodInfo)
            {
                return base.IsAction(typeInfo, methodInfo);
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

            public override RedirectResult Redirect(string url)
            {
                return base.Redirect(url + "#RedirectOverride");
            }

            [Authorize(Policy = "Base")]
            public virtual void Authorize()
            {
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

            [Authorize(Policy = "Derived")]
            public override void Authorize()
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

        private class OperatorOverloadingController : Mvc.Controller
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

            [HttpPost("Change")]
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

            [HttpDelete]
            public void Delete() { }

            [HttpPost]
            [HttpGet]
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
    }
}