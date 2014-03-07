// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    internal static class TypeHelper
    {
        private static readonly Type TaskGenericType = typeof(Task<>);

        public static Type GetTaskInnerTypeOrNull([NotNull]Type type)
        {
            if (type.GetTypeInfo().IsGenericType && !type.GetTypeInfo().IsGenericTypeDefinition)
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();
                var genericArguments = type.GetGenericArguments();
                if (genericArguments.Length == 1 && TaskGenericType == genericTypeDefinition)
                {
                    // Only Return if there is a single argument.
                    return genericArguments[0];
                }
            }

            return null;
        }
    }
}