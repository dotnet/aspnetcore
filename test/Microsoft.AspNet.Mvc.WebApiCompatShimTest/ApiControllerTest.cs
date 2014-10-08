// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.PipelineCore;
using Microsoft.AspNet.Routing;
using Newtonsoft.Json;
using Xunit;

namespace System.Web.Http
{
    public class ApiControllerTest
    {
        [Fact]
        public void AccessDependentProperties()
        {
            // Arrange
            var controller = new ConcreteApiController();

            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal();

            var routeContext = new RouteContext(httpContext);
            var actionContext = new ActionContext(routeContext, new ActionDescriptor());

            // Act
            controller.ActionContext = actionContext;

            // Assert
            Assert.Same(httpContext, controller.Context);
            Assert.Same(actionContext.ModelState, controller.ModelState);
            Assert.Same(httpContext.User, controller.User);
        }

        [Fact]
        public void AccessDependentProperties_UnsetContext()
        {
            // Arrange
            var controller = new ConcreteApiController();

            // Act & Assert
            Assert.Null(controller.Context);
            Assert.Null(controller.ModelState);
            Assert.Null(controller.User);
        }

        [Fact]
        public void ApiController_BadRequest()
        {
            // Arrange
            var controller = new ConcreteApiController();

            // Act
            var result = controller.BadRequest();

            // Assert
            Assert.Equal(400, Assert.IsType<BadRequestResult>(result).StatusCode);
        }

        [Fact]
        public void ApiController_BadRequest_Message()
        {
            // Arrange
            var controller = new ConcreteApiController();

            // Act
            var result = controller.BadRequest("Error");

            // Assert
            var badRequest = Assert.IsType<BadRequestErrorMessageResult>(result);
            Assert.Equal("Error", badRequest.Message);

            var httpError = Assert.IsType<HttpError>(badRequest.Value);
            Assert.Equal("Error", httpError.Message);
        }

        [Fact]
        public void ApiController_BadRequest_ModelState()
        {
            // Arrange
            var controller = new ConcreteApiController();

            var modelState = new ModelStateDictionary();
            modelState.AddModelError("product.Name", "Name is required");

            // Act
            var result = controller.BadRequest(modelState);

            // Assert
            var badRequest = Assert.IsType<InvalidModelStateResult>(result);

            var modelError = Assert.IsType<HttpError>(badRequest.Value).ModelState;
            Assert.Equal(new string[] { "Name is required" }, modelError["product.Name"]);
        }

        [Fact]
        public void ApiController_Created_Uri()
        {
            // Arrange
            var controller = new ConcreteApiController();

            var uri = new Uri("http://contoso.com");
            var product = new Product();

            // Act
            var result = controller.Created(uri, product);

            // Assert
            var created = Assert.IsType<CreatedNegotiatedContentResult<Product>>(result);
            Assert.Same(product, created.Content);
            Assert.Same(uri, created.Location);
        }

        [Theory]
        [InlineData("http://contoso.com/Api/Products")]
        [InlineData("/Api/Products")]
        [InlineData("Products")]
        public void ApiController_Created_String(string uri)
        {
            // Arrange
            var controller = new ConcreteApiController();

            var product = new Product();

            // Act
            var result = controller.Created(uri, product);

            // Assert
            var created = Assert.IsType<CreatedNegotiatedContentResult<Product>>(result);
            Assert.Same(product, created.Content);
            Assert.Equal(uri, created.Location.OriginalString);
        }

        [Fact]
        public void ApiController_CreatedAtRoute()
        {
            // Arrange
            var controller = new ConcreteApiController();

            var product = new Product();

            // Act
            var result = controller.CreatedAtRoute("api_route", new { controller = "Products" }, product);

            // Assert
            var created = Assert.IsType<CreatedAtRouteNegotiatedContentResult<Product>>(result);
            Assert.Same(product, created.Content);
            Assert.Equal("api_route", created.RouteName);
            Assert.Equal("Products", created.RouteValues["controller"]);
        }

        [Fact]
        public void ApiController_CreatedAtRoute_Dictionary()
        {
            // Arrange
            var controller = new ConcreteApiController();

            var product = new Product();
            var values = new RouteValueDictionary(new { controller = "Products" });

            // Act
            var result = controller.CreatedAtRoute("api_route", values, product);

            // Assert
            var created = Assert.IsType<CreatedAtRouteNegotiatedContentResult<Product>>(result);
            Assert.Same(product, created.Content);
            Assert.Equal("api_route", created.RouteName);
            Assert.Equal("Products", created.RouteValues["controller"]);
            Assert.Same(values, created.RouteValues);
        }

        [Fact]
        public void ApiController_Conflict()
        {
            // Arrange
            var controller = new ConcreteApiController();

            // Act
            var result = controller.Conflict();

            // Assert
            Assert.Equal(409, Assert.IsType<ConflictResult>(result).StatusCode);
        }

        [Fact]
        public void ApiController_Content()
        {
            // Arrange
            var controller = new ConcreteApiController();

            var content = new Product();

            // Act
            var result = controller.Content(HttpStatusCode.Found, content);

            // Assert
            var contentResult = Assert.IsType<NegotiatedContentResult<Product>>(result);
            Assert.Equal(HttpStatusCode.Found, contentResult.StatusCode);
            Assert.Equal(content, contentResult.Value);
        }

