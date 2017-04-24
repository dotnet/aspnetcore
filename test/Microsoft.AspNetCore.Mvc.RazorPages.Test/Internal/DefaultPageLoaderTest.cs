// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class DefaultPageLoaderTest
    {
        [Fact]
        public void CreateDescriptor_CopiesPropertiesFromBaseClass()
        {
            // Arrange
            var expected = new PageActionDescriptor() // We only copy the properties that are meaningful for pages.
            {
                ActionConstraints = new List<IActionConstraintMetadata>(),
                AttributeRouteInfo = new AttributeRouteInfo(),
                FilterDescriptors = new List<FilterDescriptor>(),
                RelativePath = "/Foo",
                RouteValues = new Dictionary<string, string>(),
                ViewEnginePath = "/Pages/Foo",
            };

            // Act
            var actual = DefaultPageLoader.CreateDescriptor(expected, typeof(EmptyPage).GetTypeInfo());

            // Assert
            Assert.Same(expected.ActionConstraints, actual.ActionConstraints);
            Assert.Same(expected.AttributeRouteInfo, actual.AttributeRouteInfo);
            Assert.Same(expected.FilterDescriptors, actual.FilterDescriptors);
            Assert.Same(expected.Properties, actual.Properties);
            Assert.Same(expected.RelativePath, actual.RelativePath);
            Assert.Same(expected.RouteValues, actual.RouteValues);
            Assert.Same(expected.ViewEnginePath, actual.ViewEnginePath);
        }

        // We want to test the the 'empty' page has no bound properties, and no handler methods.
        [Fact]
        public void CreateDescriptor_EmptyPage()
        {
            // Arrange
            var type = typeof(EmptyPage).GetTypeInfo();

            // Act
            var result = DefaultPageLoader.CreateDescriptor(new PageActionDescriptor(), type);

            // Assert
            Assert.Empty(result.BoundProperties);
            Assert.Empty(result.HandlerMethods);
            Assert.Same(typeof(EmptyPage).GetTypeInfo(), result.HandlerTypeInfo);
            Assert.Same(typeof(EmptyPage).GetTypeInfo(), result.ModelTypeInfo);
            Assert.Same(typeof(EmptyPage).GetTypeInfo(), result.PageTypeInfo);
        }

        // We want to test the the 'empty' page and pagemodel has no bound properties, and no handler methods.
        [Fact]
        public void CreateDescriptor_EmptyPageModel()
        {
            // Arrange
            var type = typeof(EmptyPageWithPageModel).GetTypeInfo();

            // Act
            var result = DefaultPageLoader.CreateDescriptor(new PageActionDescriptor(), type);

            // Assert
            Assert.Empty(result.BoundProperties);
            Assert.Empty(result.HandlerMethods);
            Assert.Same(typeof(EmptyPageWithPageModel).GetTypeInfo(), result.HandlerTypeInfo);
            Assert.Same(typeof(EmptyPageModel).GetTypeInfo(), result.ModelTypeInfo);
            Assert.Same(typeof(EmptyPageWithPageModel).GetTypeInfo(), result.PageTypeInfo);
        }

        private class EmptyPage : Page
        {
            // Copied from generated code
            [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
            public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
            [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
            public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
            [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
            public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
            [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
            public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
            [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
            public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<EmptyPage> Html { get; private set; }
            public global::Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<EmptyPage> ViewData => null;
            public EmptyPage Model => ViewData.Model;

            public override Task ExecuteAsync()
            {
                throw new NotImplementedException();
            }
        }

        private class EmptyPageWithPageModel : Page
        {
            // Copied from generated code
            [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
            public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
            [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
            public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
            [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
            public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
            [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
            public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
            [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
            public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<EmptyPageModel> Html { get; private set; }
            public global::Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<EmptyPageModel> ViewData => null;
            public EmptyPageModel Model => ViewData.Model;

            public override Task ExecuteAsync()
            {
                throw new NotImplementedException();
            }
        }

        private class EmptyPageModel : PageModel
        {
        }

        [Fact] // If the model has handler methods, we prefer those.
        public void CreateDescriptor_FindsHandlerMethod_OnModel()
        {
            // Arrange
            var type = typeof(PageWithHandlerThatGetsIgnored).GetTypeInfo();

            // Act
            var result = DefaultPageLoader.CreateDescriptor(new PageActionDescriptor(), type);

            // Assert
            Assert.Collection(result.BoundProperties, p => Assert.Equal("BindMe", p.Name));
            Assert.Collection(result.HandlerMethods, h => Assert.Equal("OnGet", h.MethodInfo.Name));
            Assert.Same(typeof(ModelWithHandler).GetTypeInfo(), result.HandlerTypeInfo);
            Assert.Same(typeof(ModelWithHandler).GetTypeInfo(), result.ModelTypeInfo);
            Assert.Same(typeof(PageWithHandlerThatGetsIgnored).GetTypeInfo(), result.PageTypeInfo);
        }

        private class ModelWithHandler
        {
            [ModelBinder]
            public int BindMe { get; set; }

            public void OnGet() { }
        }

        private class PageWithHandlerThatGetsIgnored
        {
            public ModelWithHandler Model => null;

            [ModelBinder]
            public int IgnoreMe { get; set; }

            public void OnPost() { }
        }


        [Fact] // If the model has no handler methods, we look at the page instead.
        public void CreateDescriptor_FindsHandlerMethodOnPage_WhenModelHasNoHandlers()
        {
            // Arrange
            var type = typeof(PageWithHandler).GetTypeInfo();

            // Act
            var result = DefaultPageLoader.CreateDescriptor(new PageActionDescriptor(), type);

            // Assert
            Assert.Collection(result.BoundProperties, p => Assert.Equal("BindMe", p.Name));
            Assert.Collection(result.HandlerMethods, h => Assert.Equal("OnGet", h.MethodInfo.Name));
            Assert.Same(typeof(PageWithHandler).GetTypeInfo(), result.HandlerTypeInfo);
            Assert.Same(typeof(PocoModel).GetTypeInfo(), result.ModelTypeInfo);
            Assert.Same(typeof(PageWithHandler).GetTypeInfo(), result.PageTypeInfo);
        }

        private class PocoModel
        {
            // Just a plain ol' model, nothing to see here.

            [ModelBinder]
            public int IgnoreMe { get; set; }
        }

        private class PageWithHandler
        {
            public PocoModel Model => null;

            [ModelBinder]
            public int BindMe { get; set; }

            public void OnGet() { }
        }

        [Fact]
        public void CreateHandlerMethods_DiscoversHandlersFromBaseType()
        {
            // Arrange
            var type = typeof(InheritsMethods).GetTypeInfo();

            // Act
            var results = DefaultPageLoader.CreateHandlerMethods(type);

            // Assert
            Assert.Collection(
                results.OrderBy(h => h.MethodInfo.Name).ToArray(),
                (handler) =>
                {
                    Assert.Equal("OnGet", handler.MethodInfo.Name);
                    Assert.Equal(typeof(InheritsMethods), handler.MethodInfo.DeclaringType);
                },
                (handler) =>
                {
                    Assert.Equal("OnGet", handler.MethodInfo.Name);
                    Assert.Equal(typeof(TestSetPageModel), handler.MethodInfo.DeclaringType);
                },
                (handler) =>
                {
                    Assert.Equal("OnPost", handler.MethodInfo.Name);
                    Assert.Equal(typeof(TestSetPageModel), handler.MethodInfo.DeclaringType);
                });
        }

        private class TestSetPageModel
        {
            public void OnGet()
            {
            }

            public void OnPost()
            {
            }
        }

        private class TestSetPageWithModel
        {
            public TestSetPageModel Model { get; set; }
        }

        private class InheritsMethods : TestSetPageModel
        {
            public new void OnGet()
            {
            }
        }

        [Fact]
        public void CreateHandlerMethods_IgnoresNonPublicMethods()
        {
            // Arrange
            var type = typeof(ProtectedModel).GetTypeInfo();

            // Act
            var results = DefaultPageLoader.CreateHandlerMethods(type);

            // Assert
            Assert.Empty(results);
        }

        private class ProtectedModel
        {
            protected void OnGet()
            {
            }

            private void OnPost()
            {
            }
        }

        [Fact]
        public void CreateHandlerMethods_IgnoreGenericTypeParameters()
        {
            // Arrange
            var type = typeof(GenericClassModel).GetTypeInfo();

            // Act
            var results = DefaultPageLoader.CreateHandlerMethods(type);

            // Assert
            Assert.Empty(results);
        }

        private class GenericClassModel
        {
            public void OnGet<T>()
            {
            }
        }

        [Fact]
        public void CreateHandlerMethods_IgnoresStaticMethods()
        {
            // Arrange
            var type = typeof(PageModelWithStaticHandler).GetTypeInfo();
            var expected = type.GetMethod(nameof(PageModelWithStaticHandler.OnGet), BindingFlags.Public | BindingFlags.Instance);

            // Act
            var results = DefaultPageLoader.CreateHandlerMethods(type);

            // Assert
            Assert.Collection(
                results,
                handler => Assert.Same(expected, handler.MethodInfo));
        }

        private class PageModelWithStaticHandler
        {
            public static void OnGet(string name)
            {
            }

            public void OnGet()
            {
            }
        }

        [Fact]
        public void CreateHandlerMethods_IgnoresAbstractMethods()
        {
            // Arrange
            var type = typeof(PageModelWithAbstractMethod).GetTypeInfo();
            var expected = type.GetMethod(nameof(PageModelWithAbstractMethod.OnGet), BindingFlags.Public | BindingFlags.Instance);

            // Act
            var results = DefaultPageLoader.CreateHandlerMethods(type);

            // Assert
            Assert.Collection(
                results,
                handler => Assert.Same(expected, handler.MethodInfo));
        }

        private abstract class PageModelWithAbstractMethod
        {
            public abstract void OnPost(string name);

            public void OnGet()
            {
            }
        }

        [Fact]
        public void CreateHandlerMethods_IgnoresMethodWithNonHandlerAttribute()
        {
            // Arrange
            var type = typeof(PageWithNonHandlerMethod).GetTypeInfo();
            var expected = type.GetMethod(nameof(PageWithNonHandlerMethod.OnGet), BindingFlags.Public | BindingFlags.Instance);

            // Act
            var results = DefaultPageLoader.CreateHandlerMethods(type);

            // Assert
            Assert.Collection(
                results,
                handler => Assert.Same(expected, handler.MethodInfo));
        }

        private class PageWithNonHandlerMethod
        {
            [NonHandler]
            public void OnPost(string name) { }

            public void OnGet()
            {
            }
        }

        // There are more tests for the parsing elsewhere, this is just testing that it's wired
        // up to the descriptor.
        [Fact]
        public void CreateHandlerMethods_ParsesMethod()
        {
            // Arrange
            var type = typeof(PageModelWithHandlerNames).GetTypeInfo();

            // Act
            var results = DefaultPageLoader.CreateHandlerMethods(type);

            // Assert
            Assert.Collection(
                results.OrderBy(h => h.MethodInfo.Name),
                handler =>
                {
                    Assert.Same(type.GetMethod(nameof(PageModelWithHandlerNames.OnPutDeleteAsync)), handler.MethodInfo);
                    Assert.Equal("Put", handler.HttpMethod);
                    Assert.Equal("Delete", handler.Name.ToString());
                });
        }

        private class PageModelWithHandlerNames
        {
            public void OnPutDeleteAsync()
            {
            }

            public void Foo() // This isn't a valid handler name.
            {
            }
        }

        [Fact]
        public void CreateHandlerMethods_AddsParameterDescriptors()
        {
            // Arrange
            var type = typeof(PageWithHandlerParameters).GetTypeInfo();
            var expected = type.GetMethod(nameof(PageWithHandlerParameters.OnPost), BindingFlags.Public | BindingFlags.Instance);

            // Act
            var results = DefaultPageLoader.CreateHandlerMethods(type);

            // Assert
            var handler = Assert.Single(results);

            Assert.Collection(
                handler.Parameters,
                p =>
                {
                    Assert.Equal(typeof(string), p.ParameterType);
                    Assert.NotNull(p.ParameterInfo);
                    Assert.Equal("name", p.Name);
                },
                p =>
                {
                    Assert.Equal(typeof(int), p.ParameterType);
                    Assert.NotNull(p.ParameterInfo);
                    Assert.Equal("id", p.Name);
                    Assert.Equal("personId", p.BindingInfo.BinderModelName);
                });
        }

        private class PageWithHandlerParameters
        {
            public void OnPost(string name, [ModelBinder(Name = "personId")] int id) { }
        }

        // We're using PropertyHelper from Common to find the properties here, which implements 
        // out standard set of semantics for properties that the framework interacts with.
        // 
        // One of the desirable consequences of that is we only find 'visible' properties. We're not 
        // retesting all of the details of PropertyHelper here, just the visibility part as a quick check 
        // that we're using PropertyHelper as expected.
        [Fact]
        public void CreateBoundProperties_UsesPropertyHelpers_ToFindProperties()
        {
            // Arrange
            var type = typeof(HidesAProperty).GetTypeInfo();

            // Act
            var results = DefaultPageLoader.CreateBoundProperties(type);

            // Assert
            Assert.Collection(
                results.OrderBy(p => p.Property.Name),
                p =>
                {
                    Assert.Equal(typeof(HidesAProperty).GetTypeInfo(), p.Property.DeclaringType.GetTypeInfo());
                });
        }

        private class HasAHiddenProperty
        {
            [BindProperty]
            public int Property { get; set; }
        }

        private class HidesAProperty : HasAHiddenProperty
        {
            [BindProperty]
            public new int Property { get; set; }
        }

        // We're using BindingInfo to make property binding opt-in here. We're not going to retest 
        // all of the semantics of BindingInfo here, as that's covered elsewhere.
        [Fact]
        public void CreateBoundProperties_UsesBindingInfo_ToFindProperties()
        {
            // Arrange
            var type = typeof(ModelWithBindingInfoProperty).GetTypeInfo();

            // Act
            var results = DefaultPageLoader.CreateBoundProperties(type);

            // Assert
            Assert.Collection(
                results.OrderBy(p => p.Property.Name),
                p =>
                {
                    Assert.Equal("Property", p.Property.Name);
                });
        }

        private class ModelWithBindingInfoProperty
        {
            [ModelBinder]
            public int Property { get; set; }

            public int IgnoreMe { get; set; }
        }

        // Additionally [BindProperty] on a property can opt-in a property
        [Fact]
        public void CreateBoundProperties_UsesBindPropertyAttribute_ToFindProperties()
        {
            // Arrange
            var type = typeof(ModelWithBindProperty).GetTypeInfo();

            // Act
            var results = DefaultPageLoader.CreateBoundProperties(type);

            // Assert
            Assert.Collection(
                results.OrderBy(p => p.Property.Name),
                p =>
                {
                    Assert.Equal("Property", p.Property.Name);
                });
        }

        private class ModelWithBindProperty
        {
            [BindProperty]
            public int Property { get; set; }

            public int IgnoreMe { get; set; }
        }

        // Additionally [BindProperty] on a property can opt-in a property
        [Fact]
        public void CreateBoundProperties_BindPropertyAttributeOnModel_OptsInAllProperties()
        {
            // Arrange
            var type = typeof(ModelWithBindPropertyOnClass).GetTypeInfo();

            // Act
            var results = DefaultPageLoader.CreateBoundProperties(type);

            // Assert
            Assert.Collection(
                results.OrderBy(p => p.Property.Name),
                p =>
                {
                    Assert.Equal("Property", p.Property.Name);
                });
        }

        [BindProperty]
        private class ModelWithBindPropertyOnClass : EmptyPageModel
        {
            public int Property { get; set; }
        }

        [Fact]
        public void CreateBoundProperties_SupportsGet_OnProperty()
        {
            // Arrange
            var type = typeof(ModelSupportsGetOnProperty).GetTypeInfo();

            // Act
            var results = DefaultPageLoader.CreateBoundProperties(type);

            // Assert
            Assert.Collection(
                results.OrderBy(p => p.Property.Name),
                p =>
                {
                    Assert.Equal("Property", p.Property.Name);
                    Assert.True(p.SupportsGet);
                });
        }

        private class ModelSupportsGetOnProperty
        {
            [BindProperty(SupportsGet = true)]
            public int Property { get; set; }

            public int IgnoreMe { get; set; }
        }
        
        [Fact]
        public void CreateBoundProperties_SupportsGet_OnClass()
        {
            // Arrange
            var type = typeof(ModelSupportsGetOnClass).GetTypeInfo();

            // Act
            var results = DefaultPageLoader.CreateBoundProperties(type);

            // Assert
            Assert.Collection(
                results.OrderBy(p => p.Property.Name),
                p =>
                {
                    Assert.Equal("Property", p.Property.Name);
                    Assert.True(p.SupportsGet);
                });
        }

        [BindProperty(SupportsGet = true)]
        private class ModelSupportsGetOnClass : EmptyPageModel
        {
            public int Property { get; set; }
        }

        [Fact]
        public void CreateBoundProperties_SupportsGet_Override()
        {
            // Arrange
            var type = typeof(ModelSupportsGetOverride).GetTypeInfo();

            // Act
            var results = DefaultPageLoader.CreateBoundProperties(type);

            // Assert
            Assert.Collection(
                results.OrderBy(p => p.Property.Name),
                p =>
                {
                    Assert.Equal("Property", p.Property.Name);
                    Assert.False(p.SupportsGet);
                });
        }

        [BindProperty(SupportsGet = true)]
        private class ModelSupportsGetOverride : EmptyPageModel
        {
            [BindProperty(SupportsGet = false)]
            public int Property { get; set; }
        }

        [Theory]
        [InlineData("Foo")]
        [InlineData("On")]
        [InlineData("OnAsync")]
        [InlineData("Async")]
        public void TryParseHandler_ParsesHandlerNames_InvalidData(string methodName)
        {
            // Arrange

            // Act
            var result = DefaultPageLoader.TryParseHandlerMethod(methodName, out var httpMethod, out var handler);

            // Assert
            Assert.False(result);
            Assert.Null(httpMethod);
            Assert.Null(handler);
        }

        [Theory]
        [InlineData("OnG", "G", null)]
        [InlineData("OnGAsync", "G", null)]
        [InlineData("OnPOST", "P", "OST")]
        [InlineData("OnPOSTAsync", "P", "OST")]
        [InlineData("OnDeleteFoo", "Delete", "Foo")]
        [InlineData("OnDeleteFooAsync", "Delete", "Foo")]
        [InlineData("OnMadeupLongHandlerName", "Madeup", "LongHandlerName")]
        [InlineData("OnMadeupLongHandlerNameAsync", "Madeup", "LongHandlerName")]
        public void TryParseHandler_ParsesHandlerNames_ValidData(string methodName, string expectedHttpMethod, string expectedHandler)
        {
            // Arrange

            // Act
            var result = DefaultPageLoader.TryParseHandlerMethod(methodName, out var httpMethod, out var handler);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedHttpMethod, httpMethod);
            Assert.Equal(expectedHandler, handler);
        }
    }
}
