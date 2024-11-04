// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.SignalR.Internal;

internal static class HubReflectionHelper
{
    public static IEnumerable<MethodInfo> GetHubMethods([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type hubType)
    {
        var methods = hubType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        var excludedInterfaceMethods = hubType.GetInterfaceMap(typeof(IDisposable)).TargetMethods;

        return methods.Except(excludedInterfaceMethods).Where(IsHubMethod);
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
