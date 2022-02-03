// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Microsoft.AspNetCore.Connections;

namespace SocialWeather;

public class SocialWeatherConnectionHandler : ConnectionHandler
{
    private readonly PersistentConnectionLifeTimeManager _lifetimeManager;
    private readonly FormatterResolver _formatterResolver;
    private readonly ILogger<SocialWeatherConnectionHandler> _logger;

    public SocialWeatherConnectionHandler(PersistentConnectionLifeTimeManager lifetimeManager,
        FormatterResolver formatterResolver, ILogger<SocialWeatherConnectionHandler> logger)
    {
        _lifetimeManager = lifetimeManager;
        _formatterResolver = formatterResolver;
        _logger = logger;
    }

    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        _lifetimeManager.OnConnectedAsync(connection);
        await ProcessRequests(connection);
        _lifetimeManager.OnDisconnectedAsync(connection);
    }

    public async Task ProcessRequests(ConnectionContext connection)
    {
        var formatter = _formatterResolver.GetFormatter<WeatherReport>(
            (string)connection.Items["format"]);

        while (true)
        {
            var result = await connection.Transport.Input.ReadAsync();
            var buffer = result.Buffer;
            try
            {
                if (!buffer.IsEmpty)
                {
                    var stream = new MemoryStream();
                    var data = buffer.ToArray();
                    await stream.WriteAsync(data, 0, data.Length);
                    stream.Position = 0;
                    var weatherReport = await formatter.ReadAsync(stream);
                    await _lifetimeManager.SendToAllAsync(weatherReport);
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
}
