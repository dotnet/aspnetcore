// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.IO.Pipelines.Text.Primitives;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Internal.Protocol
{
    public static class HubProtocolWriteMessageExtensions
    {
        public static async ValueTask<byte[]> WriteToArrayAsync(this IHubProtocol protocol, HubMessage message)
        {
            using (var memoryStream = new MemoryStream())
            {
                var pipe = memoryStream.AsPipelineWriter();

                // See https://github.com/dotnet/corefxlab/issues/1460, the TextEncoder is unimportant but required.
                var output = new PipelineTextOutput(pipe, TextEncoder.Utf8);

                // Encode the message
                if (!protocol.TryWriteMessage(message, output))
                {
                    throw new InvalidOperationException("Failed to write message to the output stream");
                }

                await output.FlushAsync();

                // Create a message
                return memoryStream.ToArray();
            }
        }
    }
}
