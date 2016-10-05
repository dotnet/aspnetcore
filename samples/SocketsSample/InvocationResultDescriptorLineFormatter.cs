using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets;

namespace SocketsSample
{
    public class InvocationResultDescriptorLineFormatter : IFormatter<InvocationResultDescriptor>
    {
        public Task<InvocationResultDescriptor> ReadAsync(Stream stream)
        {
            throw new NotImplementedException();
        }

        public async Task WriteAsync(InvocationResultDescriptor value, Stream stream)
        {
            var msg = $"RI{value.Id}," +
                (!string.IsNullOrEmpty(value.Error)
                    ? $"E{value.Error}\n"
                    : $"R{value?.Result?.ToString() ?? string.Empty}\n");

            await WriteAsync(stream, msg);
        }

        private async Task WriteAsync(Stream stream, string msg)
        {
            var writer = new StreamWriter(stream);
            await writer.WriteAsync(msg);
            await writer.FlushAsync();
        }
    }
}
