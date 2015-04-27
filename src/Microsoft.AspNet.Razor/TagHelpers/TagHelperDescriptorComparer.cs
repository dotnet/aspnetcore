// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor.TagHelpers
{
    /// <summary>
    /// An <see cref="IEqualityComparer{TagHelperDescriptor}"/> used to check equality between
    /// two <see cref="TagHelperDescriptor"/>s.
    /// </summary>
    public class TagHelperDescriptorComparer : IEqualityComparer<TagHelperDescriptor>
    {
        /// <summary>
        /// A default instance of the <see cref="TagHelperDescriptorComparer"/>.
        /// </summary>
        public static readonly TagHelperDescriptorComparer Default = new TagHelperDescriptorComparer();

        /// <summary>
        /// Determines if the two given tag helpers are equal.
        /// </summary>
        /// <param name="descriptorX">A <see cref="TagHelperDescriptor"/> to compare with the given
        /// <paramref name="descriptorY"/>.</param>
        /// <param name="descriptorY">A <see cref="TagHelperDescriptor"/> to compare with the given
        /// <paramref name="descriptorX"/>.</param>
        /// <returns><c>true</c> if <paramref name="descriptorX"/> and <paramref name="descriptorY"/> are equal,
        /// <c>false</c> otherwise.</returns>
        /// <remarks>
        /// Determines equality based on <see cref="TagHelperDescriptor.TypeName"/>,
        /// <see cref="TagHelperDescriptor.AssemblyName"/>, <see cref="TagHelperDescriptor.TagName"/>,
        /// and <see cref="TagHelperDescriptor.RequiredAttributes"/>.
        /// </remarks>
        public bool Equals(TagHelperDescriptor descriptorX, TagHelperDescriptor descriptorY)
        {
            return string.Equals(descriptorX.TypeName, descriptorY.TypeName, StringComparison.Ordinal) &&
                   string.Equals(descriptorX.TagName, descriptorY.TagName, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(descriptorX.AssemblyName, descriptorY.AssemblyName, StringComparison.Ordinal) &&
                   Enumerable.SequenceEqual(
                       descriptorX.RequiredAttributes.OrderBy(
                           attribute => attribute,
                           StringComparer.OrdinalIgnoreCase),
                       descriptorY.RequiredAttributes.OrderBy(
                           attribute => attribute,
                           StringComparer.OrdinalIgnoreCase),
                       StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns an <see cref="int"/> value that uniquely identifies the given <see cref="TagHelperDescriptor"/>.
        /// </summary>
        /// <param name="descriptor">The <see cref="TagHelperDescriptor"/> to create a hash code for.</param>
        /// <returns>An <see cref="int"/> that uniquely identifies the given <paramref name="descriptor"/>.</returns>
        public int GetHashCode(TagHelperDescriptor descriptor)
        {
            var hashCodeCombiner = HashCodeCombiner
                .Start()
                .Add(descriptor.TypeName, StringComparer.Ordinal)
                .Add(descriptor.TagName, StringComparer.OrdinalIgnoreCase)
                .Add(descriptor.AssemblyName, StringComparer.Ordinal);

            var attributes = descriptor.RequiredAttributes.OrderBy(
                attribute => attribute,
                StringComparer.OrdinalIgnoreCase);
            foreach (var attribute in attributes)
            {
                hashCodeCombiner.Add(attribute, StringComparer.OrdinalIgnoreCase);
            }

            return hashCodeCombiner.CombinedHash;
        }
    }
}