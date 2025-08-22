// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

public class DefaultPageApplicationModelProviderTest
{
    [Fact]
    public void OnProvidersExecuting_ThrowsIfPageDoesNotDeriveFromValidBaseType()
    {
        // Arrange
        var provider = CreateProvider();
        var typeInfo = typeof(InvalidPageWithWrongBaseClass).GetTypeInfo();
        var descriptor = new PageActionDescriptor();
        var context = new PageApplicationModelProviderContext(descriptor, typeInfo);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => provider.OnProvidersExecuting(context));

        // Assert
        Assert.Equal(
             $"The type '{typeInfo.FullName}' is not a valid page. A page must inherit from '{typeof(PageBase).FullName}'.",
             ex.Message);
    }

    private class InvalidPageWithWrongBaseClass : RazorPageBase
    {
        public override void BeginContext(int position, int length, bool isLiteral)
        {
            throw new NotImplementedException();
        }

        public override void EndContext()
        {
            throw new NotImplementedException();
        }

        public override void EnsureRenderedBodyOrSections()
        {
            throw new NotImplementedException();
        }

        public override Task ExecuteAsync()
        {
            throw new NotImplementedException();
        }
    }

    [Fact]
    public void OnProvidersExecuting_ThrowsIfModelPropertyDoesNotExistOnPage()
    {
        // Arrange
        var provider = CreateProvider();
        var typeInfo = typeof(PageWithoutModelProperty).GetTypeInfo();
        var descriptor = new PageActionDescriptor();
        var context = new PageApplicationModelProviderContext(descriptor, typeInfo);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => provider.OnProvidersExecuting(context));

        // Assert
        Assert.Equal(
            $"The type '{typeInfo.FullName}' is not a valid page. A page must define a public, non-static 'Model' property.",
            ex.Message);
    }

    private class PageWithoutModelProperty : PageBase
    {
        public override Task ExecuteAsync() => throw new NotImplementedException();
    }

    [Fact]
    public void OnProvidersExecuting_ThrowsIfModelPropertyIsNotPublic()
    {
        // Arrange
        var provider = CreateProvider();
        var typeInfo = typeof(PageWithNonVisibleModel).GetTypeInfo();
        var descriptor = new PageActionDescriptor();
        var context = new PageApplicationModelProviderContext(descriptor, typeInfo);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => provider.OnProvidersExecuting(context));

        // Assert
        Assert.Equal(
            $"The type '{typeInfo.FullName}' is not a valid page. A page must define a public, non-static 'Model' property.",
            ex.Message);
    }

    private class PageWithNonVisibleModel : PageBase
    {
        private object Model => null;

        public override Task ExecuteAsync() => throw new NotImplementedException();
    }

    [Fact]
    public void OnProvidersExecuting_ThrowsIfModelPropertyIsStatic()
    {
        // Arrange
        var provider = CreateProvider();
        var typeInfo = typeof(PageWithStaticModel).GetTypeInfo();
        var descriptor = new PageActionDescriptor();
        var context = new PageApplicationModelProviderContext(descriptor, typeInfo);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => provider.OnProvidersExecuting(context));

        // Assert
        Assert.Equal(
            $"The type '{typeInfo.FullName}' is not a valid page. A page must define a public, non-static 'Model' property.",
            ex.Message);
    }

    private class PageWithStaticModel : PageBase
    {
        public static object Model => null;

        public override Task ExecuteAsync() => throw new NotImplementedException();
    }

    [Fact]
    public void OnProvidersExecuting_DiscoversPropertiesFromPage_IfModelTypeDoesNotHaveAttribute()
    {
        // Arrange
        var provider = CreateProvider();
        var typeInfo = typeof(PageWithModelWithoutPageModelAttribute).GetTypeInfo();
        var descriptor = new PageActionDescriptor();
        var context = new PageApplicationModelProviderContext(descriptor, typeInfo);

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        Assert.NotNull(context.PageApplicationModel);
        var propertiesOnPage = context.PageApplicationModel.HandlerProperties
            .Where(p => p.PropertyInfo.DeclaringType.GetTypeInfo() == typeInfo);
        Assert.Collection(
            propertiesOnPage.OrderBy(p => p.PropertyName),
            property =>
            {
                Assert.Equal(typeInfo.GetProperty(nameof(PageWithModelWithoutPageModelAttribute.Model)), property.PropertyInfo);
                Assert.Equal(nameof(PageWithModelWithoutPageModelAttribute.Model), property.PropertyName);
            },
            property =>
            {
                Assert.Equal(typeInfo.GetProperty(nameof(PageWithModelWithoutPageModelAttribute.Property1)), property.PropertyInfo);
                Assert.Null(property.BindingInfo);
                Assert.Equal(nameof(PageWithModelWithoutPageModelAttribute.Property1), property.PropertyName);
            },
            property =>
            {
                Assert.Equal(typeInfo.GetProperty(nameof(PageWithModelWithoutPageModelAttribute.Property2)), property.PropertyInfo);
                Assert.Equal(nameof(PageWithModelWithoutPageModelAttribute.Property2), property.PropertyName);
                Assert.NotNull(property.BindingInfo);
                Assert.Equal(BindingSource.Path, property.BindingInfo.BindingSource);
            });
    }

    private class PageWithModelWithoutPageModelAttribute : Page
    {
        public string Property1 { get; set; }

        [FromRoute]
        public object Property2 { get; set; }

        public ModelWithoutPageModelAttribute Model => null;

        public override Task ExecuteAsync() => throw new NotImplementedException();
    }

    private class ModelWithoutPageModelAttribute
    {
    }

    [Fact]
    public void OnProvidersExecuting_DiscoversPropertiesFromPageModel_IfModelHasAttribute()
    {
        // Arrange
        var provider = CreateProvider();
        var typeInfo = typeof(PageWithModelWithPageModelAttribute).GetTypeInfo();
        var modelType = typeof(ModelWithPageModelAttribute);
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
                Assert.Equal(modelType.GetProperty(nameof(ModelWithPageModelAttribute.Property)), property.PropertyInfo);
                Assert.Equal(nameof(ModelWithPageModelAttribute.Property), property.PropertyName);
                Assert.NotNull(property.BindingInfo);
                Assert.Equal(BindingSource.Path, property.BindingInfo.BindingSource);
            });
    }

    private class PageWithModelWithPageModelAttribute : Page
    {
        public string Property1 { get; set; }

        [FromRoute]
        public object Property2 { get; set; }

        public ModelWithPageModelAttribute Model => null;
        public override Task ExecuteAsync() => throw new NotImplementedException();
    }

    [PageModel]
    private class ModelWithPageModelAttribute
    {
        [FromRoute]
        public string Property { get; set; }
    }

    [Fact]
    public void OnProvidersExecuting_DiscoversProperties_FromAllSubTypesThatDeclaresBindProperty()
    {
        // Arrange
        var provider = CreateProvider();
        var typeInfo = typeof(BindPropertyAttributeOnBaseModelPage).GetTypeInfo();
        var descriptor = new PageActionDescriptor();
        var context = new PageApplicationModelProviderContext(descriptor, typeInfo);

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        Assert.NotNull(context.PageApplicationModel);
        Assert.Collection(
            context.PageApplicationModel.HandlerProperties.OrderBy(p => p.PropertyName).Where(p => p.BindingInfo != null),
            property =>
            {
                var name = nameof(ModelLevel3.Property2);
                Assert.Equal(typeof(ModelLevel3).GetProperty(name), property.PropertyInfo);
                Assert.Equal(name, property.PropertyName);
                Assert.NotNull(property.BindingInfo);
            },
            property =>
            {
                var name = nameof(ModelLevel3.Property3);
                Assert.Equal(typeof(ModelLevel3).GetProperty(name), property.PropertyInfo);
                Assert.Equal(name, property.PropertyName);
                Assert.NotNull(property.BindingInfo);
            });
    }

    private class BindPropertyAttributeOnBaseModelPage : Page
    {
        public ModelLevel3 Model => null;
        public override Task ExecuteAsync() => throw new NotImplementedException();
    }

    private class ModelLevel1 : PageModel
    {
        public string Property1 { get; set; }
    }

    [BindProperties]
    private class ModelLevel2 : ModelLevel1
    {
        public string Property2 { get; set; }
    }

    private class ModelLevel3 : ModelLevel2
    {
        public string Property3 { get; set; }
    }

    [Fact]
    public void OnProvidersExecuting_DiscoversHandlersFromPage()
    {
        // Arrange
        var provider = CreateProvider();
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
        var provider = CreateProvider();
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
            },
            property =>
            {
                var name = nameof(TestPageModel.TestService);
                Assert.Equal(modelType.GetProperty(name), property.PropertyInfo);
                Assert.Equal(name, property.PropertyName);
                Assert.NotNull(property.BindingInfo);
                Assert.Equal(BindingSource.Services, property.BindingInfo.BindingSource);
            });
    }

    [Fact]
    public void OnProvidersExecuting_DiscoversBindingInfoFromHandler()
    {
        // Arrange
        var provider = CreateProvider();
        var typeInfo = typeof(PageWithBindPropertyModel).GetTypeInfo();
        var modelType = typeof(ModelWithBindProperty);
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
                Assert.Equal(nameof(ModelWithBindProperty.Property1), property.PropertyName);
                Assert.NotNull(property.BindingInfo);
            },
            property =>
            {
                Assert.Equal(nameof(ModelWithBindProperty.Property2), property.PropertyName);
                Assert.NotNull(property.BindingInfo);
                Assert.Equal(BindingSource.Path, property.BindingInfo.BindingSource);
            });
    }

    private class PageWithBindPropertyModel : PageBase
    {
        public ModelWithBindProperty Model => null;

        public override Task ExecuteAsync() => null;
    }

    [BindProperties]
    [PageModel]
    private class ModelWithBindProperty
    {
        public string Property1 { get; set; }

        [FromRoute]
        public string Property2 { get; set; }
    }

    [Fact]
    public void OnProvidersExecuting_DiscoversHandlersFromModel()
    {
        // Arrange
        var provider = CreateProvider();
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

    // We want to test the 'empty' page has no bound properties, and no handler methods.
    [Fact]
    public void OnProvidersExecuting_EmptyPage()
    {
        // Arrange
        var provider = CreateProvider();
        var typeInfo = typeof(EmptyPage).GetTypeInfo();
        var context = new PageApplicationModelProviderContext(new PageActionDescriptor(), typeInfo);

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        var pageModel = context.PageApplicationModel;
        Assert.DoesNotContain(pageModel.HandlerProperties, p => p.BindingInfo != null);
        Assert.Empty(pageModel.HandlerMethods);
        Assert.Same(typeof(EmptyPage).GetTypeInfo(), pageModel.HandlerType);
        Assert.Same(typeof(EmptyPage).GetTypeInfo(), pageModel.ModelType);
        Assert.Same(typeof(EmptyPage).GetTypeInfo(), pageModel.PageType);
    }

    // We want to test the 'empty' page and PageModel has no bound properties, and no handler methods.
    [Fact]
    public void OnProvidersExecuting_EmptyPageModel()
    {
        // Arrange
        var provider = CreateProvider();
        var typeInfo = typeof(EmptyPageWithPageModel).GetTypeInfo();
        var context = new PageApplicationModelProviderContext(new PageActionDescriptor(), typeInfo);

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        var pageModel = context.PageApplicationModel;
        Assert.DoesNotContain(pageModel.HandlerProperties, p => p.BindingInfo != null);
        Assert.Empty(pageModel.HandlerMethods);
        Assert.Same(typeof(EmptyPageModel).GetTypeInfo(), pageModel.DeclaredModelType);
        Assert.Same(typeof(EmptyPageModel).GetTypeInfo(), pageModel.ModelType);
        Assert.Same(typeof(EmptyPageModel).GetTypeInfo(), pageModel.HandlerType);
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

    [Fact]
    public void OnProvidersExecuting_CombinesFilters_OnPageAndPageModel()
    {
        // Arrange
        var provider = CreateProvider();
        var typeInfo = typeof(PageWithFilters).GetTypeInfo();
        var context = new PageApplicationModelProviderContext(new PageActionDescriptor(), typeInfo);

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        var pageModel = context.PageApplicationModel;
        Assert.Collection(
            pageModel.Filters,
            filter => Assert.IsType<TypeFilterAttribute>(filter),
            filter => Assert.IsType<ServiceFilterAttribute>(filter),
            filter => Assert.IsType<PageHandlerPageFilter>(filter),
            filter => Assert.IsType<HandleOptionsRequestsPageFilter>(filter));
    }

    [ServiceFilter(typeof(Guid))]
    private class PageWithFilters : Page
    {
        public PageWithFilterModel Model { get; }

        public override Task ExecuteAsync() => throw new NotImplementedException();
    }

    [TypeFilter(typeof(string))]
    private class PageWithFilterModel : PageModel
    {
    }

    [ServiceFilter(typeof(IServiceProvider))]
    private class FiltersOnPageAndPageModel : PageModel { }

    [Fact] // If the model has handler methods, we prefer those.
    public void CreateDescriptor_FindsHandlerMethod_OnModel()
    {
        // Arrange
        var provider = CreateProvider();
        var typeInfo = typeof(PageWithHandlerThatGetsIgnored).GetTypeInfo();
        var modelType = typeof(ModelWithHandler);
        var context = new PageApplicationModelProviderContext(new PageActionDescriptor(), typeInfo);

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        var pageModel = context.PageApplicationModel;
        Assert.Contains(
            pageModel.HandlerProperties,
            p => p.PropertyInfo == modelType.GetProperty(nameof(ModelWithHandler.BindMe)));

        Assert.Collection(
            pageModel.HandlerMethods,
            p => Assert.Equal(modelType.GetMethod(nameof(ModelWithHandler.OnGet)), p.MethodInfo));

        Assert.Same(typeof(ModelWithHandler).GetTypeInfo(), pageModel.HandlerType);
        Assert.Same(typeof(ModelWithHandler).GetTypeInfo(), pageModel.ModelType);
        Assert.Same(typeof(PageWithHandlerThatGetsIgnored).GetTypeInfo(), pageModel.PageType);
    }

    private class ModelWithHandler : PageModel
    {
        [ModelBinder]
        public int BindMe { get; set; }

        public void OnGet() { }
    }

    private class PageWithHandlerThatGetsIgnored : Page
    {
        public ModelWithHandler Model => null;

        [ModelBinder]
        public int IgnoreMe { get; set; }

        public void OnPost() { }

        public override Task ExecuteAsync() => throw new NotImplementedException();
    }

    [Fact] // If the model does not have the PageModelAttribute, we look at the page instead.
    public void OnProvidersExecuting_FindsHandlerMethodOnPage_WhenModelIsNotAnnotatedWithPageModelAttribute()
    {
        // Arrange
        var provider = CreateProvider();
        var typeInfo = typeof(PageWithHandler).GetTypeInfo();
        var context = new PageApplicationModelProviderContext(new PageActionDescriptor(), typeInfo);

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        var pageModel = context.PageApplicationModel;
        var propertiesOnPage = pageModel.HandlerProperties
            .Where(p => p.PropertyInfo.DeclaringType.GetTypeInfo() == typeInfo);
        Assert.Collection(
            propertiesOnPage.OrderBy(p => p.PropertyName),
            p => Assert.Equal(typeInfo.GetProperty(nameof(PageWithHandler.BindMe)), p.PropertyInfo),
            p => Assert.Equal(typeInfo.GetProperty(nameof(PageWithHandler.Model)), p.PropertyInfo));

        Assert.Collection(
            pageModel.HandlerMethods,
            p => Assert.Equal(typeInfo.GetMethod(nameof(PageWithHandler.OnGet)), p.MethodInfo));

        Assert.Same(typeof(PageWithHandler).GetTypeInfo(), pageModel.HandlerType);
        Assert.Same(typeof(PocoModel).GetTypeInfo(), pageModel.ModelType);
        Assert.Same(typeof(PageWithHandler).GetTypeInfo(), pageModel.PageType);
    }

    private class PageWithHandler : Page
    {
        public PocoModel Model => null;

        [ModelBinder]
        public int BindMe { get; set; }

        public void OnGet() { }

        public override Task ExecuteAsync() => throw new NotImplementedException();
    }

    private class PocoModel
    {
        // Just a plain ol' model, nothing to see here.

        [ModelBinder]
        public int IgnoreMe { get; set; }

        public void OnGet() { }
    }

    [Fact]
    public void PopulateHandlerMethods_DiscoversHandlersFromBaseType()
    {
        // Arrange
        var provider = CreateProvider();
        var typeInfo = typeof(InheritsMethods).GetTypeInfo();
        var baseType = typeof(TestSetPageModel);
        var pageModel = new PageApplicationModel(new PageActionDescriptor(), typeInfo, new object[0]);

        // Act
        provider.PopulateHandlerMethods(pageModel);

        // Assert
        var handlerMethods = pageModel.HandlerMethods;
        Assert.Collection(
            handlerMethods.OrderBy(h => h.MethodInfo.DeclaringType.Name).ThenBy(h => h.MethodInfo.Name),
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
    public void PopulateHandlerMethods_IgnoresNonPublicMethods()
    {
        // Arrange
        var provider = CreateProvider();
        var typeInfo = typeof(ProtectedModel).GetTypeInfo();
        var baseType = typeof(TestSetPageModel);
        var pageModel = new PageApplicationModel(new PageActionDescriptor(), typeInfo, new object[0]);

        // Act
        provider.PopulateHandlerMethods(pageModel);

        // Assert
        var handlerMethods = pageModel.HandlerMethods;
        Assert.Empty(handlerMethods);
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
    public void PopulateHandlerMethods_IgnoreGenericTypeParameters()
    {
        // Arrange
        var provider = CreateProvider();
        var typeInfo = typeof(GenericClassModel).GetTypeInfo();
        var pageModel = new PageApplicationModel(new PageActionDescriptor(), typeInfo, new object[0]);

        // Act
        provider.PopulateHandlerMethods(pageModel);

        // Assert
        var handlerMethods = pageModel.HandlerMethods;
        Assert.Empty(handlerMethods);
    }

    private class GenericClassModel
    {
        public void OnGet<T>()
        {
        }
    }

    [Fact]
    public void PopulateHandlerMethods_IgnoresStaticMethods()
    {
        // Arrange
        var provider = CreateProvider();
        var typeInfo = typeof(PageModelWithStaticHandler).GetTypeInfo();
        var expected = typeInfo.GetMethod(nameof(PageModelWithStaticHandler.OnGet), BindingFlags.Public | BindingFlags.Instance);
        var pageModel = new PageApplicationModel(new PageActionDescriptor(), typeInfo, new object[0]);

        // Act
        provider.PopulateHandlerMethods(pageModel);

        // Assert
        var handlerMethods = pageModel.HandlerMethods;
        Assert.Collection(
            handlerMethods,
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
    public void PopulateHandlerMethods_IgnoresAbstractMethods()
    {
        // Arrange
        var provider = CreateProvider();
        var typeInfo = typeof(PageModelWithAbstractMethod).GetTypeInfo();
        var expected = typeInfo.GetMethod(nameof(PageModelWithAbstractMethod.OnGet), BindingFlags.Public | BindingFlags.Instance);
        var pageModel = new PageApplicationModel(new PageActionDescriptor(), typeInfo, new object[0]);

        // Act
        provider.PopulateHandlerMethods(pageModel);

        // Assert
        var handlerMethods = pageModel.HandlerMethods;
        Assert.Collection(
            handlerMethods,
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
    public void PopulateHandlerMethods_IgnoresMethodWithNonHandlerAttribute()
    {
        // Arrange
        var provider = CreateProvider();
        var typeInfo = typeof(PageWithNonHandlerMethod).GetTypeInfo();
        var expected = typeInfo.GetMethod(nameof(PageWithNonHandlerMethod.OnGet), BindingFlags.Public | BindingFlags.Instance);
        var pageModel = new PageApplicationModel(new PageActionDescriptor(), typeInfo, new object[0]);

        // Act
        provider.PopulateHandlerMethods(pageModel);

        // Assert
        var handlerMethods = pageModel.HandlerMethods;
        Assert.Collection(
            handlerMethods,
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
        var provider = CreateProvider();
        var typeInfo = typeof(PageModelWithHandlerNames).GetTypeInfo();
        var pageModel = new PageApplicationModel(new PageActionDescriptor(), typeInfo, new object[0]);

        // Act
        provider.PopulateHandlerMethods(pageModel);

        // Assert
        var handlerMethods = pageModel.HandlerMethods;
        Assert.Collection(
            handlerMethods.OrderBy(h => h.MethodInfo.Name),
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
        var provider = CreateProvider();
        var typeInfo = typeof(PageWithHandlerParameters).GetTypeInfo();
        var expected = typeInfo.GetMethod(nameof(PageWithHandlerParameters.OnPost));
        var pageModel = new PageApplicationModel(new PageActionDescriptor(), typeInfo, new object[0]);

        // Act
        provider.PopulateHandlerMethods(pageModel);

        // Assert
        var handlerMethods = pageModel.HandlerMethods;
        var handler = Assert.Single(handlerMethods);

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
        var provider = CreateProvider();
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
        var provider = CreateProvider();
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
        var result = DefaultPageApplicationModelPartsProvider.TryParseHandlerMethod(methodName, out var httpMethod, out var handler);

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
        var result = DefaultPageApplicationModelPartsProvider.TryParseHandlerMethod(methodName, out var httpMethod, out var handler);

        // Assert
        Assert.True(result);
        Assert.Equal(expectedHttpMethod, httpMethod);
        Assert.Equal(expectedHandler, handler);
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

    public interface ITestService
    { }

    [PageModel]
    private class TestPageModel
    {
        public string Property1 { get; set; }

        [FromQuery]
        public string Property2 { get; set; }

        [FromServices]
        public ITestService TestService { get; set; }

        public void OnGetUser() { }
    }

    [Fact]
    public void PopulateFilters_AddsDisallowOptionsRequestsPageFilter()
    {
        // Arrange
        var provider = CreateProvider();
        var typeInfo = typeof(object).GetTypeInfo();
        var pageModel = new PageApplicationModel(new PageActionDescriptor(), typeInfo, typeInfo.GetCustomAttributes(inherit: true));

        // Act
        provider.PopulateFilters(pageModel);

        // Assert
        Assert.Collection(
            pageModel.Filters,
            filter => Assert.IsType<HandleOptionsRequestsPageFilter>(filter));
    }

    [Fact]
    public void PopulateFilters_AddsIFilterMetadataAttributesToModel()
    {
        // Arrange
        var provider = CreateProvider();
        var typeInfo = typeof(FilterModel).GetTypeInfo();
        var pageModel = new PageApplicationModel(new PageActionDescriptor(), typeInfo, typeInfo.GetCustomAttributes(inherit: true));

        // Act
        provider.PopulateFilters(pageModel);

        // Assert
        Assert.Collection(
            pageModel.Filters,
            filter => Assert.IsType<TypeFilterAttribute>(filter),
            filter => Assert.IsType<HandleOptionsRequestsPageFilter>(filter));
    }

    [PageModel]
    [Serializable]
    [TypeFilter(typeof(object))]
    private class FilterModel
    {
    }

    [Fact]
    public void PopulateFilters_AddsPageHandlerPageFilter_IfPageImplementsIAsyncPageFilter()
    {
        // Arrange
        var provider = CreateProvider();
        var typeInfo = typeof(ModelImplementingAsyncPageFilter).GetTypeInfo();
        var pageModel = new PageApplicationModel(new PageActionDescriptor(), typeInfo, typeInfo.GetCustomAttributes(inherit: true));

        // Act
        provider.PopulateFilters(pageModel);

        // Assert
        Assert.Collection(
            pageModel.Filters,
            filter => Assert.IsType<PageHandlerPageFilter>(filter),
            filter => Assert.IsType<HandleOptionsRequestsPageFilter>(filter));
    }

    private class ModelImplementingAsyncPageFilter : IAsyncPageFilter
    {
        public Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            throw new NotImplementedException();
        }

        public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
        {
            throw new NotImplementedException();
        }
    }

    [Fact]
    public void PopulateFilters_AddsPageHandlerPageFilter_IfPageImplementsIPageFilter()
    {
        // Arrange
        var provider = CreateProvider();
        var typeInfo = typeof(ModelImplementingPageFilter).GetTypeInfo();
        var pageModel = new PageApplicationModel(new PageActionDescriptor(), typeInfo, typeInfo.GetCustomAttributes(inherit: true));

        // Act
        provider.PopulateFilters(pageModel);

        // Assert
        Assert.Collection(
            pageModel.Filters,
            filter => Assert.IsType<PageHandlerPageFilter>(filter),
            filter => Assert.IsType<HandleOptionsRequestsPageFilter>(filter));
    }

    private class ModelImplementingPageFilter : IPageFilter
    {
        public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
        {
            throw new NotImplementedException();
        }

        public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
        {
            throw new NotImplementedException();
        }

        public void OnPageHandlerSelected(PageHandlerSelectedContext context)
        {
            throw new NotImplementedException();
        }
    }

    [Fact]
    public void PopulateFilters_AddsPageHandlerPageFilter_ForModelDerivingFromTypeImplementingPageFilter()
    {
        // Arrange
        var provider = CreateProvider();
        var typeInfo = typeof(DerivedFromPageModel).GetTypeInfo();
        var pageModel = new PageApplicationModel(new PageActionDescriptor(), typeInfo, typeInfo.GetCustomAttributes(inherit: true));

        // Act
        provider.PopulateFilters(pageModel);

        // Assert
        Assert.Collection(
            pageModel.Filters,
            filter => Assert.IsType<ServiceFilterAttribute>(filter),
            filter => Assert.IsType<PageHandlerPageFilter>(filter),
            filter => Assert.IsType<HandleOptionsRequestsPageFilter>(filter));
    }

    [ServiceFilter(typeof(IServiceProvider))]
    private class DerivedFromPageModel : PageModel { }

    private static DefaultPageApplicationModelProvider CreateProvider()
    {
        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

        return new DefaultPageApplicationModelProvider(
            modelMetadataProvider,
            Options.Create(new RazorPagesOptions()),
            new DefaultPageApplicationModelPartsProvider(modelMetadataProvider));
    }
}
