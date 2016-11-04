using System.Threading.Tasks;
using Channels;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.Logging;

namespace PersisitentConnection
{
    public class SocialWeatherEndPoint : EndPoint
    {
        private readonly PersistentConnectionLifeTimeManager _lifetimeManager = new PersistentConnectionLifeTimeManager();
        private readonly ILogger<SocialWeatherEndPoint> _logger;
        private object _lockObj = new object();
        private WeatherReport _lastWeatherReport;

        public SocialWeatherEndPoint(ILogger<SocialWeatherEndPoint> logger)
        {
            _logger = logger;
        }

        public async override Task OnConnectedAsync(Connection connection)
        {
            _lifetimeManager.OnConnectedAsync(connection);
            await DispatchMessagesAsync(connection);
            _lifetimeManager.OnDisconnectedAsync(connection);
        }

        public async Task DispatchMessagesAsync(Connection connection)
        {
            var stream = connection.Channel.GetStream();
            //var formatType = connection.Metadata.Get<string>("formatType");
            //var formatterRegistry = _serviceProvider.GetRequiredService<FormatterRegistry>();
            //var formatter = formatterRegistry.GetFormatter(formatType);
            var formatter = new JsonStreamFormatter<WeatherReport>();

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
