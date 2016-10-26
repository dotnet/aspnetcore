using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SocketsSample
{
    public class JsonInvocationAdapter : IInvocationAdapter
    {
        private JsonSerializer _serializer = new JsonSerializer();

        public JsonInvocationAdapter()
        {
        }

        public async Task<InvocationDescriptor> ReadInvocationDescriptor(Stream stream, Func<string, Type[]> getParams)
        {
            var reader = new JsonTextReader(new StreamReader(stream));
            return await Task.Run(() => _serializer.Deserialize<InvocationDescriptor>(reader));
        }

        public Task WriteInvocationResult(InvocationResultDescriptor resultDescriptor, Stream stream)
        {
            Write(resultDescriptor, stream);
            return Task.FromResult(0);
        }

        public Task WriteInvocationDescriptor(InvocationDescriptor invocationDescriptor, Stream stream)
        {
            Write(invocationDescriptor, stream);
            return Task.FromResult(0);
        }

        private void Write(object value, Stream stream)
        {
            var writer = new JsonTextWriter(new StreamWriter(stream));
            _serializer.Serialize(writer, value);
            writer.Flush();
        }
    }
}
