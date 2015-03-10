// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Testing;
using Microsoft.AspNet.WebUtilities;
#if DNX451
using Moq;
#endif
using Xunit;

namespace Microsoft.AspNet.Mvc.Test
{
    public class ControllerTests
    {
        public static IEnumerable<object[]> PublicNormalMethodsFromController
        {
            get
            {
                return typeof(Controller).GetTypeInfo()
                    .DeclaredMethods
                    .Where(method => method.IsPublic &&
                    !method.IsSpecialName &&
                    !method.Name.Equals("Dispose", StringComparison.OrdinalIgnoreCase))
                    .Select(method => new[] { method });
            }
        }

        [Fact]
        public void SettingViewData_AlsoUpdatesViewBag()
        {
            // Arrange
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var controller = new TestableController();
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
        public void Redirect_WithParameterUrl_SetsRedirectResultSameUrl()
        {
            // Arrange
            var controller = new TestableController();
            var url = "/test/url";

            // Act
            var result = controller.Redirect(url);

            // Assert
            Assert.IsType<RedirectResult>(result);
            Assert.False(result.Permanent);
            Assert.Same(url, result.Url);
        }

        [Fact]
        public void RedirectPermanent_WithParameterUrl_SetsRedirectResultPermanentAndSameUrl()
        {
            // Arrange
            var controller = new TestableController();
            var url = "/test/url";

            // Act
            var result = controller.RedirectPermanent(url);

            // Assert
            Assert.IsType<RedirectResult>(result);
            Assert.True(result.Permanent);
            Assert.Same(url, result.Url);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Redirect_WithParameter_NullOrEmptyUrl_Throws(string url)
        {
            // Arrange
            var controller = new TestableController();

            // Act & Assert
            ExceptionAssert.ThrowsArgumentNullOrEmpty(
                () => controller.Redirect(url: url), "url");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void RedirectPermanent_WithParameter_NullOrEmptyUrl_Throws(string url)
        {
            // Arrange
            var controller = new TestableController();

            // Act & Assert
            ExceptionAssert.ThrowsArgumentNullOrEmpty(
                () => controller.RedirectPermanent(url: url), "url");
        }

        [Fact]
        public void RedirectToAction_WithParameterActionName_SetsResultActionName()
        {
            // Arrange
            var controller = new TestableController();

            // Act
            var resultTemporary = controller.RedirectToAction("SampleAction");

            // Assert
            Assert.IsType<RedirectToActionResult>(resultTemporary);
            Assert.False(resultTemporary.Permanent);
            Assert.Equal("SampleAction", resultTemporary.ActionName);
        }

        [Fact]
        public void RedirectToActionPermanent_WithParameterActionName_SetsResultActionNameAndPermanent()
        {
            // Arrange
            var controller = new TestableController();

            // Act
            var resultPermanent = controller.RedirectToActionPermanent("SampleAction");

            // Assert
            Assert.IsType<RedirectToActionResult>(resultPermanent);
            Assert.True(resultPermanent.Permanent);
            Assert.Equal("SampleAction", resultPermanent.ActionName);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("SampleController")]
        public void RedirectToAction_WithParameterActionAndControllerName_SetsEqualNames(string controllerName)
        {
            // Arrange
            var controller = new TestableController();

            // Act
            var resultTemporary = controller.RedirectToAction("SampleAction", controllerName);

            // Assert
            Assert.IsType<RedirectToActionResult>(resultTemporary);
            Assert.False(resultTemporary.Permanent);
            Assert.Equal("SampleAction", resultTemporary.ActionName);
            Assert.Equal(controllerName, resultTemporary.ControllerName);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("SampleController")]
        public void RedirectToActionPermanent_WithParameterActionAndControllerName_SetsEqualNames(
            string controllerName)
        {
            // Arrange
            var controller = new TestableController();

            // Act
            var resultPermanent = controller.RedirectToActionPermanent("SampleAction", controllerName);

            // Assert
            Assert.IsType<RedirectToActionResult>(resultPermanent);
            Assert.True(resultPermanent.Permanent);
            Assert.Equal("SampleAction", resultPermanent.ActionName);
            Assert.Equal(controllerName, resultPermanent.ControllerName);
        }

        [Theory]
        [MemberData(nameof(RedirectTestData))]
        public void RedirectToAction_WithParameterActionControllerRouteValues_SetsResultProperties(
            object routeValues,
            IEnumerable<KeyValuePair<string, object>> expected)
        {
            // Arrange
            var controller = new TestableController();

            // Act
            var resultTemporary = controller.RedirectToAction("SampleAction", "SampleController", routeValues);

            // Assert
            Assert.IsType<RedirectToActionResult>(resultTemporary);
            Assert.False(resultTemporary.Permanent);
            Assert.Equal("SampleAction", resultTemporary.ActionName);
            Assert.Equal("SampleController", resultTemporary.ControllerName);
            Assert.Equal(expected, resultTemporary.RouteValues);
        }

        [Theory]
        [MemberData(nameof(RedirectTestData))]
        public void RedirectToActionPermanent_WithParameterActionControllerRouteValues_SetsResultProperties(
            object routeValues,
            IEnumerable<KeyValuePair<string, object>> expected)
        {
            // Arrange
            var controller = new TestableController();

            // Act
            var resultPermanent = controller.RedirectToActionPermanent(
                "SampleAction",
                "SampleController",
                routeValues);

            // Assert
            Assert.IsType<RedirectToActionResult>(resultPermanent);
            Assert.True(resultPermanent.Permanent);
            Assert.Equal("SampleAction", resultPermanent.ActionName);
            Assert.Equal("SampleController", resultPermanent.ControllerName);
            Assert.Equal(expected, resultPermanent.RouteValues);
        }

        [Theory]
        [MemberData(nameof(RedirectTestData))]
        public void RedirectToAction_WithParameterActionAndRouteValues_SetsResultProperties(
            object routeValues,
            IEnumerable<KeyValuePair<string, object>> expected)
        {
            // Arrange
            var controller = new TestableController();

            // Act
            var resultTemporary = controller.RedirectToAction(actionName: null, routeValues: routeValues);

            // Assert
            Assert.IsType<RedirectToActionResult>(resultTemporary);
            Assert.False(resultTemporary.Permanent);
            Assert.Null(resultTemporary.ActionName);
            Assert.Equal(expected, resultTemporary.RouteValues);
        }

        [Theory]
        [MemberData(nameof(RedirectTestData))]
        public void RedirectToActionPermanent_WithParameterActionAndRouteValues_SetsResultProperties(
            object routeValues,
            IEnumerable<KeyValuePair<string, object>> expected)
        {
            // Arrange
            var controller = new TestableController();

            // Act
            var resultPermanent = controller.RedirectToActionPermanent(null, routeValues);

            // Assert
            Assert.IsType<RedirectToActionResult>(resultPermanent);
            Assert.True(resultPermanent.Permanent);
            Assert.Null(resultPermanent.ActionName);
            Assert.Equal(expected, resultPermanent.RouteValues);
        }

        [Theory]
        [MemberData(nameof(RedirectTestData))]
        public void RedirectToRoute_WithParameterRouteValues_SetsResultEqualRouteValues(
            object routeValues,
            IEnumerable<KeyValuePair<string, object>> expected)
        {
            // Arrange
            var controller = new TestableController();

            // Act
            var resultTemporary = controller.RedirectToRoute(routeValues);

            // Assert
            Assert.IsType<RedirectToRouteResult>(resultTemporary);
            Assert.False(resultTemporary.Permanent);
            Assert.Equal(expected, resultTemporary.RouteValues);
        }

        [Theory]
        [MemberData(nameof(RedirectTestData))]
        public void RedirectToRoutePermanent_WithParameterRouteValues_SetsResultEqualRouteValuesAndPermanent(
            object routeValues,
            IEnumerable<KeyValuePair<string, object>> expected)
        {
            // Arrange
            var controller = new TestableController();

            // Act
            var resultPermanent = controller.RedirectToRoutePermanent(routeValues);

            // Assert
            Assert.IsType<RedirectToRouteResult>(resultPermanent);
            Assert.True(resultPermanent.Permanent);
            Assert.Equal(expected, resultPermanent.RouteValues);
        }

        [Fact]
        public void RedirectToRoute_WithParameterRouteName_SetsResultSameRouteName()
        {
            // Arrange
            var controller = new TestableController();
            var routeName = "CustomRouteName";

            // Act
            var resultTemporary = controller.RedirectToRoute(routeName);

            // Assert
            Assert.IsType<RedirectToRouteResult>(resultTemporary);
            Assert.False(resultTemporary.Permanent);
            Assert.Same(routeName, resultTemporary.RouteName);
        }

        [Fact]
        public void RedirectToRoutePermanent_WithParameterRouteName_SetsResultSameRouteNameAndPermanent()
        {
            // Arrange
            var controller = new TestableController();
            var routeName = "CustomRouteName";

            // Act
            var resultPermanent = controller.RedirectToRoutePermanent(routeName);

            // Assert
            Assert.IsType<RedirectToRouteResult>(resultPermanent);
            Assert.True(resultPermanent.Permanent);
            Assert.Same(routeName, resultPermanent.RouteName);
        }

        [Theory]
        [MemberData(nameof(RedirectTestData))]
        public void RedirectToRoute_WithParameterRouteNameAndRouteValues_SetsResultSameRouteNameAndRouteValues(
            object routeValues,
            IEnumerable<KeyValuePair<string, object>> expected)
        {
            // Arrange
            var controller = new TestableController();
            var routeName = "CustomRouteName";

            // Act
            var resultTemporary = controller.RedirectToRoute(routeName, routeValues);

            // Assert
            Assert.IsType<RedirectToRouteResult>(resultTemporary);
            Assert.False(resultTemporary.Permanent);
            Assert.Same(routeName, resultTemporary.RouteName);
            Assert.Equal(expected, resultTemporary.RouteValues);
        }

        [Theory]
        [MemberData(nameof(RedirectTestData))]
        public void RedirectToRoutePermanent_WithParameterRouteNameAndRouteValues_SetsResultProperties(
            object routeValues,
            IEnumerable<KeyValuePair<string, object>> expected)
        {
            // Arrange
            var controller = new TestableController();
            var routeName = "CustomRouteName";

            // Act
            var resultPermanent = controller.RedirectToRoutePermanent(routeName, routeValues);

            // Assert
            Assert.IsType<RedirectToRouteResult>(resultPermanent);
            Assert.True(resultPermanent.Permanent);
            Assert.Same(routeName, resultPermanent.RouteName);
            Assert.Equal(expected, resultPermanent.RouteValues);
        }

        [Fact]
        public void Created_WithStringParameter_SetsCreatedLocation()
        {
            // Arrange
            var controller = new TestableController();
            var uri = "http://test/url";

            // Act
            var result = controller.Created(uri, null);

            // Assert
            Assert.IsType<CreatedResult>(result);
            Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
            Assert.Same(uri, result.Location);
        }

        [Fact]
        public void Created_WithAbsoluteUriParameter_SetsCreatedLocation()
        {
            // Arrange
            var controller = new TestableController();
            var uri = new Uri("http://test/url");

            // Act
            var result = controller.Created(uri, null);

            // Assert
            Assert.IsType<CreatedResult>(result);
            Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
            Assert.Equal(uri.OriginalString, result.Location);
        }

        [Fact]
        public void Created_WithRelativeUriParameter_SetsCreatedLocation()
        {
            // Arrange
            var controller = new TestableController();
            var uri = new Uri("/test/url", UriKind.Relative);

            // Act
            var result = controller.Created(uri, null);

            // Assert
            Assert.IsType<CreatedResult>(result);
            Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
            Assert.Equal(uri.OriginalString, result.Location);
        }

        [Fact]
        public void Created_IDisposableObject_RegistersForDispose()
        {
            // Arrange
            var mockHttpContext = new Mock<DefaultHttpContext>();
            mockHttpContext.Setup(x => x.Response.OnResponseCompleted(It.IsAny<Action<object>>(), It.IsAny<object>()));
            var uri = new Uri("/test/url", UriKind.Relative);

            var controller = new TestableController()
            {
                ActionContext = new ActionContext(mockHttpContext.Object, new RouteData(), new ActionDescriptor())
            };
            var input = new DisposableObject();

            // Act
            var result = controller.Created(uri, input);

            // Assert
            Assert.IsType<CreatedResult>(result);
            Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
            Assert.Equal(uri.OriginalString, result.Location);
            Assert.Same(input, result.Value);
            mockHttpContext.Verify(
                x => x.Response.OnResponseCompleted(It.IsAny<Action<object>>(), It.IsAny<object>()),
                Times.Once());
        }

        [Fact]
        public void CreatedAtAction_WithParameterActionName_SetsResultActionName()
        {
            // Arrange
            var controller = new TestableController();

            // Act
            var result = controller.CreatedAtAction("SampleAction", null);

            // Assert
            Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
            Assert.Equal("SampleAction", result.ActionName);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("SampleController")]
        public void CreatedAtAction_WithActionControllerAndNullRouteValue_SetsSameValue(
            string controllerName)
        {
            // Arrange
            var controller = new TestableController();

            // Act
            var result = controller.CreatedAtAction("SampleAction", controllerName, null, null);

            // Assert
            Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
            Assert.Equal("SampleAction", result.ActionName);
            Assert.Equal(controllerName, result.ControllerName);
        }

        [Fact]
        public void CreatedAtAction_WithActionControllerRouteValues_SetsSameValues()
        {
            // Arrange
            var controller = new TestableController();
            var expected = new Dictionary<string, object>
                {
                    { "test", "case" },
                    { "sample", "route" },
                };

            // Act
            var result = controller.CreatedAtAction(
                "SampleAction",
                "SampleController",
                new RouteValueDictionary(expected), null);

            // Assert
            Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
            Assert.Equal("SampleAction", result.ActionName);
            Assert.Equal("SampleController", result.ControllerName);
            Assert.Equal(expected, result.RouteValues);
        }

        [Fact]
        public void CreatedAtAction_IDisposableObject_RegistersForDispose()
        {
            // Arrange
            var mockHttpContext = new Mock<DefaultHttpContext>();
            mockHttpContext.Setup(x => x.Response.OnResponseCompleted(It.IsAny<Action<object>>(), It.IsAny<object>()));

            var controller = new TestableController()
            {
                ActionContext = new ActionContext(mockHttpContext.Object, new RouteData(), new ActionDescriptor())
            };
            var input = new DisposableObject();

            // Act
            var result = controller.CreatedAtAction("SampleAction", input);

            // Assert
            Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
            Assert.Equal("SampleAction", result.ActionName);
            Assert.Same(input, result.Value);
            mockHttpContext.Verify(
                x => x.Response.OnResponseCompleted(It.IsAny<Action<object>>(), It.IsAny<object>()),
                Times.Once());
        }

        [Fact]
        public void CreatedAtRoute_WithParameterRouteName_SetsResultSameRouteName()
        {
            // Arrange
            var controller = new TestableController();
            var routeName = "SampleRoute";

            // Act
            var result = controller.CreatedAtRoute(routeName, null);

            // Assert
            Assert.IsType<CreatedAtRouteResult>(result);
            Assert.Same(routeName, result.RouteName);
        }

        [Fact]
        public void CreatedAtRoute_WithParameterRouteValues_SetsResultSameRouteValues()
        {
            // Arrange
            var controller = new TestableController();
            var expected = new Dictionary<string, object>
                {
                    { "test", "case" },
                    { "sample", "route" },
                };

            // Act
            var result = controller.CreatedAtRoute(new RouteValueDictionary(expected), null);

            // Assert
            Assert.IsType<CreatedAtRouteResult>(result);
            Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
            Assert.Equal(expected, result.RouteValues);
        }

        [Fact]
        public void CreatedAtRoute_WithParameterRouteNameAndValues_SetsResultSameProperties()
        {
            // Arrange
            var controller = new TestableController();
            var routeName = "SampleRoute";
            var expected = new Dictionary<string, object>
                {
                    { "test", "case" },
                    { "sample", "route" },
                };

            // Act
            var result = controller.CreatedAtRoute(routeName, new RouteValueDictionary(expected), null);

            // Assert
            Assert.IsType<CreatedAtRouteResult>(result);
            Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
            Assert.Same(routeName, result.RouteName);
            Assert.Equal(expected, result.RouteValues);
        }

        [Fact]
        public void CreatedAtRoute_IDisposableObject_RegistersForDispose()
        {
            // Arrange
            var mockHttpContext = new Mock<DefaultHttpContext>();
            mockHttpContext.Setup(x => x.Response.OnResponseCompleted(It.IsAny<Action<object>>(), It.IsAny<object>()));

            var controller = new TestableController()
            {
                ActionContext = new ActionContext(mockHttpContext.Object, new RouteData(), new ActionDescriptor())
            };
            var input = new DisposableObject();

            // Act
            var result = controller.CreatedAtRoute("SampleRoute", input);

            // Assert
            Assert.IsType<CreatedAtRouteResult>(result);
            Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
            Assert.Equal("SampleRoute", result.RouteName);
            Assert.Same(input, result.Value);
            mockHttpContext.Verify(
                x => x.Response.OnResponseCompleted(It.IsAny<Action<object>>(), It.IsAny<object>()),
                Times.Once());
        }

        [Fact]
        public void File_WithContents()
        {
            // Arrange
            var controller = new TestableController();
            var fileContents = new byte[0];

            // Act
            var result = controller.File(fileContents, "someContentType");

            // Assert
            Assert.NotNull(result);
            Assert.Same(fileContents, result.FileContents);
            Assert.Equal("someContentType", result.ContentType);
            Assert.Equal(string.Empty, result.FileDownloadName);
        }

        [Fact]
        public void File_WithContentsAndFileDownloadName()
        {
            // Arrange
            var controller = new TestableController();
            var fileContents = new byte[0];

            // Act
            var result = controller.File(fileContents, "someContentType", "someDownloadName");

            // Assert
            Assert.NotNull(result);
            Assert.Same(fileContents, result.FileContents);
            Assert.Equal("someContentType", result.ContentType);
            Assert.Equal("someDownloadName", result.FileDownloadName);
        }

        [Fact]
        public void File_WithPath()
        {
            // Arrange
            var controller = new TestableController();
            var path = Path.GetFullPath("somepath");

            // Act
            var result = controller.File(path, "someContentType");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(path, result.FileName);
            Assert.Equal("someContentType", result.ContentType);
            Assert.Equal(string.Empty, result.FileDownloadName);
        }

        [Fact]
        public void File_WithPathAndFileDownloadName()
        {
            // Arrange
            var controller = new TestableController();
            var path = Path.GetFullPath("somepath");

            // Act
            var result = controller.File(path, "someContentType", "someDownloadName");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(path, result.FileName);
            Assert.Equal("someContentType", result.ContentType);
            Assert.Equal("someDownloadName", result.FileDownloadName);
        }

        [Fact]
        public void File_WithStream()
        {
            // Arrange
            var mockHttpContext = new Mock<DefaultHttpContext>();
            mockHttpContext.Setup(x => x.Response.OnResponseCompleted(It.IsAny<Action<object>>(), It.IsAny<object>()));
            var controller = new TestableController()
            {
                ActionContext = new ActionContext(mockHttpContext.Object, new RouteData(), new ActionDescriptor())
            };
            var fileStream = Stream.Null;

            // Act
            var result = controller.File(fileStream, "someContentType");

            // Assert
            Assert.NotNull(result);
            Assert.Same(fileStream, result.FileStream);
            Assert.Equal("someContentType", result.ContentType);
            Assert.Equal(string.Empty, result.FileDownloadName);
        }

        [Fact]
        public void File_WithStreamAndFileDownloadName()
        {
            // Arrange
            var mockHttpContext = new Mock<DefaultHttpContext>();
            mockHttpContext.Setup(x => x.Response.OnResponseCompleted(It.IsAny<Action<object>>(), It.IsAny<object>()));

            var controller = new TestableController()
            {
                ActionContext = new ActionContext(mockHttpContext.Object, new RouteData(), new ActionDescriptor())
            };
            var fileStream = Stream.Null;

            // Act
            var result = controller.File(fileStream, "someContentType", "someDownloadName");

            // Assert
            Assert.NotNull(result);
            Assert.Same(fileStream, result.FileStream);
            Assert.Equal("someContentType", result.ContentType);
            Assert.Equal("someDownloadName", result.FileDownloadName);
            mockHttpContext.Verify(
                x => x.Response.OnResponseCompleted(It.IsAny<Action<object>>(), It.IsAny<object>()),
                Times.Once());
        }

        [Fact]
        public void HttpUnauthorized_SetsStatusCode()
        {
            // Arrange 
            var controller = new TestableController();

            // Act
            var result = controller.HttpUnauthorized();

            // Assert
            Assert.IsType<HttpUnauthorizedResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
        }

        [Fact]
        public void HttpNotFound_SetsStatusCode()
        {
            // Arrange
            var controller = new TestableController();

            // Act
            var result = controller.HttpNotFound();

            // Assert
            Assert.IsType<HttpNotFoundResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
        }

        [Fact]
        public void HttpNotFound_SetsStatusCodeAndResponseContent()
        {
            // Arrange
            var controller = new TestableController();

            // Act
            var result = controller.HttpNotFound("Test Content");

            // Assert
            Assert.IsType<HttpNotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
            Assert.Equal("Test Content", result.Value);
        }

        [Fact]
        public void HttpNotFound_IDisposableObject_RegistersForDispose()
        {
            // Arrange
            var mockHttpContext = new Mock<DefaultHttpContext>();
            mockHttpContext.Setup(x => x.Response.OnResponseCompleted(It.IsAny<Action<object>>(), It.IsAny<object>()));

            var controller = new TestableController()
            {
                ActionContext = new ActionContext(mockHttpContext.Object, new RouteData(), new ActionDescriptor())
            };
            var input = new DisposableObject();

            // Act
            var result = controller.HttpNotFound(input);

            // Assert
            Assert.IsType<HttpNotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
            Assert.Same(input, result.Value);
            mockHttpContext.Verify(
                x => x.Response.OnResponseCompleted(It.IsAny<Action<object>>(), It.IsAny<object>()),
                Times.Once());
        }

        [Fact]
        public void BadRequest_SetsStatusCode()
        {
            // Arrange
            var controller = new TestableController();

            // Act
            var result = controller.HttpBadRequest();

            // Assert
            Assert.IsType<BadRequestResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        }

        [Fact]
        public void BadRequest_SetsStatusCodeAndValue_Object()
        {
            // Arrange
            var controller = new TestableController();
            var obj = new object();

            // Act
            var result = controller.HttpBadRequest(obj);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            Assert.Equal(obj, result.Value);
        }

        [Fact]
        public void BadRequest_IDisposableObject_RegistersForDispose()
        {
            // Arrange
            var mockHttpContext = new Mock<DefaultHttpContext>();
            mockHttpContext.Setup(x => x.Response.OnResponseCompleted(It.IsAny<Action<object>>(), It.IsAny<object>()));

            var controller = new TestableController()
            {
                ActionContext = new ActionContext(mockHttpContext.Object, new RouteData(), new ActionDescriptor())
            };
            var input = new DisposableObject();

            // Act
            var result = controller.HttpBadRequest(input);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            Assert.Same(input, result.Value);
            mockHttpContext.Verify(
                x => x.Response.OnResponseCompleted(It.IsAny<Action<object>>(), It.IsAny<object>()),
                Times.Once());
        }

        [Fact]
        public void BadRequest_SetsStatusCodeAndValue_ModelState()
        {
            // Arrange
            var controller = new TestableController();

            // Act
            var result = controller.HttpBadRequest(new ModelStateDictionary());

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            var errors = Assert.IsType<SerializableError>(result.Value);
            Assert.Equal(0, errors.Count);
        }

        [Theory]
        [MemberData(nameof(PublicNormalMethodsFromController))]
        public void NonActionAttribute_IsOnEveryPublicNormalMethodFromController(MethodInfo method)
        {
            // Arrange & Act & Assert
            Assert.True(method.IsDefined(typeof(NonActionAttribute)));
        }

        [Fact]
        public void Controller_View_WithoutParameter_SetsResultNullViewNameAndNullViewDataModelAndSameTempData()
        {
            // Arrange
            var controller = new TestableController()
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = new TempDataDictionary(Mock.Of<IHttpContextAccessor>(), Mock.Of<ITempDataProvider>()),
            };

            // Act
            var actualViewResult = controller.View();

            // Assert
            Assert.IsType<ViewResult>(actualViewResult);
            Assert.Null(actualViewResult.ViewName);
            Assert.Same(controller.ViewData, actualViewResult.ViewData);
            Assert.Same(controller.TempData, actualViewResult.TempData);
            Assert.Null(actualViewResult.ViewData.Model);
        }

        [Fact]
        public void Controller_View_WithParameterViewName_SetsResultViewNameAndNullViewDataModelAndSameTempData()
        {
            // Arrange
            var controller = new TestableController()
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = new TempDataDictionary(Mock.Of<IHttpContextAccessor>(), Mock.Of<ITempDataProvider>()),
            };

            // Act
            var actualViewResult = controller.View("CustomViewName");

            // Assert
            Assert.IsType<ViewResult>(actualViewResult);
            Assert.Equal("CustomViewName", actualViewResult.ViewName);
            Assert.Same(controller.ViewData, actualViewResult.ViewData);
            Assert.Same(controller.TempData, actualViewResult.TempData);
            Assert.Null(actualViewResult.ViewData.Model);
        }

        [Fact]
        public void Controller_View_WithParameterViewModel_SetsResultNullViewNameAndViewDataModelAndSameTempData()
        {
            // Arrange
            var controller = new TestableController()
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = new TempDataDictionary(Mock.Of<IHttpContextAccessor>(), Mock.Of<ITempDataProvider>()),
            };
            var model = new object();

            // Act
            var actualViewResult = controller.View(model);

            // Assert
            Assert.IsType<ViewResult>(actualViewResult);
            Assert.Null(actualViewResult.ViewName);
            Assert.Same(controller.ViewData, actualViewResult.ViewData);
            Assert.Same(controller.TempData, actualViewResult.TempData);
            Assert.Same(model, actualViewResult.ViewData.Model);
        }

