// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Testing;
#if ASPNET50
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
        public void Redirect_WithParameterUrl_SetsRedirectResultSameUrl()
        {
            // Arrange
            var controller = new Controller();
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
            var controller = new Controller();
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
            var controller = new Controller();

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
            var controller = new Controller();

            // Act & Assert
            ExceptionAssert.ThrowsArgumentNullOrEmpty(
                () => controller.RedirectPermanent(url: url), "url");
        }

        [Fact]
        public void RedirectToAction_WithParameterActionName_SetsResultActionName()
        {
            // Arrange
            var controller = new Controller();

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
            var controller = new Controller();

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
            var controller = new Controller();

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
            var controller = new Controller();

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
            var controller = new Controller();

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
            var controller = new Controller();

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
            var controller = new Controller();

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
            var controller = new Controller();

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
            var controller = new Controller();

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
            var controller = new Controller();

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
            var controller = new Controller();
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
            var controller = new Controller();
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
            var controller = new Controller();
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
            var controller = new Controller();
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
            var controller = new Controller();
            var uri = "http://test/url";

            // Act
            var result = controller.Created(uri, null);

            // Assert
            Assert.IsType<CreatedResult>(result);
            Assert.Equal(201, result.StatusCode);
            Assert.Same(uri, result.Location);
        }

        [Fact]
        public void Created_WithAbsoluteUriParameter_SetsCreatedLocation()
        {
            // Arrange
            var controller = new Controller();
            var uri = new Uri("http://test/url");

            // Act
            var result = controller.Created(uri, null);

            // Assert
            Assert.IsType<CreatedResult>(result);
            Assert.Equal(201, result.StatusCode);
            Assert.Equal(uri.OriginalString, result.Location);
        }

        [Fact]
        public void Created_WithRelativeUriParameter_SetsCreatedLocation()
        {
            // Arrange
            var controller = new Controller();
            var uri = new Uri("/test/url", UriKind.Relative);

            // Act
            var result = controller.Created(uri, null);

            // Assert
            Assert.IsType<CreatedResult>(result);
            Assert.Equal(201, result.StatusCode);
            Assert.Equal(uri.OriginalString, result.Location);
        }

        [Fact]
        public void CreatedAtAction_WithParameterActionName_SetsResultActionName()
        {
            // Arrange
            var controller = new Controller();

            // Act
            var result = controller.CreatedAtAction("SampleAction", null);

            // Assert
            Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(201, result.StatusCode);
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
            var controller = new Controller();

            // Act
            var result = controller.CreatedAtAction("SampleAction", controllerName, null, null);

            // Assert
            Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(201, result.StatusCode);
            Assert.Equal("SampleAction", result.ActionName);
            Assert.Equal(controllerName, result.ControllerName);
        }

        [Fact]
        public void CreatedAtAction_WithActionControllerRouteValues_SetsSameValues()
        {
            // Arrange
            var controller = new Controller();
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
            Assert.Equal(201, result.StatusCode);
            Assert.Equal("SampleAction", result.ActionName);
            Assert.Equal("SampleController", result.ControllerName);
            Assert.Equal(expected, result.RouteValues);
        }

        [Fact]
        public void CreatedAtRoute_WithParameterRouteName_SetsResultSameRouteName()
        {
            // Arrange
            var controller = new Controller();
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
            var controller = new Controller();
            var expected = new Dictionary<string, object>
                {
                    { "test", "case" },
                    { "sample", "route" },
                };

            // Act
            var result = controller.CreatedAtRoute(new RouteValueDictionary(expected), null);

            // Assert
            Assert.IsType<CreatedAtRouteResult>(result);
            Assert.Equal(201, result.StatusCode);
            Assert.Equal(expected, result.RouteValues);
        }

        [Fact]
        public void CreatedAtRoute_WithParameterRouteNameAndValues_SetsResultSameProperties()
        {
            // Arrange
            var controller = new Controller();
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
            Assert.Equal(201, result.StatusCode);
            Assert.Same(routeName, result.RouteName);
            Assert.Equal(expected, result.RouteValues);
        }

        [Fact]
        public void File_WithContents()
        {
            // Arrange
            var controller = new Controller();
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
            var controller = new Controller();
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
            var controller = new Controller();
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
            var controller = new Controller();
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
            var controller = new Controller();
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
            var controller = new Controller();
            var fileStream = Stream.Null;

            // Act
            var result = controller.File(fileStream, "someContentType", "someDownloadName");

            // Assert
            Assert.NotNull(result);
            Assert.Same(fileStream, result.FileStream);
            Assert.Equal("someContentType", result.ContentType);
            Assert.Equal("someDownloadName", result.FileDownloadName);
        }


        [Fact]
        public void HttpNotFound_SetsStatusCode()
        {
            // Arrange
            var controller = new Controller();

            // Act
            var result = controller.HttpNotFound();

            // Assert
            Assert.IsType<HttpNotFoundResult>(result);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public void BadRequest_SetsStatusCode()
        {
            // Arrange
            var controller = new Controller();

            // Act
            var result = controller.HttpBadRequest();

            // Assert
            Assert.IsType<BadRequestResult>(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public void BadRequest_SetsStatusCodeAndValue_Object()
        {
            // Arrange
            var controller = new Controller();
            var obj = new object();

            // Act
            var result = controller.HttpBadRequest(obj);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal(obj, result.Value);
        }

        [Fact]
        public void BadRequest_SetsStatusCodeAndValue_ModelState()
        {
            // Arrange
            var controller = new Controller();

            // Act
            var result = controller.HttpBadRequest(new ModelStateDictionary());

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, result.StatusCode);
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
        public void Controller_View_WithoutParameter_SetsResultNullViewNameAndNullViewDataModel()
        {
            // Arrange
            var controller = new Controller()
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
            };

            // Act
            var actualViewResult = controller.View();

            // Assert
            Assert.IsType<ViewResult>(actualViewResult);
            Assert.Null(actualViewResult.ViewName);
            Assert.Same(controller.ViewData, actualViewResult.ViewData);
            Assert.Null(actualViewResult.ViewData.Model);
        }

        [Fact]
        public void Controller_View_WithParameterViewName_SetsResultViewNameAndNullViewDataModel()
        {
            // Arrange
            var controller = new Controller()
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
            };

            // Act
            var actualViewResult = controller.View("CustomViewName");

            // Assert
            Assert.IsType<ViewResult>(actualViewResult);
            Assert.Equal("CustomViewName", actualViewResult.ViewName);
            Assert.Same(controller.ViewData, actualViewResult.ViewData);
            Assert.Null(actualViewResult.ViewData.Model);
        }

        [Fact]
        public void Controller_View_WithParameterViewModel_SetsResultNullViewNameAndViewDataModel()
        {
            // Arrange
            var controller = new Controller()
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
            };
            var model = new object();

            // Act
            var actualViewResult = controller.View(model);

            // Assert
            Assert.IsType<ViewResult>(actualViewResult);
            Assert.Null(actualViewResult.ViewName);
            Assert.Same(controller.ViewData, actualViewResult.ViewData);
            Assert.Same(model, actualViewResult.ViewData.Model);
        }

        [Fact]
        public void Controller_View_WithParameterViewNameAndViewModel_SetsResultViewNameAndViewDataModel()
        {
            // Arrange
            var controller = new Controller()
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
            };
            var model = new object();

            // Act
            var actualViewResult = controller.View("CustomViewName", model);

            // Assert
            Assert.IsType<ViewResult>(actualViewResult);
            Assert.Equal("CustomViewName", actualViewResult.ViewName);
            Assert.Same(controller.ViewData, actualViewResult.ViewData);
            Assert.Same(model, actualViewResult.ViewData.Model);
        }

        [Fact]
        public void Controller_Content_WithParameterContentString_SetsResultContent()
        {
            // Arrange
            var controller = new Controller();

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
            var controller = new Controller();

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
            var controller = new Controller();

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
            var controller = new Controller();
            var data = new object();

            // Act
            var actualJsonResult = controller.Json(data);

            // Assert
            Assert.IsType<JsonResult>(actualJsonResult);
            Assert.Same(data, actualJsonResult.Value);
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
#if ASPNET50
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
            var metadataProvider = new DataAnnotationsModelMetadataProvider();
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
                  .Returns(Task.FromResult(false))
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

            var metadataProvider = new DataAnnotationsModelMetadataProvider();
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
                  .Returns(Task.FromResult(false))
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
                  .Returns(Task.FromResult(false))
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
                  .Returns(Task.FromResult(true))
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
                  .Returns(Task.FromResult(true))
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
                  .Returns(Task.FromResult(true))
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
                  .Returns(Task.FromResult(true))
                  .Verifiable();

            var controller = GetController(binder.Object, provider: null);
            var model = new MyModel();

            // Act
            await controller.TryUpdateModelAsync(model, prefix, valueProvider.Object, m => m.Property1, m => m.Property2);

            // Assert
            binder.Verify();
        }

