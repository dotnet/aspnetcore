// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.HeaderPropagation
{
    /// <summary>
    /// Provides configuration for the <see cref="HeaderPropagationMiddleware"/>.
    /// </summary>
    public class HeaderPropagationClientOptions
    {
        /// <summary>
        /// Gets or sets the headers to be captured by the <see cref="HeaderPropagationMiddleware"/>
        /// and to be propagated by the <see cref="HeaderPropagationMessageHandler"/>.
        /// </summary>
        /// <remarks>
        /// Entries in <see cref="Headers"/> are processes in order while capturing headers inside
        /// <see cref="HeaderPropagationMiddleware"/>. This can cause an earlier entry to take precedence
        /// over a later entry if they have the same <see cref="HeaderPropagationEntry.CapturedHeaderName"/>.
        /// </remarks>
        public HeaderPropagationClientEntryCollection Headers { get; set; } = new HeaderPropagationClientEntryCollection();
    }
}
