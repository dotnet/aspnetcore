// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Reflection;

namespace Microsoft.AspNetCore.Components.RenderTree;

internal static class EventArgsTypeCache
{
    private static readonly ConcurrentDictionary<MethodInfo, Type> Cache = new ConcurrentDictionary<MethodInfo, Type>();

    public static Type GetEventArgsType(MethodInfo methodInfo)
    {
        return Cache.GetOrAdd(methodInfo, methodInfo =>
        {
            var parameterInfos = methodInfo.GetParameters();
            if (parameterInfos.Length == 0)
            {
                return typeof(EventArgs);
            }
            else if (parameterInfos.Length > 1)
            {
                throw new InvalidOperationException($"The method {methodInfo} cannot be used as an event handler because it declares more than one parameter.");
            }
            else
            {
                var declaredType = parameterInfos[0].ParameterType;
                if (typeof(EventArgs).IsAssignableFrom(declaredType))
                {
                    return declaredType;
                }
                else
                {
                    throw new InvalidOperationException($"The event handler parameter type {declaredType.FullName} for event must inherit from {typeof(EventArgs).FullName}.");
                }
            }
        });
    }
}
