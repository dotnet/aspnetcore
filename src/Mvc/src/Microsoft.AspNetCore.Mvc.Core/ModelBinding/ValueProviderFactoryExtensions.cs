// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// Extension methods for <see cref="IValueProviderFactory"/>.
    /// </summary>
    public static class ValueProviderFactoryExtensions
    {
        /// <summary>
        /// Removes all value provider factories of the specified type.
        /// </summary>
        /// <param name="list">The list of <see cref="IValueProviderFactory"/>.</param>
        /// <typeparam name="TValueProviderFactory">The type to remove.</typeparam>
        public static void RemoveType<TValueProviderFactory>(this IList<IValueProviderFactory> list) where TValueProviderFactory : IValueProviderFactory
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            RemoveType(list, typeof(TValueProviderFactory));
        }

        /// <summary>
        /// Removes all value provider factories of the specified type.
        /// </summary>
        /// <param name="list">The list of <see cref="IValueProviderFactory"/>.</param>
        /// <param name="type">The type to remove.</param>
        public static void RemoveType(this IList<IValueProviderFactory> list, Type type)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            for (var i = list.Count - 1; i >= 0; i--)
            {
                var valueProviderFactory = list[i];
                if (valueProviderFactory.GetType() == type)
                {
                    list.RemoveAt(i);
                }
            }
        }
    }
}
