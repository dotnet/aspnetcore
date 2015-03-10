// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.WebUtilities;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    // Tests Controler for the unit testability with which users can simply instantiate contollers for unit tests
    public class ControllerUnitTestabilityTests
    {
        [Theory]
        [MemberData(nameof(TestabilityViewTestData))]
        public void ControllerView_InvokedInUnitTests(object model, string viewName)
        {
            // Arrange
            var controller = new TestabilityController();

            // Act
            var result = controller.View_Action(viewName, model);

            // Assert
            Assert.NotNull(result);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(viewName, viewResult.ViewName);
            Assert.NotNull(viewResult.ViewData);
            Assert.Same(model, viewResult.ViewData.Model);
            Assert.Same(controller.ViewData, viewResult.ViewData);
            Assert.Same(controller.TempData, viewResult.TempData);

            if (model != null)
            {
                Assert.IsType(model.GetType(), viewResult.ViewData.Model);
                Assert.NotNull(viewResult.ViewData.Model);
            }
        }

        [Theory]
        [MemberData(nameof(TestabilityViewTestData))]
        public void ControllerPartialView_InvokedInUnitTests(object model, string viewName)
        {
            // Arrange
            var controller = new TestabilityController();

            // Act
            var result = controller.PartialView_Action(viewName, model);

            // Assert
            Assert.NotNull(result);

            var viewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal(viewName, viewResult.ViewName);
            Assert.NotNull(viewResult.ViewData);
            Assert.Same(model, viewResult.ViewData.Model);
            Assert.Same(controller.ViewData, viewResult.ViewData);
            Assert.Same(controller.TempData, viewResult.TempData);

            if (model != null)
            {
                Assert.IsType(model.GetType(), viewResult.ViewData.Model);
                Assert.NotNull(viewResult.ViewData.Model);
            }
        }

        [Theory]
        [MemberData(nameof(TestabilityContentTestData))]
        public void ControllerContent_InvokedInUnitTests(string content, string contentType, Encoding encoding)
        {
            // Arrange
            var controller = new TestabilityController();

            // Act
            var result = controller.Content_Action(content, contentType, encoding);

            // Assert
            Assert.NotNull(result);

            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal(content, contentResult.Content);
            Assert.Equal(contentType, contentResult.ContentType);
            Assert.Equal(encoding, contentResult.ContentEncoding);
        }

        [Theory]
        [InlineData("/Created_1", "<html>CreatedBody</html>")]
        [InlineData("/Created_2", null)]
        public void ControllerCreated_InvokedInUnitTests(string uri, string content)
        {
            // Arrange
            var controller = new TestabilityController();

            // Act
            var result = controller.Created_Action(uri, content);

            // Assert
            Assert.NotNull(result);

            var createdResult = Assert.IsType<CreatedResult>(result);
            Assert.Equal(uri, createdResult.Location);
            Assert.Equal(content, createdResult.Value);
            Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        }

        [Theory]
        [InlineData("<html>CreatedBody</html>", "text/html", "Created.html")]
        [InlineData("<html>CreatedBody</html>", null, null)]
        public void ControllerFileContent_InvokedInUnitTests(string content, string contentType, string fileName)
        {
            // Arrange
            var controller = new TestabilityController();

            // Act
            var result = controller.FileContent_Action(content, contentType, fileName);

            // Assert
            Assert.NotNull(result);

            var fileContentResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal(contentType, fileContentResult.ContentType);
            Assert.Equal(fileName ?? string.Empty, fileContentResult.FileDownloadName);

            if (content == null)
            {
                Assert.Null(fileContentResult.FileContents);
            }
            else
            {
                Assert.Equal(content, Encoding.UTF8.GetString(fileContentResult.FileContents));
            }
        }

        [Theory]
        [InlineData("<html>CreatedBody</html>", "text/html", "Created.html")]
        [InlineData("<html>CreatedBody</html>", null, null)]
        public void ControllerFileStream_InvokedInUnitTests(string content, string contentType, string fileName)
        {
            // Arrange
            var mockHttpContext = new Mock<DefaultHttpContext>();
            mockHttpContext.Setup(x => x.Response.OnResponseCompleted(It.IsAny<Action<object>>(), It.IsAny<object>()));
            var controller = new TestabilityController()
            {
                ActionContext = new ActionContext(mockHttpContext.Object, new RouteData(), new ActionDescriptor())
            };

            // Act
            var result = controller.FileStream_Action(content, contentType, fileName);

            // Assert
            Assert.NotNull(result);

            var fileStreamResult = Assert.IsType<FileStreamResult>(result);
            Assert.Equal(contentType, fileStreamResult.ContentType);
            Assert.Equal(fileName ?? string.Empty, fileStreamResult.FileDownloadName);

            if (content == null)
            {
                Assert.Null(fileStreamResult.FileStream);
            }
            else
            {
                using (var stream = new StreamReader(fileStreamResult.FileStream, Encoding.UTF8))
                {
                    Assert.Equal(content, stream.ReadToEnd());
                }
            }
        }

        [Fact]
        public void ControllerJson_InvokedInUnitTests()
        {
            // Arrange
            var controller = new TestabilityController();
            var model = new MyModel() { Property1 = "Property_1" };

            // Act
            var result = controller.Json_Action(model);

            // Assert
            Assert.NotNull(result);

            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);
            Assert.Same(model, jsonResult.Value);
            Assert.IsType(model.GetType(), jsonResult.Value);

            // Arrange
            controller = new TestabilityController();

            // Act
            result = controller.Json_Action(null);

            // Assert
            Assert.NotNull(result);

            jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Null(jsonResult.Value);
        }

        [Fact]
        public void ControllerHttpNotFound_InvokedInUnitTests()
        {
            // Arrange
            var controller = new TestabilityController();

            // Act
            var result = controller.HttpNotFound_Action();

            // Assert
            Assert.NotNull(result);

            var httpNotFoundResult = Assert.IsType<HttpNotFoundResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, httpNotFoundResult.StatusCode);
        }

        [Fact]
        public void ControllerHttpNotFoundObject_InvokedInUnitTests()
        {
            // Arrange
            var controller = new TestabilityController();

            // Act
            var result = controller.HttpNotFoundObject_Action("Test Content");

            // Assert
            Assert.NotNull(result);

            var httpNotFoundObjectResult = Assert.IsType<HttpNotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, httpNotFoundObjectResult.StatusCode);
            Assert.Equal("Test Content", httpNotFoundObjectResult.Value);
        }

        [Fact]
        public void ControllerHttpBadRequest_InvokedInUnitTests()
        {
            // Arrange
            var controller = new TestabilityController();

            // Act
            var result = controller.HttpBadRequest_Action();

            // Assert
            Assert.NotNull(result);

            var httpBadRequest = Assert.IsType<BadRequestResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, httpBadRequest.StatusCode);
        }

        [Fact]
        public void ControllerHttpBadRequestObject_InvokedInUnitTests()
        {
            // Arrange
            var controller = new TestabilityController();
            var error = new { Error = "Error Message" };

            // Act
            var result = controller.HttpBadRequestObject_Action(error);

            // Assert
            Assert.NotNull(result);

            var httpBadRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, httpBadRequest.StatusCode);
            Assert.Same(error, httpBadRequest.Value);

            // Arrange
            controller = new TestabilityController();

            // Act
            result = controller.HttpBadRequestObject_Action(null);

            // Assert
            Assert.NotNull(result);

            httpBadRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, httpBadRequest.StatusCode);
            Assert.Null(httpBadRequest.Value);
        }

        [Fact]
        public void ControllerCreatedAtRoute_InvokedInUnitTests()
        {
            // Arrange
            var controller = new TestabilityController();
            var routeName = "RouteName_1";
            var routeValues = new Dictionary<string, object>() { { "route", "sample" } };
            var value = new { Value = "Value_1" };

            // Act
            var result = controller.CreatedAtRoute_Action(routeName, routeValues, value);

            // Assert
            Assert.NotNull(result);

            var createdAtRouteResult = Assert.IsType<CreatedAtRouteResult>(result);
            Assert.Equal(routeName, createdAtRouteResult.RouteName);
            Assert.Single(createdAtRouteResult.RouteValues);
            Assert.Equal("sample", createdAtRouteResult.RouteValues["route"]);
            Assert.Same(value, createdAtRouteResult.Value);

            // Arrange
            controller = new TestabilityController();

            // Act
            result = controller.CreatedAtRoute_Action(null, null, null);

            // Assert
            Assert.NotNull(result);

            createdAtRouteResult = Assert.IsType<CreatedAtRouteResult>(result);
            Assert.Null(createdAtRouteResult.RouteName);
            Assert.Empty(createdAtRouteResult.RouteValues);
            Assert.Null(createdAtRouteResult.Value);
        }

        [Fact]
        public void ControllerCreatedAtAction_InvokedInUnitTests()
        {
            // Arrange
            var controller = new TestabilityController();
            var actionName = "ActionName_1";
            var controllerName = "ControllerName_1";
            var routeValues = new Dictionary<string, object>() { { "route", "sample" } };
            var value = new { Value = "Value_1" };

            // Act
            var result = controller.CreatedAtAction_Action(actionName, controllerName, routeValues, value);

            // Assert
            Assert.NotNull(result);

            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(actionName, createdAtActionResult.ActionName);
            Assert.Equal(controllerName, createdAtActionResult.ControllerName);
            Assert.Single(createdAtActionResult.RouteValues);
            Assert.Equal("sample", createdAtActionResult.RouteValues["route"]);
            Assert.Same(value, createdAtActionResult.Value);

            // Arrange
            controller = new TestabilityController();

            // Act
            result = controller.CreatedAtAction_Action(null, null, null, null);

            // Assert
            Assert.NotNull(result);

            createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Null(createdAtActionResult.ActionName);
            Assert.Null(createdAtActionResult.ControllerName);
            Assert.Null(createdAtActionResult.Value);
            Assert.Empty(createdAtActionResult.RouteValues);
        }

        [Fact]
        public void ControllerRedirectToRoute_InvokedInUnitTests()
        {
            // Arrange
            var controller = new TestabilityController();
            var routeName = "RouteName_1";
            var routeValues = new Dictionary<string, object>() { { "route", "sample" } };

            // Act
            var result = controller.RedirectToRoute_Action(routeName, routeValues);

            // Assert
            Assert.NotNull(result);

            var redirectToRouteResult = Assert.IsType<RedirectToRouteResult>(result);
            Assert.Equal(routeName, redirectToRouteResult.RouteName);
            Assert.Single(redirectToRouteResult.RouteValues);
            Assert.Equal("sample", redirectToRouteResult.RouteValues["route"]);

            // Arrange
            controller = new TestabilityController();

            // Act
            result = controller.RedirectToRoute_Action(null, null);

            // Assert
            Assert.NotNull(result);

            redirectToRouteResult = Assert.IsType<RedirectToRouteResult>(result);
            Assert.Null(redirectToRouteResult.RouteName);
            Assert.Empty(redirectToRouteResult.RouteValues);
        }

        [Fact]
        public void ControllerRedirectToAction_InvokedInUnitTests()
        {
            // Arrange
            var controller = new TestabilityController();
            var controllerName = "ControllerName_1";
            var actionName = "ActionName_1";
            var routeValues = new Dictionary<string, object>() { { "route", "sample" } };

            // Act
            var result = controller.RedirectToAction_Action(actionName, controllerName, routeValues);

            // Assert
            Assert.NotNull(result);

            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(actionName, redirectToActionResult.ActionName);
            Assert.Equal(controllerName, redirectToActionResult.ControllerName);
            Assert.Single(redirectToActionResult.RouteValues);
            Assert.Equal("sample", redirectToActionResult.RouteValues["route"]);

            // Arrange
            controller = new TestabilityController();

            // Act
            result = controller.RedirectToAction_Action(null, null, null);

            // Assert
            Assert.NotNull(result);

            redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Null(redirectToActionResult.ControllerName);
            Assert.Null(redirectToActionResult.ActionName);
            Assert.Empty(redirectToActionResult.RouteValues);
        }

        [Fact]
        public void ControllerRedirect_InvokedInUnitTests()
        {
            // Arrange
            var controller = new TestabilityController();
            var url = "http://contoso.com";

            // Act
            var result = controller.Redirect_Action(url);

            // Assert
            Assert.NotNull(result);

            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal(url, redirectResult.Url);

            // Arrange
            controller = new TestabilityController();

            // Act && Assert
            Assert.Throws<ArgumentException>(() => controller.Redirect_Action(null));
        }

        [Fact]
        public void ControllerActionContext_ReturnsNotNull()
        {
            // Arrange && Act
            var controller = new TestabilityController();

            // Assert
            Assert.NotNull(controller.ActionContext);
            Assert.NotNull(controller.ActionContext.ModelState);
            Assert.Null(controller.ActionContext.ActionDescriptor);
            Assert.Null(controller.ActionContext.HttpContext);
            Assert.Null(controller.ActionContext.RouteData);
        }

        [Fact]
        public void ActionContextDefaultConstructor_CanBeUsedForControllerActionContext()
        {
            // Arrange
            var actionContext = new ActionContext();
            var controller = new TestabilityController();

            // Act
            controller.ActionContext = actionContext;

            // Assert
            Assert.Equal(actionContext.HttpContext, controller.Context);
            Assert.Equal(actionContext.RouteData, controller.RouteData);
            Assert.Equal(actionContext.ModelState, controller.ModelState);
        }

        [Fact]
        public void ActionContextSetters_CanBeUsedWithControllerActionContext()
        {
            // Arrange
            var actionDescriptor = new Mock<ActionDescriptor>();
            var httpContext = new Mock<HttpContext>();
            var routeData = new Mock<RouteData>();

            var actionContext = new ActionContext()
            {
                ActionDescriptor = actionDescriptor.Object,
                HttpContext = httpContext.Object,
                RouteData = routeData.Object,
            };

            var controller = new TestabilityController();

            // Act
            controller.ActionContext = actionContext;

            // Assert
            Assert.Equal(httpContext.Object, controller.Context);
            Assert.Equal(routeData.Object, controller.RouteData);
            Assert.Equal(actionContext.ModelState, controller.ModelState);
            Assert.Equal(actionDescriptor.Object, actionContext.ActionDescriptor);
        }

        [Fact]
        public void ActionContextModelState_ShouldBeSameAsViewDataAndControllerModelState()
        {
            // Arrange
            var actionContext = new ActionContext();
            var controller1 = new TestabilityController();
            var controller2 = new TestabilityController();

            // Act
            controller2.ActionContext = actionContext;

            // Assert
            Assert.Equal(controller1.ModelState, controller1.ActionContext.ModelState);
            Assert.Equal(controller1.ModelState, controller1.ViewData.ModelState);

            Assert.Equal(actionContext.ModelState, controller2.ModelState);
            Assert.Equal(actionContext.ModelState, controller2.ActionContext.ModelState);
            Assert.Equal(actionContext.ModelState, controller2.ViewData.ModelState);
        }

        public static IEnumerable<object[]> TestabilityViewTestData
        {
            get
            {
                yield return new object[]
                {
                    null,
                    null
                };

                yield return new object[]
                {
                    new MyModel { Property1 = "Property_1", Property2 = "Property_2" },
                    "ViewName_1"
                };
            }
        }

        public static IEnumerable<object[]> TestabilityContentTestData
        {
            get
            {
                yield return new object[]
                {
                    null,
                    null,
                    null
                };

                yield return new object[]
                {
                    "Content_1",
                    "text/asp",
                    Encoding.ASCII
                };
            }
        }

        private class TestabilityController : Controller
        {
            public IActionResult View_Action(string viewName, object data)
            {
                return View(viewName, data);
            }

            public IActionResult PartialView_Action(string viewName, object data)
            {
                return PartialView(viewName, data);
            }

            public IActionResult Content_Action(string content, string contentType, Encoding encoding)
            {
                return Content(content, contentType, encoding);
            }

            public IActionResult Created_Action(string uri, object data)
            {
                return Created(uri, data);
            }

            public IActionResult FileContent_Action(string content, string contentType, string fileName)
            {
                var contentArray = Encoding.UTF8.GetBytes(content);
                return File(contentArray, contentType, fileName);
            }

            public IActionResult FileStream_Action(string content, string contentType, string fileName)
            {
                var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
                return File(memoryStream, contentType, fileName);
            }

            public IActionResult Json_Action(object data)
            {
                return Json(data);
            }

            public IActionResult Redirect_Action(string url)
            {
                return Redirect(url);
            }

            public IActionResult RedirectToAction_Action(string actionName, string controllerName, object routeValues)
            {
                return RedirectToAction(actionName, controllerName, routeValues);
            }

            public IActionResult RedirectToRoute_Action(string routeName, object routeValues)
            {
                return RedirectToRoute(routeName, routeValues);
            }

            public IActionResult CreatedAtAction_Action(string actionName, string controllerName, object routeValues, object value)
            {
                return CreatedAtAction(actionName, controllerName, routeValues, value);
            }

            public IActionResult CreatedAtRoute_Action(string routeName, object routeValues, object value)
            {
                return CreatedAtRoute(routeName, routeValues, value);
            }

            public IActionResult HttpBadRequest_Action()
            {
                return HttpBadRequest();
            }

            public IActionResult HttpBadRequestObject_Action(object error)
            {
                return HttpBadRequest(error);
            }

            public IActionResult HttpNotFound_Action()
            {
                return HttpNotFound();
            }

            public IActionResult HttpNotFoundObject_Action(object value)
            {
                return HttpNotFound(value);
            }
        }

        private class MyModel
        {
            public string Property1 { get; set; }
            public string Property2 { get; set; }
        }
    }
}
