// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    internal class TypeHelper
    {
        internal static bool IsSimpleType(Type type)
        {
            return type.GetTypeInfo().IsPrimitive ||
                type.Equals(typeof(decimal)) ||
                type.Equals(typeof(string)) ||
                type.Equals(typeof(DateTime)) ||
                type.Equals(typeof(Guid)) ||
                type.Equals(typeof(DateTimeOffset)) ||
                type.Equals(typeof(TimeSpan));
        }

        internal static bool HasStringConverter(Type type)
        {
            return TypeDescriptor.GetConverter(type).CanConvertFrom(typeof(string));
        }

        internal static bool IsCollectionType(Type type)
        {
            if (type == typeof(string))
            {
                // Even though string implements IEnumerable, we don't really think of it
                // as a collection for the purposes of model binding.
                return false;
            }

            // We only need to look for IEnumerable, because IEnumerable<T> extends it.
            return typeof(IEnumerable).IsAssignableFrom(type);
        }
    }
}