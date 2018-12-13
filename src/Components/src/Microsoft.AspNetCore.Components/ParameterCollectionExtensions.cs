// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.Reflection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

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
        public unsafe static void SetParameterProperties(
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

            // We only want to iterate through the parameterCollection once, and by the end of it,
            // need to have tracked which of the parameter properties haven't yet been written.
            // To avoid allocating any list/dictionary to track that, here we stackalloc an array
            // of flags and set them based on the indices of the writers we use.
            var numWriters = writers.WritersByIndex.Count;
            var numUsedWriters = 0;

            // TODO: Once we're able to move to netstandard2.1, this can be changed to be
            // a Span<bool> and then the enclosing method no longer needs to be 'unsafe'
            bool* usageFlags = stackalloc bool[numWriters];

            foreach (var parameter in parameterCollection)
            {
                var parameterName = parameter.Name;
                if (!writers.WritersByName.TryGetValue(parameterName, out var writerWithIndex))
                {
                    ThrowForUnknownIncomingParameterName(targetType, parameterName);
                }

                try
                {
                    writerWithIndex.Writer.SetValue(target, parameter.Value);
                    usageFlags[writerWithIndex.Index] = true;
                    numUsedWriters++;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Unable to set property '{parameterName}' on object of " +
                        $"type '{target.GetType().FullName}'. The error was: {ex.Message}", ex);
                }
            }

            // Now we can determine whether any writers have not been used, and if there are
            // some unused ones, find them.
            for (var index = 0; numUsedWriters < numWriters; index++)
            {
                if (index >= numWriters)
                {
                    // This should not be possible
                    throw new InvalidOperationException("Ran out of writers before marking them all as used.");
                }

                if (!usageFlags[index])
                {
                    writers.WritersByIndex[index].SetDefaultValue(target);
                    numUsedWriters++;
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

        class WritersForType
        {
            public Dictionary<string, (int Index, IPropertySetter Writer)> WritersByName { get; }
            public List<IPropertySetter> WritersByIndex { get; }

            public WritersForType(Type targetType)
            {
                var propertySettersByName = new Dictionary<string, IPropertySetter>(StringComparer.OrdinalIgnoreCase);
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
                    if (propertySettersByName.ContainsKey(propertyName))
                    {
                        throw new InvalidOperationException(
                            $"The type '{targetType.FullName}' declares more than one parameter matching the " +
                            $"name '{propertyName.ToLowerInvariant()}'. Parameter names are case-insensitive and must be unique.");
                    }

                    propertySettersByName.Add(propertyName, propertySetter);
                }

                // Now we know all the entries, construct the resulting list/dictionary
                // with well-defined indices
                WritersByIndex = new List<IPropertySetter>();
                WritersByName = new Dictionary<string, (int, IPropertySetter)>(StringComparer.OrdinalIgnoreCase);
                foreach (var pair in propertySettersByName)
                {
                    WritersByName.Add(pair.Key, (WritersByIndex.Count, pair.Value));
                    WritersByIndex.Add(pair.Value);
                }
            }
        }
    }
}
