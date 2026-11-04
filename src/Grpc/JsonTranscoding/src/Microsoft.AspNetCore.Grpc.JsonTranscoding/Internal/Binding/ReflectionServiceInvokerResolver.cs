// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Google.Api;
using Google.Protobuf.Reflection;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Binding;

internal sealed class ReflectionServiceInvokerResolver<TService>
    : IServiceInvokerResolver<TService> where TService : class
{
    private readonly Type _declaringType;

    public ReflectionServiceInvokerResolver(Type declaringType)
    {
        _declaringType = declaringType;
    }

    public (TDelegate invoker, List<object> metadata) CreateModelCore<TDelegate>(
        string methodName,
        Type[] methodParameters,
        string verb,
        HttpRule httpRule,
        MethodDescriptor methodDescriptor) where TDelegate : Delegate
    {
        var handlerMethod = GetMethod(methodName, methodParameters);

        if (handlerMethod == null)
        {
            throw new InvalidOperationException($"Could not find '{methodName}' on {typeof(TService)}.");
        }

        var invoker = (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), handlerMethod);

        var metadata = new List<object>();
        // Add type metadata first so it has a lower priority
        metadata.AddRange(typeof(TService).GetCustomAttributes(inherit: true));
        // Add method metadata last so it has a higher priority
        metadata.AddRange(handlerMethod.GetCustomAttributes(inherit: true));
        metadata.Add(new HttpMethodMetadata(new[] { verb }));

        // Add protobuf service method descriptor.
        // Is used by swagger generation to identify gRPC JSON transcoding APIs.
        metadata.Add(new GrpcJsonTranscodingMetadata(methodDescriptor, httpRule));

        return (invoker, metadata);
    }

    private MethodInfo? GetMethod(string methodName, Type[] methodParameters)
    {
        Type? currentType = typeof(TService);
        while (currentType != null)
        {
            var matchingMethod = currentType.GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: methodParameters,
                modifiers: null);

            if (matchingMethod == null)
            {
                return null;
            }

            // Validate that the method overrides the virtual method on the base service type.
            // If there is a method with the same name it will hide the base method. Ignore it,
            // and continue searching on the base type.
            if (matchingMethod.IsVirtual)
            {
                var baseDefinitionMethod = matchingMethod.GetBaseDefinition();
                if (baseDefinitionMethod != null && baseDefinitionMethod.DeclaringType == _declaringType)
                {
                    return matchingMethod;
                }
            }

            currentType = currentType.BaseType;
        }

        return null;
    }
}
