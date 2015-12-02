// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.Razor.Buffer
{
    /// <summary>
    /// Encapsulates a <see cref="ArraySegment{RazorValue}"/>.
    /// </summary>
    public struct RazorBufferSegment
    {
        /// <summary>
        /// Initializes a new instance of <see cref="RazorBufferSegment"/>.
        /// </summary>
        /// <param name="data">The <see cref="ArraySegment{RazorValue}"/> to encapsulate.</param>
        public RazorBufferSegment(ArraySegment<RazorValue> data)
        {
            Data = data;
        }

        /// <summary>
        /// Gets the <see cref="ArraySegment{RazorValue}"/>.
        /// </summary>
        public ArraySegment<RazorValue> Data { get; }
    }
}
