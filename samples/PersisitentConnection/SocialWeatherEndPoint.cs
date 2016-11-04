using System.Threading.Tasks;
using Channels;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.Logging;

namespace PersisitentConnection
{
    public class SocialWeatherEndPoint : EndPoint
    {
        private readonly PersistentConnectionLifeTimeManager _lifetimeManager;
        private readonly FormatterResolver _formatterResolver;
        private readonly ILogger<SocialWeatherEndPoint> _logger;
        private object _lockObj = new object();
        private WeatherReport _lastWeatherReport;

        public SocialWeatherEndPoint(PersistentConnectionLifeTimeManager lifetimeManager,
            FormatterResolver formatterResolver, ILogger<SocialWeatherEndPoint> logger)
        {
            _lifetimeManager = lifetimeManager;
            _formatterResolver = formatterResolver;
            _logger = logger;
        }

        public async override Task OnConnectedAsync(Connection connection)
        {
            _lifetimeManager.OnConnectedAsync(connection);
            await ProcessRequests(connection);
            _lifetimeManager.OnDisconnectedAsync(connection);
        }

        public async Task ProcessRequests(Connection connection)
        {
            var stream = connection.Channel.GetStream();
            var formatter = _formatterResolver.GetFormatter<WeatherReport>(
                connection.Metadata.Get<string>("formatType"));

            while (true)
            {
                var weatherReport = await formatter.ReadAsync(stream);
                lock(_lockObj)
                {
                    _lastWeatherReport = weatherReport;
                }
                await _lifetimeManager.SendToAllAsync(weatherReport);
            }
        }
    }
}
