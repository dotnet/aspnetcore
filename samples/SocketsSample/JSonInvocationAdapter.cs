using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SocketsSample
{
    public class JSonInvocationAdapter : IInvocationAdapter
    {
        IServiceProvider _serviceProvider;
        private JsonSerializer _serializer = new JsonSerializer();

        public JSonInvocationAdapter(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<InvocationDescriptor> CreateInvocationDescriptor(Stream stream, Func<string, Type[]> getParams)
        {
            // TODO: use a formatter (?)
            var reader = new JsonTextReader(new StreamReader(stream));
            return await Task.Run(() => _serializer.Deserialize<InvocationDescriptor>(reader));
        }

        public Task WriteInvocationResult(Stream stream, InvocationResultDescriptor resultDescriptor)
        {
            var writer = new JsonTextWriter(new StreamWriter(stream));
            _serializer.Serialize(writer, resultDescriptor);
            writer.Flush();
            return Task.FromResult(0);
        }

        public Task InvokeClientMethod(Stream stream, InvocationDescriptor invocationDescriptor)
        {
            var writer = new JsonTextWriter(new StreamWriter(stream));
            _serializer.Serialize(writer, invocationDescriptor);
            writer.Flush();
            return Task.FromResult(0);
        }
    }
}
