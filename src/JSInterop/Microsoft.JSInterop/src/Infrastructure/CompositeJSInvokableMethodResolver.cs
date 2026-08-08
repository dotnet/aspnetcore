// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.JSInterop.Infrastructure;

internal sealed class CompositeJSInvokableMethodResolver(IReadOnlyList<IJSInvokableMethodResolver> resolvers)
{
    internal IReadOnlyList<IJSInvokableMethodResolver> Resolvers => resolvers;

    public JSInvokableMethodDescriptor Resolve(in JSInvokableMethodInfo methodInfo)
    {
        foreach (var resolver in resolvers)
        {
            if (resolver.TryResolve(methodInfo, out var descriptor))
            {
                return descriptor;
            }
        }

        return ThrowMethodNotFound(methodInfo);
    }

    [DoesNotReturn]
    private static JSInvokableMethodDescriptor ThrowMethodNotFound(in JSInvokableMethodInfo methodInfo)
    {
        if (methodInfo.IsStatic)
        {
            throw new ArgumentException($"The assembly '{methodInfo.AssemblyName}' does not contain a public invokable method with [{nameof(JSInvokableAttribute)}(\"{methodInfo.Identifier}\")].");
        }

        throw new ArgumentException($"The type '{methodInfo.TargetType!.Name}' does not contain a public invokable method with [{nameof(JSInvokableAttribute)}(\"{methodInfo.Identifier}\")].");
    }
}
