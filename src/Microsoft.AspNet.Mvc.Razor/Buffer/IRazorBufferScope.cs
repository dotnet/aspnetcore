// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.Razor.Buffer
{
    /// <summary>
    /// Creates and manages the lifetime of <see cref="RazorBufferSegment"/> instances.
    /// </summary>
    public interface IRazorBufferScope
    {
        /// <summary>
        /// Gets a <see cref="RazorBufferSegment"/>.
        /// </summary>
        /// <returns>The <see cref="RazorBufferSegment"/>.</returns>
        RazorBufferSegment GetSegment();
    }
}