        [Fact]
        public void Controller_View_WithParameterViewNameAndViewModel_SetsResultViewNameAndViewDataModelAndSameTempData()
        {
            // Arrange
            var controller = new TestableController()
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = new TempDataDictionary(Mock.Of<IHttpContextAccessor>(), Mock.Of<ITempDataProvider>()),
            };
            var model = new object();

            // Act
            var actualViewResult = controller.View("CustomViewName", model);

            // Assert
            Assert.IsType<ViewResult>(actualViewResult);
            Assert.Equal("CustomViewName", actualViewResult.ViewName);
            Assert.Same(controller.ViewData, actualViewResult.ViewData);
            Assert.Same(controller.TempData, actualViewResult.TempData);
            Assert.Same(model, actualViewResult.ViewData.Model);
        }

        [Fact]
        public void Controller_Content_WithParameterContentString_SetsResultContent()
        {
            // Arrange
            var controller = new TestableController();

            // Act
            var actualContentResult = controller.Content("TestContent");

            // Assert
            Assert.IsType<ContentResult>(actualContentResult);
            Assert.Equal("TestContent", actualContentResult.Content);
            Assert.Null(actualContentResult.ContentEncoding);
            Assert.Null(actualContentResult.ContentType);
        }

        [Fact]
        public void Controller_Content_WithParameterContentStringAndContentType_SetsResultContentAndContentType()
        {
            // Arrange
            var controller = new TestableController();

            // Act
            var actualContentResult = controller.Content("TestContent", "text/plain");

            // Assert
            Assert.IsType<ContentResult>(actualContentResult);
            Assert.Equal("TestContent", actualContentResult.Content);
            Assert.Null(actualContentResult.ContentEncoding);
            Assert.Equal("text/plain", actualContentResult.ContentType);
        }

