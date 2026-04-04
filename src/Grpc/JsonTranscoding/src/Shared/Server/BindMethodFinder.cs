#region Copyright notice and license

// Copyright 2019 The gRPC Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Grpc.Core;

namespace Grpc.Shared.Server;

internal static class BindMethodFinder
{
    private const BindingFlags BindMethodBindingFlags = BindingFlags.Public | BindingFlags.Static;

    internal static MethodInfo? GetBindMethod(Type serviceType)
    {
        // Prefer finding the bind method using attribute on the generated service
        var bindMethodInfo = GetBindMethodUsingAttribute(serviceType);

        if (bindMethodInfo == null)
        {
            // Fallback to searching for bind method using known type hierarchy that Grpc.Tools generates
            bindMethodInfo = GetBindMethodFallback(serviceType);
        }

        return bindMethodInfo;
    }

    internal static MethodInfo? GetBindMethodUsingAttribute(Type serviceType)
    {
        Type? currentServiceType = serviceType;
        BindServiceMethodAttribute? bindServiceMethod;
        do
        {
            // Search through base types for bind service attribute
            // We need to know the base service type because it is used with GetMethod below
            bindServiceMethod = currentServiceType.GetCustomAttribute<BindServiceMethodAttribute>();
            if (bindServiceMethod != null)
            {
                // Bind method will be public and static
                // Two parameters: ServiceBinderBase and the service type
                return bindServiceMethod.BindType.GetMethod(
                    bindServiceMethod.BindMethodName,
                    BindMethodBindingFlags,
                    binder: null,
                    new[] { typeof(ServiceBinderBase), currentServiceType },
                    Array.Empty<ParameterModifier>());
            }
        } while ((currentServiceType = currentServiceType.BaseType) != null);

        return null;
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
        Justification = "Fallback doesn't have BindServiceMethodAttribute so can't be verified.")]
    internal static MethodInfo? GetBindMethodFallback(Type serviceType)
    {
        // Search for the generated service base class
        var baseType = GetServiceBaseType(serviceType);
        if (baseType == null)
        {
            return null;
        }

        // We need to call Foo.BindService from the declaring type.
        var declaringType = baseType.DeclaringType;

        // The method we want to call is public static void BindService(ServiceBinderBase, BaseType)
        return declaringType?.GetMethod(
            "BindService",
            BindMethodBindingFlags,
            binder: null,
            new[] { typeof(ServiceBinderBase), baseType },
            Array.Empty<ParameterModifier>());
    }

    private static Type? GetServiceBaseType(Type serviceImplementation)
    {
        // TService is an implementation of the gRPC service. It ultimately derives from Foo.TServiceBase base class.
        // We need to access the static BindService method on Foo which implicitly derives from Object.
        var baseType = serviceImplementation.BaseType;

        // Handle services that have multiple levels of inheritence
        while (baseType?.BaseType?.BaseType != null)
        {
            baseType = baseType.BaseType;
        }

        return baseType;
    }
}
