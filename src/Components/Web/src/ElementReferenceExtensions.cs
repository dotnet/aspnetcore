// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components
{
    public static class ElementReferenceExtensions
    {
        /// <summary>
        /// Gives focus to an element given its <see cref="ElementReference"/>.
        /// </summary>
        /// <param name="elementReference">A reference to the element to focus.</param>
        /// <returns>The <see cref="ValueTask"/> representing the asynchronous focus operation.</returns>
        public static ValueTask FocusAsync(this ElementReference elementReference)
        {
            var jsRuntime = elementReference.ServiceProvider?.GetService<IJSRuntime>();

            if (jsRuntime == null)
            {
                throw new InvalidOperationException("No JavaScript runtime found.");
            }

            return jsRuntime.InvokeVoidAsync(DomWrapperInterop.Focus, elementReference);
        }
    }
}
