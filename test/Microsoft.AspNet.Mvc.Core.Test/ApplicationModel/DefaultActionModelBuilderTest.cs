// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            var builder = new AccessibleActionModelBuilder();
            var method = typeof(DerivedController).GetMethod(methodName);
            Assert.NotNull(method);

            // Act
            var isValid = builder.IsAction(method);

            // Assert
            Assert.Equal(expected, isValid);
        }

        [Fact]
        public void IsAction_OverridenMethodControllerClass()
        {
            // Arrange
            var builder = new AccessibleActionModelBuilder();
            var method = typeof(BaseController).GetMethod(nameof(BaseController.Redirect));
            Assert.NotNull(method);

            // Act
            var isValid = builder.IsAction(method);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsAction_PrivateMethod_FromUserDefinedController()
        {
            // Arrange
            var builder = new AccessibleActionModelBuilder();
            var method = typeof(DerivedController).GetMethod(
                "PrivateMethod",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(method);

            // Act
            var isValid = builder.IsAction(method);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsAction_OperatorOverloadingMethod_FromOperatorOverloadingController()
        {
            // Arrange
            var builder = new AccessibleActionModelBuilder();
            var method = typeof(OperatorOverloadingController).GetMethod("op_Addition");
            Assert.NotNull(method);
            Assert.True(method.IsSpecialName);

            // Act
            var isValid = builder.IsAction(method);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsAction_GenericMethod_FromUserDefinedController()
        {
            // Arrange
            var builder = new AccessibleActionModelBuilder();
            var method = typeof(DerivedController).GetMethod("GenericMethod");
            Assert.NotNull(method);

            // Act
            var isValid = builder.IsAction(method);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsAction_OverridenNonActionMethod()
        {
            // Arrange
            var builder = new AccessibleActionModelBuilder();
            var method = typeof(DerivedController).GetMethod("OverridenNonActionMethod");
            Assert.NotNull(method);

            // Act
            var isValid = builder.IsAction(method);

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
            var builder = new AccessibleActionModelBuilder();
            var method = typeof(DerivedController).GetMethod(
                methodName,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(method);

            // Act
            var isValid = builder.IsAction(method);

            // Assert
            Assert.False(isValid);
        }

        [Theory]
        [InlineData("StaticMethod")]
        [InlineData("ProtectedStaticMethod")]
        [InlineData("PrivateStaticMethod")]
        public void IsAction_StaticMethods(string methodName)
        {
            // Arrange
            var builder = new AccessibleActionModelBuilder();
            var method = typeof(DerivedController).GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.NotNull(method);

            // Act
            var isValid = builder.IsAction(method);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void GetActions_ConventionallyRoutedAction_WithoutHttpConstraints()
        {
            // Arrange
            var builder = new DefaultActionModelBuilder();
            var typeInfo = typeof(ConventionallyRoutedController).GetTypeInfo();
            var actionName = nameof(ConventionallyRoutedController.Edit);

            // Act
            var actions = builder.BuildActionModels(typeInfo.GetMethod(actionName));

            // Assert
            var action = Assert.Single(actions);
            Assert.Equal("Edit", action.ActionName);
            Assert.True(action.IsActionNameMatchRequired);
            Assert.Empty(action.HttpMethods);
            Assert.Null(action.AttributeRouteModel);
            Assert.Empty(action.Attributes);
        }

        [Fact]
        public void GetActions_ConventionallyRoutedAction_WithHttpConstraints()
        {
            // Arrange
            var builder = new DefaultActionModelBuilder();
            var typeInfo = typeof(ConventionallyRoutedController).GetTypeInfo();
            var actionName = nameof(ConventionallyRoutedController.Update);

            // Act
            var actions = builder.BuildActionModels(typeInfo.GetMethod(actionName));

            // Assert
            var action = Assert.Single(actions);
            Assert.Contains("PUT", action.HttpMethods);
            Assert.Contains("PATCH", action.HttpMethods);

            Assert.Equal("Update", action.ActionName);
            Assert.True(action.IsActionNameMatchRequired);
            Assert.Null(action.AttributeRouteModel);
            Assert.IsType<CustomHttpMethodsAttribute>(Assert.Single(action.Attributes));
        }

        [Fact]
        public void GetActions_ConventionallyRoutedActionWithHttpConstraints_AndInvalidRouteTemplateProvider()
        {
            // Arrange
            var builder = new DefaultActionModelBuilder();
            var typeInfo = typeof(ConventionallyRoutedController).GetTypeInfo();
            var actionName = nameof(ConventionallyRoutedController.Delete);

            // Act
            var actions = builder.BuildActionModels(typeInfo.GetMethod(actionName));

            // Assert
            var action = Assert.Single(actions);
            Assert.Equal("Delete", action.ActionName);
            Assert.True(action.IsActionNameMatchRequired);

            var httpMethod = Assert.Single(action.HttpMethods);
            Assert.Equal("DELETE", httpMethod);
            Assert.Null(action.AttributeRouteModel);

            Assert.IsType<HttpDeleteAttribute>(Assert.Single(action.Attributes));
        }

        [Fact]
        public void GetActions_ConventionallyRoutedAction_WithMultipleHttpConstraints()
        {
            // Arrange
            var builder = new DefaultActionModelBuilder();
            var typeInfo = typeof(ConventionallyRoutedController).GetTypeInfo();
            var actionName = nameof(ConventionallyRoutedController.Details);

            // Act
            var actions = builder.BuildActionModels(typeInfo.GetMethod(actionName));

            // Assert
            var action = Assert.Single(actions);
            Assert.Contains("GET", action.HttpMethods);
            Assert.Contains("POST", action.HttpMethods);
            Assert.Equal("Details", action.ActionName);
            Assert.True(action.IsActionNameMatchRequired);
            Assert.Null(action.AttributeRouteModel);
        }

        [Fact]
        public void GetActions_ConventionallyRoutedAction_WithMultipleOverlappingHttpConstraints()
        {
            // Arrange
            var builder = new DefaultActionModelBuilder();
            var typeInfo = typeof(ConventionallyRoutedController).GetTypeInfo();
            var actionName = nameof(ConventionallyRoutedController.List);

            // Act
            var actions = builder.BuildActionModels(typeInfo.GetMethod(actionName));

            // Assert
            var action = Assert.Single(actions);
            Assert.Contains("GET", action.HttpMethods);
            Assert.Contains("PUT", action.HttpMethods);
            Assert.Contains("POST", action.HttpMethods);
            Assert.Equal("List", action.ActionName);
            Assert.True(action.IsActionNameMatchRequired);
            Assert.Null(action.AttributeRouteModel);
        }

        [Fact]
        public void GetActions_AttributeRouteOnAction()
        {
            // Arrange
            var builder = new DefaultActionModelBuilder();
            var typeInfo = typeof(NoRouteAttributeOnControllerController).GetTypeInfo();
            var actionName = nameof(NoRouteAttributeOnControllerController.Edit);

            // Act
            var actions = builder.BuildActionModels(typeInfo.GetMethod(actionName));

            // Assert
            var action = Assert.Single(actions);

            Assert.Equal("Edit", action.ActionName);
            Assert.True(action.IsActionNameMatchRequired);

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
            var builder = new DefaultActionModelBuilder();
            var typeInfo = typeof(NoRouteAttributeOnControllerController).GetTypeInfo();
            var actionName = nameof(NoRouteAttributeOnControllerController.Update);

            // Act
            var actions = builder.BuildActionModels(typeInfo.GetMethod(actionName));

            // Assert
            var action = Assert.Single(actions);

            Assert.Equal("Update", action.ActionName);
            Assert.True(action.IsActionNameMatchRequired);

            Assert.Empty(action.HttpMethods);

            Assert.NotNull(action.AttributeRouteModel);
            Assert.Equal("Update", action.AttributeRouteModel.Template);

            Assert.IsType<RouteAttribute>(Assert.Single(action.Attributes));
        }

        [Fact]
        public void GetActions_AttributeRouteOnAction_AcceptVerbsAttributeWithTemplate()
        {
            // Arrange
            var builder = new DefaultActionModelBuilder();
            var typeInfo = typeof(NoRouteAttributeOnControllerController).GetTypeInfo();
            var actionName = nameof(NoRouteAttributeOnControllerController.List);

            // Act
            var actions = builder.BuildActionModels(typeInfo.GetMethod(actionName));

            // Assert
            var action = Assert.Single(actions);

            Assert.Equal("List", action.ActionName);
            Assert.True(action.IsActionNameMatchRequired);

            Assert.Equal(new[] { "GET", "HEAD" }, action.HttpMethods.OrderBy(m => m, StringComparer.Ordinal));

            Assert.NotNull(action.AttributeRouteModel);
            Assert.Equal("ListAll", action.AttributeRouteModel.Template);

            Assert.IsType<AcceptVerbsAttribute>(Assert.Single(action.Attributes));
        }

        [Fact]
        public void GetActions_AttributeRouteOnAction_CreatesOneActionInforPerRouteTemplate()
        {
            // Arrange
            var builder = new DefaultActionModelBuilder();
            var typeInfo = typeof(NoRouteAttributeOnControllerController).GetTypeInfo();
            var actionName = nameof(NoRouteAttributeOnControllerController.Index);

            // Act
            var actions = builder.BuildActionModels(typeInfo.GetMethod(actionName));

            // Assert
            Assert.Equal(2, actions.Count());

            foreach (var action in actions)
            {
                Assert.Equal("Index", action.ActionName);
                Assert.True(action.IsActionNameMatchRequired);

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
            var builder = new DefaultActionModelBuilder();
            var typeInfo = typeof(NoRouteAttributeOnControllerController).GetTypeInfo();
            var actionName = nameof(NoRouteAttributeOnControllerController.Remove);

            // Act
            var actions = builder.BuildActionModels(typeInfo.GetMethod(actionName));

            // Assert
            var action = Assert.Single(actions);

            Assert.Equal("Remove", action.ActionName);
            Assert.True(action.IsActionNameMatchRequired);

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
            var builder = new DefaultActionModelBuilder();
            var typeInfo = controller.GetTypeInfo();

            // Act
            var actions = builder.BuildActionModels(typeInfo.GetMethod("Delete"));

            // Assert
            var action = Assert.Single(actions);

            Assert.Equal("Delete", action.ActionName);
            Assert.True(action.IsActionNameMatchRequired);

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
            var builder = new DefaultActionModelBuilder();
            var typeInfo = controller.GetTypeInfo();

            // Act
            var actions = builder.BuildActionModels(typeInfo.GetMethod("Index"));

            // Assert
            Assert.Equal(2, actions.Count());

            foreach (var action in actions)
            {
                Assert.Equal("Index", action.ActionName);
                Assert.True(action.IsActionNameMatchRequired);

                var httpMethod = Assert.Single(action.HttpMethods);
                Assert.Equal("GET", httpMethod);

                Assert.NotNull(action.AttributeRouteModel.Template);

                Assert.IsType<HttpGetAttribute>(Assert.Single(action.Attributes));
            }

            Assert.Single(actions, ai => ai.AttributeRouteModel.Template.Equals("List"));
            Assert.Single(actions, ai => ai.AttributeRouteModel.Template.Equals("All"));
        }

        private class AccessibleActionModelBuilder : DefaultActionModelBuilder
        {
            public new bool IsAction([NotNull]MethodInfo methodInfo)
            {
                return base.IsAction(methodInfo);
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