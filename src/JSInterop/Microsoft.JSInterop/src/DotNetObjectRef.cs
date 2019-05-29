// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.JSInterop
{
    /// <summary>
    /// Provides convenience methods to produce a <see cref="DotNetObjectRef{TValue}" />.
    /// </summary>
    public static class DotNetObjectRef
    {
        /// <summary>
        /// Creates a new instance of <see cref="DotNetObjectRef{TValue}" />.
        /// </summary>
        /// <param name="value">The reference type to track.</param>
        /// <returns>An instance of <see cref="DotNetObjectRef{TValue}" />.</returns>
        public static DotNetObjectRef<TValue> Create<TValue>(TValue value) where TValue : class
        {
            return new DotNetObjectRef<TValue>(value);
        }
    }
}