        [Fact]
        public void Controller_Content_WithParameterContentAndTypeAndEncoding_SetsResultContentAndTypeAndEncoding()
        {
            // Arrange
            var controller = new TestableController();

            // Act
            var actualContentResult = controller.Content("TestContent", "text/plain", Encoding.UTF8);

            // Assert
            Assert.IsType<ContentResult>(actualContentResult);
            Assert.Equal("TestContent", actualContentResult.Content);
            Assert.Same(Encoding.UTF8, actualContentResult.ContentEncoding);
            Assert.Equal("text/plain", actualContentResult.ContentType);
        }

        [Fact]
        public void Controller_Json_WithParameterValue_SetsResultData()
        {
            // Arrange
            var controller = new TestableController();
            var data = new object();

            // Act
            var actualJsonResult = controller.Json(data);

            // Assert
            Assert.IsType<JsonResult>(actualJsonResult);
            Assert.Same(data, actualJsonResult.Value);
        }

        [Fact]
        public void Controller_Json_IDisposableObject_RegistersForDispose()
        {
            // Arrange
            var mockHttpContext = new Mock<DefaultHttpContext>();
            mockHttpContext.Setup(x => x.Response.OnResponseCompleted(It.IsAny<Action<object>>(), It.IsAny<object>()));

            var controller = new TestableController()
            {
                ActionContext = new ActionContext(mockHttpContext.Object, new RouteData(), new ActionDescriptor())
            };
            var input = new DisposableObject();

            // Act
            var result = controller.Json(input);

            // Assert
            Assert.IsType<JsonResult>(result);
            Assert.Same(input, result.Value);
            mockHttpContext.Verify(
                x => x.Response.OnResponseCompleted(It.IsAny<Action<object>>(), It.IsAny<object>()),
                Times.Once());
        }

