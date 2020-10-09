// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// A <see cref="ElementReferenceContext"/> for a web element.
    /// </summary>
    public class WebElementReferenceContext : ElementReferenceContext
    {
        internal IJSRuntime JSRuntime { get; }

        /// <summary>
        /// Initialize a new instance of <see cref="WebElementReferenceContext"/>.
        /// </summary>
        /// <param name="jsRuntime">The <see cref="IJSRuntime"/>.</param>
        public WebElementReferenceContext(IJSRuntime jsRuntime)
        {
            JSRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        }
    }
}
