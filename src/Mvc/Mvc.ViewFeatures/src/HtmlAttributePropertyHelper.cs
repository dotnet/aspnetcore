// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
