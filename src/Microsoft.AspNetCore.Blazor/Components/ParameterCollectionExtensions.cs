// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Microsoft.AspNetCore.Blazor.Components
{
    /// <summary>
    /// Extension methods for the <see cref="ParameterCollection"/> type.
    /// </summary>
    public static class ParameterCollectionExtensions
    {
        /// <summary>
        /// Iterates through the <see cref="ParameterCollection"/>, assigning each parameter
        /// to a property of the same name on <paramref name="target"/>.
        /// </summary>
        /// <param name="parameterCollection">The <see cref="ParameterCollection"/>.</param>
        /// <param name="target">An object that has a public writable property matching each parameter's name and type.</param>
        public static void AssignToProperties(
            this ParameterCollection parameterCollection,
            object target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            foreach (var parameter in parameterCollection)
            {
                AssignToProperty(target, parameter);
            }
        }

        private static void AssignToProperty(object target, Parameter parameter)
        {
            // TODO: Don't just use naive reflection like this. Possible ways to make it faster:
            // (a) Create and cache a property-assigning open delegate for each (target type,
            //     property name) pair, e.g., using propertyInfo.GetSetMethod().CreateDelegate(...)
            //     That's much faster than caching the PropertyInfo, at least on JIT-enabled platforms.
            // (b) Or possibly just code-gen an IComponent.SetParameters implementation for each
            //     Razor component. However that might not work well with code-behind inheritance,
            //     because the code-behind wouldn't be able to override it.

            var propertyInfo = GetPropertyInfo(target.GetType(), parameter.Name);
            try
            {
                propertyInfo.SetValue(target, parameter.Value);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Unable to set property '{parameter.Name}' on object of " +
                    $"type '{target.GetType().FullName}'. The error was: {ex.Message}", ex);
            }
        }

        private static PropertyInfo GetPropertyInfo(Type targetType, string propertyName)
        {
            var property = targetType.GetProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Object of type '{targetType.FullName}' does not have a property " +
                    $"matching the name '{propertyName}'.");
            }

            return property;
        }
    }
}
