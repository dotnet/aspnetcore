// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;
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

        public Task<InvocationMessage> ReadMessageAsync(Stream stream, IInvocationBinder binder, CancellationToken cancellationToken)
        {
            var reader = new JsonTextReader(new StreamReader(stream));
            // REVIEW: Task.Run()
            return Task.Run<InvocationMessage>(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var json = _serializer.Deserialize<JObject>(reader);
                if (json == null)
                {
                    return null;
                }

                // Determine the type of the message
                if (json["Result"] != null)
                {
                    // It's a result
                    return BindInvocationResultDescriptor(json, binder, cancellationToken);
                }
                else
                {
                    return BindInvocationDescriptor(json, binder, cancellationToken);
                }
            }, cancellationToken);
        }

        public Task WriteMessageAsync(InvocationMessage message, Stream stream, CancellationToken cancellationToken)
        {
            var writer = new JsonTextWriter(new StreamWriter(stream));
            _serializer.Serialize(writer, message);
            writer.Flush();
            return TaskCache.CompletedTask;
        }

        private InvocationDescriptor BindInvocationDescriptor(JObject json, IInvocationBinder binder, CancellationToken cancellationToken)
        {
            var invocation = new InvocationDescriptor
            {
                Id = json.Value<string>("Id"),
                Method = json.Value<string>("Method"),
            };

            var paramTypes = binder.GetParameterTypes(invocation.Method);
            invocation.Arguments = new object[paramTypes.Length];

            var args = json.Value<JArray>("Arguments");
            for (var i = 0; i < paramTypes.Length; i++)
            {
                var paramType = paramTypes[i];
                invocation.Arguments[i] = args[i].ToObject(paramType, _serializer);
            }

            return invocation;
        }

        private InvocationResultDescriptor BindInvocationResultDescriptor(JObject json, IInvocationBinder binder, CancellationToken cancellationToken)
        {
            var id = json.Value<string>("Id");
            var returnType = binder.GetReturnType(id);
            var result = new InvocationResultDescriptor()
            {
                Id = id,
                Result = returnType == null ? null : json["Result"].ToObject(returnType, _serializer),
                Error = json.Value<string>("Error")
            };
            return result;
        }
    }
}
