// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Blazor.RenderTree;

namespace Microsoft.AspNetCore.Blazor.Components
{
    /// <summary>
    /// Represents a collection of parameters supplied to an <see cref="IComponent"/>
    /// by its parent in the render tree.
    /// </summary>
    public readonly struct ParameterCollection
    {
        private static readonly RenderTreeFrame[] _emptyCollectionFrames = new RenderTreeFrame[]
        {
            RenderTreeFrame.Element(0, string.Empty).WithComponentSubtreeLength(1)
        };

        private static readonly ParameterCollection _emptyCollection
            = new ParameterCollection(_emptyCollectionFrames, 0);

        private readonly RenderTreeFrame[] _frames;
        private readonly int _ownerIndex;

        internal ParameterCollection(RenderTreeFrame[] frames, int ownerIndex)
        {
            _frames = frames;
            _ownerIndex = ownerIndex;
        }

        /// <summary>
        /// Gets an empty <see cref="ParameterCollection"/>.
        /// </summary>
        public static ParameterCollection Empty => _emptyCollection;

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="ParameterCollection"/>.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public ParameterEnumerator GetEnumerator()
            => new ParameterEnumerator(_frames, _ownerIndex);

        /// <summary>
        /// Gets the value of the parameter with the specified name.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="result">Receives the result, if any.</param>
        /// <returns>True if a matching parameter was found; false otherwise.</returns>
        public bool TryGetValue<T>(string parameterName, out T result)
        {
            foreach (var entry in this)
            {
                if (string.Equals(entry.Name, parameterName))
                {
                    result = (T)entry.Value;
                    return true;
                }
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Gets the value of the parameter with the specified name, or a default value
        /// if no such parameter exists in the collection.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <returns>The parameter value if found; otherwise the default value for the specified type.</returns>
        public T GetValueOrDefault<T>(string parameterName)
            => GetValueOrDefault<T>(parameterName, default);

        /// <summary>
        /// Gets the value of the parameter with the specified name, or a specified default value
        /// if no such parameter exists in the collection.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="defaultValue">The default value to return if no such parameter exists in the collection.</param>
        /// <returns>The parameter value if found; otherwise <paramref name="defaultValue"/>.</returns>
        public T GetValueOrDefault<T>(string parameterName, T defaultValue)
            => TryGetValue<T>(parameterName, out T result) ? result : defaultValue;

        /// <summary>
        /// Returns a dictionary populated with the contents of the <see cref="ParameterCollection"/>.
        /// </summary>
        /// <returns>A dictionary populated with the contents of the <see cref="ParameterCollection"/>.</returns>
        public IReadOnlyDictionary<string, object> ToDictionary()
        {
            var result = new Dictionary<string, object>();
            foreach (var entry in this)
            {
                result[entry.Name] = entry.Value;
            }
            return result;
        }

        // It's internal because there isn't a known use case for user code comparing
        // ParameterCollection instances, and even if there was, it's unlikely it should
        // use these equality rules which are designed for their effect on rendering.
        internal bool DefinitelyEquals(ParameterCollection oldParameters)
        {
            // In general we can't detect mutations on arbitrary objects. We can't trust
            // things like .Equals or .GetHashCode because they usually only tell us about
            // shallow changes, not deep mutations. So we return false if both:
            //  [1] All the parameters are known to be immutable (i.e., Type.IsPrimitive
            //      or is in a known set of common immutable types)
            //  [2] And all the parameter values are equal to their previous values
            // Otherwise be conservative and return false.
            // To make this check cheaper, since parameters are virtually always generated in
            // a deterministic order, we don't bother to account for reordering, so if any
            // of the names don't match sequentially we just return false too.
            //
            // The logic here may look kind of epic, and would certainly be simpler if we
            // used ParameterEnumerator.GetEnumerator(), but it's perf-critical and this
            // implementation requires a lot fewer instructions than a GetEnumerator-based one.

            var oldIndex = oldParameters._ownerIndex;
            var newIndex = _ownerIndex;
            var oldEndIndexExcl = oldIndex + oldParameters._frames[oldIndex].ComponentSubtreeLength;
            var newEndIndexExcl = newIndex + _frames[newIndex].ComponentSubtreeLength;
            while (true)
            {
                // First, stop if we've reached the end of either subtree
                oldIndex++;
                newIndex++;
                var oldFinished = oldIndex == oldEndIndexExcl;
                var newFinished = newIndex == newEndIndexExcl;
                if (oldFinished || newFinished)
                {
                    return oldFinished == newFinished; // Same only if we have same number of parameters
                }
                else
                {
                    // Since neither subtree has finished, it's safe to read the next frame from both
                    ref var oldFrame = ref oldParameters._frames[oldIndex];
                    ref var newFrame = ref _frames[newIndex];

                    // Stop if we've reached the end of either subtree's sequence of attributes
                    oldFinished = oldFrame.FrameType != RenderTreeFrameType.Attribute;
                    newFinished = newFrame.FrameType != RenderTreeFrameType.Attribute;
                    if (oldFinished || newFinished)
                    {
                        return oldFinished == newFinished; // Same only if we have same number of parameters
                    }
                    else
                    {
                        if (!string.Equals(oldFrame.AttributeName, newFrame.AttributeName, StringComparison.Ordinal))
                        {
                            return false; // Different names
                        }

                        var oldValue = oldFrame.AttributeValue;
                        var newValue = newFrame.AttributeValue;
                        var oldIsNotNull = oldValue != null;
                        var newIsNotNull = newValue != null;
                        if (oldIsNotNull != newIsNotNull)
                        {
                            return false; // One's null and the other isn't, so different
                        }
                        else if (oldIsNotNull) // i.e., both are not null (considering previous check)
                        {
                            var oldValueType = oldValue.GetType();
                            var newValueType = newValue.GetType();
                            if (oldValueType != newValueType            // Definitely different
                                || !IsKnownImmutableType(oldValueType)  // Maybe different
                                || !oldValue.Equals(newValue))          // Somebody says they are different
                            {
                                return false;
                            }
                        }
                        else
                        {
                            // Both null, hence equal, so continue
                        }
                    }
                }
            }
        }

        // The contents of this list need to trade off false negatives against computation
        // time. So we don't want a huge list of types to check (or would have to move to
        // a hashtable lookup, which is differently expensive). It's better not to include
        // uncommon types here even if they are known to be immutable.
        private bool IsKnownImmutableType(Type type)
            => type.IsPrimitive
            || type == typeof(string)
            || type == typeof(DateTime)
            || type == typeof(decimal);
    }
}
