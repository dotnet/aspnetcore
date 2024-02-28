// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop.Implementation;

namespace Microsoft.JSInterop.WebAssembly;

internal sealed class WebAssemblyJSObjectReferenceJsonConverter : JsonConverter<IJSObjectReference>
{
    private readonly WebAssemblyJSRuntime _jsRuntime;

    public WebAssemblyJSObjectReferenceJsonConverter(WebAssemblyJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert == typeof(WebAssemblyJSObjectReference) ||
            typeToConvert == typeof(IJSObjectReference) ||
            typeToConvert == typeof(IJSInProcessObjectReference);
    }

    public override IJSObjectReference? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var id = JSObjectReferenceJsonWorker.ReadJSObjectReferenceIdentifier(ref reader);
        return new WebAssemblyJSObjectReference(_jsRuntime, id);
    }

    public override void Write(Utf8JsonWriter writer, IJSObjectReference value, JsonSerializerOptions options)
    {
        JSObjectReferenceJsonWorker.WriteJSObjectReference(writer, (JSObjectReference)value);
    }
}
