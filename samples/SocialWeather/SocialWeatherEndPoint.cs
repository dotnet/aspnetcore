// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.Logging;

namespace SocialWeather
{
    public class SocialWeatherEndPoint : StreamingEndPoint
    {
        private readonly PersistentConnectionLifeTimeManager _lifetimeManager;
        private readonly FormatterResolver _formatterResolver;
        private readonly ILogger<SocialWeatherEndPoint> _logger;

        public SocialWeatherEndPoint(PersistentConnectionLifeTimeManager lifetimeManager,
            FormatterResolver formatterResolver, ILogger<SocialWeatherEndPoint> logger)
        {
            _lifetimeManager = lifetimeManager;
            _formatterResolver = formatterResolver;
            _logger = logger;
        }

        public async override Task OnConnectedAsync(StreamingConnection connection)
        {
            _lifetimeManager.OnConnectedAsync(connection);
            await ProcessRequests(connection);
            _lifetimeManager.OnDisconnectedAsync(connection);
        }

        public async Task ProcessRequests(StreamingConnection connection)
        {
            var stream = connection.Transport.GetStream();
            var formatter = _formatterResolver.GetFormatter<WeatherReport>(
                connection.Metadata.Get<string>("formatType"));

            WeatherReport weatherReport;
            while ((weatherReport = await formatter.ReadAsync(stream)) != null)
            {
                await _lifetimeManager.SendToAllAsync(weatherReport);
            }
        }
    }
}
