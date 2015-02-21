// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Framework.Internal;

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
        ///
        /// If the object is already an <see cref="IDictionary{string, object}"/> instance, then a copy
        /// is returned.
        /// </summary>
        /// <remarks>
        /// The implementation of PropertyHelper will cache the property accessors per-type. This is
        /// faster when the the same type is used multiple times with ObjectToDictionary.
        /// </remarks>
        public static IDictionary<string, object> ObjectToDictionary(object value)
        {
            var dictionary = value as IDictionary<string, object>;
            if (dictionary != null)
            {
                return new Dictionary<string, object>(dictionary, StringComparer.OrdinalIgnoreCase);
            }

            dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            if (value != null)
            {
                foreach (var helper in PropertyHelper.GetProperties(value))
                {
                    dictionary[helper.Name] = helper.GetValue(value);
                }
            }

            return dictionary;
        }
    }
}