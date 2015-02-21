// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Rendering
{
    internal class HtmlAttributePropertyHelper : PropertyHelper
    {
        private static readonly ConcurrentDictionary<Type, PropertyHelper[]> ReflectionCache =
            new ConcurrentDictionary<Type, PropertyHelper[]>();

        public static new PropertyHelper[] GetProperties(object instance)
        {
            return GetProperties(instance.GetType(), CreateInstance, ReflectionCache);
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
            get
            {
                return base.Name;
            }

            protected set
            {
                base.Name = value == null ? null : value.Replace('_', '-');
            }
        }
    }
}