        public static IEnumerable<object[]> RedirectTestData
        {
            get
            {
                yield return new object[]
                {
                    null,
                    Enumerable.Empty<KeyValuePair<string, object>>()
                };

                yield return new object[]
                {
                    new Dictionary<string, object> { { "hello", "world" } },
                    new[] { new KeyValuePair<string, object>("hello", "world") }
                };

                var expected2 = new Dictionary<string, object>
                {
                    { "test", "case" },
                    { "sample", "route" },
                };

                yield return new object[]
                {
                    new RouteValueDictionary(expected2),
                    expected2
                };
            }
        }

        // These tests share code with the ActionFilterAttribute tests because the various filter
        // implementations need to behave the same way.
#if DNX451
        [Fact]
        public async Task Controller_ActionFilter_SettingResult_ShortCircuits()
        {
            // Arrange, Act &  Assert
            await ActionFilterAttributeTests.ActionFilter_SettingResult_ShortCircuits(
                new Mock<Controller>());
        }

        [Fact]
        public async Task Controller_ActionFilter_Calls_OnActionExecuted()
        {
            // Arrange, Act &  Assert
            await ActionFilterAttributeTests.ActionFilter_Calls_OnActionExecuted(
                new Mock<Controller>());
        }

        [Fact]
        public async Task TryUpdateModel_FallsBackOnEmptyPrefix_IfNotSpecified()
        {
            // Arrange
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var valueProvider = Mock.Of<IValueProvider>();
            var binder = new Mock<IModelBinder>();
            binder.Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                  .Callback((ModelBindingContext context) =>
                  {
                      Assert.Empty(context.ModelName);
                      Assert.Same(valueProvider, context.ValueProvider);

                      // Include and exclude should be null, resulting in property
                      // being included.
                      Assert.True(context.PropertyFilter(context, "Property1"));
                      Assert.True(context.PropertyFilter(context, "Property2"));
                  })
                  .Returns(Task.FromResult<ModelBindingResult>(null))
                  .Verifiable();

            var controller = GetController(binder.Object, valueProvider);
            var model = new MyModel();

            // Act
            var result = await controller.TryUpdateModelAsync(model);

            // Assert
            binder.Verify();
        }

