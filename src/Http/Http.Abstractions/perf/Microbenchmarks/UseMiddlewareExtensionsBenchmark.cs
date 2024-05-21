// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http.Abstractions.Microbenchmarks;

public class UseMiddlewareExtensionsBenchmark
{
    internal const string Key = "benchmark";
    internal const string ReflectionFallbackMethodName = "ReflectionFallback";
    internal const string InvokeMethodName = "Invoke";

    private readonly DummyKeyedServiceProvider _serviceProvider = null!;
    private readonly BenchmarkTestMiddleware _middleware = null!;
    private readonly HttpContext _httpContext = null!;
    private readonly MethodInfo _invokeMethod = null!;
    private readonly ParameterInfo[] _parameters = null!;
    private readonly Func<object, HttpContext, IServiceProvider, Task> _reflectionCallbackfactory = null!;
    private readonly Func<object, HttpContext, IServiceProvider, Task> _reflectionFallbackCachedKeyAndAllParametersFactory = null!;
    private readonly Func<object, HttpContext, IServiceProvider, Task> _reflectionFallbackCachedServiceResolverByParametersFactory = null!;

    public UseMiddlewareExtensionsBenchmark()
    {
        _serviceProvider = new DummyKeyedServiceProvider();
        _serviceProvider.AddKeyedService(Key, typeof(IVehicleFactory), new CarFactory());
        _middleware = new BenchmarkTestMiddleware(null!);
        _httpContext = new DefaultHttpContext() { RequestServices = _serviceProvider };
        _invokeMethod = _middleware.GetType().GetMethod(nameof(_middleware.InvokeAsync))!;
        _parameters = _invokeMethod.GetParameters();
        _reflectionCallbackfactory = BenchMarkTestFactory.ReflectionCallback<object>(_invokeMethod, _parameters);
        _reflectionFallbackCachedKeyAndAllParametersFactory = BenchMarkTestFactory.ReflectionFallbackCachedKeyAndAllParameters<object>(_invokeMethod, _parameters);
        _reflectionFallbackCachedServiceResolverByParametersFactory = BenchMarkTestFactory.ReflectionFallbackCachedServiceResolverByParameters<object>(_invokeMethod, _parameters);
    }

    [Benchmark]
    [IterationCount(10)]
    public void OnReflectionFallBack()
    {
        _reflectionCallbackfactory(_middleware, _httpContext, _serviceProvider);
    }

    [Benchmark]
    [IterationCount(10)]
    public void OnReflectionFallbackCachedKeyAndAllParameters()
    {
        _reflectionFallbackCachedKeyAndAllParametersFactory(_middleware, _httpContext, _serviceProvider);
    }

    [Benchmark]
    [IterationCount(10)]
    public void OnReflectionFallbackCachedServiceResolverByParameters()
    {
        _reflectionFallbackCachedServiceResolverByParametersFactory(_middleware, _httpContext, _serviceProvider);
    }

