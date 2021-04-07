// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// Extension methods for <see cref="IModelBinderProvider"/>.
    /// </summary>
    public static class ModelBinderProviderExtensions
    {
        /// <summary>
        /// Removes all model binder providers of the specified type.
        /// </summary>
        /// <param name="list">The list of <see cref="IModelBinderProvider"/>s.</param>
        /// <typeparam name="TModelBinderProvider">The type to remove.</typeparam>
        public static void RemoveType<TModelBinderProvider>(this IList<IModelBinderProvider> list) where TModelBinderProvider : IModelBinderProvider
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            RemoveType(list, typeof(TModelBinderProvider));
        }

        /// <summary>
        /// Removes all model binder providers of the specified type.
        /// </summary>
        /// <param name="list">The list of <see cref="IModelBinderProvider"/>s.</param>
        /// <param name="type">The type to remove.</param>
        public static void RemoveType(this IList<IModelBinderProvider> list, Type type)
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
                var modelBinderProvider = list[i];
                if (modelBinderProvider.GetType() == type)
                {
                    list.RemoveAt(i);
                }
            }
        }
    }
}