        [Fact]
        public async Task TryUpdateModel_UsesModelTypeNameIfSpecified()
        {
            // Arrange
            var modelName = "mymodel";

            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var valueProvider = Mock.Of<IValueProvider>();
            var binder = new Mock<IModelBinder>();
            binder.Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                  .Callback((ModelBindingContext context) =>
                  {
                      Assert.Equal(modelName, context.ModelName);
                      Assert.Same(valueProvider, context.ValueProvider);

                      // Include and exclude should be null, resulting in property
                      // being included.
                      Assert.True(context.PropertyFilter(context, "Property1"));
                      Assert.True(context.PropertyFilter(context, "Property2"));
                  })
                  .Returns(Task.FromResult<ModelBindingResult>(null))
                  .Verifiable();

            var controller = GetController(binder.Object, valueProvider);
            var model = new MyModel();

            // Act
            var result = await controller.TryUpdateModelAsync(model, modelName);

            // Assert
            binder.Verify();
        }

        [Fact]
        public async Task TryUpdateModel_UsesModelValueProviderIfSpecified()
        {
            // Arrange
            var modelName = "mymodel";

            var valueProvider = Mock.Of<IValueProvider>();
            var binder = new Mock<IModelBinder>();
            binder.Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                  .Callback((ModelBindingContext context) =>
                  {
                      Assert.Equal(modelName, context.ModelName);
                      Assert.Same(valueProvider, context.ValueProvider);

                      // Include and exclude should be null, resulting in property
                      // being included.
                      Assert.True(context.PropertyFilter(context, "Property1"));
                      Assert.True(context.PropertyFilter(context, "Property2"));
                  })
                  .Returns(Task.FromResult<ModelBindingResult>(null))
                  .Verifiable();

            var controller = GetController(binder.Object, provider: null);
            var model = new MyModel();

            // Act
            var result = await controller.TryUpdateModelAsync(model, modelName, valueProvider);

            // Assert
            binder.Verify();
        }

