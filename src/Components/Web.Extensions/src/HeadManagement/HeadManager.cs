// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    internal class HeadManager : CircuitHandler
    {
        private const string JsFunctionsPrefix = "_blazorHeadManager";

        private readonly IJSRuntime _jsRuntime;

        public bool IsPrerendering { get; private set; }

        public HeadManager(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;

            IsPrerendering = !RuntimeInformation.IsOSPlatform(OSPlatform.Browser);
        }

        public void BuildHeadElementComment<TElement>(RenderTreeBuilder builder, TElement element) where TElement : IHeadElement
        {
            builder.AddMarkupContent(0, $"<!--Head:{JsonSerializer.Serialize(element, JsonSerializerOptionsProvider.Options)}-->");
        }

        public async ValueTask SetTitleAsync(string title)
        {
            await _jsRuntime.InvokeVoidAsync($"{JsFunctionsPrefix}.setTitle", title);
        }

        public async ValueTask ApplyTagAsync(TagElement tag, string id)
        {
            await _jsRuntime.InvokeVoidAsync($"{JsFunctionsPrefix}.applyHeadTag", tag, id);
        }

        public async ValueTask RemoveTagAsync(string id)
        {
            await _jsRuntime.InvokeVoidAsync($"{JsFunctionsPrefix}.removeHeadTag", id);
        }

        public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            IsPrerendering = false;

            await _jsRuntime.InvokeVoidAsync($"{JsFunctionsPrefix}.removePrerenderedHeadTags");
        }
    }
}
