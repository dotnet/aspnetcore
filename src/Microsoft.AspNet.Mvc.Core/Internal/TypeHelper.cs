// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Given an object, adds each instance property with a public get method as a key and its 
        /// associated value to a dictionary.
        /// </summary>
        //
        // The implementation of PropertyHelper will cache the property accessors per-type. This is
        // faster when the the same type is used multiple times with ObjectToDictionary.
        public static IDictionary<string, object> ObjectToDictionary(object value)
        {
            var dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            if (value != null)
            {
                foreach (var helper in PropertyHelper.GetProperties(value))
                {
                    dictionary.Add(helper.Name, helper.GetValue(value));
                }
            }

            return dictionary;
        }
    }
}