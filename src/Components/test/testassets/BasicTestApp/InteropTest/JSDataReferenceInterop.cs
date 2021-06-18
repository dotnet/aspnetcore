// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace BasicTestApp.InteropTest
{
    public class JSDataReferenceInterop
    {
        [JSInvokable]
        public static async Task<string> JSToDotNetStreamParameterAsync(IJSDataReference jsDataReference)
        {
            using var dataReferenceStream = await jsDataReference.OpenReadStreamAsync();
            return await ValidateStreamValuesAsync(dataReferenceStream);
        }

        [JSInvokable]
        public static async Task<string> JSToDotNetStreamWrapperObjectParameterAsync(JSDataReferenceWrapper jsDataReferenceWrapper)
        {
            if (jsDataReferenceWrapper.StrVal != "SomeStr")
            {
                return $"StrVal did not match expected 'SomeStr', received {jsDataReferenceWrapper.StrVal}.";
            }
            else if (jsDataReferenceWrapper.IntVal != 5)
            {
                return $"IntVal did not match expected '5', received {jsDataReferenceWrapper.IntVal}.";
            }
            else
            {
                using var dataWrapperReferenceStream = await jsDataReferenceWrapper.JSDataReferenceVal.OpenReadStreamAsync();
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

        public class JSDataReferenceWrapper
        {
            public string StrVal { get; set; }

            [JsonPropertyName("jsDataReferenceVal")]
            public IJSDataReference JSDataReferenceVal { get; set; }

            public int IntVal { get; set; }
        }
    }
}
