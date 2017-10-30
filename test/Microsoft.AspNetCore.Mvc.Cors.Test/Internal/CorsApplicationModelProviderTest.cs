// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Cors.Internal;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.Extensions.Options;
using Moq;
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
            var defaultProvider = new DefaultApplicationModelProvider(Options.Create(new MvcOptions()));

            var context = new ApplicationModelProviderContext(new [] { typeof(CorsController).GetTypeInfo() });
            defaultProvider.OnProvidersExecuting(context);

            // Act
            corsProvider.OnProvidersExecuting(context);

            // Assert
            var model = Assert.Single(context.Result.Controllers);
            Assert.Single(model.Filters, f => f is CorsAuthorizationFilterFactory);
            var action = Assert.Single(model.Actions);
            var selector = Assert.Single(action.Selectors);
            var constraint = Assert.Single(selector.ActionConstraints, c => c is HttpMethodActionConstraint);
            Assert.IsType<CorsHttpMethodActionConstraint>(constraint);
        }

        [Fact]
        public void CreateControllerModel_DisableCorsAttributeAddsDisableCorsAuthorizationFilter()
        {
            // Arrange
            var corsProvider = new CorsApplicationModelProvider();
            var defaultProvider = new DefaultApplicationModelProvider(Options.Create(new MvcOptions()));

            var context = new ApplicationModelProviderContext(new[] { typeof(DisableCorsController).GetTypeInfo() });
            defaultProvider.OnProvidersExecuting(context);

            // Act
            corsProvider.OnProvidersExecuting(context);

            // Assert
            var model = Assert.Single(context.Result.Controllers);
            Assert.Single(model.Filters, f => f is DisableCorsAuthorizationFilter);
            var action = Assert.Single(model.Actions);
            var selector = Assert.Single(action.Selectors);
            var constraint = Assert.Single(selector.ActionConstraints, c => c is HttpMethodActionConstraint);
            Assert.IsType<CorsHttpMethodActionConstraint>(constraint);
        }

        [Fact]
        public void CreateControllerModel_CustomCorsFilter_ReplacesHttpConstraints()
        {
            // Arrange
            var corsProvider = new CorsApplicationModelProvider();
            var defaultProvider = new DefaultApplicationModelProvider(Options.Create(new MvcOptions()));

            var context = new ApplicationModelProviderContext(new[] { typeof(CustomCorsFilterController).GetTypeInfo() });
            defaultProvider.OnProvidersExecuting(context);

            // Act
            corsProvider.OnProvidersExecuting(context);

            // Assert
            var controller = Assert.Single(context.Result.Controllers);
            var action = Assert.Single(controller.Actions);
            var selector = Assert.Single(action.Selectors);
            var constraint = Assert.Single(selector.ActionConstraints, c => c is HttpMethodActionConstraint);
            Assert.IsType<CorsHttpMethodActionConstraint>(constraint);
        }

        [Fact]
        public void BuildActionModel_EnableCorsAttributeAddsCorsAuthorizationFilterFactory()
        {
            // Arrange
            var corsProvider = new CorsApplicationModelProvider();
            var defaultProvider = new DefaultApplicationModelProvider(Options.Create(new MvcOptions()));

            var context = new ApplicationModelProviderContext(new[] { typeof(EnableCorsController).GetTypeInfo() });
            defaultProvider.OnProvidersExecuting(context);

            // Act
            corsProvider.OnProvidersExecuting(context);

            // Assert
            var controller = Assert.Single(context.Result.Controllers);
            var action = Assert.Single(controller.Actions);
            Assert.Single(action.Filters, f => f is CorsAuthorizationFilterFactory);
            var selector = Assert.Single(action.Selectors);
            var constraint = Assert.Single(selector.ActionConstraints, c => c is HttpMethodActionConstraint);
            Assert.IsType<CorsHttpMethodActionConstraint>(constraint);
        }

        [Fact]
        public void BuildActionModel_DisableCorsAttributeAddsDisableCorsAuthorizationFilter()
        {
            // Arrange
            var corsProvider = new CorsApplicationModelProvider();
            var defaultProvider = new DefaultApplicationModelProvider(Options.Create(new MvcOptions()));

            var context = new ApplicationModelProviderContext(new[] { typeof(DisableCorsActionController).GetTypeInfo() });
            defaultProvider.OnProvidersExecuting(context);

            // Act
            corsProvider.OnProvidersExecuting(context);

            // Assert
            var controller = Assert.Single(context.Result.Controllers);
            var action = Assert.Single(controller.Actions);
            Assert.Contains(action.Filters, f => f is DisableCorsAuthorizationFilter);
            var selector = Assert.Single(action.Selectors);
            var constraint = Assert.Single(selector.ActionConstraints, c => c is HttpMethodActionConstraint);
            Assert.IsType<CorsHttpMethodActionConstraint>(constraint);
        }

        [Fact]
        public void BuildActionModel_CustomCorsAuthorizationFilterOnAction_ReplacesHttpConstraints()
        {
            // Arrange
            var corsProvider = new CorsApplicationModelProvider();
            var defaultProvider = new DefaultApplicationModelProvider(Options.Create(new MvcOptions()));

            var context = new ApplicationModelProviderContext(new[] { typeof(CustomCorsFilterOnActionController).GetTypeInfo() });
            defaultProvider.OnProvidersExecuting(context);

            // Act
            corsProvider.OnProvidersExecuting(context);

            // Assert
            var controller = Assert.Single(context.Result.Controllers);
            var action = Assert.Single(controller.Actions);
            var selector = Assert.Single(action.Selectors);
            var constraint = Assert.Single(selector.ActionConstraints, c => c is HttpMethodActionConstraint);
            Assert.IsType<CorsHttpMethodActionConstraint>(constraint);
        }

        [Fact]
        public void CreateControllerModel_EnableCorsGloballyReplacesHttpMethodConstraints()
        {
            // Arrange
            var corsProvider = new CorsApplicationModelProvider();
            var defaultProvider = new DefaultApplicationModelProvider(Options.Create(new MvcOptions()));

            var context = new ApplicationModelProviderContext(new[] { typeof(RegularController).GetTypeInfo() });
            context.Result.Filters.Add(new CorsAuthorizationFilter(Mock.Of<ICorsService>(), Mock.Of<ICorsPolicyProvider>()));
            defaultProvider.OnProvidersExecuting(context);

            // Act
            corsProvider.OnProvidersExecuting(context);

            // Assert
            var model = Assert.Single(context.Result.Controllers);
            var action = Assert.Single(model.Actions);
            var selector = Assert.Single(action.Selectors);
            var constraint = Assert.Single(selector.ActionConstraints, c => c is HttpMethodActionConstraint);
            Assert.IsType<CorsHttpMethodActionConstraint>(constraint);
        }

        [Fact]
        public void CreateControllerModel_DisableCorsGloballyReplacesHttpMethodConstraints()
        {
            // Arrange
            var corsProvider = new CorsApplicationModelProvider();
            var defaultProvider = new DefaultApplicationModelProvider(Options.Create(new MvcOptions()));

            var context = new ApplicationModelProviderContext(new[] { typeof(RegularController).GetTypeInfo() });
            context.Result.Filters.Add(new DisableCorsAuthorizationFilter());
            defaultProvider.OnProvidersExecuting(context);

            // Act
            corsProvider.OnProvidersExecuting(context);

            // Assert
            var model = Assert.Single(context.Result.Controllers);
            var action = Assert.Single(model.Actions);
            var selector = Assert.Single(action.Selectors);
            var constraint = Assert.Single(selector.ActionConstraints, c => c is HttpMethodActionConstraint);
            Assert.IsType<CorsHttpMethodActionConstraint>(constraint);
        }

        [Fact]
        public void CreateControllerModel_CustomCorsFilterGloballyReplacesHttpMethodConstraints()
        {
            // Arrange
            var corsProvider = new CorsApplicationModelProvider();
            var defaultProvider = new DefaultApplicationModelProvider(Options.Create(new MvcOptions()));

            var context = new ApplicationModelProviderContext(new[] { typeof(RegularController).GetTypeInfo() });
            context.Result.Filters.Add(new CustomCorsFilterAttribute());
            defaultProvider.OnProvidersExecuting(context);

            // Act
            corsProvider.OnProvidersExecuting(context);

            // Assert
            var model = Assert.Single(context.Result.Controllers);
            var action = Assert.Single(model.Actions);
            var selector = Assert.Single(action.Selectors);
            var constraint = Assert.Single(selector.ActionConstraints, c => c is HttpMethodActionConstraint);
            Assert.IsType<CorsHttpMethodActionConstraint>(constraint);
        }

        [Fact]
        public void CreateControllerModel_CorsNotInUseDoesNotOverrideHttpConstraints()
        {
            // Arrange
            var corsProvider = new CorsApplicationModelProvider();
            var defaultProvider = new DefaultApplicationModelProvider(Options.Create(new MvcOptions()));

            var context = new ApplicationModelProviderContext(new[] { typeof(RegularController).GetTypeInfo() });
            defaultProvider.OnProvidersExecuting(context);

            // Act
            corsProvider.OnProvidersExecuting(context);

            // Assert
            var model = Assert.Single(context.Result.Controllers);
            var action = Assert.Single(model.Actions);
            var selector = Assert.Single(action.Selectors);
            var constraint = Assert.Single(selector.ActionConstraints, c => c is HttpMethodActionConstraint);
            Assert.IsNotType<CorsHttpMethodActionConstraint>(constraint);
        }

        private class EnableCorsController
        {
            [EnableCors("policy")]
            [HttpGet]
            public IActionResult Action()
            {
                return null;
            }
        }

        private class DisableCorsActionController
        {
            [DisableCors]
            [HttpGet]
            public void Action()
            {
            }
        }

        [EnableCors("policy")]
        public class CorsController
        {
            [HttpGet]
            public IActionResult Action()
            {
                return null;
            }
        }

        [DisableCors]
        public class DisableCorsController
        {
            [HttpOptions]
            public IActionResult Action()
            {
                return null;
            }
        }

        public class RegularController
        {
            [HttpPost]
            public IActionResult Action()
            {
                return null;
            }
        }

        [CustomCorsFilter]
        public class CustomCorsFilterController
        {
            [HttpPost]
            public IActionResult Action()
            {
                return null;
            }
        }

        public class CustomCorsFilterOnActionController
        {
            [HttpPost]
            [CustomCorsFilter]
            public IActionResult Action()
            {
                return null;
            }
        }

        public class CustomCorsFilterAttribute : Attribute, ICorsAuthorizationFilter
        {
            public int Order { get; } = 1000;

            public Task OnAuthorizationAsync(AuthorizationFilterContext context)
            {
                return Task.FromResult(0);
            }
        }
    }
}