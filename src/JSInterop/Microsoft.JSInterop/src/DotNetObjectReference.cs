// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.JSInterop
{
    /// <summary>
    /// Provides convenience methods to produce a <see cref="DotNetObjectReference{TValue}" />.
    /// </summary>
    public static class DotNetObjectReference
    {
        /// <summary>
        /// Creates a new instance of <see cref="DotNetObjectReference{TValue}" />.
        /// </summary>
        /// <param name="value">The reference type to track.</param>
        /// <returns>An instance of <see cref="DotNetObjectReference{TValue}" />.</returns>
        public static DotNetObjectReference<TValue> Create<TValue>(TValue value) where TValue : class
        {
            return new DotNetObjectReference<TValue>(DotNetObjectReferenceManager.Current, value);
        }
    }
}
