// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace BasicTestApp.InteropTest;

public class JSStreamReferenceInterop
{
    [JSInvokable]
    public static async Task<string> JSToDotNetStreamParameterAsync(IJSStreamReference jsStreamReference)
    {
        using var dataReferenceStream = await jsStreamReference.OpenReadStreamAsync();
        return await ValidateStreamValuesAsync(dataReferenceStream);
    }

    [JSInvokable]
    public static async Task<string> JSToDotNetStreamWrapperObjectParameterAsync(JSStreamReferenceWrapper jsStreamReferenceWrapper)
    {
        if (jsStreamReferenceWrapper.StrVal != "SomeStr")
        {
            return $"StrVal did not match expected 'SomeStr', received {jsStreamReferenceWrapper.StrVal}.";
        }
        else if (jsStreamReferenceWrapper.IntVal != 5)
        {
            return $"IntVal did not match expected '5', received {jsStreamReferenceWrapper.IntVal}.";
        }
        else
        {
            using var dataWrapperReferenceStream = await jsStreamReferenceWrapper.JSStreamReferenceVal.OpenReadStreamAsync();
            return await ValidateStreamValuesAsync(dataWrapperReferenceStream);
        }
    }

    private static async Task<string> ValidateStreamValuesAsync(Stream stream)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        var buffer = memoryStream.ToArray();

        for (var i = 0; i < buffer.Length; i++)
        {
            var expectedValue = i % 256;
            if (buffer[i] != expectedValue)
            {
                return $"Failure at index {i}.";
            }
        }

        if (buffer.Length != 100_000)
        {
            return $"Failure, got a stream of length {buffer.Length}, expected a length of 100,000.";
        }

        return "Success";
    }

    public class JSStreamReferenceWrapper
    {
        public string StrVal { get; set; }

        [JsonPropertyName("jsStreamReferenceVal")]
        public IJSStreamReference JSStreamReferenceVal { get; set; }

        public int IntVal { get; set; }
    }
}
