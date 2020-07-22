// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Extensions.Head
{
    internal static class HeadManagementJSRuntimeExtensions
    {
        private const string JsFunctionsPrefix = "_blazorHeadManager";

        public static ValueTask SetTitleAsync(this IJSRuntime jsRuntime, string title)
        {
            return jsRuntime.InvokeVoidAsync($"{JsFunctionsPrefix}.setTitle", title);
        }

        public static ValueTask AddOrUpdateHeadTagAsync(this IJSRuntime jsRuntime, TagElement tag, string id)
        {
            return jsRuntime.InvokeVoidAsync($"{JsFunctionsPrefix}.addOrUpdateHeadTag", tag, id);
        }

        public static ValueTask RemoveHeadTagAsync(this IJSRuntime jsRuntime, string id)
        {
            return jsRuntime.InvokeVoidAsync($"{JsFunctionsPrefix}.removeHeadTag", id);
        }
    }
}
