// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core
{
    public class ControllerTests
    {
        public static IEnumerable<object[]> PublicNormalMethodsFromController
        {
            get
            {
                return typeof(Controller).GetTypeInfo()
                    .DeclaredMethods
                    .Where(method => method.IsPublic && !method.IsSpecialName)
                    .Select(method => new [] { method });
            }
        }

        [Fact]
        public void SettingViewData_AlsoUpdatesViewBag()
        {
            // Arrange
            var metadataProvider = new DataAnnotationsModelMetadataProvider();
            var controller = new Controller();
            var originalViewData = controller.ViewData = new ViewDataDictionary<object>(metadataProvider);
            var replacementViewData = new ViewDataDictionary<object>(metadataProvider);

            // Act
            controller.ViewBag.Hello = "goodbye";
            controller.ViewData = replacementViewData;
            controller.ViewBag.Another = "property";

            // Assert
            Assert.NotSame(originalViewData, controller.ViewData);
            Assert.Same(replacementViewData, controller.ViewData);
            Assert.Null(controller.ViewBag.Hello);
            Assert.Equal("property", controller.ViewBag.Another);
            Assert.Equal("property", controller.ViewData["Another"]);
        }

        [Fact]
        public void Redirect_Temporary_SetsSameUrl()
        {
            // Arrange
            var controller = new Controller();

            // Act
            var result = controller.Redirect("sample\\url");

            // Assert
            Assert.False(result.Permanent);
            Assert.Equal("sample\\url", result.Url);
        }

        [Fact]
        public void Redirect_Permanent_SetsSameUrl()
        {
            // Arrange
            var controller = new Controller();

            // Act
            var result = controller.RedirectPermanent("sample\\url");

            // Assert
            Assert.True(result.Permanent);
            Assert.Equal("sample\\url", result.Url);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Redirect_NullOrEmptyUrl_Throws(string url)
        {
            // Arrange
            var controller = new Controller();

            // Act & Assert
            ExceptionAssert.ThrowsArgument(
                () => controller.Redirect(url: url), "url", "The value cannot be null or empty");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void RedirectPermanent_NullOrEmptyUrl_Throws(string url)
        {
            // Arrange
            var controller = new Controller();

            // Act & Assert
            ExceptionAssert.ThrowsArgument(
                () => controller.RedirectPermanent(url: url), "url", "The value cannot be null or empty");
        }

        [Fact]
        public void RedirectToAction_Temporary_Returns_SameAction()
        {
            // Arrange
            var controller = new Controller();

            // Act
            var resultTemporary = controller.RedirectToAction("SampleAction");

            // Assert
            Assert.False(resultTemporary.Permanent);
            Assert.Equal("SampleAction", resultTemporary.ActionName);
        }

        [Fact]
        public void RedirectToAction_Permanent_Returns_SameAction()
        {
            // Arrange
            var controller = new Controller();

            // Act
            var resultPermanent = controller.RedirectToActionPermanent("SampleAction");

            // Assert
            Assert.True(resultPermanent.Permanent);
            Assert.Equal("SampleAction", resultPermanent.ActionName);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("SampleController")]
        public void RedirectToAction_Temporary_Returns_SameController(string controllerName)
        {
            // Arrange
            var controller = new Controller();

            // Act
            var resultTemporary = controller.RedirectToAction("SampleAction", controllerName);

            // Assert
            Assert.False(resultTemporary.Permanent);
            Assert.Equal("SampleAction", resultTemporary.ActionName);
            Assert.Equal(controllerName, resultTemporary.ControllerName);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("SampleController")]
        public void RedirectToAction_Permanent_Returns_SameController(string controllerName)
        {
            // Arrange
            var controller = new Controller();

            // Act
            var resultPermanent = controller.RedirectToActionPermanent("SampleAction", controllerName);

            // Assert
            Assert.True(resultPermanent.Permanent);
            Assert.Equal("SampleAction", resultPermanent.ActionName);
            Assert.Equal(controllerName, resultPermanent.ControllerName);
        }

        [Theory]
        [MemberData("RedirectTestData")]
        public void RedirectToAction_Temporary_Returns_SameActionControllerAndRouteValues(object routeValues)
        {
            // Arrange
            var controller = new Controller();

            // Act
            var resultTemporary = controller.RedirectToAction("SampleAction", "SampleController", routeValues);

            // Assert
            Assert.False(resultTemporary.Permanent);
            Assert.Equal("SampleAction", resultTemporary.ActionName);
            Assert.Equal("SampleController", resultTemporary.ControllerName);
            Assert.Equal(TypeHelper.ObjectToDictionary(routeValues), resultTemporary.RouteValues);
        }

        [Theory]
        [MemberData("RedirectTestData")]
        public void RedirectToAction_Permanent_Returns_SameActionControllerAndRouteValues(object routeValues)
        {
            // Arrange
            var controller = new Controller();

            // Act
            var resultPermanent = controller.RedirectToActionPermanent("SampleAction", "SampleController", routeValues);

            // Assert
            Assert.True(resultPermanent.Permanent);
            Assert.Equal("SampleAction", resultPermanent.ActionName);
            Assert.Equal("SampleController", resultPermanent.ControllerName);
            Assert.Equal(TypeHelper.ObjectToDictionary(routeValues), resultPermanent.RouteValues);
        }

        [Theory]
        [MemberData("RedirectTestData")]
        public void RedirectToAction_Temporary_Returns_SameActionAndRouteValues(object routeValues)
        {
            // Arrange
            var controller = new Controller();

            // Act
            var resultTemporary = controller.RedirectToAction(actionName: null, routeValues: routeValues);

            // Assert
            Assert.False(resultTemporary.Permanent);
            Assert.Null(resultTemporary.ActionName);
            Assert.Equal(TypeHelper.ObjectToDictionary(routeValues), resultTemporary.RouteValues);
        }

        [Theory]
        [MemberData("RedirectTestData")]
        public void RedirectToAction_Permanent_Returns_SameActionAndRouteValues(object routeValues)
        {
            // Arrange
            var controller = new Controller();

            // Act
            var resultPermanent = controller.RedirectToActionPermanent(null, routeValues);

            // Assert
            Assert.True(resultPermanent.Permanent);
            Assert.Null(resultPermanent.ActionName);
            Assert.Equal(TypeHelper.ObjectToDictionary(routeValues), resultPermanent.RouteValues);
        }

        [Theory]
        [MemberData("RedirectTestData")]
        public void RedirectToRoute_Temporary_Returns_SameRouteValues(object routeValues)
        {
            // Arrange
            var controller = new Controller();

            // Act
            var resultTemporary = controller.RedirectToRoute(routeValues);

            // Assert
            Assert.False(resultTemporary.Permanent);
            Assert.Equal(TypeHelper.ObjectToDictionary(routeValues), resultTemporary.RouteValues);
        }

        [Theory]
        [MemberData("RedirectTestData")]
        public void RedirectToRoute_Permanent_Returns_SameRouteValues(object routeValues)
        {
            // Arrange
            var controller = new Controller();

            // Act
            var resultPermanent = controller.RedirectToRoutePermanent(routeValues);

            // Assert
            Assert.True(resultPermanent.Permanent);
            Assert.Equal(TypeHelper.ObjectToDictionary(routeValues), resultPermanent.RouteValues);
        }

        [Theory]
        [MemberData("PublicNormalMethodsFromController")]
        public void NonActionAttribute_IsOnEveryPublicNormalMethodFromController(MethodInfo method)
        {
            // Arrange & Act & Assert
            Assert.True(method.IsDefined(typeof(NonActionAttribute)));
        }

        public static IEnumerable<object[]> RedirectTestData
        {
            get
            {
                yield return new object[] { null };
                yield return
                    new object[] {
                        new Dictionary<string, string>() { { "hello", "world" } }
                    };
                yield return
                    new object[] {
                        new RouteValueDictionary(new Dictionary<string, string>() { 
                                                        { "test", "case" }, { "sample", "route" } })
                    };
            }
        }
    }
}
