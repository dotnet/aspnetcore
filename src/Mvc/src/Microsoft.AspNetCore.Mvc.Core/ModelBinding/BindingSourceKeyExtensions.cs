// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    internal static class BindingSourceKeyExtensions
    {
        private static readonly BindingSource[] BindingSources = new[]
        {
            BindingSource.Body,
            BindingSource.Custom,
            BindingSource.Form,
            BindingSource.FormFile,
            BindingSource.Header,
            BindingSource.ModelBinding,
            BindingSource.Path,
            BindingSource.Query,
            BindingSource.Services,
            BindingSource.Special,
        };

        /// <summary>
        ///
        /// </summary>
        /// <param name="bindingSource">
        /// The <see cref="BindingSourceKey"/> that indicates the <see cref="BindingSource"/> of interest.
        /// </param>
        /// <returns>The <see cref="BindingSource"/> that <paramref name="bindingSource"/> indicates.</returns>
        /// <exception cref="InvalidEnumArgumentException">
        /// Thrown if the <paramref name="bindingSource"/> is not a defined <see cref="BindingSourceKey"/> value.
        /// </exception>
        public static BindingSource GetBindingSource(this BindingSourceKey bindingSource)
        {
            var sourcesIndex = (int)bindingSource;
            if (!Enum.IsDefined(typeof(BindingSourceKey), bindingSource))
            {
                throw new InvalidEnumArgumentException(nameof(bindingSource), sourcesIndex, typeof(BindingSourceKey));
            }

            return BindingSources[sourcesIndex];
        }
    }
}
