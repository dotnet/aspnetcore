// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    /// <summary>
    /// A component that adds a link element to the HTML head.
    /// </summary>
    public class Link : ComponentBase, IDisposable
    {
        private readonly string _linkTagId = Guid.NewGuid().ToString("N");

        [Inject]
        private IJSRuntime JSRuntime { get; set; } = default!;

        /// <summary>
        /// Gets or sets a collection of additional attributes that will be applied to the link element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object>? Attributes { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            await JSRuntime.InvokeVoidAsync(HeadManagementInterop.SetTag, "link", _linkTagId, Attributes!);
        }

        public void Dispose()
        {
            Task.Run(() => JSRuntime.InvokeVoidAsync(HeadManagementInterop.RemoveTag, "link", _linkTagId));
        }
    }
}
