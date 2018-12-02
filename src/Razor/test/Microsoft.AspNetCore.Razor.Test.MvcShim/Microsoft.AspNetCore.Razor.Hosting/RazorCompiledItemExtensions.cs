// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Hosting
{
    /// <summary>
    /// Extension methods for <see cref="RazorCompiledItem"/>.
    /// </summary>
    public static class RazorCompiledItemExtensions
    {
        /// <summary>
        /// Gets the list of <see cref="IRazorSourceChecksumMetadata"/> associated with <paramref name="item"/>.
        /// </summary>
        /// <param name="item">The <see cref="RazorCompiledItem"/>.</param>
        /// <returns>A list of <see cref="IRazorSourceChecksumMetadata"/>.</returns>
        public static IReadOnlyList<IRazorSourceChecksumMetadata> GetChecksumMetadata(this RazorCompiledItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            return item.Metadata.OfType<IRazorSourceChecksumMetadata>().ToArray();
        }
    }
}
