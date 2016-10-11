using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SocketsSample
{
    public class JSonInvocationAdapter : IInvocationAdapter
    {
        private JsonSerializer _serializer = new JsonSerializer();

        public JSonInvocationAdapter()
        {
        }

        public async Task<InvocationDescriptor> CreateInvocationDescriptor(Stream stream, Func<string, Type[]> getParams)
        {
            var reader = new JsonTextReader(new StreamReader(stream));
            return await Task.Run(() => _serializer.Deserialize<InvocationDescriptor>(reader));
        }

        public Task WriteInvocationResult(Stream stream, InvocationResultDescriptor resultDescriptor)
        {
            Write(stream, resultDescriptor);
            return Task.FromResult(0);
        }

        public Task InvokeClientMethod(Stream stream, InvocationDescriptor invocationDescriptor)
        {
            Write(stream, invocationDescriptor);
            return Task.FromResult(0);
        }

        private void Write(Stream stream, object value)
        {
            var writer = new JsonTextWriter(new StreamWriter(stream));
            _serializer.Serialize(writer, value);
            writer.Flush();
        }
    }
}
