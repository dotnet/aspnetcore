// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer
{
    /// <summary>
    /// Extension methods for <see cref="ApiDescription"/>.
    /// </summary>
    public static class ApiDescriptionExtensions
    {
        /// <summary>
        /// Gets the value of a property from the <see cref="ApiDescription.Properties"/> collection
        /// using the provided value of <typeparamref name="T"/> as the key.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="apiDescription">The <see cref="ApiDescription"/>.</param>
        /// <returns>The property or the default value of <typeparamref name="T"/>.</returns>
        public static T GetProperty<T>(this ApiDescription apiDescription)
        {
            if (apiDescription == null)
            {
                throw new ArgumentNullException(nameof(apiDescription));
            }

            object value;
            if (apiDescription.Properties.TryGetValue(typeof(T), out value))
            {
                return (T)value;
            }
            else
            {
                return default(T);
            }
        }

        /// <summary>
        /// Sets the value of an property in the <see cref="ApiDescription.Properties"/> collection using
        /// the provided value of <typeparamref name="T"/> as the key.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="apiDescription">The <see cref="ApiDescription"/>.</param>
        /// <param name="value">The value of the property.</param>
        public static void SetProperty<T>(this ApiDescription apiDescription, T value)
        {
            if (apiDescription == null)
            {
                throw new ArgumentNullException(nameof(apiDescription));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            apiDescription.Properties[typeof(T)] = value;
        }
    }
}