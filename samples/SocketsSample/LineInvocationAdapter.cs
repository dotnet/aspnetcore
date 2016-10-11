
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SocketsSample
{
    public class LineInvocationAdapter : IInvocationAdapter
    {
        public async Task<InvocationDescriptor> CreateInvocationDescriptor(Stream stream, Func<string, Type[]> getParams)
        {
            var streamReader = new StreamReader(stream);
            var line = await streamReader.ReadLineAsync();
            var values = line.Split(',');

            var method = values[1].Substring(1);

            return new InvocationDescriptor
            {
                Id = values[0].Substring(2),
                Method = method,
                Arguments = values.Skip(2).Zip(getParams(method), (v, t) => Convert.ChangeType(v, t)).ToArray()
            };
        }

        public async Task InvokeClientMethod(Stream stream, InvocationDescriptor invocationDescriptor)
        {
            var msg = $"CI{invocationDescriptor.Id},M{invocationDescriptor.Method},{string.Join(",", invocationDescriptor.Arguments.Select(a => a.ToString()))}\n";
            await WriteAsync(stream, msg);
        }

        public async Task WriteInvocationResult(Stream stream, InvocationResultDescriptor resultDescriptor)
        {
            if (string.IsNullOrEmpty(resultDescriptor.Error))
            {
                await WriteAsync(stream, $"RI{resultDescriptor.Id},E{resultDescriptor.Error}\n");
            }
            else
            {
                await WriteAsync(stream, $"RI{resultDescriptor.Id},R{(resultDescriptor.Result != null ? resultDescriptor.Result.ToString() : string.Empty)}\n");
            }
        }

        private async Task WriteAsync(Stream stream, string msg)
        {
            var writer = new StreamWriter(stream);
            await writer.WriteAsync(msg);
            await writer.FlushAsync();
        }
    }
}