#endif

        [Fact]
        public void ControllerExposes_RequestServices()
        {
            // Arrange
            var controller = new Controller();

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
            var controller = new Controller();

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
            var controller = new Controller();

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
            var controller = new Controller();

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
        public void ControllerExpose_ViewEngine()
        {
            // Arrange
            var controller = new Controller();

            var viewEngine = Mock.Of<ICompositeViewEngine>();

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(s => s.GetService(It.Is<Type>(t => t == typeof(ICompositeViewEngine))))
                .Returns(viewEngine);

            var httpContext = new Mock<HttpContext>();
            httpContext
                .Setup(c => c.RequestServices)
                .Returns(serviceProvider.Object);

            controller.ActionContext = new ActionContext(httpContext.Object,
                                                  Mock.Of<RouteData>(),
                                                  new ActionDescriptor());

            // Act
            var innerViewEngine = controller.ViewEngine;

            // Assert
            Assert.Same(viewEngine, innerViewEngine);
        }

        [Fact]
        public void ControllerView_UsesControllerViewEngine()
        {
            // Arrange
            var controller = new Controller();

            var viewEngine = Mock.Of<ICompositeViewEngine>();

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(s => s.GetService(It.Is<Type>(t => t == typeof(ICompositeViewEngine))))
                .Returns(viewEngine);

            var httpContext = new Mock<HttpContext>();
            httpContext
                .Setup(c => c.RequestServices)
                .Returns(serviceProvider.Object);

            controller.ActionContext = new ActionContext(httpContext.Object,
                                                  Mock.Of<RouteData>(),
                                                  new ActionDescriptor());

            // Act
            var unsused = controller.ViewEngine;
            var result = controller.View();

            // Assert
            Assert.Same(viewEngine, result.ViewEngine);
        }

        private static Controller GetController(IModelBinder binder, IValueProvider provider)
        {
            var metadataProvider = new DataAnnotationsModelMetadataProvider();
            var actionContext = new ActionContext(Mock.Of<HttpContext>(), new RouteData(), new ActionDescriptor());

            var viewData = new ViewDataDictionary(metadataProvider, new ModelStateDictionary());

            var bindingContext = new ActionBindingContext()
            {
                ModelBinder = binder,
                ValueProvider = provider,
            };

            return new Controller()
            {
                ActionContext = actionContext,
                BindingContext = bindingContext,
                MetadataProvider = metadataProvider,
                ViewData = viewData
            };
        }

        private class MyModel
        {
            public string Property1 { get; set; }
            public string Property2 { get; set; }
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

        private class DisposableController : Controller
        {
            public bool DisposeCalled { get; private set; }

            protected override void Dispose(bool disposing)
            {
                DisposeCalled = true;
            }
        }
    }
}
