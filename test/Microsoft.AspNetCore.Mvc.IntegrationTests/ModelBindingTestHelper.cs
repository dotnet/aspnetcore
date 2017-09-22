// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
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
        public static ModelBindingTestContext GetTestContext(
            Action<HttpRequest> updateRequest = null,
            Action<MvcOptions> updateOptions = null,
            ControllerActionDescriptor actionDescriptor = null)
        {
            var httpContext = GetHttpContext(updateRequest, updateOptions);
            var services = httpContext.RequestServices;
            var options = services.GetRequiredService<IOptions<MvcOptions>>();

            var context = new ModelBindingTestContext()
            {
                ActionDescriptor = actionDescriptor ?? new ControllerActionDescriptor(),
                HttpContext = httpContext,
                MetadataProvider = TestModelMetadataProvider.CreateDefaultProvider(),
                RouteData = new RouteData(),
                ValueProviderFactories = new List<IValueProviderFactory>(options.Value.ValueProviderFactories),
            };

            return context;
        }

        public static ParameterBinder GetParameterBinder(
            MvcOptions options = null,
            IModelBinderProvider binderProvider = null)
        {
            if (options == null)
            {
                var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
                return GetParameterBinder(metadataProvider, binderProvider);
            }
            else
            {
                var metadataProvider = TestModelMetadataProvider.CreateProvider(options.ModelMetadataDetailsProviders);
                return GetParameterBinder(metadataProvider, binderProvider, options);
            }
        }

        public static ParameterBinder GetParameterBinder(
            IModelMetadataProvider metadataProvider,
            IModelBinderProvider binderProvider = null,
            MvcOptions mvcOptions = null)
        {
            var services = GetServices();
            var options = mvcOptions != null
                ? Options.Create(mvcOptions)
                : services.GetRequiredService<IOptions<MvcOptions>>();

            if (binderProvider != null)
            {
                options.Value.ModelBinderProviders.Insert(0, binderProvider);
            }

            return new ParameterBinder(
                metadataProvider,
                GetModelBinderFactory(metadataProvider, options),
                new CompositeModelValidatorProvider(GetModelValidatorProviders(options)));
        }

        public static IModelBinderFactory GetModelBinderFactory(
            IModelMetadataProvider metadataProvider,
            IOptions<MvcOptions> options = null)
        {
            if (options == null)
            {
                options = GetServices().GetRequiredService<IOptions<MvcOptions>>();
            }

            return new ModelBinderFactory(metadataProvider, options);
        }

        public static IObjectModelValidator GetObjectValidator(
            IModelMetadataProvider metadataProvider,
            IOptions<MvcOptions> options = null)
        {
            return new DefaultObjectValidator(
                metadataProvider,
                GetModelValidatorProviders(options));
        }

        private static IList<IModelValidatorProvider> GetModelValidatorProviders(IOptions<MvcOptions> options)
        {
            if (options == null)
            {
                return TestModelValidatorProvider.CreateDefaultProvider().ValidatorProviders;
            }
            else
            {
                return options.Value.ModelValidatorProviders;
            }
        }

        private static HttpContext GetHttpContext(
            Action<HttpRequest> updateRequest = null,
            Action<MvcOptions> updateOptions = null)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Features.Set<IHttpRequestLifetimeFeature>(new CancellableRequestLifetimeFeature());

            updateRequest?.Invoke(httpContext.Request);

            httpContext.RequestServices = GetServices(updateOptions);
            return httpContext;
        }

        private static IServiceProvider GetServices(Action<MvcOptions> updateOptions = null)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddAuthorization();
            serviceCollection.AddSingleton(new ApplicationPartManager());
            serviceCollection.AddMvc();
            serviceCollection
                .AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>()
                .AddTransient<ILoggerFactory, LoggerFactory>()
                .AddTransient<ILogger<DefaultAuthorizationService>, Logger<DefaultAuthorizationService>>();

            if (updateOptions != null)
            {
                serviceCollection.Configure(updateOptions);
            }

            return serviceCollection.BuildServiceProvider();
        }

        private class CancellableRequestLifetimeFeature : IHttpRequestLifetimeFeature
        {
            private readonly CancellationTokenSource _cts = new CancellationTokenSource();

            public CancellationToken RequestAborted { get => _cts.Token; set => throw new NotImplementedException(); }

            public void Abort()
            {
                _cts.Cancel();
            }
        }
    }
}
