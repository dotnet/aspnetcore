// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace SocketsSample
{
    public class LineInvocationAdapter : IInvocationAdapter
    {
        public async Task<InvocationMessage> ReadMessageAsync(Stream stream, IInvocationBinder binder, CancellationToken cancellationToken)
        {
            var streamReader = new StreamReader(stream);
            var line = await streamReader.ReadLineAsync();
            if (line == null)
            {
                return null;
            }

            var values = line.Split(',');

            var type = values[0].Substring(0, 2);
            var id = values[0].Substring(2);

            if (type.Equals("RI"))
            {
                var resultType = values[1].Substring(0, 1);
                var result = values[1].Substring(1);
                return new InvocationResultDescriptor()
                {
                    Id = id,
                    Result = resultType.Equals("E") ? null : result,
                    Error = resultType.Equals("E") ? result : null,
                };
            }
            else
            {
                var method = values[1].Substring(1);

                return new InvocationDescriptor
                {
                    Id = id,
                    Method = method,
                    Arguments = values.Skip(2).Zip(binder.GetParameterTypes(method), (v, t) => Convert.ChangeType(v, t)).ToArray()
                };
            }
        }

        public Task WriteMessageAsync(InvocationMessage message, Stream stream, CancellationToken cancellationToken)
        {
            var invocationDescriptor = message as InvocationDescriptor;
            if (invocationDescriptor != null)
            {
                return WriteInvocationDescriptorAsync(invocationDescriptor, stream);
            }
            else
            {
                return WriteInvocationResultAsync((InvocationResultDescriptor)message, stream);
            }
        }

        private Task WriteInvocationDescriptorAsync(InvocationDescriptor invocationDescriptor, Stream stream)
        {
            var msg = $"CI{invocationDescriptor.Id},M{invocationDescriptor.Method},{string.Join(",", invocationDescriptor.Arguments.Select(a => a.ToString()))}\n";
            return WriteAsync(msg, stream);
        }

        private Task WriteInvocationResultAsync(InvocationResultDescriptor resultDescriptor, Stream stream)
        {
            if (string.IsNullOrEmpty(resultDescriptor.Error))
            {
                return WriteAsync($"RI{resultDescriptor.Id},E{resultDescriptor.Error}\n", stream);
            }
            else
            {
                return WriteAsync($"RI{resultDescriptor.Id},R{(resultDescriptor.Result != null ? resultDescriptor.Result.ToString() : string.Empty)}\n", stream);
            }
        }

        private async Task WriteAsync(string msg, Stream stream)
        {
            var writer = new StreamWriter(stream);
            await writer.WriteAsync(msg);
            await writer.FlushAsync();
        }
    }
}
