// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.OptionDescriptors;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Extension methods for adding Input formatters to a collection.
    /// </summary>
    public static class InputFormatterDescriptorExtensions
    {
        /// <summary>
        /// Adds a type representing a <see cref="IInputFormatter"/> to a descriptor collection.
        /// </summary>
        /// <param name="descriptors">A list of InputFormatterDescriptors</param>
        /// <param name="inputFormatterType">Type representing an <see cref="IInputFormatter"/>.</param>
        /// <returns>InputFormatterDescriptor representing the added instance.</returns>
        public static InputFormatterDescriptor Add([NotNull] this IList<InputFormatterDescriptor> descriptors,
                                                   [NotNull] Type inputFormatterType)
        {
            var descriptor = new InputFormatterDescriptor(inputFormatterType);
            descriptors.Add(descriptor);
            return descriptor;
        }

        /// <summary>
        /// Inserts a type representing a <see cref="IInputFormatter"/> to a descriptor collection.
        /// </summary>
        /// <param name="descriptors">A list of InputFormatterDescriptors</param>
        /// <param name="inputFormatterType">Type representing an <see cref="IInputFormatter"/>.</param>
        /// <returns>InputFormatterDescriptor representing the inserted instance.</returns>
        public static InputFormatterDescriptor Insert([NotNull] this IList<InputFormatterDescriptor> descriptors,
                                                      int index,
                                                      [NotNull] Type inputFormatterType)
        {
            if (index < 0 || index > descriptors.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            var descriptor = new InputFormatterDescriptor(inputFormatterType);
            descriptors.Insert(index, descriptor);
            return descriptor;
        }

        /// <summary>
        /// Adds an <see cref="IInputFormatter"/> to a descriptor collection.
        /// </summary>
        /// <param name="descriptors">A list of InputFormatterDescriptors</param>
        /// <param name="inputFormatter">An <see cref="IInputFormatter"/> instance.</param>
        /// <returns>InputFormatterDescriptor representing the added instance.</returns>
        public static InputFormatterDescriptor Add([NotNull] this IList<InputFormatterDescriptor> descriptors,
                                                   [NotNull] IInputFormatter inputFormatter)
        {
            var descriptor = new InputFormatterDescriptor(inputFormatter);
            descriptors.Add(descriptor);
            return descriptor;
        }

        /// <summary>
        /// Insert an <see cref="IInputFormatter"/> to a descriptor collection.
        /// </summary>
        /// <param name="descriptors">A list of InputFormatterDescriptors</param>
        /// <param name="inputFormatter">An <see cref="IInputFormatter"/> instance.</param>
        /// <returns>InputFormatterDescriptor representing the added instance.</returns>
        public static InputFormatterDescriptor Insert([NotNull] this IList<InputFormatterDescriptor> descriptors,
                                                      int index,
                                                      [NotNull] IInputFormatter inputFormatter)
        {
            if (index < 0 || index > descriptors.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            var descriptor = new InputFormatterDescriptor(inputFormatter);
            descriptors.Insert(index, descriptor);
            return descriptor;
        }
    }
}