// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.DataAnnotations.Internal;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Test
{
    public class ControllerTest
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
                TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
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
        public void Controller_View_WithoutParameter_MaintainsModelInViewData()
        {
            // Arrange
            var controller = new TestableController()
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
            };

            var model = new object();
            controller.ViewData.Model = model;

            // Act
            var actualViewResult = controller.View();

            // Assert
            Assert.IsType<ViewResult>(actualViewResult);
            Assert.Null(actualViewResult.ViewName);
            Assert.Same(controller.ViewData, actualViewResult.ViewData);
            Assert.Same(controller.TempData, actualViewResult.TempData);
            Assert.Same(model, actualViewResult.ViewData.Model);
        }

        [Fact]
        public void Controller_View_WithParameterViewName_SetsResultViewNameAndNullViewDataModelAndSameTempData()
        {
            // Arrange
            var controller = new TestableController()
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
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
                TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
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
                TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
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
        public void Controller_View_WithNullModelParameter_OverwritesViewDataModel()
        {
            // Arrange
            var controller = new TestableController()
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
            };
            controller.ViewData.Model = new object();

            // Act
            var actualViewResult = controller.View(model: null);

            // Assert
            Assert.IsType<ViewResult>(actualViewResult);
            Assert.Null(actualViewResult.ViewName);
            Assert.Same(controller.ViewData, actualViewResult.ViewData);
            Assert.Same(controller.TempData, actualViewResult.TempData);
            Assert.Null(actualViewResult.ViewData.Model);
        }

        [Fact]
        public void Controller_PartialView_WithoutParameter_MaintainsModelInViewData()
        {
            // Arrange
            var controller = new TestableController()
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
            };

            var model = new object();
            controller.ViewData.Model = model;

            // Act
            var actualViewResult = controller.PartialView();

            // Assert
            Assert.IsType<PartialViewResult>(actualViewResult);
            Assert.Null(actualViewResult.ViewName);
            Assert.Same(controller.ViewData, actualViewResult.ViewData);
            Assert.Same(controller.TempData, actualViewResult.TempData);
            Assert.Same(model, actualViewResult.ViewData.Model);
        }

        [Fact]
        public void Controller_PartialView_WithParameterViewName_SetsResultViewNameAndMaintainsSameViewDataModelAndTempData()
        {
            // Arrange
            var controller = new TestableController()
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
            };
            var model = new object();
            controller.ViewData.Model = model;

            // Act
            var actualViewResult = controller.PartialView("CustomViewName");

            // Assert
            Assert.IsType<PartialViewResult>(actualViewResult);
            Assert.Equal("CustomViewName", actualViewResult.ViewName);
            Assert.Same(controller.ViewData, actualViewResult.ViewData);
            Assert.Same(controller.TempData, actualViewResult.TempData);
            Assert.Same(model, actualViewResult.ViewData.Model);
        }

        [Fact]
        public void Controller_PartialView_WithParameterViewModel_SetsResultNullViewNameAndViewDataModelAndSameTempData()
        {
            // Arrange
            var controller = new TestableController()
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
            };
            var model = new object();

            // Act
            var actualViewResult = controller.PartialView(model);

            // Assert
            Assert.IsType<PartialViewResult>(actualViewResult);
            Assert.Null(actualViewResult.ViewName);
            Assert.Same(controller.ViewData, actualViewResult.ViewData);
            Assert.Same(controller.TempData, actualViewResult.TempData);
            Assert.Same(model, actualViewResult.ViewData.Model);
        }

        [Fact]
        public void Controller_PartialView_WithParameterViewNameAndViewModel_SetsResultViewNameAndViewDataModelAndSameTempData()
        {
            // Arrange
            var controller = new TestableController()
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
            };
            var model = new object();

            // Act
            var actualViewResult = controller.PartialView("CustomViewName", model);

            // Assert
            Assert.IsType<PartialViewResult>(actualViewResult);
            Assert.Equal("CustomViewName", actualViewResult.ViewName);
            Assert.Same(controller.ViewData, actualViewResult.ViewData);
            Assert.Same(controller.TempData, actualViewResult.TempData);
            Assert.Same(model, actualViewResult.ViewData.Model);
        }

        [Fact]
        public void Controller_PartialView_WithNullModelParameter_OverwritesViewDataModel()
        {
            // Arrange
            var controller = new TestableController()
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
            };
            controller.ViewData.Model = new object();

            // Act
            var actualViewResult = controller.PartialView(model: null);

            // Assert
            Assert.IsType<PartialViewResult>(actualViewResult);
            Assert.Null(actualViewResult.ViewName);
            Assert.Same(controller.ViewData, actualViewResult.ViewData);
            Assert.Same(controller.TempData, actualViewResult.TempData);
            Assert.Null(actualViewResult.ViewData.Model);
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
        public void Controller_Json_WithParameterValue_SetsRespectiveProperty()
        {
            // Arrange
            var controller = new TestableController();
            var data = new object();
            var serializerSettings = new JsonSerializerSettings();

            // Act
            var actualJsonResult = controller.Json(data, serializerSettings);

            // Assert
            Assert.IsType<JsonResult>(actualJsonResult);
            Assert.Same(data, actualJsonResult.Value);
        }

        // These tests share code with the ActionFilterAttribute tests because the various filter
        // implementations need to behave the same way.
        [Fact]
        public async Task Controller_ActionFilter_SettingResult_ShortCircuits()
        {
            // Arrange, Act &  Assert
            await CommonFilterTest.ActionFilter_SettingResult_ShortCircuits(
                new Mock<Controller>());
        }

        [Fact]
        public async Task Controller_ActionFilter_Calls_OnActionExecuted()
        {
            // Arrange, Act &  Assert
            await CommonFilterTest.ActionFilter_Calls_OnActionExecuted(
                new Mock<Controller>());
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

        public static TheoryData<object, Type> IncompatibleModelData
        {
            get
            {
                // Small grab bag of instances and expected types with no common base except typeof(object).
                return new TheoryData<object, Type>
                {
                    { null, typeof(object) },
                    { true, typeof(bool) },
                    { 43.78, typeof(double) },
                    { "test string", typeof(string) },
                    { new List<int>(), typeof(List<int>) },
                    { new List<string>(), typeof(List<string>) },
                };
            }
        }

        [Theory]
        [MemberData(nameof(IncompatibleModelData))]
        public void ViewDataModelSetter_DoesNotThrow(object model, Type expectedType)
        {
            // Arrange
            var activator = new ViewDataDictionaryControllerPropertyActivator(new EmptyModelMetadataProvider());
            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                new ControllerActionDescriptor());
            var controllerContext = new ControllerContext(actionContext);
            var controller = new TestableController();
            activator.Activate(controllerContext, controller);

            // Guard
            Assert.NotNull(controller.ViewData);

            // Act (does not throw)
            controller.ViewData.Model = model;

            // Assert
            Assert.NotNull(controller.ViewData.ModelMetadata);
            Assert.Equal(expectedType, controller.ViewData.ModelMetadata.ModelType);
        }

        private static Controller GetController(IModelBinder binder, IValueProvider valueProvider)
        {
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var httpContext = new DefaultHttpContext();

            var viewData = new ViewDataDictionary(metadataProvider, new ModelStateDictionary());
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            var valiatorProviders = new[]
            {
                new DataAnnotationsModelValidatorProvider(
                    new ValidationAttributeAdapterProvider(),
                    Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                    stringLocalizerFactory: null),
            };

            valueProvider = valueProvider ?? new SimpleValueProvider();
            var controllerContext = new ControllerContext()
            {
                HttpContext = httpContext,
                ValueProviderFactories = new[] { new SimpleValueProviderFactory(valueProvider), },
            };

            var controller = new TestableController()
            {
                ControllerContext = controllerContext,
                MetadataProvider = metadataProvider,
                ObjectValidator = new DefaultObjectValidator(metadataProvider, valiatorProviders),
                TempData = tempData,
                ViewData = viewData,
            };
            return controller;
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
