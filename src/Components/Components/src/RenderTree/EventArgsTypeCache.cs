// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components.HotReload;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.RenderTree;

internal static class EventArgsTypeCache
{
    private static readonly ConcurrentDictionary<MethodInfo, Type> Cache = new ConcurrentDictionary<MethodInfo, Type>();

    static EventArgsTypeCache()
    {
        if (HotReloadManager.IsSupported)
        {
            HotReloadManager.Default.OnDeltaApplied += Cache.Clear;
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2073", Justification = "This returns the event handler parameter type, which is later used for JSON deserialization of custom browser event args. The return annotation communicates the required members to the trimmer.")]
    [return: DynamicallyAccessedMembers(JsonSerialized)]
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