        [Fact]
        public async Task TryUpdateModel_PredicateOverload_UsesPassedArguments()
        {
            // Arrange
            var modelName = "mymodel";

            Func<ModelBindingContext, string, bool> includePredicate = (context, propertyName) =>
                string.Equals(propertyName, "include1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(propertyName, "include2", StringComparison.OrdinalIgnoreCase);

            var binder = new Mock<IModelBinder>();
            var valueProvider = Mock.Of<IValueProvider>();
            binder.Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                  .Callback((ModelBindingContext context) =>
                  {
                      Assert.Equal(modelName, context.ModelName);
                      Assert.Same(valueProvider, context.ValueProvider);

                      Assert.True(context.PropertyFilter(context, "include1"));
                      Assert.True(context.PropertyFilter(context, "include2"));

                      Assert.False(context.PropertyFilter(context, "exclude1"));
                      Assert.False(context.PropertyFilter(context, "exclude2"));
                  })
                  .Returns(Task.FromResult<ModelBindingResult>(null))
                  .Verifiable();

            var controller = GetController(binder.Object, valueProvider);
            var model = new MyModel();

            // Act
            await controller.TryUpdateModelAsync(model, modelName, includePredicate);

            // Assert
            binder.Verify();
        }

        [Fact]
        public async Task TryUpdateModel_PredicateWithValueProviderOverload_UsesPassedArguments()
        {
            // Arrange
            var modelName = "mymodel";

            Func<ModelBindingContext, string, bool> includePredicate =
               (context, propertyName) => string.Equals(propertyName, "include1", StringComparison.OrdinalIgnoreCase) ||
                                          string.Equals(propertyName, "include2", StringComparison.OrdinalIgnoreCase);

            var binder = new Mock<IModelBinder>();
            var valueProvider = Mock.Of<IValueProvider>();
            binder.Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                  .Callback((ModelBindingContext context) =>
                  {
                      Assert.Equal(modelName, context.ModelName);
                      Assert.Same(valueProvider, context.ValueProvider);

                      Assert.True(context.PropertyFilter(context, "include1"));
                      Assert.True(context.PropertyFilter(context, "include2"));

                      Assert.False(context.PropertyFilter(context, "exclude1"));
                      Assert.False(context.PropertyFilter(context, "exclude2"));
                  })
                  .Returns(Task.FromResult<ModelBindingResult>(null))
                  .Verifiable();

            var controller = GetController(binder.Object, provider: null);

            var model = new MyModel();

            // Act
            await controller.TryUpdateModelAsync(model, modelName, valueProvider, includePredicate);

            // Assert
            binder.Verify();
        }

        [Theory]
        [InlineData("")]
        [InlineData("prefix")]
        public async Task TryUpdateModel_IncludeExpressionOverload_UsesPassedArguments(string prefix)
        {
            // Arrange
            var binder = new Mock<IModelBinder>();
            var valueProvider = Mock.Of<IValueProvider>();
            binder.Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                  .Callback((ModelBindingContext context) =>
                  {
                      Assert.Equal(prefix, context.ModelName);
                      Assert.Same(valueProvider, context.ValueProvider);

                      Assert.True(context.PropertyFilter(context, "Property1"));
                      Assert.True(context.PropertyFilter(context, "Property2"));

                      Assert.False(context.PropertyFilter(context, "exclude1"));
                      Assert.False(context.PropertyFilter(context, "exclude2"));
                  })
                  .Returns(Task.FromResult<ModelBindingResult>(null))
                  .Verifiable();


            var controller = GetController(binder.Object, valueProvider);
            var model = new MyModel();

            // Act
            await controller.TryUpdateModelAsync(model, prefix, m => m.Property1, m => m.Property2);

            // Assert
            binder.Verify();
        }

        [Theory]
        [InlineData("")]
        [InlineData("prefix")]
        public async Task
            TryUpdateModel_IncludeExpressionWithValueProviderOverload_UsesPassedArguments(string prefix)
        {
            // Arrange
            var binder = new Mock<IModelBinder>();
            var valueProvider = new Mock<IValueProvider>();
            binder.Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                  .Callback((ModelBindingContext context) =>
                  {
                      Assert.Equal(prefix, context.ModelName);
                      Assert.Same(valueProvider.Object, context.ValueProvider);

                      Assert.True(context.PropertyFilter(context, "Property1"));
                      Assert.True(context.PropertyFilter(context, "Property2"));

                      Assert.False(context.PropertyFilter(context, "exclude1"));
                      Assert.False(context.PropertyFilter(context, "exclude2"));
                  })
                  .Returns(Task.FromResult<ModelBindingResult>(null))
                  .Verifiable();

            var controller = GetController(binder.Object, provider: null);
            var model = new MyModel();

            // Act
            await controller.TryUpdateModelAsync(model, prefix, valueProvider.Object, m => m.Property1, m => m.Property2);

            // Assert
            binder.Verify();
        }

        [Fact]
        public async Task TryUpdateModelNonGeneric_PredicateWithValueProviderOverload_UsesPassedArguments()
        {
            // Arrange
            var modelName = "mymodel";

            Func<ModelBindingContext, string, bool> includePredicate =
               (context, propertyName) => string.Equals(propertyName, "include1", StringComparison.OrdinalIgnoreCase) ||
                                          string.Equals(propertyName, "include2", StringComparison.OrdinalIgnoreCase);

            var binder = new Mock<IModelBinder>();
            var valueProvider = Mock.Of<IValueProvider>();
            binder.Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                  .Callback((ModelBindingContext context) =>
                  {
                      Assert.Equal(modelName, context.ModelName);
                      Assert.Same(valueProvider, context.ValueProvider);

                      Assert.True(context.PropertyFilter(context, "include1"));
                      Assert.True(context.PropertyFilter(context, "include2"));

                      Assert.False(context.PropertyFilter(context, "exclude1"));
                      Assert.False(context.PropertyFilter(context, "exclude2"));
                  })
                  .Returns(Task.FromResult<ModelBindingResult>(null))
                  .Verifiable();

            var controller = GetController(binder.Object, provider: null);

            var model = new MyModel();

            // Act
            await controller.TryUpdateModelAsync(model, model.GetType(), modelName, valueProvider, includePredicate);

            // Assert
            binder.Verify();
        }

        [Fact]
        public async Task TryUpdateModelNonGeneric_ModelTypeOverload_UsesPassedArguments()
        {
            // Arrange
            var modelName = "mymodel";

            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var valueProvider = Mock.Of<IValueProvider>();
            var binder = new Mock<IModelBinder>();
            binder.Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                  .Callback((ModelBindingContext context) =>
                  {
                      Assert.Equal(modelName, context.ModelName);
                      Assert.Same(valueProvider, context.ValueProvider);

                      // Include and exclude should be null, resulting in property
                      // being included.
                      Assert.True(context.PropertyFilter(context, "Property1"));
                      Assert.True(context.PropertyFilter(context, "Property2"));
                  })
                  .Returns(Task.FromResult<ModelBindingResult>(null))
                  .Verifiable();

            var controller = GetController(binder.Object, valueProvider);
            var model = new MyModel();

            // Act
            var result = await controller.TryUpdateModelAsync(model, model.GetType(), modelName);

            // Assert
            binder.Verify();
        }

        [Fact]
        public async Task TryUpdateModelNonGeneric_BindToBaseDeclaredType_ModelTypeOverload()
        {
            // Arrange
            var modelName = "mymodel";

            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var valueProvider = Mock.Of<IValueProvider>();
            var binder = new Mock<IModelBinder>();
            binder.Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                  .Callback((ModelBindingContext context) =>
                  {
                      Assert.Equal(modelName, context.ModelName);
                      Assert.Same(valueProvider, context.ValueProvider);

                      // Include and exclude should be null, resulting in property
                      // being included.
                      Assert.True(context.PropertyFilter(context, "Property1"));
                      Assert.True(context.PropertyFilter(context, "Property2"));
                  })
                  .Returns(Task.FromResult<ModelBindingResult>(null))
                  .Verifiable();

            var controller = GetController(binder.Object, valueProvider);
            MyModel model = new MyDerivedModel();

            // Act
            var result = await controller.TryUpdateModelAsync(model, model.GetType(), modelName);

            // Assert
            binder.Verify();
        }

#endif

        [Fact]
        public void ControllerExposes_RequestServices()
        {
            // Arrange
            var controller = new TestableController();

            var serviceProvider = Mock.Of<IServiceProvider>();
            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.RequestServices)
                           .Returns(serviceProvider);

            controller.ActionContext = new ActionContext(httpContext.Object,
                                                  Mock.Of<RouteData>(),
                                                  new ActionDescriptor());

            // Act
            var innerServiceProvider = controller.Resolver;

            // Assert
            Assert.Same(serviceProvider, innerServiceProvider);
        }

