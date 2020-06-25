// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components
{
    public class WebElementReferenceContext : ElementReferenceContext
    {
        internal IJSRuntime JSRuntime { get; }

        public WebElementReferenceContext(IJSRuntime jsRuntime)
        {
            JSRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        }
    }
}
