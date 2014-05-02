// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Microsoft.AspNet.Mvc.Rendering
{
    internal class HtmlAttributePropertyHelper : PropertyHelper
    {
        private static readonly ConcurrentDictionary<Type, PropertyHelper[]> ReflectionCache = 
            new ConcurrentDictionary<Type, PropertyHelper[]>();

        public static new PropertyHelper[] GetProperties(object instance)
        {
            return GetProperties(instance, CreateInstance, ReflectionCache);
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
