// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    /// <summary>
    /// A component that changes the title of the document.
    /// </summary>
    public class Title : ComponentBase
    {
        [Inject]
        private IJSRuntime JSRuntime { get; set; } = default!;

        /// <summary>
        /// Gets or sets the value to use as the document's title.
        /// </summary>
        [Parameter]
        public string Value { get; set; } = string.Empty;

        protected override async Task OnParametersSetAsync()
        {
             await JSRuntime.InvokeVoidAsync(HeadManagementInterop.SetTitle, Value);
        }
    }
}
