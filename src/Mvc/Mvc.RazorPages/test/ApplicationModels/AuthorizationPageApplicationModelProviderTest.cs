// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

public class AuthorizationPageApplicationModelProviderTest
{
    private readonly IOptions<MvcOptions> OptionsWithoutEndpointRouting = Options.Create(new MvcOptions { EnableEndpointRouting = false });

    [Fact]
    public void OnProvidersExecuting_IgnoresAttributesOnHandlerMethods()
    {
        // Arrange
        var policyProvider = new DefaultAuthorizationPolicyProvider(Options.Create(new AuthorizationOptions()));
        var authorizationProvider = new AuthorizationPageApplicationModelProvider(policyProvider, OptionsWithoutEndpointRouting);
        var typeInfo = typeof(PageWithAuthorizeHandlers).GetTypeInfo();
        var context = GetApplicationProviderContext(typeInfo);

        // Act
        authorizationProvider.OnProvidersExecuting(context);

        // Assert
        Assert.Collection(
            context.PageApplicationModel.Filters,
            f => Assert.IsType<PageHandlerPageFilter>(f),
            f => Assert.IsType<HandleOptionsRequestsPageFilter>(f));
    }

    private class PageWithAuthorizeHandlers : Page
    {
        public ModelWithAuthorizeHandlers Model => null;

        public override Task ExecuteAsync() => throw new NotImplementedException();
    }

    public class ModelWithAuthorizeHandlers : PageModel
    {
        [Authorize]
        public void OnGet()
        {
        }
    }

    [Fact]
    public void OnProvidersExecuting_DoesNothingWithEndpointRouting()
    {
        // Arrange
        var policyProvider = new DefaultAuthorizationPolicyProvider(Options.Create(new AuthorizationOptions()));
        var authorizationProvider = new AuthorizationPageApplicationModelProvider(policyProvider, Options.Create(new MvcOptions()));
        var typeInfo = typeof(TestPage).GetTypeInfo();
        var context = GetApplicationProviderContext(typeInfo);

        // Act
        authorizationProvider.OnProvidersExecuting(context);

        // Assert
        Assert.Collection(
            context.PageApplicationModel.Filters,
            f => Assert.IsType<PageHandlerPageFilter>(f),
            f => Assert.IsType<HandleOptionsRequestsPageFilter>(f));
    }

    [Fact]
    public void OnProvidersExecuting_AddsAuthorizeFilter_IfModelHasAuthorizationAttributes()
    {
        // Arrange
        var policyProvider = new DefaultAuthorizationPolicyProvider(Options.Create(new AuthorizationOptions()));
        var authorizationProvider = new AuthorizationPageApplicationModelProvider(policyProvider, OptionsWithoutEndpointRouting);
        var context = GetApplicationProviderContext(typeof(TestPage).GetTypeInfo());

        // Act
        authorizationProvider.OnProvidersExecuting(context);

        // Assert
        Assert.Collection(
            context.PageApplicationModel.Filters,
            f => Assert.IsType<PageHandlerPageFilter>(f),
            f => Assert.IsType<HandleOptionsRequestsPageFilter>(f),
            f => Assert.IsType<AuthorizeFilter>(f));
    }

    private class TestPage : Page
    {
        public TestModel Model => null;

        public override Task ExecuteAsync() => throw new NotImplementedException();
    }

    [Authorize]
    private class TestModel : PageModel
    {
        public virtual void OnGet()
        {
        }
    }

    [Fact]
    public void OnProvidersExecuting_CollatesAttributesFromInheritedTypes()
    {
        // Arrange
        var options = Options.Create(new AuthorizationOptions());
        options.Value.AddPolicy("Base", policy => policy.RequireClaim("Basic").RequireClaim("Basic2"));
        options.Value.AddPolicy("Derived", policy => policy.RequireClaim("Derived"));

        var policyProvider = new DefaultAuthorizationPolicyProvider(options);
        var authorizationProvider = new AuthorizationPageApplicationModelProvider(policyProvider, OptionsWithoutEndpointRouting);

        var context = GetApplicationProviderContext(typeof(TestPageWithDerivedModel).GetTypeInfo());

        // Act
        authorizationProvider.OnProvidersExecuting(context);

        // Assert
        AuthorizeFilter authorizeFilter = null;
        Assert.Collection(
            context.PageApplicationModel.Filters,
            f => Assert.IsType<PageHandlerPageFilter>(f),
            f => Assert.IsType<HandleOptionsRequestsPageFilter>(f),
            f => authorizeFilter = Assert.IsType<AuthorizeFilter>(f));

        // Basic + Basic2 + Derived authorize
        Assert.Equal(3, authorizeFilter.Policy.Requirements.Count);
    }

    private class TestPageWithDerivedModel : Page
    {
        public DerivedModel Model => null;

        public override Task ExecuteAsync() => throw new NotImplementedException();
    }

    [Authorize(Policy = "Base")]
    public class BaseModel : PageModel
    {
    }

    [Authorize(Policy = "Derived")]
    private class DerivedModel : BaseModel
    {
        public virtual void OnGet()
        {
        }
    }

    [Fact]
    public void OnProvidersExecuting_AddsAllowAnonymousFilter()
    {
        // Arrange
        var policyProvider = new DefaultAuthorizationPolicyProvider(Options.Create(new AuthorizationOptions()));
        var authorizationProvider = new AuthorizationPageApplicationModelProvider(policyProvider, OptionsWithoutEndpointRouting);
        var context = GetApplicationProviderContext(typeof(PageWithAnonymousModel).GetTypeInfo());

        // Act
        authorizationProvider.OnProvidersExecuting(context);

        // Assert
        Assert.Collection(
            context.PageApplicationModel.Filters,
            f => Assert.IsType<PageHandlerPageFilter>(f),
            f => Assert.IsType<HandleOptionsRequestsPageFilter>(f),
            f => Assert.IsType<AllowAnonymousFilter>(f));
    }

    private class PageWithAnonymousModel : Page
    {
        public AnonymousModel Model => null;

        public override Task ExecuteAsync() => throw new NotImplementedException();
    }

    [AllowAnonymous]
    public class AnonymousModel : PageModel
    {
        public void OnGet() { }
    }

    private static PageApplicationModelProviderContext GetApplicationProviderContext(TypeInfo typeInfo)
    {
        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

        var defaultProvider = new DefaultPageApplicationModelProvider(
            modelMetadataProvider,
            Options.Create(new RazorPagesOptions()),
            new DefaultPageApplicationModelPartsProvider(modelMetadataProvider));

        var context = new PageApplicationModelProviderContext(new PageActionDescriptor(), typeInfo);
        defaultProvider.OnProvidersExecuting(context);

        return context;
    }
}
