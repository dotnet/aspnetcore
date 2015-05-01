// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc.IntegrationTests
{
    public static class ModelBindingTestHelper
    {
        public static HttpContext GetHttpContext(
            Action<HttpRequest> updateRequest = null,
            Action<MvcOptions> updateOptions = null)
        {
            var httpContext = new DefaultHttpContext();

            if (updateRequest != null)
            {
                updateRequest(httpContext.Request);
            }

            InitializeServices(httpContext, updateOptions);
            return httpContext;
        }

        public static OperationBindingContext GetOperationBindingContext(
            Action<HttpRequest> updateRequest = null,
            Action<MvcOptions> updateOptions = null)
        {
            var httpContext = GetHttpContext(updateRequest, updateOptions);

            var services = httpContext.RequestServices;
            var actionBindingContext = services.GetRequiredService<IScopedInstance<ActionBindingContext>>().Value;

            return new OperationBindingContext()
            {
                BodyBindingState = BodyBindingState.NotBodyBased,
                HttpContext = httpContext,
                MetadataProvider = TestModelMetadataProvider.CreateDefaultProvider(),
                ValidatorProvider = actionBindingContext.ValidatorProvider,
                ValueProvider = actionBindingContext.ValueProvider,
                ModelBinder = actionBindingContext.ModelBinder
            };
        }

        public static DefaultControllerActionArgumentBinder GetArgumentBinder()
        {
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            return new DefaultControllerActionArgumentBinder(
                metadataProvider,
                GetObjectValidator());
        }

        public static IObjectModelValidator GetObjectValidator()
        {
            var options = new TestMvcOptions();
            options.Options.MaxModelValidationErrors = 5;
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            return new DefaultObjectValidator(
                    options.Options.ValidationExcludeFilters,
                    metadataProvider);
        }

        private static void InitializeServices(HttpContext httpContext, Action<MvcOptions> updateOptions = null)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddMvc();

            httpContext.RequestServices = serviceCollection.BuildServiceProvider();

            var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor());

            var actionContextAccessor =
                httpContext.RequestServices.GetRequiredService<IScopedInstance<ActionContext>>();
            actionContextAccessor.Value = actionContext;

            var options = new TestMvcOptions().Options;
            if (updateOptions != null)
            {
                updateOptions(options);
            }

            var actionBindingContextAccessor =
                httpContext.RequestServices.GetRequiredService<IScopedInstance<ActionBindingContext>>();
            actionBindingContextAccessor.Value = GetActionBindingContext(options, actionContext);
        }

        private static ActionBindingContext GetActionBindingContext(MvcOptions options, ActionContext actionContext)
        {
            var valueProviderFactoryContext = new ValueProviderFactoryContext(
                actionContext.HttpContext,
                actionContext.RouteData.Values);

            var valueProvider = CompositeValueProvider.Create(
                options.ValueProviderFactories,
                valueProviderFactoryContext);

            return new ActionBindingContext()
            {
                InputFormatters = options.InputFormatters,
                OutputFormatters = options.OutputFormatters, // Not required for model binding.
                ValidatorProvider = new TestModelValidatorProvider(options.ModelValidatorProviders),
                ModelBinder = new CompositeModelBinder(options.ModelBinders),
                ValueProvider = valueProvider
            };
        }
    }
}
