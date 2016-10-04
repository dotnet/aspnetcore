using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets;

namespace SocketsSample
{
    public class RpcTextFormatter : IFormatter
    {
        public async Task<T> ReadAsync<T>(Stream stream)
        {
            var streamReader = new StreamReader(stream);
            var line = await streamReader.ReadLineAsync();
            var values = line.Split(',');

            object x = new InvocationDescriptor
            {
                Id = values[0].Substring(2),
                Method = values[1].Substring(1),
                Arguments = values.Skip(2).ToArray()
            };

            return (T)x;
        }

        public async Task WriteAsync<T>(T value, Stream stream)
        {
            var result = value as InvocationResultDescriptor;
            if (result != null)
            {
                var msg = $"RI{result.Id}," + string.IsNullOrEmpty(result.Error) != null
                    ? $"E{result.Error}\n"
                    : $"R{result.Result.ToString()}\n";

                await WriteAsync(stream, msg);
                return;
            }

            var invocation = value as InvocationDescriptor;
            if (invocation != null)
            {
                var msg = $"CI{invocation.Id},M{invocation.Method},{string.Join(",", invocation.Arguments.Select(a => a.ToString()))}\n";
                await WriteAsync(stream, msg);
                return;
            }

            throw new NotImplementedException("Unsupported type");
        }

        private async Task WriteAsync(Stream stream, string msg)
        {
            var writer = new StreamWriter(stream);
            await writer.WriteAsync(msg);
            await writer.FlushAsync();
        }
    }
}
