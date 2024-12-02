// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Google.Api;
using Google.Protobuf.Reflection;
using Grpc.AspNetCore.Server;
using Grpc.AspNetCore.Server.Model;
using Grpc.Core;
using Grpc.Shared;
using Grpc.Shared.Server;
using Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.CallHandlers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Logging;
using MethodOptions = global::Grpc.Shared.Server.MethodOptions;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Binding;

internal sealed partial class JsonTranscodingProviderServiceBinder<TService> : ServiceBinderBase where TService : class
{
    private delegate (RequestDelegate RequestDelegate, List<object> Metadata) CreateRequestDelegate<TRequest, TResponse>(
        Method<TRequest, TResponse> method,
        string httpVerb,
        HttpRule httpRule,
        MethodDescriptor methodDescriptor,
        CallHandlerDescriptorInfo descriptorInfo,
        MethodOptions methodOptions);

    private readonly ServiceMethodProviderContext<TService> _context;
    private readonly IServiceInvokerResolver<TService> _invokerResolver;
    private readonly ServiceDescriptor _serviceDescriptor;
    private readonly GrpcServiceOptions _globalOptions;
    private readonly GrpcServiceOptions<TService> _serviceOptions;
    private readonly IGrpcServiceActivator<TService> _serviceActivator;
    private readonly GrpcJsonTranscodingOptions _jsonTranscodingOptions;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;

    internal JsonTranscodingProviderServiceBinder(
        ServiceMethodProviderContext<TService> context,
        IServiceInvokerResolver<TService> invokerResolver,
        ServiceDescriptor serviceDescriptor,
        GrpcServiceOptions globalOptions,
        GrpcServiceOptions<TService> serviceOptions,
        ILoggerFactory loggerFactory,
        IGrpcServiceActivator<TService> serviceActivator,
        GrpcJsonTranscodingOptions jsonTranscodingOptions)
    {
        _context = context;
        _invokerResolver = invokerResolver;
        _serviceDescriptor = serviceDescriptor;
        _globalOptions = globalOptions;
        _serviceOptions = serviceOptions;
        _serviceActivator = serviceActivator;
        _jsonTranscodingOptions = jsonTranscodingOptions;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<JsonTranscodingProviderServiceBinder<TService>>();
    }

    public override void AddMethod<TRequest, TResponse>(Method<TRequest, TResponse> method, ClientStreamingServerMethod<TRequest, TResponse>? handler)
    {
        if (TryGetMethodDescriptor(method.Name, out var methodDescriptor) &&
            ServiceDescriptorHelpers.TryGetHttpRule(methodDescriptor, out _))
        {
            Log.StreamingMethodNotSupported(_logger, method.Name, method.ServiceName);
        }
    }

    public override void AddMethod<TRequest, TResponse>(Method<TRequest, TResponse> method, DuplexStreamingServerMethod<TRequest, TResponse>? handler)
    {
        if (TryGetMethodDescriptor(method.Name, out var methodDescriptor) &&
            ServiceDescriptorHelpers.TryGetHttpRule(methodDescriptor, out _))
        {
            Log.StreamingMethodNotSupported(_logger, method.Name, method.ServiceName);
        }
    }

    public override void AddMethod<TRequest, TResponse>(Method<TRequest, TResponse> method, ServerStreamingServerMethod<TRequest, TResponse>? handler)
    {
        if (TryGetMethodDescriptor(method.Name, out var methodDescriptor))
        {
            if (ServiceDescriptorHelpers.TryGetHttpRule(methodDescriptor, out var httpRule))
            {
                LogMethodHttpRule(method, httpRule);
                ProcessHttpRule(method, methodDescriptor, httpRule, CreateServerStreamingRequestDelegate);
            }
            else
            {
                // Consider setting to enable mapping to methods without HttpRule
                // AddMethodCore(method, method.FullName, "GET", string.Empty, string.Empty, methodDescriptor);
            }
        }
        else
        {
            Log.MethodDescriptorNotFound(_logger, method.Name, typeof(TService));
        }
    }

