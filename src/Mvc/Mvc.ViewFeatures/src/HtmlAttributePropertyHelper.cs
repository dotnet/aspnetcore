// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    internal class HtmlAttributePropertyHelper : PropertyHelper
    {
        private static readonly ConcurrentDictionary<Type, PropertyHelper[]> ReflectionCache =
            new ConcurrentDictionary<Type, PropertyHelper[]>();

        public static new PropertyHelper[] GetProperties(Type type)
        {
            return GetProperties(type, CreateInstance, ReflectionCache);
        }

        private static PropertyHelper CreateInstance(PropertyInfo property)
        {
            return new HtmlAttributePropertyHelper(property);
        }

        public HtmlAttributePropertyHelper(PropertyInfo property)
            : base(property)
        {
        }

        public override string Name
        {
            get => base.Name;

            protected set
            {
                base.Name = value == null ? null : value.Replace('_', '-');
            }
        }
    }
}
