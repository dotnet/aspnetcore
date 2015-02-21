// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.OptionDescriptors;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Extension methods for collections of option descriptors.
    /// </summary>
    public static class OptionDescriptorExtensions
    {
        /// <summary>
        /// Returns the only instance of <typeparamref name="TInstance"/> from a sequence of option descriptors.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance to find.</typeparam>
        /// <param name="descriptors">A sequence of <see cref="IOptionDescriptor{object}"/>.</param>
        /// <returns>The only instance of <typeparamref name="TInstance"/> from a sequence of 
        /// <see cref="IOptionDescriptor{object}"/>.</returns>
        /// <exception cref="System.InvalidOperationException"> 
        /// Thrown if there is not exactly one <typeparamref name="TInstance"/> in the sequence.</exception>
        public static TInstance InstanceOf<TInstance>(
            [NotNull] this IEnumerable<IOptionDescriptor<object>> descriptors)
        {
            var instance = descriptors
                .Single(descriptor => descriptor.OptionType == typeof(TInstance) && descriptor.Instance != null)
                .Instance;
            return (TInstance)instance;
        }

        /// <summary>
        /// Returns the only instance of <typeparamref name="TInstance"/> from a sequence of option descriptors, 
        /// or a default value if the sequence is empty.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance to find.</typeparam>
        /// <param name="descriptors">A sequence of <see cref="IOptionDescriptor{object}"/>.</param>
        /// <returns>The only instance of <typeparamref name="TInstance"/> from a sequence of 
        /// <see cref="IOptionDescriptor{object}"/>, 
        /// or a default value if the sequence is empty.</returns>
        /// <exception cref="System.InvalidOperationException"> 
        /// Thrown if there is more than one <typeparamref name="TInstance"/> in the sequence.</exception>
        public static TInstance InstanceOfOrDefault<TInstance>([NotNull] this
            IEnumerable<IOptionDescriptor<object>> descriptors)
        {
            var item = descriptors
                .SingleOrDefault(
                    descriptor => descriptor.OptionType == typeof(TInstance) && descriptor.Instance != null);
            var instance = default(TInstance);
            if (item != null)
            {
                instance = (TInstance)item.Instance;
            }
            return instance;
        }

        /// <summary>
        /// Returns all the instances of <typeparamref name="TInstance"/> from a sequence of option descriptors.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instances to find.</typeparam>
        /// <param name="descriptors">A sequence of <see cref="IOptionDescriptor{object}"/>.</param>
        /// <returns>An IEnumerable of <typeparamref name="TInstance"/> that contains instances from a sequence 
        /// of <see cref="IOptionDescriptor{object}"/>.</returns>
        public static IEnumerable<TInstance> InstancesOf<TInstance>([NotNull] this
            IEnumerable<IOptionDescriptor<object>> descriptors)
        {
            var instances = descriptors
                .Where(descriptor => descriptor.OptionType == typeof(TInstance) && descriptor.Instance != null)
                .Select(d => (TInstance)d.Instance);
            return instances;
        }
    }
}