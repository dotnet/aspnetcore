// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.Abstractions
{
    /// <summary>
    /// Extension methods for <see cref="ActionDescriptor"/>.
    /// </summary>
    public static class ActionDescriptorExtensions
    {
        /// <summary>
        /// Gets the value of a property from the <see cref="ActionDescriptor.Properties"/> collection
        /// using the provided value of <typeparamref name="T"/> as the key.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="actionDescriptor">The action descriptor.</param>
        /// <returns>The property or the default value of <typeparamref name="T"/>.</returns>
        public static T GetProperty<T>(this ActionDescriptor actionDescriptor)
        {
            if (actionDescriptor == null)
            {
                throw new ArgumentNullException(nameof(actionDescriptor));
            }

            object value;
            if (actionDescriptor.Properties.TryGetValue(typeof(T), out value))
            {
                return (T)value;
            }
            else
            {
                return default(T);
            }
        }

        /// <summary>
        /// Sets the value of an property in the <see cref="ActionDescriptor.Properties"/> collection using
        /// the provided value of <typeparamref name="T"/> as the key.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="actionDescriptor">The action descriptor.</param>
        /// <param name="value">The value of the property.</param>
        public static void SetProperty<T>(this ActionDescriptor actionDescriptor, T value)
        {
            if (actionDescriptor == null)
            {
                throw new ArgumentNullException(nameof(actionDescriptor));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            actionDescriptor.Properties[typeof(T)] = value;
        }
    }
}