        [Fact]
        public void ApiController_InternalServerError()
        {
            // Arrange
            var controller = new ConcreteApiController();

            // Act
            var result = controller.InternalServerError();

            // Assert
            Assert.Equal(500, Assert.IsType<InternalServerErrorResult>(result).StatusCode);
        }

        [Fact]
        public void ApiController_InternalServerError_Exception()
        {
            // Arrange
            var controller = new ConcreteApiController();
            var exception = new ArgumentException();

            // Act
            var result = controller.InternalServerError(exception);

            // Assert
            var exceptionResult = Assert.IsType<ExceptionResult>(result);
            Assert.Same(exception, exceptionResult.Exception);
        }

        [Fact]
        public void ApiController_Json()
        {
            // Arrange
            var controller = new ConcreteApiController();
            var product = new Product();

            // Act
            var result = controller.Json(product);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Same(product, objectResult.Value);
        }

        [Fact]
        public void ApiController_Json_Settings()
        {
            // Arrange
            var controller = new ConcreteApiController();
            var product = new Product();
            var settings = new JsonSerializerSettings();

            // Act
            var result = controller.Json(product, settings);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Same(product, objectResult.Value);

            var formatter = Assert.IsType<JsonOutputFormatter>(Assert.Single(objectResult.Formatters));
            Assert.Same(settings, formatter.SerializerSettings);
        }

        [Fact]
        public void ApiController_Json_Settings_Encoding()
        {
            // Arrange
            var controller = new ConcreteApiController();
            var product = new Product();
            var settings = new JsonSerializerSettings();

            // Act
            var result = controller.Json(product, settings, Encoding.UTF8);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Same(product, objectResult.Value);

            var formatter = Assert.IsType<JsonOutputFormatter>(Assert.Single(objectResult.Formatters));
            Assert.Same(settings, formatter.SerializerSettings);
            Assert.Same(Encoding.UTF8, Assert.Single(formatter.SupportedEncodings));
        }

        [Fact]
        public void ApiController_NotFound()
        {
            // Arrange
            var controller = new ConcreteApiController();

            // Act
            var result = controller.NotFound();

            // Assert
            Assert.Equal(404, Assert.IsType<HttpNotFoundResult>(result).StatusCode);
        }

        [Fact]
        public void ApiController_Ok()
        {
            // Arrange
            var controller = new ConcreteApiController();

            // Act
            var result = controller.Ok();

            // Assert
            Assert.Equal(200, Assert.IsType<OkResult>(result).StatusCode);
        }


        [Fact]
        public void ApiController_Ok_Content()
        {
            // Arrange
            var controller = new ConcreteApiController();
            var product = new Product();

            // Act
            var result = controller.Ok(product);

            // Assert
            var okResult = Assert.IsType<OkNegotiatedContentResult<Product>>(result);
            Assert.Same(product, okResult.Content);
        }

        [Fact]
        public void ApiController_Redirect()
        {
            // Arrange
            var controller = new ConcreteApiController();

            var uri = new Uri("http://contoso.com");

            // Act
            var result = controller.Redirect(uri);

            // Assert
            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Equal(uri.AbsoluteUri, result.Url);
        }

        [Theory]
        [InlineData("http://contoso.com/Api/Products")]
        public void ApiController_Redirect_String(string uri)
        {
            // Arrange
            var controller = new ConcreteApiController();

            // Act
            var result = controller.Redirect(uri);

            // Assert
            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Equal(uri, result.Url);
        }

        [Fact]
        public void ApiController_RedirectToRoute()
        {
            // Arrange
            var controller = new ConcreteApiController();

            // Act
            var result = controller.RedirectToRoute("api_route", new { controller = "Products" });

            // Assert
            var created = Assert.IsType<RedirectToRouteResult>(result);
            Assert.Equal("api_route", created.RouteName);
            Assert.Equal("Products", created.RouteValues["controller"]);
        }

        [Fact]
        public void ApiController_RedirectToRoute_Dictionary()
        {
            // Arrange
            var controller = new ConcreteApiController();

            var product = new Product();
            var values = new RouteValueDictionary(new { controller = "Products" });

            // Act
            var result = controller.RedirectToRoute("api_route", values);

            // Assert
            var created = Assert.IsType<RedirectToRouteResult>(result);
            Assert.Equal("api_route", created.RouteName);
            Assert.Equal("Products", created.RouteValues["controller"]);
        }

        [Fact]
        public void ApiController_ResponseMessage()
        {
            // Arrange
            var controller = new ConcreteApiController();

            var response = new HttpResponseMessage(HttpStatusCode.NoContent);

            // Act
            var result = controller.ResponseMessage(response);

            // Assert
            var responseResult = Assert.IsType<ResponseMessageResult>(result);
            Assert.Same(response, responseResult.Response);
        }

        [Fact]
        public void ApiController_StatusCode()
        {
            // Arrange
            var controller = new ConcreteApiController();

            // Act
            var result = controller.StatusCode(HttpStatusCode.ExpectationFailed);

            // Assert
            Assert.Equal(417, Assert.IsType<HttpStatusCodeResult>(result).StatusCode);
        }

        private class Product
        {
            public string Name { get; set; }

            public int Id { get; set; }
        }

        private class ConcreteApiController : ApiController
        {
        }
    }
}