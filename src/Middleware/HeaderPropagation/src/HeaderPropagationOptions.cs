// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.HeaderPropagation
{
    /// <summary>
    /// Provides configuration for the <see cref="HeaderPropagationMiddleware"/>.
    /// </summary>
    public class HeaderPropagationOptions
    {
        /// <summary>
        /// Gets or sets the headers to be collected by the <see cref="HeaderPropagationMiddleware"/>
        /// and to be propagated by the <see cref="HeaderPropagationMessageHandler"/>.
        /// </summary>
        public IDictionary<string, HeaderPropagationEntry> Headers { get; set; } = new Dictionary<string, HeaderPropagationEntry>();
    }
}
