// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    internal static class HubReflectionHelper
    {
        private static readonly Type[] _excludeInterfaces = new[] { typeof(IDisposable) };

        public static IEnumerable<MethodInfo> GetHubMethods(Type hubType)
        {
            var methods = hubType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            var allInterfaceMethods = _excludeInterfaces.SelectMany(i => GetInterfaceMethods(hubType, i));

            return methods.Except(allInterfaceMethods).Where(m => IsHubMethod(m));
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
            var baseDefinition = methodInfo.GetBaseDefinition().DeclaringType;
            if (typeof(object) == baseDefinition || methodInfo.IsSpecialName)
            {
                return false;
            }

            var baseType = baseDefinition.GetTypeInfo().IsGenericType ? baseDefinition.GetGenericTypeDefinition() : baseDefinition;
            return typeof(Hub) != baseType;
        }
    }
}
