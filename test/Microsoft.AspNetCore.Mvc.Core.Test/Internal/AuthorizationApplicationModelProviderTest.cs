// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class AuthorizationApplicationModelProviderTest
    {
        [Fact]
        public void CreateControllerModel_AuthorizeAttributeAddsAuthorizeFilter()
        {
            // Arrange
            var provider = new AuthorizationApplicationModelProvider(new DefaultAuthorizationPolicyProvider(new TestOptionsManager<AuthorizationOptions>()));
            var defaultProvider = new DefaultApplicationModelProvider(new TestOptionsManager<MvcOptions>());

            var context = new ApplicationModelProviderContext(new[] { typeof(AccountController).GetTypeInfo() });
            defaultProvider.OnProvidersExecuting(context);

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var controller = Assert.Single(context.Result.Controllers);
            Assert.Single(controller.Filters, f => f is AuthorizeFilter);
        }

        [Fact]
        public void BuildActionModels_BaseAuthorizeFiltersAreStillValidWhenOverriden()
        {
            // Arrange
            var options = new TestOptionsManager<AuthorizationOptions>();
            options.Value.AddPolicy("Base", policy => policy.RequireClaim("Basic").RequireClaim("Basic2"));
            options.Value.AddPolicy("Derived", policy => policy.RequireClaim("Derived"));

            var provider = new AuthorizationApplicationModelProvider(new DefaultAuthorizationPolicyProvider(options));
            var defaultProvider = new DefaultApplicationModelProvider(new TestOptionsManager<MvcOptions>());

            var context = new ApplicationModelProviderContext(new[] { typeof(DerivedController).GetTypeInfo() });
            defaultProvider.OnProvidersExecuting(context);

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var controller = Assert.Single(context.Result.Controllers);
            var action = Assert.Single(controller.Actions);
            Assert.Equal("Authorize", action.ActionName);

            var attributeRoutes = action.Selectors.Where(sm => sm.AttributeRouteModel != null);
            Assert.Empty(attributeRoutes);
            var authorizeFilters = action.Filters.OfType<AuthorizeFilter>();
            Assert.Single(authorizeFilters);

            Assert.NotNull(authorizeFilters.First().PolicyProvider);
            Assert.Equal(3, authorizeFilters.First().AuthorizeData.Count());
        }

        [Fact]
        public void CreateControllerModelAndActionModel_AllowAnonymousAttributeAddsAllowAnonymousFilter()
        {
            // Arrange
            var provider = new AuthorizationApplicationModelProvider(new DefaultAuthorizationPolicyProvider(new TestOptionsManager<AuthorizationOptions>()));
            var defaultProvider = new DefaultApplicationModelProvider(new TestOptionsManager<MvcOptions>());

            var context = new ApplicationModelProviderContext(new[] { typeof(AnonymousController).GetTypeInfo() });
            defaultProvider.OnProvidersExecuting(context);

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var controller = Assert.Single(context.Result.Controllers);
            Assert.Single(controller.Filters, f => f is AllowAnonymousFilter);
            var action = Assert.Single(controller.Actions);
            Assert.Single(action.Filters, f => f is AllowAnonymousFilter);
        }

        private class BaseController
        {
            [Authorize(Policy = "Base")]
            public virtual void Authorize()
            {
            }
        }

        private class DerivedController : BaseController
        {
            [Authorize(Policy = "Derived")]
            public override void Authorize()
            {
            }
        }

        [Authorize]
        public class AccountController
        {
        }

        [AllowAnonymous]
        public class AnonymousController
        {
            [AllowAnonymous]
            public void SomeAction()
            {
            }
        }
    }
}