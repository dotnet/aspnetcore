// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests
{
    public class AuthorizeFilterIntegrationTest
    {
        // This is a test for security, because we can't assume that any IAuthorizationPolicyProvider other than
        // DefaultAuthorizationPolicyProvider will return the same result for the same input. So a cache could cause
        // undesired access.
        [Fact]
        public async Task AuthorizeFilter_CalledTwiceWithNonDefaultProvider()
        {
            // Arrange
            var applicationModelProviderContext = new ApplicationModelProviderContext(
                new[] { typeof(AuthorizeController).GetTypeInfo() });

            var policyProvider = new TestAuthorizationPolicyProvider();
            var defaultProvider = new DefaultApplicationModelProvider(Options.Create(new MvcOptions()));

            defaultProvider.OnProvidersExecuting(applicationModelProviderContext);

            var controller = Assert.Single(applicationModelProviderContext.Result.Controllers);
            var action = Assert.Single(controller.Actions);
            var authorizeData = action.Attributes.OfType<AuthorizeAttribute>();
            var authorizeFilter = new AuthorizeFilter(policyProvider, authorizeData);

            var actionContext = new ActionContext(GetHttpContext(), new RouteData(), new ControllerActionDescriptor());

            var authorizationFilterContext = new AuthorizationFilterContext(actionContext, action.Filters);

            // Act
            await authorizeFilter.OnAuthorizationAsync(authorizationFilterContext);
            await authorizeFilter.OnAuthorizationAsync(authorizationFilterContext);

            // Assert
            Assert.Equal(2, policyProvider.GetPolicyCount);
        }

        private HttpContext GetHttpContext()
        {
            var httpContext = new DefaultHttpContext();

            httpContext.RequestServices = GetServices();
            return httpContext;
        }

        private static IServiceProvider GetServices()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddAuthorization();
            serviceCollection.AddMvc();
            serviceCollection
                .AddTransient<ILoggerFactory, LoggerFactory>()
                .AddTransient<ILogger<DefaultAuthorizationService>, Logger<DefaultAuthorizationService>>();

            return serviceCollection.BuildServiceProvider();
        }

        public class TestAuthorizationPolicyProvider : IAuthorizationPolicyProvider
        {
            public int GetPolicyCount = 0;

            public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
            {
                throw new NotImplementedException();
            }

            public Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
            {
                GetPolicyCount++;

                var requirements = new IAuthorizationRequirement[]
                {
                    new AssertionRequirement((con) => { return true; })
                };
                return Task.FromResult(new AuthorizationPolicy(requirements, new string[] { }));
            }
        }

        public class AuthorizeController
        {
            [Authorize(Policy = "Base")]
            public virtual void Authorize()
            { }
        }
    }
}
