// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Features;

namespace SignalRSamples.ConnectionHandlers;

public class MessagesConnectionHandler : ConnectionHandler
{
    private ConnectionList Connections { get; } = new ConnectionList();

    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        Connections.Add(connection);

        var transportType = connection.Features.Get<IHttpTransportFeature>()?.TransportType;

        await Broadcast($"{connection.ConnectionId} connected ({transportType})");

        try
        {
            while (true)
            {
                var result = await connection.Transport.Input.ReadAsync();
                var buffer = result.Buffer;

                try
                {
                    if (!buffer.IsEmpty)
                    {
                        // We can avoid the copy here but we'll deal with that later
                        var text = Encoding.UTF8.GetString(buffer.ToArray());
                        text = $"{connection.ConnectionId}: {text}";
                        await Broadcast(Encoding.UTF8.GetBytes(text));
                    }
                    else if (result.IsCompleted)
                    {
                        break;
                    }
                }
                finally
                {
                    connection.Transport.Input.AdvanceTo(buffer.End);
                }
            }
        }
        finally
        {
            Connections.Remove(connection);

            await Broadcast($"{connection.ConnectionId} disconnected ({transportType})");
        }
    }

    private Task Broadcast(string text)
    {
        return Broadcast(Encoding.UTF8.GetBytes(text));
    }

    private Task Broadcast(byte[] payload)
    {
        var tasks = new List<Task>(Connections.Count);
        foreach (var c in Connections)
        {
            tasks.Add(c.Transport.Output.WriteAsync(payload).AsTask());
        }

        return Task.WhenAll(tasks);
    }
}
