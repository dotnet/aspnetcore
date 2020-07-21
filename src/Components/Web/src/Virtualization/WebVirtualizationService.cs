// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Virtualization
{
    /// <summary>
    /// A service that provides web-specific virtualization helpers.
    /// </summary>
    public class WebVirtualizationService : IVirtualizationService
    {
        private readonly IJSRuntime _jsRuntime;

        /// <summary>
        /// Instantiates a new <see cref="WebVirtualizationService"/> instance.
        /// </summary>
        /// <param name="jsRuntime">The <see cref="IJSRuntime"/> dependency.</param>
        public WebVirtualizationService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        /// <inheritdoc />
        public IVirtualizationHelper CreateVirtualizationHelper()
            => new WebVirtualizationHelper(_jsRuntime);
    }
}
