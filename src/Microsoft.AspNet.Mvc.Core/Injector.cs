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
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    public static class Injector
    {
        public static void CallInitializer([NotNull] object obj, [NotNull] IServiceProvider services)
        {
            var type = obj.GetType();

            var initializeMethod = 
                type.GetRuntimeMethods()
                .FirstOrDefault(m => m.Name.Equals("Initialize", StringComparison.OrdinalIgnoreCase));

            if (initializeMethod == null)
            {
                return;
            }

            var args = 
                initializeMethod.GetParameters()
                .Select(p => services.GetService(p.ParameterType))
                .ToArray();

            initializeMethod.Invoke(obj, args);
        }

        public static void InjectProperty<TProperty>([NotNull] object obj, [NotNull] string propertyName, TProperty value)
        {
            var type = obj.GetType();

            var property = type.GetRuntimeProperty(propertyName);
            if (property == null)
            {
                return;
            }

            if (property.PropertyType.IsAssignableFrom(typeof(TProperty)))
            {
                property.SetValue(obj, value);
            }
        }

        public static void InjectProperty<TProperty>([NotNull] object obj, [NotNull] string propertyName, [NotNull] IServiceProvider services)
        {
            var type = obj.GetType();

            var property = type.GetRuntimeProperty(propertyName);
            if (property == null)
            {
                return;
            }

            if (property.PropertyType.IsAssignableFrom(typeof(TProperty)))
            {
                property.SetValue(obj, services.GetService<TProperty>());
            }
        }
    }
}