        [Fact]
        public void ControllerExposes_Request()
        {
            // Arrange
            var controller = new TestableController();

            var request = Mock.Of<HttpRequest>();
            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.Request)
                           .Returns(request);

            controller.ActionContext = new ActionContext(httpContext.Object,
                                                  Mock.Of<RouteData>(),
                                                  new ActionDescriptor());

            // Act
            var innerRequest = controller.Request;

            // Assert
            Assert.Same(request, innerRequest);
        }

        [Fact]
        public void ControllerExposes_Response()
        {
            // Arrange
            var controller = new TestableController();

            var response = Mock.Of<HttpResponse>();
            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.Response)
                           .Returns(response);

            controller.ActionContext = new ActionContext(httpContext.Object,
                                                  Mock.Of<RouteData>(),
                                                  new ActionDescriptor());

            // Act
            var innerResponse = controller.Response;

            // Assert
            Assert.Same(response, innerResponse);
        }

        [Fact]
        public void ControllerExposes_RouteData()
        {
            // Arrange
            var controller = new TestableController();

            var routeData = Mock.Of<RouteData>();

            controller.ActionContext = new ActionContext(Mock.Of<HttpContext>(),
                                                  routeData,
                                                  new ActionDescriptor());

            // Act
            var innerRouteData = controller.RouteData;

            // Assert
            Assert.Same(routeData, innerRouteData);
        }

        [Fact]
        public void ControllerDispose_CallsDispose()
        {
            // Arrange
            var controller = new DisposableController();

            // Act
            controller.Dispose();

            // Assert
            Assert.True(controller.DisposeCalled);
        }

        [Fact]
        public void TryValidateModelWithValidModel_ReturnsTrue()
        {
            // Arrange
            var binder = new Mock<IModelBinder>();
            var controller = GetController(binder.Object, provider: null);
            controller.BindingContext.ValidatorProvider = Mock.Of<IModelValidatorProvider>();
            var model = new TryValidateModelModel();

            // Act
            var result = controller.TryValidateModel(model);

            // Assert
            Assert.True(result);
            Assert.True(controller.ModelState.IsValid);
        }

        [Fact]
        public void TryValidateModelWithInvalidModelWithPrefix_ReturnsFalse()
        {
            // Arrange
            var model = new TryValidateModelModel();
            var validationResult = new[]
            {
                new ModelValidationResult(string.Empty, "Out of range!")
            };

            var validator1 = new Mock<IModelValidator>();

            validator1.Setup(v => v.Validate(It.IsAny<ModelValidationContext>()))
               .Returns(validationResult);

            var provider = new Mock<IModelValidatorProvider>();
            provider.Setup(v => v.GetValidators(It.IsAny<ModelMetadata>()))
                .Returns(new[] { validator1.Object });

            var binder = new Mock<IModelBinder>();
            var controller = GetController(binder.Object, provider: null);
            controller.BindingContext.ValidatorProvider = provider.Object;

            // Act
            var result = controller.TryValidateModel(model, "Prefix");

            // Assert
            Assert.False(result);
            Assert.Equal(1, controller.ModelState.Count);
            var error = Assert.Single(controller.ModelState["Prefix.IntegerProperty"].Errors);
            Assert.Equal("Out of range!", error.ErrorMessage);
        }

        [Fact]
        public void TryValidateModelWithInvalidModelNoPrefix_ReturnsFalse()
        {
            // Arrange
            var model = new TryValidateModelModel();
            var validationResult = new[]
            {
                new ModelValidationResult(string.Empty, "Out of range!")
            };

            var validator1 = new Mock<IModelValidator>();

            validator1.Setup(v => v.Validate(It.IsAny<ModelValidationContext>()))
               .Returns(validationResult);

            var provider = new Mock<IModelValidatorProvider>();
            provider.Setup(v => v.GetValidators(It.IsAny<ModelMetadata>()))
                .Returns(new[] { validator1.Object });

            var binder = new Mock<IModelBinder>();
            var controller = GetController(binder.Object, provider: null);
            controller.BindingContext.ValidatorProvider = provider.Object;

            // Act
            var result = controller.TryValidateModel(model);

            // Assert
            Assert.False(result);
            Assert.Equal(1, controller.ModelState.Count);
            var error = Assert.Single(controller.ModelState["IntegerProperty"].Errors);
            Assert.Equal("Out of range!", error.ErrorMessage);
        }

        [Fact]
        public void TryValidateModelEmptyBindingContextThrowsException()
        {
            // Arrange
            var controller = new TestableController();
            var model = new TryValidateModelModel();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => controller.TryValidateModel(model));
            Assert.Equal("The 'BindingContext' property of 'Microsoft.AspNet.Mvc.Controller' must not be null.", exception.Message);
        }

        [Fact]
        public void TempData_CanSetAndGetValues()
        {
            // Arrange
            var controller = GetController(null, null);
            var input = "Foo";

            // Act
            controller.TempData["key"] = input;
            var result = controller.TempData["key"];

            // Assert
            Assert.Equal(input, result);
        }

        private static Controller GetController(IModelBinder binder, IValueProvider provider)
        {
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            var viewData = new ViewDataDictionary(metadataProvider, new ModelStateDictionary());
            var tempData = new TempDataDictionary(Mock.Of<IHttpContextAccessor>(), Mock.Of<ITempDataProvider>());

            var bindingContext = new ActionBindingContext()
            {
                ModelBinder = binder,
                ValueProvider = provider,
                ValidatorProvider = new DataAnnotationsModelValidatorProvider()
            };

            return new TestableController()
            {
                ActionContext = actionContext,
                BindingContext = bindingContext,
                MetadataProvider = metadataProvider,
                ViewData = viewData,
                TempData = tempData,
                ObjectValidator = new DefaultObjectValidator(Mock.Of<IValidationExcludeFiltersProvider>(), metadataProvider)
            };
        }

        private class MyModel
        {
            public string Property1 { get; set; }
            public string Property2 { get; set; }
        }

        private class MyDerivedModel : MyModel
        {
            public string Property3 { get; set; }
        }

        private class User
        {
            public User(int id)
            {
                Id = id;
            }

            public int Id { get; set; }

            public string Name { get; set; }

            public Address Address { get; set; }

        }

        private class Address
        {
            public string Street { get; set; }
            public string City { get; set; }
            public int Zip { get; set; }
        }

        private class TryValidateModelModel
        {
            public int IntegerProperty { get; set; }
        }

        private class DisposableController : Controller
        {
            public bool DisposeCalled { get; private set; }

            protected override void Dispose(bool disposing)
            {
                DisposeCalled = true;
            }
        }

        private class TestableController : Controller
        {

        }

        private class DisposableObject : IDisposable
        {
            public void Dispose()
            {
                throw new NotImplementedException();
            }
        }
    }
}
