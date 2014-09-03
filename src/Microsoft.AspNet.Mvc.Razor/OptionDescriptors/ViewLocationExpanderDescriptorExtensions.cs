// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.Razor.OptionDescriptors;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Extension methods for adding view location expanders to a collection.
    /// </summary>
    public static class ViewLocationExpanderDescriptorExtensions
    {
        /// <summary>
        /// Adds a type representing a <see cref="IViewLocationExpander"/> to <paramref name="descriptors"/>.
        /// </summary>
        /// <param name="descriptors">A list of <see cref="ViewLocationExpanderDescriptor"/>.</param>
        /// <param name="viewLocationExpanderType">Type representing an <see cref="IViewLocationExpander"/></param>
        /// <returns>A <see cref="ViewLocationExpanderDescriptor"/> representing the added instance.</returns>
        public static ViewLocationExpanderDescriptor Add(
            [NotNull] this IList<ViewLocationExpanderDescriptor> descriptors,
            [NotNull] Type viewLocationExpanderType)
        {
            var descriptor = new ViewLocationExpanderDescriptor(viewLocationExpanderType);
            descriptors.Add(descriptor);
            return descriptor;
        }

        /// <summary>
        /// Inserts a type representing a <see cref="IViewLocationExpander"/> in to <paramref name="descriptors"/> at
        /// the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="descriptors">A list of <see cref="ViewLocationExpanderDescriptor"/>.</param>
        /// <param name="index">The zero-based index at which <paramref name="viewLocationExpanderType"/> 
        /// should be inserted.</param>
        /// <param name="viewLocationExpanderType">Type representing an <see cref="IViewLocationExpander"/></param>
        /// <returns>A <see cref="ViewLocationExpanderDescriptor"/> representing the inserted instance.</returns>
        public static ViewLocationExpanderDescriptor Insert(
            [NotNull] this IList<ViewLocationExpanderDescriptor> descriptors,
            int index,
            [NotNull] Type viewLocationExpanderType)
        {
            if (index < 0 || index > descriptors.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var descriptor = new ViewLocationExpanderDescriptor(viewLocationExpanderType);
            descriptors.Insert(index, descriptor);
            return descriptor;
        }

        /// <summary>
        /// Adds an <see cref="IViewLocationExpander"/> to <paramref name="descriptors"/>.
        /// </summary>
        /// <param name="descriptors">A list of <see cref="ViewLocationExpanderDescriptor"/>.</param>
        /// <param name="viewLocationExpander">An <see cref="IViewLocationExpander"/> instance.</param>
        /// <returns>A <see cref="ViewLocationExpanderDescriptor"/> representing the added instance.</returns>
        public static ViewLocationExpanderDescriptor Add(
            [NotNull] this IList<ViewLocationExpanderDescriptor> descriptors,
            [NotNull] IViewLocationExpander viewLocationExpander)
        {
            var descriptor = new ViewLocationExpanderDescriptor(viewLocationExpander);
            descriptors.Add(descriptor);
            return descriptor;
        }

        /// <summary>
        /// Insert an <see cref="IViewLocationExpander"/> in to <paramref name="descriptors"/> at the specified
        /// <paramref name="index"/>.
        /// </summary>
        /// <param name="descriptors">A list of <see cref="ViewLocationExpanderDescriptor"/>.</param>
        /// <param name="index">The zero-based index at which <paramref name="viewLocationExpander"/> 
        /// should be inserted.</param>
        /// <param name="viewLocationExpander">An <see cref="IViewLocationExpander"/> instance.</param>
        /// <returns>A <see cref="ViewLocationExpanderDescriptor"/> representing the added instance.</returns>
        public static ViewLocationExpanderDescriptor Insert(
            [NotNull] this IList<ViewLocationExpanderDescriptor> descriptors,
            int index,
            [NotNull] IViewLocationExpander viewLocationExpander)
        {
            if (index < 0 || index > descriptors.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var descriptor = new ViewLocationExpanderDescriptor(viewLocationExpander);
            descriptors.Insert(index, descriptor);
            return descriptor;
        }
    }
}