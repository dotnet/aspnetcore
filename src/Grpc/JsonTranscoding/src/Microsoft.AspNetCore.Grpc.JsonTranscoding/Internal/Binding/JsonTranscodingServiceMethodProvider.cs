// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Google.Protobuf.Reflection;
using Grpc.AspNetCore.Server;
using Grpc.AspNetCore.Server.Model;
using Grpc.Shared;
using Grpc.Shared.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Binding;

internal sealed partial class JsonTranscodingServiceMethodProvider<TService> : IServiceMethodProvider<TService> where TService : class
{
    private readonly ILogger<JsonTranscodingServiceMethodProvider<TService>> _logger;
    private readonly GrpcServiceOptions _globalOptions;
    private readonly GrpcServiceOptions<TService> _serviceOptions;
    private readonly GrpcJsonTranscodingOptions _jsonTranscodingOptions;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IGrpcServiceActivator<TService> _serviceActivator;
    private readonly DescriptorRegistry _serviceDescriptorRegistry;

    public JsonTranscodingServiceMethodProvider(
        ILoggerFactory loggerFactory,
        IOptions<GrpcServiceOptions> globalOptions,
        IOptions<GrpcServiceOptions<TService>> serviceOptions,
        IGrpcServiceActivator<TService> serviceActivator,
        IOptions<GrpcJsonTranscodingOptions> jsonTranscodingOptions,
        DescriptorRegistry serviceDescriptorRegistry)
    {
        _logger = loggerFactory.CreateLogger<JsonTranscodingServiceMethodProvider<TService>>();
        _globalOptions = globalOptions.Value;
        _serviceOptions = serviceOptions.Value;
        _jsonTranscodingOptions = jsonTranscodingOptions.Value;
        _loggerFactory = loggerFactory;
        _serviceActivator = serviceActivator;
        _serviceDescriptorRegistry = serviceDescriptorRegistry;
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
                _serviceDescriptorRegistry.RegisterFileDescriptor(serviceDescriptor.File);

                var binder = new JsonTranscodingProviderServiceBinder<TService>(
                    context,
                    new ReflectionServiceInvokerResolver<TService>(serviceParameter.ParameterType),
                    serviceDescriptor,
                    _globalOptions,
                    _serviceOptions,
                    _loggerFactory,
                    _serviceActivator,
                    _jsonTranscodingOptions);

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
