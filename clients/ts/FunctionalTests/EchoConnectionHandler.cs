// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace FunctionalTests
{
    public class EchoConnectionHandler : ConnectionHandler
    {
        public async override Task OnConnectedAsync(ConnectionContext connection)
        {
            var result = await connection.Transport.Input.ReadAsync();
            var buffer = result.Buffer;

            try
            {
                if (!buffer.IsEmpty)
                {
                    await connection.Transport.Output.WriteAsync(buffer.ToArray());
                }
            }
            finally
            {
                connection.Transport.Input.AdvanceTo(result.Buffer.End);
            }

            // Wait for the user to close
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            connection.Transport.Input.OnWriterCompleted((ex, state) => 
            {
                if (ex != null) 
                {
                    ((TaskCompletionSource<object>)state).TrySetException(ex);
                }
                else
                {
                    ((TaskCompletionSource<object>)state).TrySetResult(null);
                }
            }, tcs);
            await tcs.Task;
        }
    }
}