    private class BenchmarkTestMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context, [FromKeyedServices("benchmark")] IVehicleFactory benchmarkTestFactory)
        {
            if (next != null)
            {
                await next(context);
            }
        }
    }
    private interface IVehicleFactory { }
    private class CarFactory : IVehicleFactory { }

    private class DummyKeyedServiceProvider : IKeyedServiceProvider
    {
        private readonly Dictionary<object, Tuple<Type, object>> _services = new Dictionary<object, Tuple<Type, object>>();

        public DummyKeyedServiceProvider() { }

        public void AddKeyedService(object key, Type type, object value) => _services[key] = new Tuple<Type, object>(type, value);

        public object? GetKeyedService(Type serviceType, object? serviceKey)
        {
            if (_services.TryGetValue(serviceKey!, out var value))
            {
                return value.Item2;
            }

            return null;
        }

        public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
        {
            var service = GetKeyedService(serviceType, serviceKey);

            if (service == null)
            {
                throw new InvalidOperationException(Resources.FormatException_NoServiceRegistered(serviceType));
            }

            return service;
        }

        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IServiceProvider))
            {
                return this;
            }

            if (_services.TryGetValue(serviceType, out var value))
            {
                return value;
            }
            return null;
        }
    }

    private static class BenchMarkTestFactory
    {
        private static bool TryGetServiceKey(ParameterInfo parameterInfo, [NotNullWhen(true)] out object? key)
        {
            key = parameterInfo.GetCustomAttribute<FromKeyedServicesAttribute>(false)?.Key;

            return key != null;
        }

        private static void ParameterTypeIsByRefGuard(ParameterInfo[] parameters)
        {
            for (var i = 1; i < parameters.Length; i++)
            {
                var parameterType = parameters[i].ParameterType;
                if (parameterType.IsByRef)
                {
                    throw new NotSupportedException(Resources.FormatException_InvokeDoesNotSupportRefOrOutParams(InvokeMethodName));
                }
            }
        }

        private static object GetService(IServiceProvider sp, Type type, Type middleware)
        {
            var service = sp.GetService(type) ?? throw new InvalidOperationException(Resources.FormatException_InvokeMiddlewareNoService(type, middleware));

            return service;
        }

        private static object GetKeyedService(IServiceProvider sp, object key, Type type, Type middleware)
        {
            if (sp is IKeyedServiceProvider ksp)
            {
                var service = ksp.GetKeyedService(type, key) ?? throw new InvalidOperationException(Resources.FormatException_InvokeMiddlewareNoService(type, middleware));

                return service;
            }

            throw new InvalidOperationException(Resources.Exception_KeyedServicesNotSupported);
        }

        private static Func<IServiceProvider, object> GetServiceDelegate(Type parameterType, Type declaringType)
            => sp => GetService(sp, parameterType, declaringType);

        private static Func<IServiceProvider, object> GetKeyedServiceDelegate(object key, Type parameterType, Type declaringType)
            => sp => GetKeyedService(sp, key, parameterType, declaringType);

        public static Func<T, HttpContext, IServiceProvider, Task> ReflectionCallback<T>(MethodInfo methodInfo, ParameterInfo[] parameters)
        {
            var reflectionFallbackMethodInfo = typeof(UseMiddlewareExtensions).GetMethod(ReflectionFallbackMethodName, BindingFlags.Static | BindingFlags.NonPublic)!.MakeGenericMethod(typeof(object));

            return (Func<T, HttpContext, IServiceProvider, Task>)reflectionFallbackMethodInfo.Invoke(null, [methodInfo, parameters])!;
        }

        public static Func<T, HttpContext, IServiceProvider, Task> ReflectionFallbackCachedKeyAndAllParameters<T>(MethodInfo methodInfo, ParameterInfo[] parameters)
        {
            Debug.Assert(!RuntimeFeature.IsDynamicCodeSupported, "Use reflection fallback when dynamic code is not supported.");

            ParameterTypeIsByRefGuard(parameters);

            // Performance optimization: Precompute and cache the results for each parameter
            var parameterData = new (object? key, Type parameterType, Type declaringType)[parameters.Length];
            for (var i = 1; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                _ = TryGetServiceKey(parameter, out object? key);
                parameterData[i] = (key, parameter.ParameterType, methodInfo.DeclaringType!);
            }

            return (middleware, context, serviceProvider) =>
            {
                var methodArguments = new object[parameters.Length];
                methodArguments[0] = context;
                for (var i = 1; i < parameters.Length; i++)
                {
                    var (key, parameterType, declaringType) = parameterData[i];

                    methodArguments[i] = key == null ? GetService(serviceProvider, parameterType, declaringType) : GetKeyedService(serviceProvider, key, parameterType, declaringType);
                }

                return (Task)methodInfo.Invoke(middleware, BindingFlags.DoNotWrapExceptions, binder: null, methodArguments, culture: null)!;
            };
        }        

        public static Func<T, HttpContext, IServiceProvider, Task> ReflectionFallbackCachedServiceResolverByParameters<T>(MethodInfo methodInfo, ParameterInfo[] parameters)
        {
            Debug.Assert(!RuntimeFeature.IsDynamicCodeSupported, "Use reflection fallback when dynamic code is not supported.");

            ParameterTypeIsByRefGuard(parameters);

            // Performance optimization: Precompute and cache the results for each parameter
            var serviceResolvers = new Func<IServiceProvider, object>[parameters.Length];
            for (var i = 1; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                _ = TryGetServiceKey(parameter, out object? key);
                serviceResolvers[i] = key == null
                    ? GetServiceDelegate(parameter.ParameterType, methodInfo.DeclaringType!)
                    : GetKeyedServiceDelegate(key, parameter.ParameterType, methodInfo.DeclaringType!);
            }

            return (middleware, context, serviceProvider) =>
            {
                var methodArguments = new object[parameters.Length];
                methodArguments[0] = context;
                for (var i = 1; i < parameters.Length; i++)
                {
                    methodArguments[i] = serviceResolvers[i](serviceProvider);
                }

                return (Task)methodInfo.Invoke(middleware, BindingFlags.DoNotWrapExceptions, binder: null, methodArguments, culture: null)!;
            };
        }
    }
}
