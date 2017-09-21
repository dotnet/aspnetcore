// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ApiControllerApplicationModelProviderTest
    {
        [Fact]
        public void OnProvidersExecuting_AddsModelStateInvalidFilter_IfTypeIsAnnotatedWithAttribute()
        {
            // Arrange
            var context = GetContext(typeof(TestApiController));
            var options = new TestOptionsManager<ApiBehaviorOptions>(new ApiBehaviorOptions
            {
                InvalidModelStateResponseFactory = _ => null,
            });
            
            var provider = new ApiControllerApplicationModelProvider(options, NullLoggerFactory.Instance);

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var controllerModel = Assert.Single(context.Result.Controllers);
            Assert.IsType<ModelStateInvalidFilter>(controllerModel.Filters.Last());
        }

        [Fact]
        public void OnProvidersExecuting_DoesNotAddModelStateInvalidFilterToController_IfFeatureIsDisabledViaOptions()
        {
            // Arrange
            var context = GetContext(typeof(TestApiController));
            var options = new TestOptionsManager<ApiBehaviorOptions>(new ApiBehaviorOptions
            {
                EnableModelStateInvalidFilter = false,
            });

            var provider = new ApiControllerApplicationModelProvider(options, NullLoggerFactory.Instance);

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var controllerModel = Assert.Single(context.Result.Controllers);
            Assert.DoesNotContain(typeof(ModelStateInvalidFilter), controllerModel.Filters.Select(f => f.GetType()));
        }

        [Fact]
        public void OnProvidersExecuting_AddsModelStateInvalidFilter_IfActionIsAnnotatedWithAttribute()
        {
            // Arrange
            var context = GetContext(typeof(SimpleController));
            var options = new TestOptionsManager<ApiBehaviorOptions>(new ApiBehaviorOptions
            {
                InvalidModelStateResponseFactory = _ => null,
            });

            var provider = new ApiControllerApplicationModelProvider(options, NullLoggerFactory.Instance);

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(
                Assert.Single(context.Result.Controllers).Actions.OrderBy(a => a.ActionName),
                action =>
                {
                    Assert.Contains(typeof(ModelStateInvalidFilter), action.Filters.Select(f => f.GetType()));
                },
                action =>
                {
                    Assert.DoesNotContain(typeof(ModelStateInvalidFilter), action.Filters.Select(f => f.GetType()));
                });
        }

        [Fact]
        public void OnProvidersExecuting_SkipsAddingFilterToActionIfFeatureIsDisabledUsingOptions()
        {
            // Arrange
            var context = GetContext(typeof(SimpleController));
            var options = new TestOptionsManager<ApiBehaviorOptions>(new ApiBehaviorOptions
            {
                EnableModelStateInvalidFilter = false,
            });

            var provider = new ApiControllerApplicationModelProvider(options, NullLoggerFactory.Instance);

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(
                Assert.Single(context.Result.Controllers).Actions.OrderBy(a => a.ActionName),
                action =>
                {
                    Assert.DoesNotContain(typeof(ModelStateInvalidFilter), action.Filters.Select(f => f.GetType()));
                },
                action =>
                {
                    Assert.DoesNotContain(typeof(ModelStateInvalidFilter), action.Filters.Select(f => f.GetType()));
                });
        }

        private static ApplicationModelProviderContext GetContext(Type type)
        {
            var context = new ApplicationModelProviderContext(new[] { type.GetTypeInfo() });
            new DefaultApplicationModelProvider(new TestOptionsManager<MvcOptions>()).OnProvidersExecuting(context);
            return context;
        }

        [ApiController]
        private class TestApiController : Controller
        {
            public IActionResult TestAction() => null;
        }

        
        private class SimpleController : Controller
        {
            public IActionResult ActionWithoutFilter() => null;

            [TestApiBehavior]
            public IActionResult ActionWithFilter() => null;
        }

        [AttributeUsage(AttributeTargets.Method)]
        private class TestApiBehavior : Attribute, IApiBehaviorMetadata
        {
        }
    }
}
