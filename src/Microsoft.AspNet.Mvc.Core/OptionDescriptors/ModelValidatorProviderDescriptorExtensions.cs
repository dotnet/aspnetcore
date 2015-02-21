// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.OptionDescriptors;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Extension methods for adding validator provider to a collection.
    /// </summary>
    public static class ModelValidatorProviderDescriptorExtensions
    {
        /// <summary>
        /// Adds a type representing a <see cref="IModelValidatorProvider"/> to <paramref name="descriptors"/>.
        /// </summary>
        /// <param name="descriptors">A list of <see cref="ModelValidatorProviderDescriptor"/>.</param>
        /// <param name="modelValidatorProviderType">Type representing an <see cref="IModelValidatorProvider"/></param>
        /// <returns>A <see cref="ModelValidatorProviderDescriptor"/> representing the added instance.</returns>
        public static ModelValidatorProviderDescriptor Add(
            [NotNull] this IList<ModelValidatorProviderDescriptor> descriptors,
            [NotNull] Type modelValidatorProviderType)
        {
            var descriptor = new ModelValidatorProviderDescriptor(modelValidatorProviderType);
            descriptors.Add(descriptor);
            return descriptor;
        }

        /// <summary>
        /// Inserts a type representing a <see cref="IModelValidatorProvider"/> in to <paramref name="descriptors"/> at
        /// the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="descriptors">A list of <see cref="ModelValidatorProviderDescriptor"/>.</param>
        /// <param name="index">The zero-based index at which <paramref name="modelValidatorProviderType"/>
        /// should be inserted.</param>
        /// <param name="modelValidatorProviderType">Type representing an <see cref="IModelValidatorProvider"/></param>
        /// <returns>A <see cref="ModelValidatorProviderDescriptor"/> representing the inserted instance.</returns>
        public static ModelValidatorProviderDescriptor Insert(
            [NotNull] this IList<ModelValidatorProviderDescriptor> descriptors,
            int index,
            [NotNull] Type modelValidatorProviderType)
        {
            if (index < 0 || index > descriptors.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var descriptor = new ModelValidatorProviderDescriptor(modelValidatorProviderType);
            descriptors.Insert(index, descriptor);
            return descriptor;
        }

        /// <summary>
        /// Adds an <see cref="IModelValidatorProvider"/> to <paramref name="descriptors"/>.
        /// </summary>
        /// <param name="descriptors">A list of <see cref="ModelValidatorProviderDescriptor"/>.</param>
        /// <param name="modelValidatorProvider">An <see cref="IModelBinder"/> instance.</param>
        /// <returns>A <see cref="ModelValidatorProviderDescriptor"/> representing the added instance.</returns>
        public static ModelValidatorProviderDescriptor Add(
            [NotNull] this IList<ModelValidatorProviderDescriptor> descriptors,
            [NotNull] IModelValidatorProvider modelValidatorProvider)
        {
            var descriptor = new ModelValidatorProviderDescriptor(modelValidatorProvider);
            descriptors.Add(descriptor);
            return descriptor;
        }

        /// <summary>
        /// Insert an <see cref="IModelValidatorProvider"/> in to <paramref name="descriptors"/> at the specified
        /// <paramref name="index"/>.
        /// </summary>
        /// <param name="descriptors">A list of <see cref="ModelValidatorProviderDescriptor"/>.</param>
        /// <param name="index">The zero-based index at which <paramref name="modelValidatorProvider"/>
        /// should be inserted.</param>
        /// <param name="modelValidatorProvider">An <see cref="IModelBinder"/> instance.</param>
        /// <returns>A <see cref="ModelValidatorProviderDescriptor"/> representing the added instance.</returns>
        public static ModelValidatorProviderDescriptor Insert(
            [NotNull] this IList<ModelValidatorProviderDescriptor> descriptors,
            int index,
            [NotNull] IModelValidatorProvider modelValidatorProvider)
        {
            if (index < 0 || index > descriptors.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var descriptor = new ModelValidatorProviderDescriptor(modelValidatorProvider);
            descriptors.Insert(index, descriptor);
            return descriptor;
        }
    }
}