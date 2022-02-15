// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Google.Protobuf.Reflection;
using Grpc.AspNetCore.Server;
using Grpc.AspNetCore.Server.Model;
using Grpc.Shared;
using Grpc.Shared.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Grpc.HttpApi.Internal.Binding;

internal sealed partial class HttpApiServiceMethodProvider<TService> : IServiceMethodProvider<TService> where TService : class
{
    private readonly ILogger<HttpApiServiceMethodProvider<TService>> _logger;
    private readonly GrpcServiceOptions _globalOptions;
    private readonly GrpcServiceOptions<TService> _serviceOptions;
    private readonly GrpcHttpApiOptions _httpApiOptions;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IGrpcServiceActivator<TService> _serviceActivator;

    public HttpApiServiceMethodProvider(
        ILoggerFactory loggerFactory,
        IOptions<GrpcServiceOptions> globalOptions,
        IOptions<GrpcServiceOptions<TService>> serviceOptions,
        IGrpcServiceActivator<TService> serviceActivator,
        IOptions<GrpcHttpApiOptions> httpApiOptions)
    {
        _logger = loggerFactory.CreateLogger<HttpApiServiceMethodProvider<TService>>();
        _globalOptions = globalOptions.Value;
        _serviceOptions = serviceOptions.Value;
        _httpApiOptions = httpApiOptions.Value;
        _loggerFactory = loggerFactory;
        _serviceActivator = serviceActivator;
    }

    public void OnServiceMethodDiscovery(ServiceMethodProviderContext<TService> context)
    {
        var bindMethodInfo = BindMethodFinder.GetBindMethod(typeof(TService));

        // Invoke BindService(ServiceBinderBase, BaseType)
        if (bindMethodInfo != null)
        {
            // The second parameter is always the service base type
            var serviceParameter = bindMethodInfo.GetParameters()[1];

            ServiceDescriptor? serviceDescriptor = null;
            try
            {
                serviceDescriptor = ServiceDescriptorHelpers.GetServiceDescriptor(bindMethodInfo.DeclaringType!);
            }
            catch (Exception ex)
            {
                Log.ServiceDescriptorError(_logger, typeof(TService), ex);
            }

            if (serviceDescriptor != null)
            {
                var binder = new HttpApiProviderServiceBinder<TService>(
                    context,
                    new ReflectionServiceInvokerResolver<TService>(serviceParameter.ParameterType),
                    serviceDescriptor,
                    _globalOptions,
                    _serviceOptions,
                    _loggerFactory,
                    _serviceActivator,
                    _httpApiOptions);

                try
                {
                    bindMethodInfo.Invoke(null, new object?[] { binder, null });
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error binding gRPC service '{typeof(TService).Name}'.", ex);
                }
            }
        }
        else
        {
            Log.BindMethodNotFound(_logger, typeof(TService));
        }
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Warning, "Could not find bind method for {ServiceType}.", EventName = "BindMethodNotFound")]
        public static partial void BindMethodNotFound(ILogger logger, Type serviceType);

        [LoggerMessage(2, LogLevel.Warning, "Error getting service descriptor for {ServiceType}.", EventName = "ServiceDescriptorError")]
        public static partial void ServiceDescriptorError(ILogger logger, Type serviceType, Exception ex);
    }
}
