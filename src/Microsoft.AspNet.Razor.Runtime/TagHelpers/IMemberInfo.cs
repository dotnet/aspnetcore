// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Metadata common to types and properties.
    /// </summary>
    public interface IMemberInfo
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Retrieves a collection of custom <see cref="Attribute"/>s of type <typeparamref name="TAttribute"/> applied
        /// to this instance of <see cref="IMemberInfo"/>.
        /// </summary>
        /// <typeparam name="TAttribute">The type of <see cref="Attribute"/> to search for.</typeparam>
        /// <returns>A sequence of custom <see cref="Attribute"/>s of type
        /// <typeparamref name="TAttribute"/>.</returns>
        /// <remarks>Result not include inherited <see cref="Attribute"/>s.</remarks>
        IEnumerable<TAttribute> GetCustomAttributes<TAttribute>()
            where TAttribute : Attribute;
    }
}
