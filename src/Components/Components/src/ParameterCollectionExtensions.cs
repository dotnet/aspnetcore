// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components.Reflection;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Extension methods for the <see cref="ParameterCollection"/> type.
    /// </summary>
    public static class ParameterCollectionExtensions
    {
        private const BindingFlags _bindablePropertyFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase;

        private readonly static ConcurrentDictionary<Type, WritersForType> _cachedWritersByType
            = new ConcurrentDictionary<Type, WritersForType>();

        /// <summary>
        /// For each parameter property on <paramref name="target"/>, updates its value to
        /// match the corresponding entry in the <see cref="ParameterCollection"/>.
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
            if (!_cachedWritersByType.TryGetValue(targetType, out var writers))
            {
                writers = new WritersForType(targetType);
                _cachedWritersByType[targetType] = writers;
            }

            // The logic is a little convoluted now that we have "Extra" parameters. We want to avoid allocations where
            // possible.
            //
            // Error cases that are possible here:
            // 1. Using an unknown parameter when there is no "Extra" parameter defined
            // 2. Using an unknown parameter when there is an "Extra" parameter defined *AND* explicitly
            //    providing a value for the extra parameter.
            //
            // The second case has to be an error because we want to allow users to set the "Extra" parameter
            // explicitly, but we don't ever want to mutate a value the user gives us. We also don't want to
            // implicitly copy a value the user gives us. Either one of those implementation choices would do
            // something unexpected.

            var isExtraParameterSetExplicitly = false;
            Dictionary<string, object> extras = null;
            foreach (var parameter in parameterCollection)
            {
                var parameterName = parameter.Name;
                if (string.Equals(parameterName, writers.CaptureExtraAttributesPropertyName, StringComparison.OrdinalIgnoreCase))
                {
                    isExtraParameterSetExplicitly = true;
                }

                bool isExtraParameter;
                if (!writers.WritersByName.TryGetValue(parameterName, out var writer) &&
                    writers.CaptureExtraAttributesWriter == null)
                {
                    // Case 1: There is nowhere to put this value.
                    ThrowForUnknownIncomingParameterName(targetType, parameterName);
                    throw null; // Unreachable
                }

                isExtraParameter = writer == null;

                if (isExtraParameter)
                {
                    extras ??= new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    extras[parameterName] = parameter.Value;
                }
                else
                {
                    SetProperty(targetType, target, writer, parameterName, parameter.Value);
                }
            }

            if (extras != null && isExtraParameterSetExplicitly)
            {
                // Case 2: Conflict between "Extra" parameters.
                ThrowForExtraParameterConflict(targetType, writers.CaptureExtraAttributesPropertyName, extras);
                throw null; // Unreachable
            }
            else if (extras != null)
            {
                // We had some extra values, set the "Extra" property
                SetProperty(targetType, target, writers.CaptureExtraAttributesWriter, writers.CaptureExtraAttributesPropertyName, extras);
            }

            static void SetProperty(Type targetType, object target, IPropertySetter writer, string parameterName, object value)
            {
                try
                {
                    writer.SetValue(target, value);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Unable to set property '{parameterName}' on object of " +
                        $"type '{target.GetType().FullName}'. The error was: {ex.Message}", ex);
                }
            }
        }

        internal static IEnumerable<PropertyInfo> GetCandidateBindableProperties(Type targetType)
            => MemberAssignment.GetPropertiesIncludingInherited(targetType, _bindablePropertyFlags);

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

        private static void ThrowForExtraParameterConflict(Type targetType, string parameterName, Dictionary<string, object> extras)
        {
            throw new InvalidOperationException(
                $"The property '{parameterName}' on component type '{targetType.FullName}' cannot be set explicitly " +
                $"when also used to capture extra parameter values. Extra parameters:" + Environment.NewLine +
                string.Join(Environment.NewLine, extras.Keys.OrderBy(k => k)));
        }

        private static void ThrowForMultipleCaptureExtraAttributesParameters(Type targetType)
        {
            // We don't care about perf here, we want to report an accurate and useful error.
            var propertyNames = targetType
                .GetProperties(_bindablePropertyFlags)
                .Where(p => p.GetCustomAttribute<ParameterAttribute>()?.CaptureExtraAttributes == true)
                .Select(p => p.Name)
                .OrderBy(p => p)
                .ToArray();

            throw new InvalidOperationException(
                $"Multiple properties were found on component type '{targetType.FullName}' with " +
                $"'{nameof(ParameterAttribute)}.{nameof(ParameterAttribute.CaptureExtraAttributes)}'. Only a single property " +
                $"per type can use '{nameof(ParameterAttribute)}.{nameof(ParameterAttribute.CaptureExtraAttributes)}'. Properties:" + Environment.NewLine +
                string.Join(Environment.NewLine, propertyNames));
        }

        private static void ThrowForInvalidCaptureExtraParameterType(Type targetType, PropertyInfo propertyInfo)
        {
            throw new InvalidOperationException(
                $"The property '{propertyInfo.Name}' on component type '{targetType.FullName}' cannot be used " +
                $"with '{nameof(ParameterAttribute)}.{nameof(ParameterAttribute.CaptureExtraAttributes)}' because it has the wrong type. " +
                $"The property must be assignable from 'Dictionary<string, object>'.");
        }

        private class WritersForType
        {
            public WritersForType(Type targetType)
            {
                WritersByName = new Dictionary<string, IPropertySetter>(StringComparer.OrdinalIgnoreCase);
                foreach (var propertyInfo in GetCandidateBindableProperties(targetType))
                {
                    var parameterAttribute = propertyInfo.GetCustomAttribute<ParameterAttribute>();
                    var isParameter = parameterAttribute != null || propertyInfo.IsDefined(typeof(CascadingParameterAttribute));
                    if (!isParameter)
                    {
                        continue;
                    }

                    var propertySetter = MemberAssignment.CreatePropertySetter(targetType, propertyInfo);

                    var propertyName = propertyInfo.Name;
                    if (WritersByName.ContainsKey(propertyName))
                    {
                        throw new InvalidOperationException(
                            $"The type '{targetType.FullName}' declares more than one parameter matching the " +
                            $"name '{propertyName.ToLowerInvariant()}'. Parameter names are case-insensitive and must be unique.");
                    }

                    WritersByName.Add(propertyName, propertySetter);

                    if (parameterAttribute != null && parameterAttribute.CaptureExtraAttributes)
                    {
                        // This is an "Extra" parameter.
                        //
                        // There should only be one of these.
                        if (CaptureExtraAttributesWriter != null)
                        {
                            ThrowForMultipleCaptureExtraAttributesParameters(targetType);
                        }

                        // It must be able to hold a Dictionary<string, object> since that's what we create.
                        if (!propertyInfo.PropertyType.IsAssignableFrom(typeof(Dictionary<string, object>)))
                        {
                            ThrowForInvalidCaptureExtraParameterType(targetType, propertyInfo);
                        }

                        CaptureExtraAttributesWriter = MemberAssignment.CreatePropertySetter(targetType, propertyInfo);
                        CaptureExtraAttributesPropertyName = propertyInfo.Name;
                    }
                }
            }

            public Dictionary<string, IPropertySetter> WritersByName { get; }

            public IPropertySetter CaptureExtraAttributesWriter { get; }

            public string CaptureExtraAttributesPropertyName { get; }
        }
    }
}
