// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.OptionDescriptors;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Extension methods for adding view engines to a descriptor collection.
    /// </summary>
    public static class ViewEngineDescriptorExtensions
    {
        /// <summary>
        /// Adds a type representing a <see cref="IViewEngine"/> to a descriptor collection.
        /// </summary>
        /// <param name="descriptors">A list of ViewEngineDescriptors</param>
        /// <param name="viewEngineType">Type representing an <see cref="IViewEngine"/>.</param>
        /// <returns>ViewEngineDescriptor representing the added instance.</returns>
        public static ViewEngineDescriptor Add([NotNull] this IList<ViewEngineDescriptor> descriptors,
                                               [NotNull] Type viewEngineType)
        {
            var descriptor = new ViewEngineDescriptor(viewEngineType);
            descriptors.Add(descriptor);
            return descriptor;
        }

        /// <summary>
        /// Inserts a type representing a <see cref="IViewEngine"/> to a descriptor collection.
        /// </summary>
        /// <param name="descriptors">A list of ViewEngineDescriptors</param>
        /// <param name="viewEngineType">Type representing an <see cref="IViewEngine"/>.</param>
        /// <returns>ViewEngineDescriptor representing the inserted instance.</returns>
        public static ViewEngineDescriptor Insert([NotNull] this IList<ViewEngineDescriptor> descriptors,
                                                   int index,
                                                   [NotNull] Type viewEngineType)
        {
            if (index < 0 || index > descriptors.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            var descriptor = new ViewEngineDescriptor(viewEngineType);
            descriptors.Insert(index, descriptor);
            return descriptor;
        }

        /// <summary>
        /// Adds an <see cref="IViewEngine"/> to a descriptor collection.
        /// </summary>
        /// <param name="descriptors">A list of ViewEngineDescriptors</param>
        /// <param name="viewEngine">An <see cref="IViewEngine"/> instance.</param>
        /// <returns>ViewEngineDescriptor representing the added instance.</returns>
        public static ViewEngineDescriptor Add([NotNull] this IList<ViewEngineDescriptor> descriptors,
                                               [NotNull] IViewEngine viewEngine)
        {
            var descriptor = new ViewEngineDescriptor(viewEngine);
            descriptors.Add(descriptor);
            return descriptor;
        }

        /// <summary>
        /// Insert an <see cref="IViewEngine"/> to a descriptor collection.
        /// </summary>
        /// <param name="descriptors">A list of ViewEngineDescriptors</param>
        /// <param name="viewEngine">An <see cref="IViewEngine"/> instance.</param>
        /// <returns>ViewEngineDescriptor representing the added instance.</returns>
        public static ViewEngineDescriptor Insert([NotNull] this IList<ViewEngineDescriptor> descriptors,
                                                   int index,
                                                   [NotNull] IViewEngine viewEngine)
        {
            if (index < 0 || index > descriptors.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            var descriptor = new ViewEngineDescriptor(viewEngine);
            descriptors.Insert(index, descriptor);
            return descriptor;
        }
    }
}