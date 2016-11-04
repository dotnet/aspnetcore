using System;
using System.IO;
using System.Threading.Tasks;

namespace PersisitentConnection
{
    public class ProtobufWeatherStreamFormatter : IStreamFormatter<WeatherReport>
    {
        public Task<WeatherReport> ReadAsync(Stream stream)
        {
            throw new NotImplementedException();
        }

        public Task WriteAsync(WeatherReport value, Stream stream)
        {
            throw new NotImplementedException();
        }
    }
}
