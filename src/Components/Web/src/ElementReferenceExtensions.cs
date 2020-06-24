// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components
{
    public static class ElementReferenceExtensions
    {
        /// <summary>
        /// Gives focus to an element given its <see cref="ElementReference"/>.
        /// </summary>
        /// <param name="elementReference">A reference to the element to focus.</param>
        /// <param name="jsRuntime">The <see cref="IJSRuntime"/> used to perform the focus.</param>
        /// <returns>The <see cref="ValueTask"/> representing the asynchronous focus operation.</returns>
        public static ValueTask FocusAsync(this ElementReference elementReference, IJSRuntime jsRuntime)
        {
            return jsRuntime.InvokeVoidAsync(DomWrapperInterop.Focus, elementReference);
        }
    }
}
