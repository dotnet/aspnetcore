// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.SignalR
{
    public class JsonNetInvocationAdapter : IInvocationAdapter
    {
        private JsonSerializer _serializer = new JsonSerializer();

        public JsonNetInvocationAdapter()
        {
        }

        public Task<InvocationDescriptor> ReadInvocationDescriptorAsync(Stream stream, Func<string, Type[]> getParams)
        {
            var reader = new JsonTextReader(new StreamReader(stream));
            // REVIEW: Task.Run()
            return Task.Run(() =>
            {
                var jsonInvocation = _serializer.Deserialize<JsonNetInvocationDescriptor>(reader);
                var invocation = new InvocationDescriptor
                {
                    Id = jsonInvocation.Id,
                    Method = jsonInvocation.Method,
                };

                var paramTypes = getParams(jsonInvocation.Method);
                invocation.Arguments = new object[paramTypes.Length];

                for (int i = 0; i < paramTypes.Length; i++)
                {
                    var paramType = paramTypes[i];
                    invocation.Arguments[i] = jsonInvocation.Arguments[i].ToObject(paramType, _serializer);
                }

                return invocation;
            });
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

        private class JsonNetInvocationDescriptor
        {
            public string Id { get; set; }

            public string Method { get; set; }

            public JArray Arguments { get; set; }
        }
    }
}
