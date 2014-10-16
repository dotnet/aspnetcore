// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.DependencyInjection;

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
                .Select(p => services.GetRequiredService(p.ParameterType))
                .ToArray();

            initializeMethod.Invoke(obj, args);
        }

        public static void InjectProperty<TProperty>(
            [NotNull] object obj, 
            [NotNull] string propertyName, 
            TProperty value)
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

        public static void InjectProperty<TProperty>(
            [NotNull] object obj, 
            [NotNull] string propertyName, 
            [NotNull] IServiceProvider services)
        {
            var type = obj.GetType();

            var property = type.GetRuntimeProperty(propertyName);
            if (property == null)
            {
                return;
            }

            if (property.PropertyType.IsAssignableFrom(typeof(TProperty)))
            {
                property.SetValue(obj, services.GetRequiredService<TProperty>());
            }
        }
    }
}
