// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
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
            };
        }

        public static ControllerArgumentBinder GetArgumentBinder(
            MvcOptions options = null,
            IModelBinderProvider binderProvider = null)
        {
            if (options == null)
            {
                var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
                return GetArgumentBinder(metadataProvider, binderProvider);
            }
            else
            {
                var metadataProvider = TestModelMetadataProvider.CreateProvider(options.ModelMetadataDetailsProviders);
                return GetArgumentBinder(metadataProvider, binderProvider);
            }
        }

        public static ControllerArgumentBinder GetArgumentBinder(
            IModelMetadataProvider metadataProvider, 
            IModelBinderProvider binderProvider = null)
        {
            var services = GetServices();
            var options = services.GetRequiredService<IOptions<MvcOptions>>();

            if (binderProvider != null)
            {
                options.Value.ModelBinderProviders.Insert(0, binderProvider);
            }

            return new ControllerArgumentBinder(
                metadataProvider,
                new ModelBinderFactory(metadataProvider, options),
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

            httpContext.RequestServices = GetServices(updateOptions);
            return httpContext;
        }

        private static IServiceProvider GetServices(Action<MvcOptions> updateOptions = null)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(new ApplicationPartManager());
            serviceCollection.AddMvc();
            serviceCollection
                .AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>()
                .AddTransient<ILoggerFactory, LoggerFactory>();

            if (updateOptions != null)
            {
                serviceCollection.Configure(updateOptions);
            }

            return serviceCollection.BuildServiceProvider();
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
                ValueProviders = valueProviderFactoryContext.ValueProviders
            };
        }
    }
}
