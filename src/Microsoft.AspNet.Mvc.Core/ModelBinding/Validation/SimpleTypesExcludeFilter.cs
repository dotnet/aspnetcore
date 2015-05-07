// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
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

            if (type.GetTypeInfo().IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                actualTypes = type.GenericTypeArguments;
            }
            else
            {
                actualTypes = new Type[] { type };
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