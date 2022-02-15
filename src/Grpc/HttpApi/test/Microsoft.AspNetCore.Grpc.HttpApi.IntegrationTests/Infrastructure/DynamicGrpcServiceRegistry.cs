// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Google.Api;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Grpc.AspNetCore.Server;
using Grpc.AspNetCore.Server.Model;
using Grpc.Core;
using IntegrationTestsWebsite.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Grpc.HttpApi.Internal;
using Microsoft.AspNetCore.Grpc.HttpApi.Internal.Binding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Grpc.HttpApi.IntegrationTests.Infrastructure;

/// <summary>
/// Used by tests to add new service methods.
/// </summary>
public class DynamicGrpcServiceRegistry
{
    private readonly DynamicEndpointDataSource _endpointDataSource;
    private readonly IServiceProvider _serviceProvider;

    public DynamicGrpcServiceRegistry(DynamicEndpointDataSource endpointDataSource, IServiceProvider serviceProvider)
    {
        _endpointDataSource = endpointDataSource;
        _serviceProvider = serviceProvider;
    }

    public Method<TRequest, TResponse> AddUnaryMethod<TRequest, TResponse>(UnaryServerMethod<TRequest, TResponse> callHandler, MethodDescriptor methodDescriptor)
        where TRequest : class, IMessage, new()
        where TResponse : class, IMessage, new()
    {
        var method = CreateMethod<TRequest, TResponse>(MethodType.Unary, methodDescriptor.Name);

        AddServiceCore(c =>
        {
            var unaryMethod = new UnaryServerMethod<DynamicService, TRequest, TResponse>((service, request, context) => callHandler(request, context));
            var binder = CreateHttpApiBinder<TRequest, TResponse>(methodDescriptor, c, new DynamicServiceInvokerResolver(unaryMethod));

            binder.AddMethod(method, callHandler);
        });

        return method;
    }

    public Method<TRequest, TResponse> AddServerStreamingMethod<TRequest, TResponse>(ServerStreamingServerMethod<TRequest, TResponse> callHandler, MethodDescriptor methodDescriptor)
        where TRequest : class, IMessage, new()
        where TResponse : class, IMessage, new()
    {
        var method = CreateMethod<TRequest, TResponse>(MethodType.ServerStreaming, methodDescriptor.Name);

        AddServiceCore(c =>
        {
            var serverStreamingMethod = new ServerStreamingServerMethod<DynamicService, TRequest, TResponse>((service, request, stream, context) => callHandler(request, stream, context));
            var binder = CreateHttpApiBinder<TRequest, TResponse>(methodDescriptor, c, new DynamicServiceInvokerResolver(serverStreamingMethod));

            binder.AddMethod(method, callHandler);
        });

        return method;
    }

    private void AddServiceCore(Action<ServiceMethodProviderContext<DynamicService>> action)
    {
        // Set action for adding dynamic method
        var serviceMethodProviders = _serviceProvider.GetServices<IServiceMethodProvider<DynamicService>>().ToList();
        var dynamicServiceModelProvider = serviceMethodProviders.OfType<DynamicServiceModelProvider>().Single();
        dynamicServiceModelProvider.CreateMethod = action;

        // Add to dynamic endpoint route builder
        var routeBuilder = new DynamicEndpointRouteBuilder(_serviceProvider);
        routeBuilder.MapGrpcService<DynamicService>();

        var endpoints = routeBuilder.DataSources.SelectMany(ds => ds.Endpoints).ToList();
        _endpointDataSource.AddEndpoints(endpoints);
    }

    private Method<TRequest, TResponse> CreateMethod<TRequest, TResponse>(MethodType methodType, string methodName)
        where TRequest : class, IMessage, new()
        where TResponse : class, IMessage, new()
    {
        return new Method<TRequest, TResponse>(
            methodType,
            typeof(DynamicService).Name,
            methodName,
            CreateMarshaller<TRequest>(),
            CreateMarshaller<TResponse>());
    }

    private Marshaller<TMessage> CreateMarshaller<TMessage>()
          where TMessage : class, IMessage, new()
    {
        return new Marshaller<TMessage>(
            m => m.ToByteArray(),
            d =>
            {
                var m = new TMessage();
                m.MergeFrom(d);
                return m;
            });
    }

    private class DynamicEndpointRouteBuilder : IEndpointRouteBuilder
    {
        public DynamicEndpointRouteBuilder(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }

        public ICollection<EndpointDataSource> DataSources { get; } = new List<EndpointDataSource>();

        public IApplicationBuilder CreateApplicationBuilder()
        {
            return new ApplicationBuilder(ServiceProvider);
        }
    }

    private HttpApiProviderServiceBinder<DynamicService> CreateHttpApiBinder<TRequest, TResponse>(
        MethodDescriptor methodDescriptor,
        ServiceMethodProviderContext<DynamicService> context,
        DynamicServiceInvokerResolver invokerResolver)
        where TRequest : class, IMessage, new()
        where TResponse : class, IMessage, new()
    {
        var httpApiOptions = _serviceProvider.GetRequiredService<IOptions<GrpcHttpApiOptions>>().Value;
        var binder = new HttpApiProviderServiceBinder<DynamicService>(
            context,
            invokerResolver,
            methodDescriptor.Service,
            _serviceProvider.GetRequiredService<IOptions<GrpcServiceOptions>>().Value,
            _serviceProvider.GetRequiredService<IOptions<GrpcServiceOptions<DynamicService>>>().Value,
            _serviceProvider.GetRequiredService<ILoggerFactory>(),
            _serviceProvider.GetRequiredService<IGrpcServiceActivator<DynamicService>>(),
            httpApiOptions);

        return binder;
    }

    private class DynamicServiceInvokerResolver : IServiceInvokerResolver<DynamicService>
    {
        private readonly Delegate _testDelegate;

        public DynamicServiceInvokerResolver(Delegate testDelegate)
        {
            _testDelegate = testDelegate;
        }

        public (TDelegate invoker, List<object> metadata) CreateModelCore<TDelegate>(
            string methodName,
            Type[] methodParameters,
            string verb,
            HttpRule httpRule,
            MethodDescriptor methodDescriptor) where TDelegate : Delegate
        {
            return ((TDelegate)_testDelegate, new List<object>());
        }
    }
}
