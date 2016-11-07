// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.SignalR
{
    public class JsonNetInvocationAdapter : IInvocationAdapter
    {
        private JsonSerializer _serializer = new JsonSerializer();

        public JsonNetInvocationAdapter()
        {
        }

        public async Task<InvocationDescriptor> ReadInvocationDescriptorAsync(Stream stream, Func<string, Type[]> getParams)
        {
            var reader = new JsonTextReader(new StreamReader(stream));
            // REVIEW: Task.Run()
            return await Task.Run(() => _serializer.Deserialize<InvocationDescriptor>(reader));
        }

        public Task WriteInvocationResultAsync(InvocationResultDescriptor resultDescriptor, Stream stream)
        {
            Write(resultDescriptor, stream);
            return Task.FromResult(0);
        }

        public Task WriteInvocationDescriptorAsync(InvocationDescriptor invocationDescriptor, Stream stream)
        {
            Write(invocationDescriptor, stream);
            return Task.FromResult(0);
        }

        private void Write(object value, Stream stream)
        {
            var writer = new JsonTextWriter(new StreamWriter(stream));
            _serializer.Serialize(writer, value);
            writer.Flush();
        }
    }
}
