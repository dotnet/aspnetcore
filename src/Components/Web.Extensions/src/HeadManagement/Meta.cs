// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    /// <summary>
    /// A component that adds a meta element in the HTML head.
    /// </summary>
    public class Meta : ComponentBase, IDisposable
    {
        private readonly string _metaTagId = Guid.NewGuid().ToString("N");

        [Inject]
        private IJSRuntime JSRuntime { get; set; } = default!;

        /// <summary>
        /// Gets or sets a collection of additional attributes that will be applied to the link element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object>? Attributes { get; set; }

        /// <inheritdoc />
        protected override async Task OnParametersSetAsync()
        {
            await JSRuntime.InvokeVoidAsync(HeadManagementInterop.SetTag, "meta", _metaTagId, Attributes!);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Task.Run(() => JSRuntime.InvokeVoidAsync(HeadManagementInterop.RemoveTag, "meta", _metaTagId).AsTask());
        }
    }
}
