// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.Reflection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Extension methods for the <see cref="ParameterCollection"/> type.
    /// </summary>
    public static class ParameterCollectionExtensions
    {
        private const BindingFlags _bindablePropertyFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase;

        private readonly static IDictionary<Type, IDictionary<string, IPropertySetter>> _cachedParameterWriters = new ConcurrentDictionary<Type, IDictionary<string, IPropertySetter>>();

        /// <summary>
        /// Iterates through the <see cref="ParameterCollection"/>, assigning each parameter
        /// to a property of the same name on <paramref name="target"/>.
        /// </summary>
        /// <param name="parameterCollection">The <see cref="ParameterCollection"/>.</param>
        /// <param name="target">An object that has a public writable property matching each parameter's name and type.</param>
        public static void SetParameterProperties(
            in this ParameterCollection parameterCollection,
            object target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            var targetType = target.GetType();
            if (!_cachedParameterWriters.TryGetValue(targetType, out var parameterWriters))
            {
                parameterWriters = CreateParameterWriters(targetType);
                _cachedParameterWriters[targetType] = parameterWriters;
            }

            var localParameterWriter = parameterWriters.Values.ToList();

            foreach (var parameter in parameterCollection)
            {
                var parameterName = parameter.Name;
                if (!parameterWriters.TryGetValue(parameterName, out var parameterWriter))
                {
                    ThrowForUnknownIncomingParameterName(targetType, parameterName);
                }

                try
                {
                    parameterWriter.SetValue(target, parameter.Value);
                    localParameterWriter.Remove(parameterWriter);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Unable to set property '{parameterName}' on object of " +
                        $"type '{target.GetType().FullName}'. The error was: {ex.Message}", ex);
                }
            }

            foreach (var nonUsedParameter in localParameterWriter)
            {
                nonUsedParameter.SetValue(target, nonUsedParameter.GetDefaultValue());
            }
        }

        internal static IEnumerable<PropertyInfo> GetCandidateBindableProperties(Type targetType)
            => MemberAssignment.GetPropertiesIncludingInherited(targetType, _bindablePropertyFlags);

        private static IDictionary<string, IPropertySetter> CreateParameterWriters(Type targetType)
        {
            var result = new Dictionary<string, IPropertySetter>(StringComparer.OrdinalIgnoreCase);

            foreach (var propertyInfo in GetCandidateBindableProperties(targetType))
            {
                var shouldCreateWriter = propertyInfo.IsDefined(typeof(ParameterAttribute))
                    || propertyInfo.IsDefined(typeof(CascadingParameterAttribute));
                if (!shouldCreateWriter)
                {
                    continue;
                }

                var propertySetter = MemberAssignment.CreatePropertySetter(targetType, propertyInfo);

                var propertyName = propertyInfo.Name;
                if (result.ContainsKey(propertyName))
                {
                    throw new InvalidOperationException(
                        $"The type '{targetType.FullName}' declares more than one parameter matching the " +
                        $"name '{propertyName.ToLowerInvariant()}'. Parameter names are case-insensitive and must be unique.");
                }

                result.Add(propertyName, propertySetter);
            }

            return result;
        }

        private static void ThrowForUnknownIncomingParameterName(Type targetType, string parameterName)
        {
            // We know we're going to throw by this stage, so it doesn't matter that the following
            // reflection code will be slow. We're just trying to help developers see what they did wrong.
            var propertyInfo = targetType.GetProperty(parameterName, _bindablePropertyFlags);
            if (propertyInfo != null)
            {
                if (!propertyInfo.IsDefined(typeof(ParameterAttribute)) && !propertyInfo.IsDefined(typeof(CascadingParameterAttribute)))
                {
                    throw new InvalidOperationException(
                        $"Object of type '{targetType.FullName}' has a property matching the name '{parameterName}', " +
                        $"but it does not have [{nameof(ParameterAttribute)}] or [{nameof(CascadingParameterAttribute)}] applied.");
                }
                else
                {
                    // This should not happen
                    throw new InvalidOperationException(
                        $"No writer was cached for the property '{propertyInfo.Name}' on type '{targetType.FullName}'.");
                }
            }
            else
            {
                throw new InvalidOperationException(
                    $"Object of type '{targetType.FullName}' does not have a property " +
                    $"matching the name '{parameterName}'.");
            }
        }
    }
}
