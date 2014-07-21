// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.OptionDescriptors;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Extension methods for adding <see cref="IValueProviderFactory"/> to a descriptor collection.
    /// </summary>
    public static class ValueProviderFactoryDescriptorExtensions
    {
        /// <summary>
        /// Adds a type representing a <see cref="IValueProviderFactory"/> to a descriptor collection.
        /// </summary>
        /// <param name="descriptors">A list of ValueProviderFactoryDescriptors</param>
        /// <param name="valueProviderType">Type representing an <see cref="IValueProviderFactory"/>.</param>
        /// <returns>ValueProviderFactoryDescriptor representing the added instance.</returns>
        public static ValueProviderFactoryDescriptor Add(
            [NotNull] this IList<ValueProviderFactoryDescriptor> descriptors,
            [NotNull] Type valueProviderType)
        {
            var descriptor = new ValueProviderFactoryDescriptor(valueProviderType);
            descriptors.Add(descriptor);
            return descriptor;
        }

        /// <summary>
        /// Inserts a type representing a <see cref="IValueProviderFactory"/> to a descriptor collection.
        /// </summary>
        /// <param name="descriptors">A list of ValueProviderFactoryDescriptors</param>
        /// <param name="viewEngineType">Type representing an <see cref="IValueProviderFactory"/>.</param>
        /// <returns>ValueProviderFactoryDescriptor representing the inserted instance.</returns>
        public static ValueProviderFactoryDescriptor Insert(
            [NotNull] this IList<ValueProviderFactoryDescriptor> descriptors,
            int index,
            [NotNull] Type viewEngineType)
        {
            if (index < 0 || index > descriptors.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            var descriptor = new ValueProviderFactoryDescriptor(viewEngineType);
            descriptors.Insert(index, descriptor);
            return descriptor;
        }

        /// <summary>
        /// Adds an <see cref="IValueProviderFactory"/> to a descriptor collection.
        /// </summary>
        /// <param name="descriptors">A list of ValueProviderFactoryDescriptors</param>
        /// <param name="valueProviderFactory">An <see cref="IValueProviderFactory"/> instance.</param>
        /// <returns>ValueProviderFactoryDescriptor representing the added instance.</returns>
        public static ValueProviderFactoryDescriptor Add(
            [NotNull] this IList<ValueProviderFactoryDescriptor> descriptors,
            [NotNull] IValueProviderFactory valueProviderFactory)
        {
            var descriptor = new ValueProviderFactoryDescriptor(valueProviderFactory);
            descriptors.Add(descriptor);
            return descriptor;
        }

        /// <summary>
        /// Insert an <see cref="IValueProviderFactory"/> to a descriptor collection.
        /// </summary>
        /// <param name="descriptors">A list of ValueProviderFactoryDescriptors</param>
        /// <param name="valueProviderFactory">An <see cref="IValueProviderFactory"/> instance.</param>
        /// <returns>ValueProviderFactoryDescriptor representing the added instance.</returns>
        public static ValueProviderFactoryDescriptor Insert(
            [NotNull] this IList<ValueProviderFactoryDescriptor> descriptors,
            int index,
            [NotNull] IValueProviderFactory valueProviderFactory)
        {
            if (index < 0 || index > descriptors.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            var descriptor = new ValueProviderFactoryDescriptor(valueProviderFactory);
            descriptors.Insert(index, descriptor);
            return descriptor;
        }
    }
}