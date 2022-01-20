// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;

namespace FunctionalTests;

public class EchoConnectionHandler : ConnectionHandler
{
    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        var context = connection.GetHttpContext();
        // The 'withCredentials' tests test if the connection is established, they don't send any messages
        if (context.Request.Query.TryGetValue("withCredentials", out var value))
        {
            if (value == "true")
            {
                return;
            }
        }

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
