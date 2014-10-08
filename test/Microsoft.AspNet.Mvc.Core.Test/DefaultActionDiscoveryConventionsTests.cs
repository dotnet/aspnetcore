// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Mvc.DefaultActionDiscoveryConventionsControllers;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultActionDiscoveryConventionsTests
    {
        [Theory]
        [InlineData("GetFromDerived", true)]
        [InlineData("NewMethod", true)] // "NewMethod" is a public method declared with keyword "new".
        [InlineData("GetFromBase", true)]
        public void IsValidActionMethod_WithInheritedMethods(string methodName, bool expected)
        {
            // Arrange
            var conventions = new DefaultActionDiscoveryConventions();
            var method = typeof(DerivedController).GetMethod(methodName);
            Assert.NotNull(method);

            // Act
            var isValid = conventions.IsValidActionMethod(method);

            // Assert
            Assert.Equal(expected, isValid);
        }

        [Fact]
        public void IsValidActionMethod_OverridenMethodControllerClass()
        {
            // Arrange
            var conventions = new DefaultActionDiscoveryConventions();
            var method = typeof(BaseController).GetMethod("Redirect");
            Assert.NotNull(method);

            // Act
            var isValid = conventions.IsValidActionMethod(method);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidActionMethod_PrivateMethod_FromUserDefinedController()
        {
            // Arrange
            var conventions = new DefaultActionDiscoveryConventions();
            var method = typeof(DerivedController).GetMethod(
                "PrivateMethod",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(method);

            // Act
            var isValid = conventions.IsValidActionMethod(method);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidActionMethod_OperatorOverloadingMethod_FromOperatorOverloadingController()
        {
            // Arrange
            var conventions = new DefaultActionDiscoveryConventions();
            var method = typeof(OperatorOverloadingController).GetMethod("op_Addition");
            Assert.NotNull(method);
            Assert.True(method.IsSpecialName);

            // Act
            var isValid = conventions.IsValidActionMethod(method);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidActionMethod_GenericMethod_FromUserDefinedController()
        {
            // Arrange
            var conventions = new DefaultActionDiscoveryConventions();
            var method = typeof(DerivedController).GetMethod("GenericMethod");
            Assert.NotNull(method);

            // Act
            var isValid = conventions.IsValidActionMethod(method);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidActionMethod_OverridenNonActionMethod()
        {
            // Arrange
            var conventions = new DefaultActionDiscoveryConventions();
            var method = typeof(DerivedController).GetMethod("OverridenNonActionMethod");
            Assert.NotNull(method);

            // Act
            var isValid = conventions.IsValidActionMethod(method);

            // Assert
            Assert.False(isValid);
        }

        [Theory]
        [InlineData("Equals")]
        [InlineData("GetHashCode")]
        [InlineData("MemberwiseClone")]
        [InlineData("ToString")]
        public void IsValidActionMethod_OverriddenMethodsFromObjectClass(string methodName)
        {
            // Arrange
            var conventions = new DefaultActionDiscoveryConventions();
            var method = typeof(DerivedController).GetMethod(
                methodName,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(method);

            // Act
            var isValid = conventions.IsValidActionMethod(method);

            // Assert
            Assert.False(isValid);
        }

        [Theory]
        [InlineData("StaticMethod")]
        [InlineData("ProtectedStaticMethod")]
        [InlineData("PrivateStaticMethod")]
        public void IsValidActionMethod_StaticMethods(string methodName)
        {
            // Arrange
            var conventions = new DefaultActionDiscoveryConventions();
            var method = typeof(DerivedController).GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.NotNull(method);

            // Act
            var isValid = conventions.IsValidActionMethod(method);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void GetActions_ConventionallyRoutedAction_WithoutHttpConstraints()
        {
            // Arrange
            var conventions = new DefaultActionDiscoveryConventions();
            var typeInfo = typeof(ConventionallyRoutedController).GetTypeInfo();
            var actionName = nameof(ConventionallyRoutedController.Edit);

            // Act
            var actionInfos = conventions.GetActions(typeInfo.GetMethod(actionName), typeInfo);

            // Assert
            var action = Assert.Single(actionInfos);
            Assert.Equal("Edit", action.ActionName);
            Assert.True(action.RequireActionNameMatch);
            Assert.Null(action.HttpMethods);
            Assert.Null(action.AttributeRoute);
            Assert.Empty(action.Attributes);
        }

        [Fact]
        public void GetActions_ConventionallyRoutedAction_WithHttpConstraints()
        {
            // Arrange
            var conventions = new DefaultActionDiscoveryConventions();
            var typeInfo = typeof(ConventionallyRoutedController).GetTypeInfo();
            var actionName = nameof(ConventionallyRoutedController.Update);

            // Act
            var actionInfos = conventions.GetActions(typeInfo.GetMethod(actionName), typeInfo);

            // Assert
            Assert.Equal(2, actionInfos.Count());
            Assert.Single(actionInfos, a => a.HttpMethods.Contains("PUT"));
            Assert.Single(actionInfos, a => a.HttpMethods.Contains("PATCH"));

            foreach (var action in actionInfos)
            {
                Assert.Equal("Update", action.ActionName);
                Assert.True(action.RequireActionNameMatch);
                Assert.Null(action.AttributeRoute);
                Assert.IsType<CustomHttpMethodsAttribute>(Assert.Single(action.Attributes));
            }
        }

        [Fact]
        public void GetActions_ConventionallyRoutedActionWithHttpConstraints_AndInvalidRouteTemplateProvider()
        {
            // Arrange
            var conventions = new DefaultActionDiscoveryConventions();
            var typeInfo = typeof(ConventionallyRoutedController).GetTypeInfo();
            var actionName = nameof(ConventionallyRoutedController.Delete);

            // Act
            var actionInfos = conventions.GetActions(typeInfo.GetMethod(actionName), typeInfo);

            // Assert
            var action = Assert.Single(actionInfos);
            Assert.Equal("Delete", action.ActionName);
            Assert.True(action.RequireActionNameMatch);

            var httpMethod = Assert.Single(action.HttpMethods);
            Assert.Equal("DELETE", httpMethod);
            Assert.Null(action.AttributeRoute);

            Assert.IsType<HttpDeleteAttribute>(Assert.Single(action.Attributes));
        }

        [Fact]
        public void GetActions_ConventionallyRoutedAction_WithMultipleHttpConstraints()
        {
            // Arrange
            var conventions = new DefaultActionDiscoveryConventions();
            var typeInfo = typeof(ConventionallyRoutedController).GetTypeInfo();
            var actionName = nameof(ConventionallyRoutedController.Details);

            // Act
            var actionInfos = conventions.GetActions(typeInfo.GetMethod(actionName), typeInfo);

            // Assert
            Assert.Equal(2, actionInfos.Count());
            Assert.Single(actionInfos, a => a.HttpMethods.Contains("GET"));
            Assert.Single(actionInfos, a => a.HttpMethods.Contains("POST"));

            foreach (var action in actionInfos)
            {

                Assert.Equal("Details", action.ActionName);
                Assert.True(action.RequireActionNameMatch);
                Assert.Null(action.AttributeRoute);

                Assert.Equal(2, action.Attributes.Length);
                Assert.Single(action.Attributes, a => a is HttpGetAttribute);
                Assert.Single(action.Attributes, a => a is HttpPostAttribute);
            }
        }

        [Fact]
        public void GetActions_ConventionallyRoutedAction_WithMultipleOverlappingHttpConstraints()
        {
            // Arrange
            var conventions = new DefaultActionDiscoveryConventions();
            var typeInfo = typeof(ConventionallyRoutedController).GetTypeInfo();
            var actionName = nameof(ConventionallyRoutedController.List);

            // Act
            var actionInfos = conventions.GetActions(typeInfo.GetMethod(actionName), typeInfo);

            // Assert
            Assert.Equal(3, actionInfos.Count());
            Assert.Single(actionInfos, a => a.HttpMethods.Contains("GET"));
            Assert.Single(actionInfos, a => a.HttpMethods.Contains("POST"));
            Assert.Single(actionInfos, a => a.HttpMethods.Contains("PUT"));

            foreach (var action in actionInfos)
            {
                Assert.Equal("List", action.ActionName);
                Assert.True(action.RequireActionNameMatch);
                Assert.Null(action.AttributeRoute);

                Assert.Equal(3, action.Attributes.Length);
                Assert.Single(action.Attributes, a => a is HttpPutAttribute);
                Assert.Single(action.Attributes, a => a is HttpGetAttribute);
                Assert.Single(action.Attributes, a => a is AcceptVerbsAttribute);
            }
        }

        [Fact]
        public void GetActions_AttributeRouteOnAction()
        {
            // Arrange
            var conventions = new DefaultActionDiscoveryConventions();
            var typeInfo = typeof(NoRouteAttributeOnControllerController).GetTypeInfo();
            var actionName = nameof(NoRouteAttributeOnControllerController.Edit);

            // Act
            var actionInfos = conventions.GetActions(typeInfo.GetMethod(actionName), typeInfo);

            // Assert
            var action = Assert.Single(actionInfos);

            Assert.Equal("Edit", action.ActionName);
            Assert.True(action.RequireActionNameMatch);

            var httpMethod = Assert.Single(action.HttpMethods);
            Assert.Equal("POST", httpMethod);

            Assert.NotNull(action.AttributeRoute);
            Assert.Equal("Change", action.AttributeRoute.Template);

            Assert.IsType<HttpPostAttribute>(Assert.Single(action.Attributes));
        }

        [Fact]
        public void GetActions_AttributeRouteOnAction_RouteAttribute()
        {
            // Arrange
            var conventions = new DefaultActionDiscoveryConventions();
            var typeInfo = typeof(NoRouteAttributeOnControllerController).GetTypeInfo();
            var actionName = nameof(NoRouteAttributeOnControllerController.Update);

            // Act
            var actionInfos = conventions.GetActions(typeInfo.GetMethod(actionName), typeInfo);

            // Assert
            var action = Assert.Single(actionInfos);

            Assert.Equal("Update", action.ActionName);
            Assert.True(action.RequireActionNameMatch);

            Assert.Null(action.HttpMethods);

            Assert.NotNull(action.AttributeRoute);
            Assert.Equal("Update", action.AttributeRoute.Template);

            Assert.IsType<RouteAttribute>(Assert.Single(action.Attributes));
        }

        [Fact]
        public void GetActions_AttributeRouteOnAction_AcceptVerbsAttributeWithTemplate()
        {
            // Arrange
            var conventions = new DefaultActionDiscoveryConventions();
            var typeInfo = typeof(NoRouteAttributeOnControllerController).GetTypeInfo();
            var actionName = nameof(NoRouteAttributeOnControllerController.List);

            // Act
            var actionInfos = conventions.GetActions(typeInfo.GetMethod(actionName), typeInfo);

            // Assert
            var action = Assert.Single(actionInfos);

            Assert.Equal("List", action.ActionName);
            Assert.True(action.RequireActionNameMatch);

            Assert.Equal(new[] { "GET", "HEAD" }, action.HttpMethods.OrderBy(m => m, StringComparer.Ordinal));

            Assert.NotNull(action.AttributeRoute);
            Assert.Equal("ListAll", action.AttributeRoute.Template);

            Assert.IsType<AcceptVerbsAttribute>(Assert.Single(action.Attributes));
        }

        [Fact]
        public void GetActions_AttributeRouteOnAction_CreatesOneActionInforPerRouteTemplate()
        {
            // Arrange
            var conventions = new DefaultActionDiscoveryConventions();
            var typeInfo = typeof(NoRouteAttributeOnControllerController).GetTypeInfo();
            var actionName = nameof(NoRouteAttributeOnControllerController.Index);

            // Act
            var actionInfos = conventions.GetActions(typeInfo.GetMethod(actionName), typeInfo);

            // Assert
            Assert.Equal(2, actionInfos.Count());

            foreach (var action in actionInfos)
            {
                Assert.Equal("Index", action.ActionName);
                Assert.True(action.RequireActionNameMatch);

                Assert.NotNull(action.AttributeRoute);
            }

            var list = Assert.Single(actionInfos, ai => ai.AttributeRoute.Template.Equals("List"));
            var listMethod = Assert.Single(list.HttpMethods);
            Assert.Equal("POST", listMethod);
            Assert.IsType<HttpPostAttribute>(Assert.Single(list.Attributes));

            var all = Assert.Single(actionInfos, ai => ai.AttributeRoute.Template.Equals("All"));
            var allMethod = Assert.Single(all.HttpMethods);
            Assert.Equal("GET", allMethod);
            Assert.IsType<HttpGetAttribute>(Assert.Single(all.Attributes));
        }

        [Fact]
        public void GetActions_NoRouteOnController_AllowsConventionallyRoutedActions_OnTheSameController()
        {
            // Arrange
            var conventions = new DefaultActionDiscoveryConventions();
            var typeInfo = typeof(NoRouteAttributeOnControllerController).GetTypeInfo();
            var actionName = nameof(NoRouteAttributeOnControllerController.Remove);

            // Act
            var actionInfos = conventions.GetActions(typeInfo.GetMethod(actionName), typeInfo);

            // Assert
            var action = Assert.Single(actionInfos);

            Assert.Equal("Remove", action.ActionName);
            Assert.True(action.RequireActionNameMatch);

            Assert.Null(action.HttpMethods);

            Assert.Null(action.AttributeRoute);

            Assert.Empty(action.Attributes);
        }

        [Theory]
        [InlineData(typeof(SingleRouteAttributeController))]
        [InlineData(typeof(MultipleRouteAttributeController))]
        public void GetActions_RouteAttributeOnController_CreatesAttributeRoute_ForNonAttributedActions(Type controller)
        {
            // Arrange
            var conventions = new DefaultActionDiscoveryConventions();
            var typeInfo = controller.GetTypeInfo();

            // Act
            var actionInfos = conventions.GetActions(typeInfo.GetMethod("Delete"), typeInfo);

            // Assert
            var action = Assert.Single(actionInfos);

            Assert.Equal("Delete", action.ActionName);
            Assert.True(action.RequireActionNameMatch);

            Assert.Null(action.HttpMethods);

            Assert.Null(action.AttributeRoute);

            Assert.Empty(action.Attributes);
        }

        [Theory]
        [InlineData(typeof(SingleRouteAttributeController))]
        [InlineData(typeof(MultipleRouteAttributeController))]
        public void GetActions_RouteOnController_CreatesOneActionInforPerRouteTemplateOnAction(Type controller)
        {
            // Arrange
            var conventions = new DefaultActionDiscoveryConventions();
            var typeInfo = controller.GetTypeInfo();

            // Act
            var actionInfos = conventions.GetActions(typeInfo.GetMethod("Index"), typeInfo);

            // Assert
            Assert.Equal(2, actionInfos.Count());

            foreach (var action in actionInfos)
            {
                Assert.Equal("Index", action.ActionName);
                Assert.True(action.RequireActionNameMatch);

                var httpMethod = Assert.Single(action.HttpMethods);
                Assert.Equal("GET", httpMethod);

                Assert.NotNull(action.AttributeRoute);

                Assert.IsType<HttpGetAttribute>(Assert.Single(action.Attributes));
            }

            Assert.Single(actionInfos, ai => ai.AttributeRoute.Template.Equals("List"));
            Assert.Single(actionInfos, ai => ai.AttributeRoute.Template.Equals("All"));
        }

        [Fact]
        public void IsController_UserDefinedClass()
        {
            // Arrange
            var conventions = new DefaultActionDiscoveryConventions();
            var typeInfo = typeof(BaseController).GetTypeInfo();

            // Act
            var isController = conventions.IsController(typeInfo);

            // Assert
            Assert.True(isController);
        }

        [Fact]
        public void IsController_FrameworkControllerClass()
        {
            // Arrange
            var conventions = new DefaultActionDiscoveryConventions();
            var typeInfo = typeof(Controller).GetTypeInfo();

            // Act
            var isController = conventions.IsController(typeInfo);

            // Assert
            Assert.False(isController);
        }

        [Fact]
        public void IsController_UserDefinedControllerClass()
        {
            // Arrange
            var conventions = new DefaultActionDiscoveryConventions();
            var typeInfo = typeof(DefaultActionDiscoveryConventionsControllers.Controller).GetTypeInfo();

            // Act
            var isController = conventions.IsController(typeInfo);

            // Assert
            Assert.False(isController);
        }

        [Fact]
        public void IsController_Interface()
        {
            // Arrange
            var conventions = new DefaultActionDiscoveryConventions();
            var typeInfo = typeof(IController).GetTypeInfo();

            // Act
            var isController = conventions.IsController(typeInfo);

            // Assert
            Assert.False(isController);
        }

        [Fact]
        public void IsController_AbstractClass()
        {
            // Arrange
            var conventions = new DefaultActionDiscoveryConventions();
            var typeInfo = typeof(AbstractController).GetTypeInfo();

            // Act
            var isController = conventions.IsController(typeInfo);

            // Assert
            Assert.False(isController);
        }

        [Fact]
        public void IsController_DerivedAbstractClass()
        {
            // Arrange
            var conventions = new DefaultActionDiscoveryConventions();
            var typeInfo = typeof(DerivedAbstractController).GetTypeInfo();

            // Act
            var isController = conventions.IsController(typeInfo);

            // Assert
            Assert.True(isController);
        }

        [Fact]
        public void IsController_OpenGenericClass()
        {
            // Arrange
            var conventions = new DefaultActionDiscoveryConventions();
            var typeInfo = typeof(OpenGenericController<>).GetTypeInfo();

            // Act
            var isController = conventions.IsController(typeInfo);

            // Assert
            Assert.False(isController);
        }

        [Fact]
        public void IsController_ClosedGenericClass()
        {
            // Arrange
            var conventions = new DefaultActionDiscoveryConventions();
            var typeInfo = typeof(OpenGenericController<string>).GetTypeInfo();

            // Act
            var isController = conventions.IsController(typeInfo);

            // Assert
            Assert.False(isController);
        }

        [Fact]
        public void IsController_DerivedGenericClass()
        {
            // Arrange
            var conventions = new DefaultActionDiscoveryConventions();
            var typeInfo = typeof(DerivedGenericController).GetTypeInfo();

            // Act
            var isController = conventions.IsController(typeInfo);

            // Assert
            Assert.True(isController);
        }

        [Fact]
        public void IsController_Poco_WithNamingConvention()
        {
            // Arrange
            var conventions = new DefaultActionDiscoveryConventions();
            var typeInfo = typeof(PocoController).GetTypeInfo();

            // Act
            var isController = conventions.IsController(typeInfo);

            // Assert
            Assert.True(isController);
        }

        [Fact]
        public void IsController_NoControllerSuffix()
        {
            // Arrange
            var conventions = new DefaultActionDiscoveryConventions();
            var typeInfo = typeof(NoSuffix).GetTypeInfo();

            // Act
            var isController = conventions.IsController(typeInfo);

            // Assert
            Assert.True(isController);
        }
    }
}

// These controllers are used to test the DefaultActionDiscoveryConventions implementation
// which REQUIRES that they be public top-level classes. To avoid having to stub out the
// implementation of this class to test it, they are just top level classes. Don't reuse
// these outside this test - find a better way or use nested classes to keep the tests
// independent.
namespace Microsoft.AspNet.Mvc.DefaultActionDiscoveryConventionsControllers
{
    public abstract class AbstractController : Mvc.Controller
    {
    }

    public class DerivedAbstractController : AbstractController
    {
    }

    public class BaseController : Mvc.Controller
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

    public class DerivedController : BaseController
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

    public class Controller
    {
    }

    public class OpenGenericController<T>
    {
    }

    public class DerivedGenericController : OpenGenericController<string>
    {
    }

    public interface IController
    {
    }

    public class NoSuffix : Mvc.Controller
    {
    }

    public class PocoController
    {
    }

    public class OperatorOverloadingController : Mvc.Controller
    {
        public static OperatorOverloadingController operator +(
            OperatorOverloadingController c1,
            OperatorOverloadingController c2)
        {
            return new OperatorOverloadingController();
        }
    }

    public class NoRouteAttributeOnControllerController : Controller
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
    public class SingleRouteAttributeController : Controller
    {
        [HttpGet("All")]
        [HttpGet("List")]
        public void Index() { }

        public void Delete() { }
    }

    [Route("Products")]
    [Route("Items")]
    public class MultipleRouteAttributeController : Controller
    {
        [HttpGet("All")]
        [HttpGet("List")]
        public void Index() { }

        public void Delete() { }
    }

    // Here the constraints on the methods are acting as an IActionHttpMethodProvider and
    // not as an IRouteTemplateProvider given that there is no RouteAttribute
    // on the controller and the template for all the constraints on a method is null.
    public class ConventionallyRoutedController : Controller
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

    public class CustomHttpMethodsAttribute : Attribute, IActionHttpMethodProvider
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