// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests
{
    public static class ModelBindingTestHelper
    {
        public static OperationBindingContext GetOperationBindingContext(
            Action<HttpRequest> updateRequest = null,
            Action<MvcOptions> updateOptions = null,
            ControllerActionDescriptor actionDescriptor = null)
        {
            var httpContext = GetHttpContext(updateRequest, updateOptions);
            var services = httpContext.RequestServices;

            actionDescriptor = actionDescriptor ?? new ControllerActionDescriptor();

            var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);
            var controllerContext = GetControllerContext(
                services.GetRequiredService<IOptions<MvcOptions>>().Value,
                actionContext);

            return new OperationBindingContext()
            {
                ActionContext = controllerContext,
                InputFormatters = controllerContext.InputFormatters,
                MetadataProvider = TestModelMetadataProvider.CreateDefaultProvider(),
                ValidatorProvider = new CompositeModelValidatorProvider(controllerContext.ValidatorProviders),
                ValueProvider = new CompositeValueProvider(controllerContext.ValueProviders),
                ModelBinder = new CompositeModelBinder(controllerContext.ModelBinders),
            };
        }

        public static ControllerArgumentBinder GetArgumentBinder(MvcOptions options = null)
        {
            if (options == null)
            {
                var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
                return GetArgumentBinder(metadataProvider);
            }
            else
            {
                var metadataProvider = TestModelMetadataProvider.CreateProvider(options.ModelMetadataDetailsProviders);
                return GetArgumentBinder(metadataProvider);
            }
        }

        public static ControllerArgumentBinder GetArgumentBinder(IModelMetadataProvider metadataProvider)
        {
            return new ControllerArgumentBinder(
                metadataProvider,
                GetObjectValidator(metadataProvider));
        }

        public static IObjectModelValidator GetObjectValidator(IModelMetadataProvider metadataProvider)
        {
            return new DefaultObjectValidator(metadataProvider, new ValidatorCache());
        }

        private static HttpContext GetHttpContext(
            Action<HttpRequest> updateRequest = null,
            Action<MvcOptions> updateOptions = null)
        {
            var httpContext = new DefaultHttpContext();

            if (updateRequest != null)
            {
                updateRequest(httpContext.Request);
            }

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddMvc();
            serviceCollection
                .AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>()
                .AddTransient<ILoggerFactory, LoggerFactory>();

            if (updateOptions != null)
            {
                serviceCollection.Configure(updateOptions);
            }

            httpContext.RequestServices = serviceCollection.BuildServiceProvider();
            return httpContext;
        }

        private static ControllerContext GetControllerContext(MvcOptions options, ActionContext context)
        {
            var valueProviderFactoryContext = new ValueProviderFactoryContext(context);
            foreach (var factory in options.ValueProviderFactories)
            {
                factory.CreateValueProviderAsync(valueProviderFactoryContext).GetAwaiter().GetResult();
            }

            return new ControllerContext(context)
            {
                InputFormatters = options.InputFormatters,
                ValidatorProviders = options.ModelValidatorProviders,
                ModelBinders = options.ModelBinders,
                ValueProviders = valueProviderFactoryContext.ValueProviders
            };
        }
    }
}
