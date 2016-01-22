// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Cors.Internal
{
    public class CorsApplicationModelProviderTest
    {

        [Fact]
        public void CreateControllerModel_EnableCorsAttributeAddsCorsAuthorizationFilterFactory()
        {
            // Arrange
            var corsProvider = new CorsApplicationModelProvider();
            var defaultProvider = new DefaultApplicationModelProvider(new TestOptionsManager<MvcOptions>());

            var context = new ApplicationModelProviderContext(new [] { typeof(CorsController).GetTypeInfo() });
            defaultProvider.OnProvidersExecuting(context);

            // Act
            corsProvider.OnProvidersExecuting(context);

            // Assert
            var model = Assert.Single(context.Result.Controllers);
            Assert.Single(model.Filters, f => f is CorsAuthorizationFilterFactory);
        }

        [Fact]
        public void CreateControllerModel_DisableCorsAttributeAddsDisableCorsAuthorizationFilter()
        {
            // Arrange
            var corsProvider = new CorsApplicationModelProvider();
            var defaultProvider = new DefaultApplicationModelProvider(new TestOptionsManager<MvcOptions>());

            var context = new ApplicationModelProviderContext(new[] { typeof(DisableCorsController).GetTypeInfo() });
            defaultProvider.OnProvidersExecuting(context);

            // Act
            corsProvider.OnProvidersExecuting(context);

            // Assert
            var model = Assert.Single(context.Result.Controllers);
            Assert.Single(model.Filters, f => f is DisableCorsAuthorizationFilter);
        }

        [Fact]
        public void BuildActionModel_EnableCorsAttributeAddsCorsAuthorizationFilterFactory()
        {
            // Arrange
            var corsProvider = new CorsApplicationModelProvider();
            var defaultProvider = new DefaultApplicationModelProvider(new TestOptionsManager<MvcOptions>());

            var context = new ApplicationModelProviderContext(new[] { typeof(EnableCorsController).GetTypeInfo() });
            defaultProvider.OnProvidersExecuting(context);

            // Act
            corsProvider.OnProvidersExecuting(context);

            // Assert
            var controller = Assert.Single(context.Result.Controllers);
            var action = Assert.Single(controller.Actions);
            Assert.Single(action.Filters, f => f is CorsAuthorizationFilterFactory);
        }

        [Fact]
        public void BuildActionModel_DisableCorsAttributeAddsDisableCorsAuthorizationFilter()
        {
            // Arrange
            var corsProvider = new CorsApplicationModelProvider();
            var defaultProvider = new DefaultApplicationModelProvider(new TestOptionsManager<MvcOptions>());

            var context = new ApplicationModelProviderContext(new[] { typeof(DisableCorsActionController).GetTypeInfo() });
            defaultProvider.OnProvidersExecuting(context);

            // Act
            corsProvider.OnProvidersExecuting(context);

            // Assert
            var controller = Assert.Single(context.Result.Controllers);
            var action = Assert.Single(controller.Actions);
            Assert.True(action.Filters.Any(f => f is DisableCorsAuthorizationFilter));
        }

        private class EnableCorsController
        {
            [EnableCors("policy")]
            public void Action()
            {
            }
        }

        private class DisableCorsActionController
        {
            [DisableCors]
            public void Action()
            {
            }
        }

        [EnableCors("policy")]
        public class CorsController
        {
        }

        [DisableCors]
        public class DisableCorsController
        {
        }
    }
}