    public override void AddMethod<TRequest, TResponse>(Method<TRequest, TResponse> method, UnaryServerMethod<TRequest, TResponse>? handler)
    {
        if (TryGetMethodDescriptor(method.Name, out var methodDescriptor))
        {
            if (ServiceDescriptorHelpers.TryGetHttpRule(methodDescriptor, out var httpRule))
            {
                LogMethodHttpRule(method, httpRule);
                ProcessHttpRule(method, methodDescriptor, httpRule, CreateUnaryRequestDelegate);
            }
            else
            {
                // Consider setting to enable mapping to methods without HttpRule
                // AddMethodCore(method, method.FullName, "GET", string.Empty, string.Empty, methodDescriptor);
            }
        }
        else
        {
            Log.MethodDescriptorNotFound(_logger, method.Name, typeof(TService));
        }
    }

    private void LogMethodHttpRule(IMethod method, HttpRule httpRule)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            Log.HttpRuleFound(_logger, method.Name, method.ServiceName, httpRule.ToString());
        }
    }

    private void ProcessHttpRule<TRequest, TResponse>(
        Method<TRequest, TResponse> method,
        MethodDescriptor methodDescriptor,
        HttpRule httpRule,
        CreateRequestDelegate<TRequest, TResponse> createRequestDelegate)
        where TRequest : class
        where TResponse : class
    {
        if (ServiceDescriptorHelpers.TryResolvePattern(httpRule, out var pattern, out var httpVerb))
        {
            AddMethodCore(method, httpRule, pattern, httpVerb, httpRule.Body, httpRule.ResponseBody, methodDescriptor, createRequestDelegate);
        }

        foreach (var additionalRule in httpRule.AdditionalBindings)
        {
            ProcessHttpRule(method, methodDescriptor, additionalRule, createRequestDelegate);
        }
    }

    private (RequestDelegate RequestDelegate, List<object> Metadata) CreateUnaryRequestDelegate<TRequest, TResponse>(
        Method<TRequest, TResponse> method,
        string httpVerb,
        HttpRule httpRule,
        MethodDescriptor methodDescriptor,
        CallHandlerDescriptorInfo descriptorInfo,
        MethodOptions methodOptions)
        where TRequest : class
        where TResponse : class
    {
        var (invoker, metadata) = _invokerResolver.CreateModelCore<UnaryServerMethod<TService, TRequest, TResponse>>(
            method.Name,
            new[] { typeof(TRequest), typeof(ServerCallContext) },
            httpVerb,
            httpRule,
            methodDescriptor);

        var methodInvoker = new UnaryServerMethodInvoker<TService, TRequest, TResponse>(invoker, method, methodOptions, _serviceActivator);
        var callHandler = new UnaryServerCallHandler<TService, TRequest, TResponse>(
            methodInvoker,
            _loggerFactory,
            descriptorInfo,
            _jsonTranscodingOptions.UnarySerializerOptions);

        return (callHandler.HandleCallAsync, metadata);
    }

    private (RequestDelegate RequestDelegate, List<object> Metadata) CreateServerStreamingRequestDelegate<TRequest, TResponse>(
        Method<TRequest, TResponse> method,
        string httpVerb,
        HttpRule httpRule,
        MethodDescriptor methodDescriptor,
        CallHandlerDescriptorInfo descriptorInfo,
        MethodOptions methodOptions)
        where TRequest : class
        where TResponse : class
    {
        var (invoker, metadata) = _invokerResolver.CreateModelCore<ServerStreamingServerMethod<TService, TRequest, TResponse>>(
            method.Name,
            new[] { typeof(TRequest), typeof(IServerStreamWriter<TResponse>), typeof(ServerCallContext) },
            httpVerb,
            httpRule,
            methodDescriptor);

        var methodInvoker = new ServerStreamingServerMethodInvoker<TService, TRequest, TResponse>(invoker, method, methodOptions, _serviceActivator);
        var callHandler = new ServerStreamingServerCallHandler<TService, TRequest, TResponse>(
            methodInvoker,
            _loggerFactory,
            descriptorInfo,
            _jsonTranscodingOptions.ServerStreamingSerializerOptions);

        return (callHandler.HandleCallAsync, metadata);
    }

    private void AddMethodCore<TRequest, TResponse>(
        Method<TRequest, TResponse> method,
        HttpRule httpRule,
        string pattern,
        string httpVerb,
        string body,
        string responseBody,
        MethodDescriptor methodDescriptor,
        CreateRequestDelegate<TRequest, TResponse> createRequestDelegate)
        where TRequest : class
        where TResponse : class
    {
        try
        {
            var (routePattern, descriptorInfo) = ParseRoute(pattern, body, responseBody, methodDescriptor);
            var methodOptions = MethodOptions.Create(new[] { _globalOptions, _serviceOptions });

            var (requestDelegate, metadata) = createRequestDelegate(method, httpVerb, httpRule, methodDescriptor, descriptorInfo, methodOptions);

            _context.AddMethod<TRequest, TResponse>(method, routePattern, metadata, requestDelegate);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error binding {method.Name} on {typeof(TService).Name} to HTTP API.", ex);
        }
    }

    private static (RoutePattern routePattern, CallHandlerDescriptorInfo descriptorInfo) ParseRoute(string pattern, string body, string responseBody, MethodDescriptor methodDescriptor)
    {
        var httpRoutePattern = HttpRoutePattern.Parse(pattern);
        var adapter = JsonTranscodingRouteAdapter.Parse(httpRoutePattern);

        return (RoutePatternFactory.Parse(adapter.ResolvedRouteTemplate), CreateDescriptorInfo(body, responseBody, methodDescriptor, adapter));
    }

    private static CallHandlerDescriptorInfo CreateDescriptorInfo(string body, string responseBody, MethodDescriptor methodDescriptor, JsonTranscodingRouteAdapter routeAdapter)
    {
        var routeParameterDescriptors = ServiceDescriptorHelpers.ResolveRouteParameterDescriptors(routeAdapter.HttpRoutePattern.Variables, methodDescriptor.InputType);

        var bodyDescriptor = ServiceDescriptorHelpers.ResolveBodyDescriptor(body, typeof(TService), methodDescriptor);
        var responseBodyDescriptor = ServiceDescriptorHelpers.ResolveResponseBodyDescriptor(responseBody, methodDescriptor);

        var descriptorInfo = new CallHandlerDescriptorInfo(
            responseBodyDescriptor,
            bodyDescriptor?.Descriptor,
            bodyDescriptor?.IsDescriptorRepeated ?? false,
            bodyDescriptor?.FieldDescriptor,
            routeParameterDescriptors,
            routeAdapter);
        return descriptorInfo;
    }

    private bool TryGetMethodDescriptor(string methodName, [NotNullWhen(true)]out MethodDescriptor? methodDescriptor)
    {
        for (var i = 0; i < _serviceDescriptor.Methods.Count; i++)
        {
            var method = _serviceDescriptor.Methods[i];
            if (method.Name == methodName)
            {
                methodDescriptor = method;
                return true;
            }
        }

        methodDescriptor = null;
        return false;
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Warning, "Unable to find method descriptor for {MethodName} on {ServiceType}.", EventName = "MethodDescriptorNotFound")]
        public static partial void MethodDescriptorNotFound(ILogger logger, string methodName, Type serviceType);

        [LoggerMessage(2, LogLevel.Warning, "Unable to bind {MethodName} on {ServiceName} to gRPC JSON transcoding. Client and bidirectional streaming methods are not supported.", EventName = "StreamingMethodNotSupported")]
        public static partial void StreamingMethodNotSupported(ILogger logger, string methodName, string serviceName);

        [LoggerMessage(3, LogLevel.Trace, "Found HttpRule mapping. Method {MethodName} on {ServiceName}. HttpRule payload: {HttpRulePayload}", EventName = "HttpRuleFound", SkipEnabledCheck = true)]
        public static partial void HttpRuleFound(ILogger logger, string methodName, string serviceName, string httpRulePayload);
    }
}
