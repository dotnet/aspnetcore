// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
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
        public static ViewEngineDescriptor Add(
            [NotNull] this IList<ViewEngineDescriptor> descriptors,
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
        public static ViewEngineDescriptor Insert(
            [NotNull] this IList<ViewEngineDescriptor> descriptors,
            int index,
            [NotNull] Type viewEngineType)
        {
            if (index < 0 || index > descriptors.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
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
        public static ViewEngineDescriptor Add(
            [NotNull] this IList<ViewEngineDescriptor> descriptors,
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
        public static ViewEngineDescriptor Insert(
            [NotNull] this IList<ViewEngineDescriptor> descriptors,
            int index,
            [NotNull] IViewEngine viewEngine)
        {
            if (index < 0 || index > descriptors.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var descriptor = new ViewEngineDescriptor(viewEngine);
            descriptors.Insert(index, descriptor);
            return descriptor;
        }

        /// <summary>
        /// Returns the only instance of <typeparamref name="TInstance"/> from <paramref name="descriptors" />.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance to find.</typeparam>
        /// <param name="descriptors">The <see cref="IList{ViewEngineDescriptor}"/> to search.</param>
        /// <returns>The only instance of <typeparamref name="TInstance"/> in <paramref name="descriptors"/>.</returns>
        /// <exception cref="InvalidOperationException"> 
        /// Thrown if there is not exactly one <typeparamref name="TInstance"/> in <paramref name="descriptors" />.
        /// </exception>
        public static TInstance InstanceOf<TInstance>(
            [NotNull] this IList<ViewEngineDescriptor> descriptors)
        {
            return descriptors
                .Select(descriptor => descriptor.ViewEngine)
                .OfType<TInstance>()
                .Single();
        }

        /// <summary>
        /// Returns the only instance of <typeparamref name="TInstance"/> from <paramref name="descriptors" />
        /// or <c>null</c> if the sequence is empty.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance to find.</typeparam>
        /// <param name="descriptors">The <see cref="IList{ViewEngineDescriptor}"/> to search.</param>
        /// <exception cref="InvalidOperationException"> 
        /// Thrown if there is more than one <typeparamref name="TInstance"/> in <paramref name="descriptors" />.
        /// </exception>
        public static TInstance InstanceOfOrDefault<TInstance>(
            [NotNull] this IList<ViewEngineDescriptor> descriptors)
        {
            return descriptors
                 .Select(descriptor => descriptor.ViewEngine)
                 .OfType<TInstance>()
                 .SingleOrDefault();
        }

        /// <summary>
        /// Returns all instances of <typeparamref name="TInstance"/> from <paramref name="descriptors" />.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instances to find.</typeparam>
        /// <param name="descriptors">The <see cref="IList{ViewEngineDescriptor}"/> to search.</param>
        /// <returns>An IEnumerable of <typeparamref name="TInstance"/> that contains instances from
        /// <paramref name="descriptors"/>.</returns>
        public static IEnumerable<TInstance> InstancesOf<TInstance>(
            [NotNull] this IList<ViewEngineDescriptor> descriptors)
        {
            return descriptors
                 .Select(descriptor => descriptor.ViewEngine)
                 .OfType<TInstance>();
        }
    }
}