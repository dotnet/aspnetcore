using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SocialWeather.Json
{
    public class JsonStreamFormatter<T> : IStreamFormatter<T>
    {
        private JsonSerializer _serializer = new JsonSerializer();

        public async Task<T> ReadAsync(Stream stream)
        {
            var reader = new JsonTextReader(new StreamReader(stream));
            // REVIEW: Task.Run()
            return await Task.Run(() => _serializer.Deserialize<T>(reader));
        }

        public Task WriteAsync(T value, Stream stream)
        {
            var writer = new JsonTextWriter(new StreamWriter(stream));
            _serializer.Serialize(writer, value);
            writer.Flush();
            return Task.FromResult(0);
        }
    }
}
