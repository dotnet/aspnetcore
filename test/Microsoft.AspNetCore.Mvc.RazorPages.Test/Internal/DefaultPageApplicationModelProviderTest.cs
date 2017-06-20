// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public partial class DefaultPageApplicationModelProviderTest
    {
        [Fact]
        public void OnProvidersExecuting_SetsPageAsHandlerType_IfModelPropertyDoesNotExist()
        {
            // Arrange
            var provider = new TestPageApplicationModelProvider();
            var typeInfo = typeof(TestPage).GetTypeInfo();
            var descriptor = new PageActionDescriptor();
            var context = new PageApplicationModelProviderContext(descriptor, typeInfo);

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.NotNull(context.PageApplicationModel);
            Assert.Same(context.PageApplicationModel.PageType, context.PageApplicationModel.HandlerType);
        }

        [Fact]
        public void OnProvidersExecuting_SetsPageAsHandlerType_IfModelTypeDoesNotHaveAnyHandlers()
        {
            // Arrange
            var provider = new TestPageApplicationModelProvider();
            var typeInfo = typeof(PageWithModelWithoutHandlers).GetTypeInfo();
            var descriptor = new PageActionDescriptor();
            var context = new PageApplicationModelProviderContext(descriptor, typeInfo);

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.NotNull(context.PageApplicationModel);
            Assert.Same(context.PageApplicationModel.PageType, context.PageApplicationModel.HandlerType);
        }

        [Fact]
        public void OnProvidersExecuting_SetsModelAsHandlerType()
        {
            // Arrange
            var provider = new TestPageApplicationModelProvider();
            var typeInfo = typeof(PageWithModel).GetTypeInfo();
            var descriptor = new PageActionDescriptor();
            var context = new PageApplicationModelProviderContext(descriptor, typeInfo);

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.NotNull(context.PageApplicationModel);
            Assert.Same(typeof(TestPageModel).GetTypeInfo(), context.PageApplicationModel.HandlerType);
        }

        [Fact]
        public void OnProvidersExecuting_DiscoversPropertiesFromPage()
        {
            // Arrange
            var provider = new TestPageApplicationModelProvider();
            var typeInfo = typeof(TestPage).GetTypeInfo();
            var descriptor = new PageActionDescriptor();
            var context = new PageApplicationModelProviderContext(descriptor, typeInfo);

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.NotNull(context.PageApplicationModel);
            Assert.Collection(
                context.PageApplicationModel.HandlerProperties.OrderBy(p => p.PropertyName),
                property =>
                {
                    Assert.Equal(typeInfo.GetProperty(nameof(TestPage.Property1)), property.PropertyInfo);
                    Assert.Null(property.BindingInfo);
                    Assert.Equal(nameof(TestPage.Property1), property.PropertyName);
                },
                property =>
                {
                    Assert.Equal(typeInfo.GetProperty(nameof(TestPage.Property2)), property.PropertyInfo);
                    Assert.Equal(nameof(TestPage.Property2), property.PropertyName);
                    Assert.NotNull(property.BindingInfo);
                    Assert.Equal(BindingSource.Path, property.BindingInfo.BindingSource);
                });
        }

        [Fact]
        public void OnProvidersExecuting_DiscoversHandlersFromPage()
        {
            // Arrange
            var provider = new TestPageApplicationModelProvider();
            var typeInfo = typeof(PageWithModelWithoutHandlers).GetTypeInfo();
            var descriptor = new PageActionDescriptor();
            var context = new PageApplicationModelProviderContext(descriptor, typeInfo);

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.NotNull(context.PageApplicationModel);
            Assert.Collection(
                context.PageApplicationModel.HandlerMethods.OrderBy(p => p.Name),
                handler =>
                {
                    var name = nameof(PageWithModelWithoutHandlers.OnGet);
                    Assert.Equal(typeInfo.GetMethod(name), handler.MethodInfo);
                    Assert.Equal(name, handler.Name);
                    Assert.Equal("Get", handler.HttpMethod);
                    Assert.Null(handler.HandlerName);
                },
                handler =>
                {
                    var name = nameof(PageWithModelWithoutHandlers.OnPostAsync);
                    Assert.Equal(typeInfo.GetMethod(name), handler.MethodInfo);
                    Assert.Equal(name, handler.Name);
                    Assert.Equal("Post", handler.HttpMethod);
                    Assert.Null(handler.HandlerName);
                },
                handler =>
                {
                    var name = nameof(PageWithModelWithoutHandlers.OnPostDeleteCustomerAsync);
                    Assert.Equal(typeInfo.GetMethod(name), handler.MethodInfo);
                    Assert.Equal(name, handler.Name);
                    Assert.Equal("Post", handler.HttpMethod);
                    Assert.Equal("DeleteCustomer", handler.HandlerName);
                });
        }

        [Fact]
        public void OnProvidersExecuting_DiscoversPropertiesFromModel()
        {
            // Arrange
            var provider = new TestPageApplicationModelProvider();
            var typeInfo = typeof(PageWithModel).GetTypeInfo();
            var modelType = typeof(TestPageModel);
            var descriptor = new PageActionDescriptor();
            var context = new PageApplicationModelProviderContext(descriptor, typeInfo);

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.NotNull(context.PageApplicationModel);
            Assert.Collection(
                context.PageApplicationModel.HandlerProperties.OrderBy(p => p.PropertyName),
                property =>
                {
                    var name = nameof(TestPageModel.Property1);
                    Assert.Equal(modelType.GetProperty(name), property.PropertyInfo);
                    Assert.Null(property.BindingInfo);
                    Assert.Equal(name, property.PropertyName);
                },
                property =>
                {
                    var name = nameof(TestPageModel.Property2);
                    Assert.Equal(modelType.GetProperty(name), property.PropertyInfo);
                    Assert.Equal(name, property.PropertyName);
                    Assert.NotNull(property.BindingInfo);
                    Assert.Equal(BindingSource.Query, property.BindingInfo.BindingSource);
                });
        }

        [Fact]
        public void OnProvidersExecuting_DiscoversHandlersFromModel()
        {
            // Arrange
            var provider = new TestPageApplicationModelProvider();
            var typeInfo = typeof(PageWithModel).GetTypeInfo();
            var modelType = typeof(TestPageModel);
            var descriptor = new PageActionDescriptor();
            var context = new PageApplicationModelProviderContext(descriptor, typeInfo);

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.NotNull(context.PageApplicationModel);
            Assert.Collection(
                context.PageApplicationModel.HandlerMethods.OrderBy(p => p.Name),
                handler =>
                {
                    var name = nameof(TestPageModel.OnGetUser);
                    Assert.Equal(modelType.GetMethod(name), handler.MethodInfo);
                    Assert.Equal(name, handler.Name);
                    Assert.Equal("Get", handler.HttpMethod);
                    Assert.Equal("User", handler.HandlerName);
                });
        }

        // We want to test the the 'empty' page has no bound properties, and no handler methods.
        [Fact]
        public void OnProvidersExecuting_EmptyPage()
        {
            // Arrange
            var provider = new TestPageApplicationModelProvider();
            var typeInfo = typeof(EmptyPage).GetTypeInfo();
            var context = new PageApplicationModelProviderContext(new PageActionDescriptor(), typeInfo);

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var pageModel = context.PageApplicationModel;
            Assert.Empty(pageModel.HandlerProperties.Where(p => p.BindingInfo != null));
            Assert.Empty(pageModel.HandlerMethods);
            Assert.Same(typeof(EmptyPage).GetTypeInfo(), pageModel.HandlerType);
            Assert.Same(typeof(EmptyPage).GetTypeInfo(), pageModel.ModelType);
            Assert.Same(typeof(EmptyPage).GetTypeInfo(), pageModel.PageType);
        }

        // We want to test the the 'empty' page and pagemodel has no bound properties, and no handler methods.
        [Fact]
        public void OnProvidersExecuting_EmptyPageModel()
        {
            // Arrange
            var provider = new TestPageApplicationModelProvider();
            var typeInfo = typeof(EmptyPageWithPageModel).GetTypeInfo();
            var context = new PageApplicationModelProviderContext(new PageActionDescriptor(), typeInfo);

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var pageModel = context.PageApplicationModel;
            Assert.Empty(pageModel.HandlerProperties.Where(p => p.BindingInfo != null));
            Assert.Empty(pageModel.HandlerMethods);
            Assert.Same(typeof(EmptyPageWithPageModel).GetTypeInfo(), pageModel.HandlerType);
            Assert.Same(typeof(EmptyPageModel).GetTypeInfo(), pageModel.ModelType);
            Assert.Same(typeof(EmptyPageWithPageModel).GetTypeInfo(), pageModel.PageType);
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
            var provider = new TestPageApplicationModelProvider();
            var typeInfo = typeof(PageWithHandlerThatGetsIgnored).GetTypeInfo();
            var modelType = typeof(ModelWithHandler);
            var context = new PageApplicationModelProviderContext(new PageActionDescriptor(), typeInfo);

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var pageModel = context.PageApplicationModel;
            Assert.Collection(
                pageModel.HandlerProperties,
                p => Assert.Equal(modelType.GetProperty(nameof(ModelWithHandler.BindMe)), p.PropertyInfo));

            Assert.Collection(
                pageModel.HandlerMethods,
                p => Assert.Equal(modelType.GetMethod(nameof(ModelWithHandler.OnGet)), p.MethodInfo));

            Assert.Same(typeof(ModelWithHandler).GetTypeInfo(), pageModel.HandlerType);
            Assert.Same(typeof(ModelWithHandler).GetTypeInfo(), pageModel.ModelType);
            Assert.Same(typeof(PageWithHandlerThatGetsIgnored).GetTypeInfo(), pageModel.PageType);
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
        public void OnProvidersExecuting_FindsHandlerMethodOnPage_WhenModelHasNoHandlers()
        {
            // Arrange
            var provider = new TestPageApplicationModelProvider();
            var typeInfo = typeof(PageWithHandler).GetTypeInfo();
            var context = new PageApplicationModelProviderContext(new PageActionDescriptor(), typeInfo);

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var pageModel = context.PageApplicationModel;
            Assert.Collection(
                pageModel.HandlerProperties.OrderBy(p => p.PropertyName),
                p => Assert.Equal(typeInfo.GetProperty(nameof(PageWithHandler.BindMe)), p.PropertyInfo),
                p => Assert.Equal(typeInfo.GetProperty(nameof(PageWithHandler.Model)), p.PropertyInfo));

            Assert.Collection(
                pageModel.HandlerMethods,
                p => Assert.Equal(typeInfo.GetMethod(nameof(PageWithHandler.OnGet)), p.MethodInfo));

            Assert.Same(typeof(PageWithHandler).GetTypeInfo(), pageModel.HandlerType);
            Assert.Same(typeof(PocoModel).GetTypeInfo(), pageModel.ModelType);
            Assert.Same(typeof(PageWithHandler).GetTypeInfo(), pageModel.PageType);
        }

        private class PageWithHandler
        {
            public PocoModel Model => null;

            [ModelBinder]
            public int BindMe { get; set; }

            public void OnGet() { }
        }

        [Fact]
        public void CreateHandlerModels_DiscoversHandlersFromBaseType()
        {
            // Arrange
            var provider = new TestPageApplicationModelProvider();
            var typeInfo = typeof(InheritsMethods).GetTypeInfo();
            var baseType = typeof(TestSetPageModel);

            // Act
            var handlerModels = provider.CreateHandlerModels(typeInfo);

            // Assert
            Assert.Collection(
                handlerModels.OrderBy(h => h.MethodInfo.DeclaringType.Name).ThenBy(h => h.MethodInfo.Name),
                handler =>
                {
                    Assert.Equal(nameof(InheritsMethods.OnGet), handler.MethodInfo.Name);
                    Assert.Equal(typeInfo, handler.MethodInfo.DeclaringType.GetTypeInfo());
                },
                handler =>
                {
                    Assert.Equal(nameof(TestSetPageModel.OnGet), handler.MethodInfo.Name);
                    Assert.Equal(baseType, handler.MethodInfo.DeclaringType);
                },
                handler =>
                {
                    Assert.Equal(nameof(TestSetPageModel.OnPost), handler.MethodInfo.Name);
                    Assert.Equal(baseType, handler.MethodInfo.DeclaringType);
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

        private class InheritsMethods : TestSetPageModel
        {
            public new void OnGet()
            {
            }
        }

        [Fact]
        public void CreateHandlerModels_IgnoresNonPublicMethods()
        {
            // Arrange
            var provider = new TestPageApplicationModelProvider();
            var typeInfo = typeof(ProtectedModel).GetTypeInfo();
            var baseType = typeof(TestSetPageModel);

            // Act
            var handlerModels = provider.CreateHandlerModels(typeInfo);

            // Assert
            Assert.Empty(handlerModels);
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
        public void CreateHandlerModels_IgnoreGenericTypeParameters()
        {
            // Arrange
            var provider = new TestPageApplicationModelProvider();
            var typeInfo = typeof(GenericClassModel).GetTypeInfo();

            // Act
            var handlerModels = provider.CreateHandlerModels(typeInfo);

            // Assert
            Assert.Empty(handlerModels);
        }

        private class GenericClassModel
        {
            public void OnGet<T>()
            {
            }
        }

        [Fact]
        public void CreateHandlerModels_IgnoresStaticMethods()
        {
            // Arrange
            var provider = new TestPageApplicationModelProvider();
            var typeInfo = typeof(PageModelWithStaticHandler).GetTypeInfo();
            var expected = typeInfo.GetMethod(nameof(PageModelWithStaticHandler.OnGet), BindingFlags.Public | BindingFlags.Instance);

            // Act
            var handlerModels = provider.CreateHandlerModels(typeInfo);

            // Assert
            Assert.Collection(
                handlerModels,
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
        public void CreateHandlerModels_IgnoresAbstractMethods()
        {
            // Arrange
            var provider = new TestPageApplicationModelProvider();
            var typeInfo = typeof(PageModelWithAbstractMethod).GetTypeInfo();
            var expected = typeInfo.GetMethod(nameof(PageModelWithAbstractMethod.OnGet), BindingFlags.Public | BindingFlags.Instance);

            // Act
            var handlerModels = provider.CreateHandlerModels(typeInfo);
            
            // Assert
            Assert.Collection(
                handlerModels,
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
        public void CreateHandlerModels_IgnoresMethodWithNonHandlerAttribute()
        {
            // Arrange
            var provider = new TestPageApplicationModelProvider();
            var typeInfo = typeof(PageWithNonHandlerMethod).GetTypeInfo();
            var expected = typeInfo.GetMethod(nameof(PageWithNonHandlerMethod.OnGet), BindingFlags.Public | BindingFlags.Instance);

            // Act
            var handlerModels = provider.CreateHandlerModels(typeInfo);

            // Assert
            Assert.Collection(
                handlerModels,
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
        // up to the model.
        [Fact]
        public void CreateHandlerModel_ParsesMethod()
        {
            // Arrange
            var provider = new TestPageApplicationModelProvider();
            var typeInfo = typeof(PageModelWithHandlerNames).GetTypeInfo();

            // Act
            var handlerModels = provider.CreateHandlerModels(typeInfo);

            // Assert
            Assert.Collection(
                handlerModels.OrderBy(h => h.MethodInfo.Name),
                handler =>
                {
                    Assert.Same(typeInfo.GetMethod(nameof(PageModelWithHandlerNames.OnPutDeleteAsync)), handler.MethodInfo);
                    Assert.Equal("Put", handler.HttpMethod);
                    Assert.Equal("Delete", handler.HandlerName);
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
            var provider = new TestPageApplicationModelProvider();
            var typeInfo = typeof(PageWithHandlerParameters).GetTypeInfo();
            var expected = typeInfo.GetMethod(nameof(PageWithHandlerParameters.OnPost));

            // Act
            var handlerModels = provider.CreateHandlerModels(typeInfo);

            // Assert
            var handler = Assert.Single(handlerModels);

            Assert.Collection(
                handler.Parameters,
                p =>
                {
                    Assert.NotNull(p.ParameterInfo);
                    Assert.Equal(typeof(string), p.ParameterInfo.ParameterType);
                    Assert.Equal("name", p.ParameterName);
                },
                p =>
                {
                    Assert.NotNull(p.ParameterInfo);
                    Assert.Equal(typeof(int), p.ParameterInfo.ParameterType);
                    Assert.Equal("id", p.ParameterName);
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
        public void PopulateHandlerProperties_UsesPropertyHelpers_ToFindProperties()
        {
            // Arrange
            var provider = new TestPageApplicationModelProvider();
            var typeInfo = typeof(HidesAProperty).GetTypeInfo();
            var pageModel = new PageApplicationModel(new PageActionDescriptor(), typeInfo, new object[0]);

            // Act
            provider.PopulateHandlerProperties(pageModel);

            // Assert
            var properties = pageModel.HandlerProperties;
            Assert.Collection(
                properties,
                p =>
                {
                    Assert.Equal(typeof(HidesAProperty).GetTypeInfo(), p.PropertyInfo.DeclaringType.GetTypeInfo());
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

        [Fact]
        public void PopulateHandlerProperties_SupportsGet_OnProperty()
        {
            // Arrange
            var provider = new TestPageApplicationModelProvider();
            var typeInfo = typeof(ModelSupportsGetOnProperty).GetTypeInfo();
            var pageModel = new PageApplicationModel(new PageActionDescriptor(), typeInfo, new object[0]);

            // Act
            provider.PopulateHandlerProperties(pageModel);

            // Assert
            var properties = pageModel.HandlerProperties;
            Assert.Collection(
                properties.OrderBy(p => p.PropertyName),
                p =>
                {
                    Assert.Equal(typeInfo.GetProperty(nameof(ModelSupportsGetOnProperty.Property)), p.PropertyInfo);
                    Assert.NotNull(p.BindingInfo.RequestPredicate);
                    Assert.True(p.BindingInfo.RequestPredicate(new ActionContext
                    {
                        HttpContext = new DefaultHttpContext
                        {
                            Request =
                            {
                                Method ="GET",
                            }
                        }
                    }));
                });
        }

        private class ModelSupportsGetOnProperty
        {
            [BindProperty(SupportsGet = true)]
            public int Property { get; set; }
        }

        [Theory]
        [InlineData("Foo")]
        [InlineData("On")]
        [InlineData("OnAsync")]
        [InlineData("Async")]
        public void TryParseHandler_ParsesHandlerNames_InvalidData(string methodName)
        {
            // Act
            var result = DefaultPageApplicationModelProvider.TryParseHandlerMethod(methodName, out var httpMethod, out var handler);

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
            var result = DefaultPageApplicationModelProvider.TryParseHandlerMethod(methodName, out var httpMethod, out var handler);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedHttpMethod, httpMethod);
            Assert.Equal(expectedHandler, handler);
        }

        private class TestPageApplicationModelProvider : DefaultPageApplicationModelProvider
        {
            public TestPageApplicationModelProvider(IOptions<MvcOptions> mvcOptions = null) 
                : base(mvcOptions : new TestOptionsManager<MvcOptions>())
            {
            }
        }

        private class TestPage
        {
            public string Property1 { get; set; }

            [FromRoute]
            public object Property2 { get; set; }
        }

        private class PageWithModelWithoutHandlers : Page
        {
            public ModelWithoutHandler Model { get; }

            public override Task ExecuteAsync() => throw new NotImplementedException();

            public void OnGet() { }

            public void OnPostAsync() { }

            public void OnPostDeleteCustomerAsync() { }

            public class ModelWithoutHandler
            {
            }
        }

        private class PageWithModel : Page
        {
            public TestPageModel Model { get; }

            public override Task ExecuteAsync() => throw new NotImplementedException();
        }

        private class TestPageModel
        {
            public string Property1 { get; set; }

            [FromQuery]
            public string Property2 { get; set; }

            public void OnGetUser() { }
        }
    }
}
