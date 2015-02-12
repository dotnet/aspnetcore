// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Identifies the simple types that the default model binding validation will exclude. 
    /// </summary>
    public class SimpleTypesExcludeFilter : IExcludeTypeValidationFilter
    {
        /// <summary>
        /// Returns true if the given type will be excluded from the default model validation. 
        /// </summary>
        public bool IsTypeExcluded(Type type)
        {
            Type[] actualTypes;

            var enumerable = type.ExtractGenericInterface(typeof(IEnumerable<>));
            if (enumerable == null)
            {
                actualTypes = new Type[] { type };
            }
            else
            {
                actualTypes = enumerable.GenericTypeArguments;
                // The following special case is for IEnumerable<KeyValuePair<K,V>>,
                // supertype of IDictionary<K,V>, and IReadOnlyDictionary<K,V>.
                if (actualTypes.Length == 1
                    && actualTypes[0].IsGenericType() 
                    && actualTypes[0].GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    actualTypes = actualTypes[0].GenericTypeArguments;
                }
            }

            foreach (var actualType in actualTypes)
            {
                var underlyingType = Nullable.GetUnderlyingType(actualType) ?? actualType;
                if (!IsSimpleType(underlyingType))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns true if the given type is the underlying types that <see cref="IsTypeExcluded"/> will exclude.
        /// </summary>
        protected virtual bool IsSimpleType(Type type)
        {
            return TypeHelper.IsSimpleType(type);
        }
    }
}