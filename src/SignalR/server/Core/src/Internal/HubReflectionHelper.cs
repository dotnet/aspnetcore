// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.SignalR.Internal;

internal static class HubReflectionHelper
{
    private static readonly Type[] _excludeInterfaces = new[] { typeof(IDisposable) };

    public static IEnumerable<MethodInfo> GetHubMethods(Type hubType)
    {
        var methods = hubType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        var allInterfaceMethods = _excludeInterfaces.SelectMany(i => GetInterfaceMethods(hubType, i));

        return methods.Except(allInterfaceMethods).Where(IsHubMethod);
    }

    private static IEnumerable<MethodInfo> GetInterfaceMethods(Type type, Type iface)
    {
        if (!iface.IsAssignableFrom(type))
        {
            return Enumerable.Empty<MethodInfo>();
        }

        return type.GetInterfaceMap(iface).TargetMethods;
    }

    private static bool IsHubMethod(MethodInfo methodInfo)
    {
        var baseDefinition = methodInfo.GetBaseDefinition().DeclaringType!;
        if (typeof(object) == baseDefinition || methodInfo.IsSpecialName)
        {
            return false;
        }

        var baseType = baseDefinition.IsGenericType ? baseDefinition.GetGenericTypeDefinition() : baseDefinition;
        return typeof(Hub) != baseType;
    }
}
