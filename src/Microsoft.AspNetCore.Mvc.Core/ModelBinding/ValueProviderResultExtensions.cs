// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// Extensions methods for <see cref="ValueProviderResult"/>.
    /// </summary>
    [Obsolete("This type is obsolete and will be removed in a future version. " +
        "The recommended alternative is System.ComponentModel.TypeDescriptor.GetConverter().")]
    public static class ValueProviderResultExtensions
    {
        /// <summary>
        /// Attempts to convert the values in <paramref name="result"/> to the specified type.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> for conversion.</typeparam>
        /// <param name="result">The <see cref="ValueProviderResult"/>.</param>
        /// <returns>
        /// The converted value, or the default value of <typeparamref name="T"/> if the value could not be converted.
        /// </returns>
        public static T ConvertTo<T>(this ValueProviderResult result)
        {
            object valueToConvert = null;
            if (result.Values.Count == 1)
            {
                valueToConvert = result.Values[0];
            }
            else if (result.Values.Count > 1)
            {
                valueToConvert = result.Values.ToArray();
            }
            return ModelBindingHelper.ConvertTo<T>(valueToConvert, result.Culture);
        }

        /// <summary>
        /// Attempts to convert the values in <paramref name="result"/> to the specified type.
        /// </summary>
        /// <param name="result">The <see cref="ValueProviderResult"/>.</param>
        /// <param name="type">The <see cref="Type"/> for conversion.</param>
        /// <returns>
        /// The converted value, or the default value of <paramref name="type"/> if the value could not be converted.
        /// </returns>
        public static object ConvertTo(this ValueProviderResult result, Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            object valueToConvert = null;
            if (result.Values.Count == 1)
            {
                valueToConvert = result.Values[0];
            }
            else if (result.Values.Count > 1)
            {
                valueToConvert = result.Values.ToArray();
            }
            return ModelBindingHelper.ConvertTo(valueToConvert, type, result.Culture);
        }
    }
}
