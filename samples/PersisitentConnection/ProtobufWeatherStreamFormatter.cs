using System;
using System.IO;
using System.Threading.Tasks;

namespace PersisitentConnection
{
    public class ProtobufWeatherStreamFormatter : IStreamFormatter<Weather>
    {
        public Task<Weather> ReadAsync(Stream stream)
        {
            throw new NotImplementedException();
        }

        public Task WriteAsync(Weather value, Stream stream)
        {
            throw new NotImplementedException();
        }
    }
}
