// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Microsoft.AspNetCore.Connections;

namespace FunctionalTests;

public class EchoConnectionHandler : ConnectionHandler
{
    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        while (true)
        {
            var result = await connection.Transport.Input.ReadAsync();
            var buffer = result.Buffer;

            try
            {
                if (!buffer.IsEmpty)
                {
                    await connection.Transport.Output.WriteAsync(buffer.ToArray());
                }
                else if (result.IsCompleted)
                {
                    break;
                }
            }
            finally
            {
                connection.Transport.Input.AdvanceTo(result.Buffer.End);
            }
        }
    }
}
