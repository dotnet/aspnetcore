// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class AuthorizationPageApplicationModelProviderTest
    {
        [Fact]
        public void OnProvidersExecuting_IgnoresAttributesOnHandlerMethods()
        {
            // Arrange
            var policyProvider = new DefaultAuthorizationPolicyProvider(new TestOptionsManager<AuthorizationOptions>());
            var autorizationProvider = new AuthorizationPageApplicationModelProvider(policyProvider);
            var typeInfo = typeof(PageWiithAuthorizeHandlers).GetTypeInfo();
            var context = GetApplicationProviderContext(typeInfo);

            // Act
            autorizationProvider.OnProvidersExecuting(context);

            // Assert
            Assert.Empty(context.PageApplicationModel.Filters);
        }

        private class PageWiithAuthorizeHandlers
        {
            public ModelWuthAuthorizeHandlers Model => null;
        }

        public class ModelWuthAuthorizeHandlers
        {
            [Authorize]
            public void OnGet()
            {
            }
        }

        [Fact]
        public void OnProvidersExecuting_AddsAuthorizeFilter_IfModelHasAuthorizationAttributes()
        {
            // Arrange
            var policyProvider = new DefaultAuthorizationPolicyProvider(new TestOptionsManager<AuthorizationOptions>());
            var autorizationProvider = new AuthorizationPageApplicationModelProvider(policyProvider);
            var context = GetApplicationProviderContext(typeof(TestPage).GetTypeInfo());

            // Act
            autorizationProvider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(
                context.PageApplicationModel.Filters,
                f => Assert.IsType<AuthorizeFilter>(f));
        }

        private class TestPage
        {
            public TestModel Model => null;
        }

        [Authorize]
        private class TestModel
        {
            public virtual void OnGet()
            {
            }
        }

        [Fact]
        public void OnProvidersExecuting_CollatesAttributesFromInheritedTypes()
        {
            // Arrange
            var options = new TestOptionsManager<AuthorizationOptions>();
            options.Value.AddPolicy("Base", policy => policy.RequireClaim("Basic").RequireClaim("Basic2"));
            options.Value.AddPolicy("Derived", policy => policy.RequireClaim("Derived"));

            var policyProvider = new DefaultAuthorizationPolicyProvider(options);
            var autorizationProvider = new AuthorizationPageApplicationModelProvider(policyProvider);

            var context = GetApplicationProviderContext(typeof(TestPageWithDerivedModel).GetTypeInfo());

            // Act
            autorizationProvider.OnProvidersExecuting(context);

            // Assert
            var authorizeFilter = Assert.IsType<AuthorizeFilter>(Assert.Single(context.PageApplicationModel.Filters));
            // Basic + Basic2 + Derived authorize
            Assert.Equal(3, authorizeFilter.Policy.Requirements.Count);
        }

        private class TestPageWithDerivedModel
        {
            public DeriviedModel Model => null;
        }

        [Authorize(Policy = "Base")]
        public class BaseModel
        {
        }

        [Authorize(Policy = "Derived")]
        private class DeriviedModel : BaseModel
        {
            public virtual void OnGet()
            {
            }
        }

        [Fact]
        public void OnProvidersExecuting_AddsAllowAnonymousFilter()
        {
            // Arrange
            var policyProvider = new DefaultAuthorizationPolicyProvider(new TestOptionsManager<AuthorizationOptions>());
            var autorizationProvider = new AuthorizationPageApplicationModelProvider(policyProvider);
            var context = GetApplicationProviderContext(typeof(PageWithAnonymousModel).GetTypeInfo());

            // Act
            autorizationProvider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(
                context.PageApplicationModel.Filters,
                f => Assert.IsType<AllowAnonymousFilter>(f));
        }

        private class PageWithAnonymousModel
        {
            public AnonymousModel Model => null;
        }

        [AllowAnonymous]
        public class AnonymousModel
        {
            public void OnGet() { }
        }

        private static PageApplicationModelProviderContext GetApplicationProviderContext(TypeInfo typeInfo)
        {
            var defaultProvider = new DefaultPageApplicationModelProvider(new TestOptionsManager<MvcOptions>());
            var context = new PageApplicationModelProviderContext(new PageActionDescriptor(), typeInfo);
            defaultProvider.OnProvidersExecuting(context);
            return context;
        }
    }
}