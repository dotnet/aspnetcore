// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Components.Reflection
{
    internal static class ComponentProperties
    {
        private const BindingFlags _bindablePropertyFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase;

        // Right now it's not possible for a component to define a Parameter and a Cascading Parameter with
        // the same name. We don't give you a way to express this in code (would create duplicate properties),
        // and we don't have the ability to represent it in our data structures.
        private readonly static ConcurrentDictionary<Type, WritersForType> _cachedWritersByType
            = new ConcurrentDictionary<Type, WritersForType>();

        public static void SetProperties(in ParameterView parameters, object target)
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

            // The logic is split up for simplicity now that we have CaptureUnmatchedValues parameters.
            if (writers.CaptureUnmatchedValuesWriter == null)
            {
                // Logic for components without a CaptureUnmatchedValues parameter
                foreach (var parameter in parameters)
                {
                    var parameterName = parameter.Name;
                    if (!writers.WritersByName.TryGetValue(parameterName, out var writer))
                    {
                        // Case 1: There is nowhere to put this value.
                        ThrowForUnknownIncomingParameterName(targetType, parameterName);
                        throw null; // Unreachable
                    }
                    else if (writer.Cascading && !parameter.Cascading)
                    {
                        // We don't allow you to set a cascading parameter with a non-cascading value. Put another way:
                        // cascading parameters are not part of the public API of a component, so it's not reasonable
                        // for someone to set it directly.
                        //
                        // If we find a strong reason for this to work in the future we can reverse our decision since
                        // this throws today.
                        ThrowForSettingCascadingParameterWithNonCascadingValue(targetType, parameterName);
                        throw null; // Unreachable
                    }
                    else if (!writer.Cascading && parameter.Cascading)
                    {
                        // We're giving a more specific error here because trying to set a non-cascading parameter
                        // with a cascading value is likely deliberate (but not supported), or is a bug in our code.
                        ThrowForSettingParameterWithCascadingValue(targetType, parameterName);
                        throw null; // Unreachable
                    }

                    SetProperty(target, writer, parameterName, parameter.Value);
                }
            }
            else
            {
                // Logic with components with a CaptureUnmatchedValues parameter
                var isCaptureUnmatchedValuesParameterSetExplicitly = false;
                Dictionary<string, object> unmatched = null;
                foreach (var parameter in parameters)
                {
                    var parameterName = parameter.Name;
                    if (string.Equals(parameterName, writers.CaptureUnmatchedValuesPropertyName, StringComparison.OrdinalIgnoreCase))
                    {
                        isCaptureUnmatchedValuesParameterSetExplicitly = true;
                    }

                    var isUnmatchedValue = !writers.WritersByName.TryGetValue(parameterName, out var writer);

                    if ((isUnmatchedValue && parameter.Cascading) || (writer != null && !writer.Cascading && parameter.Cascading))
                    {
                        // Don't allow an "extra" cascading value to be collected - or don't allow a non-cascading
                        // parameter to be set with a cascading value.
                        //
                        // This is likely a bug in our infrastructure or an attempt to deliberately do something unsupported.
                        ThrowForSettingParameterWithCascadingValue(targetType, parameterName);
                        throw null; // Unreachable

                    }
                    else if (isUnmatchedValue ||

                        // Allow unmatched parameters to collide with the names of cascading parameters. This is
                        // valid because cascading parameter names are not part of the public API. There's no
                        // way for the user of a component to know what the names of cascading parameters
                        // are.
                        (writer.Cascading && !parameter.Cascading))
                    {
                        unmatched ??= new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                        unmatched[parameterName] = parameter.Value;
                    }
                    else
                    {
                        Debug.Assert(writer != null);
                        SetProperty(target, writer, parameterName, parameter.Value);
                    }
                }

                if (unmatched != null && isCaptureUnmatchedValuesParameterSetExplicitly)
                {
                    // This has to be an error because we want to allow users to set the CaptureUnmatchedValues
                    // parameter explicitly and ....
                    // 1. We don't ever want to mutate a value the user gives us.
                    // 2. We also don't want to implicitly copy a value the user gives us.
                    //
                    // Either one of those implementation choices would do something unexpected.
                    ThrowForCaptureUnmatchedValuesConflict(targetType, writers.CaptureUnmatchedValuesPropertyName, unmatched);
                    throw null; // Unreachable
                }
                else if (unmatched != null)
                {
                    // We had some unmatched values, set the CaptureUnmatchedValues property
                    SetProperty(target, writers.CaptureUnmatchedValuesWriter, writers.CaptureUnmatchedValuesPropertyName, unmatched);
                }
            }

            static void SetProperty(object target, IPropertySetter writer, string parameterName, object value)
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

        private static void ThrowForSettingCascadingParameterWithNonCascadingValue(Type targetType, string parameterName)
        {
            throw new InvalidOperationException(
                $"Object of type '{targetType.FullName}' has a property matching the name '{parameterName}', " +
                $"but it does not have [{nameof(ParameterAttribute)}] applied.");
        }

        private static void ThrowForSettingParameterWithCascadingValue(Type targetType, string parameterName)
        {
            throw new InvalidOperationException(
                $"The property '{parameterName}' on component type '{targetType.FullName}' cannot be set " +
                $"using a cascading value.");
        }

        private static void ThrowForCaptureUnmatchedValuesConflict(Type targetType, string parameterName, Dictionary<string, object> unmatched)
        {
            throw new InvalidOperationException(
                $"The property '{parameterName}' on component type '{targetType.FullName}' cannot be set explicitly " +
                $"when also used to capture unmatched values. Unmatched values:" + Environment.NewLine +
                string.Join(Environment.NewLine, unmatched.Keys.OrderBy(k => k)));
        }

        private static void ThrowForMultipleCaptureUnmatchedValuesParameters(Type targetType)
        {
            // We don't care about perf here, we want to report an accurate and useful error.
            var propertyNames = targetType
                .GetProperties(_bindablePropertyFlags)
                .Where(p => p.GetCustomAttribute<ParameterAttribute>()?.CaptureUnmatchedValues == true)
                .Select(p => p.Name)
                .OrderBy(p => p)
                .ToArray();

            throw new InvalidOperationException(
                $"Multiple properties were found on component type '{targetType.FullName}' with " +
                $"'{nameof(ParameterAttribute)}.{nameof(ParameterAttribute.CaptureUnmatchedValues)}'. Only a single property " +
                $"per type can use '{nameof(ParameterAttribute)}.{nameof(ParameterAttribute.CaptureUnmatchedValues)}'. Properties:" + Environment.NewLine +
                string.Join(Environment.NewLine, propertyNames));
        }

        private static void ThrowForInvalidCaptureUnmatchedValuesParameterType(Type targetType, PropertyInfo propertyInfo)
        {
            throw new InvalidOperationException(
                $"The property '{propertyInfo.Name}' on component type '{targetType.FullName}' cannot be used " +
                $"with '{nameof(ParameterAttribute)}.{nameof(ParameterAttribute.CaptureUnmatchedValues)}' because it has the wrong type. " +
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
                    var cascadingParameterAttribute = propertyInfo.GetCustomAttribute<CascadingParameterAttribute>();
                    var isParameter = parameterAttribute != null || cascadingParameterAttribute != null;
                    if (!isParameter)
                    {
                        continue;
                    }

                    var propertyName = propertyInfo.Name;
                    if (parameterAttribute != null && (propertyInfo.SetMethod == null || !propertyInfo.SetMethod.IsPublic))
                    {
                        throw new InvalidOperationException(
                            $"The type '{targetType.FullName}' declares a parameter matching the name '{propertyName}' that is not public. Parameters must be public.");
                    }

                    var propertySetter = MemberAssignment.CreatePropertySetter(targetType, propertyInfo, cascading: cascadingParameterAttribute != null);

                    if (WritersByName.ContainsKey(propertyName))
                    {
                        throw new InvalidOperationException(
                            $"The type '{targetType.FullName}' declares more than one parameter matching the " +
                            $"name '{propertyName.ToLowerInvariant()}'. Parameter names are case-insensitive and must be unique.");
                    }

                    WritersByName.Add(propertyName, propertySetter);

                    if (parameterAttribute != null && parameterAttribute.CaptureUnmatchedValues)
                    {
                        // This is an "Extra" parameter.
                        //
                        // There should only be one of these.
                        if (CaptureUnmatchedValuesWriter != null)
                        {
                            ThrowForMultipleCaptureUnmatchedValuesParameters(targetType);
                        }

                        // It must be able to hold a Dictionary<string, object> since that's what we create.
                        if (!propertyInfo.PropertyType.IsAssignableFrom(typeof(Dictionary<string, object>)))
                        {
                            ThrowForInvalidCaptureUnmatchedValuesParameterType(targetType, propertyInfo);
                        }

                        CaptureUnmatchedValuesWriter = MemberAssignment.CreatePropertySetter(targetType, propertyInfo, cascading: false);
                        CaptureUnmatchedValuesPropertyName = propertyInfo.Name;
                    }
                }
            }

            public Dictionary<string, IPropertySetter> WritersByName { get; }

            public IPropertySetter CaptureUnmatchedValuesWriter { get; }

            public string CaptureUnmatchedValuesPropertyName { get; }
        }
    }
}
