// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    public class ControllerActionInvokerProviderTest
    {
        [Fact]
        public void OnExecuting_ConfiguresModelState_WithMvcOptions()
        {
            // Arrange
            var provider = CreateInvokerProvider(new MvcOptions() { MaxValidationDepth = 1, MaxModelBindingRecursionDepth = 2, MaxModelValidationErrors = 3 });

            var context = new ActionInvokerProviderContext(new ActionContext()
            {
                ActionDescriptor = GetControllerActionDescriptor(),
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData(),
            });

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var invoker = Assert.IsType<ControllerActionInvoker>(context.Result);
            Assert.Equal(1, invoker.ControllerContext.ModelState.MaxValidationDepth);
            Assert.Equal(2, invoker.ControllerContext.ModelState.MaxStateDepth);
            Assert.Equal(3, invoker.ControllerContext.ModelState.MaxAllowedErrors);

        }

        private static ControllerActionDescriptor GetControllerActionDescriptor()
        {
            var method = typeof(TestActions).GetMethod(nameof(TestActions.GetAction));
            var actionDescriptor = new ControllerActionDescriptor
            {
                MethodInfo = method,
                Parameters = Array.Empty<ParameterDescriptor>(),
                BoundProperties = Array.Empty<ParameterDescriptor>(),
                FilterDescriptors = new List<FilterDescriptor>(),
                ControllerTypeInfo = typeof(TestActions).GetTypeInfo(),
            };

            foreach (var filterAttribute in method.GetCustomAttributes().OfType<IFilterMetadata>())
            {
                actionDescriptor.FilterDescriptors.Add(new FilterDescriptor(filterAttribute, FilterScope.Action));
            }

            return actionDescriptor;
        }

        private static ControllerActionInvokerProvider CreateInvokerProvider(MvcOptions mvcOptions = null)
        {
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var modelBinderFactory = TestModelBinderFactory.CreateDefault();
            mvcOptions ??= new MvcOptions();

            var parameterBinder = new ParameterBinder(
                modelMetadataProvider,
                TestModelBinderFactory.CreateDefault(),
                Mock.Of<IObjectModelValidator>(),
                Options.Create(mvcOptions),
                NullLoggerFactory.Instance);

            var cache = new ControllerActionInvokerCache(
                Mock.Of<IActionDescriptorCollectionProvider>(c => c.ActionDescriptors == new ActionDescriptorCollection(new List<ActionDescriptor>(), 1)),
                parameterBinder,
                modelBinderFactory,
                modelMetadataProvider,
                new[] { new DefaultFilterProvider() },
                Mock.Of<IControllerFactoryProvider>(),
                Options.Create(mvcOptions));

            return new ControllerActionInvokerProvider(
                cache,
                Options.Create(mvcOptions),
                NullLoggerFactory.Instance,
                new DiagnosticListener("Microsoft.AspNetCore"),
                new ActionResultTypeMapper());
        }

        private class TestActions : Controller
        {
            public IActionResult GetAction() => new OkResult();
        }
    }
}
