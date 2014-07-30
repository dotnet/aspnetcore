// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.OptionDescriptors;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Extension methods for adding output formatters to a collection.
    /// </summary>
    public static class OutputFormatterDescriptorExtensions
    {
        /// <summary>
        /// Adds a type representing a <see cref="IOutputFormatter"/> to a descriptor collection.
        /// </summary>
        /// <param name="descriptors">A list of OutputFormatterDescriptors</param>
        /// <param name="outputFormatterType">Type representing an <see cref="IOutputFormatter"/>.</param>
        /// <returns>OutputFormatterDescriptor representing the added instance.</returns>
        public static OutputFormatterDescriptor Add([NotNull] this IList<OutputFormatterDescriptor> descriptors,
                                                    [NotNull] Type outputFormatterType)
        {
            var descriptor = new OutputFormatterDescriptor(outputFormatterType);
            descriptors.Add(descriptor);
            return descriptor;
        }

        /// <summary>
        /// Inserts a type representing a <see cref="IOutputFormatter"/> to a descriptor collection.
        /// </summary>
        /// <param name="descriptors">A list of OutputFormatterDescriptors</param>
        /// <param name="outputFormatterType">Type representing an <see cref="IOutputFormatter"/>.</param>
        /// <returns>OutputFormatterDescriptor representing the inserted instance.</returns>
        public static OutputFormatterDescriptor Insert([NotNull] this IList<OutputFormatterDescriptor> descriptors,
                                                       int index,
                                                       [NotNull] Type outputFormatterType)
        {
            if (index < 0 || index > descriptors.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            var descriptor = new OutputFormatterDescriptor(outputFormatterType);
            descriptors.Insert(index, descriptor);
            return descriptor;
        }

        /// <summary>
        /// Adds an <see cref="IOutputFormatter"/> to a descriptor collection.
        /// </summary>
        /// <param name="descriptors">A list of OutputFormatterDescriptors</param>
        /// <param name="outputFormatter">An <see cref="IOutputFormatter"/> instance.</param>
        /// <returns>OutputFormatterDescriptor representing the added instance.</returns>
        public static OutputFormatterDescriptor Add([NotNull] this IList<OutputFormatterDescriptor> descriptors,
                                                    [NotNull] IOutputFormatter outputFormatter)
        {
            var descriptor = new OutputFormatterDescriptor(outputFormatter);
            descriptors.Add(descriptor);
            return descriptor;
        }

        /// <summary>
        /// Insert an <see cref="IOutputFormatter"/> to a descriptor collection.
        /// </summary>
        /// <param name="descriptors">A list of OutputFormatterDescriptors</param>
        /// <param name="outputFormatter">An <see cref="IOutputFormatter"/> instance.</param>
        /// <returns>OutputFormatterDescriptor representing the added instance.</returns>
        public static OutputFormatterDescriptor Insert([NotNull] this IList<OutputFormatterDescriptor> descriptors,
                                                       int index,
                                                       [NotNull] IOutputFormatter outputFormatter)
        {
            if (index < 0 || index > descriptors.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            var descriptor = new OutputFormatterDescriptor(outputFormatter);
            descriptors.Insert(index, descriptor);
            return descriptor;
        }
    }
}