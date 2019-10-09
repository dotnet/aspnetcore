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
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests
{
    public static class ModelBindingTestHelper
    {
        public static ModelBindingTestContext GetTestContext(
            Action<HttpRequest> updateRequest = null,
            Action<MvcOptions> updateOptions = null,
            ControllerActionDescriptor actionDescriptor = null,
            IModelMetadataProvider metadataProvider = null,
            MvcOptions mvcOptions = null)
        {
            var httpContext = GetHttpContext(metadataProvider, updateRequest, updateOptions, mvcOptions);
            var services = httpContext.RequestServices;
            metadataProvider = metadataProvider ?? services.GetRequiredService<IModelMetadataProvider>();
            var options = services.GetRequiredService<IOptions<MvcOptions>>();

            var context = new ModelBindingTestContext
            {
                ActionDescriptor = actionDescriptor ?? new ControllerActionDescriptor(),
                HttpContext = httpContext,
                MetadataProvider = metadataProvider,
                MvcOptions = options.Value,
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

        public static ParameterBinder GetParameterBinder(ModelBindingTestContext testContext)
        {
            return GetParameterBinder(testContext.HttpContext.RequestServices);
        }

        public static ParameterBinder GetParameterBinder(IServiceProvider serviceProvider)
        {
            var metadataProvider = serviceProvider.GetRequiredService<IModelMetadataProvider>();
            var options = serviceProvider.GetRequiredService<IOptions<MvcOptions>>();

            return new ParameterBinder(
                metadataProvider,
                new ModelBinderFactory(metadataProvider, options, serviceProvider),
                new DefaultObjectValidator(
                    metadataProvider,
                    new[] { new CompositeModelValidatorProvider(GetModelValidatorProviders(options)) },
                    options.Value),
                options,
                NullLoggerFactory.Instance);
        }

        public static ParameterBinder GetParameterBinder(
            IModelMetadataProvider metadataProvider,
            IModelBinderProvider binderProvider = null,
            MvcOptions mvcOptions = null)
        {
            var services = GetServices(metadataProvider, mvcOptions: mvcOptions);
            var options = services.GetRequiredService<IOptions<MvcOptions>>();

            if (binderProvider != null)
            {
                options.Value.ModelBinderProviders.Insert(0, binderProvider);
            }

            return new ParameterBinder(
                metadataProvider,
                new ModelBinderFactory(metadataProvider, options, services),
                new DefaultObjectValidator(
                    metadataProvider,
                    new[] { new CompositeModelValidatorProvider(GetModelValidatorProviders(options)) },
                    options.Value),
                options,
                NullLoggerFactory.Instance);
        }

        public static IModelBinderFactory GetModelBinderFactory(
            IModelMetadataProvider metadataProvider,
            IServiceProvider services = null)
        {
            if (services == null)
            {
                services = GetServices(metadataProvider);
            }

            var options = services.GetRequiredService<IOptions<MvcOptions>>();

            return new ModelBinderFactory(metadataProvider, options, services);
        }

        public static IObjectModelValidator GetObjectValidator(
            IModelMetadataProvider metadataProvider,
            IOptions<MvcOptions> options = null)
        {
            return new DefaultObjectValidator(
                metadataProvider,
                GetModelValidatorProviders(options),
                options?.Value ?? new MvcOptions());
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
            IModelMetadataProvider metadataProvider,
            Action<HttpRequest> updateRequest = null,
            Action<MvcOptions> updateOptions = null,
            MvcOptions mvcOptions = null)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Features.Set<IHttpRequestLifetimeFeature>(new CancellableRequestLifetimeFeature());

            updateRequest?.Invoke(httpContext.Request);

            httpContext.RequestServices = GetServices(metadataProvider, updateOptions, mvcOptions);
            return httpContext;
        }

        public static IServiceProvider GetServices(
            IModelMetadataProvider metadataProvider,
            Action<MvcOptions> updateOptions = null,
            MvcOptions mvcOptions = null)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(new ApplicationPartManager());
            if (metadataProvider != null)
            {
                serviceCollection.AddSingleton(metadataProvider);
            }
            else if (updateOptions != null || mvcOptions != null)
            {
                serviceCollection.AddSingleton(services =>
                {
                    var optionsAccessor = services.GetRequiredService<IOptions<MvcOptions>>();
                    return TestModelMetadataProvider.CreateProvider(optionsAccessor.Value.ModelMetadataDetailsProviders);
                });
            }
            else
            {
                serviceCollection.AddSingleton<IModelMetadataProvider>(services =>
                {
                    return TestModelMetadataProvider.CreateDefaultProvider();
                });
            }

            if (mvcOptions != null)
            {
                serviceCollection.AddSingleton(Options.Create(mvcOptions));
            }

            serviceCollection.AddMvc()
                .AddNewtonsoftJson();
            serviceCollection
                .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
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
