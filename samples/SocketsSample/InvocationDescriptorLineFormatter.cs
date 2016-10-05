using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets;

namespace SocketsSample
{
    public class InvocationDescriptorLineFormatter: IFormatter<InvocationDescriptor>
    {
        public async Task<InvocationDescriptor> ReadAsync(Stream stream)
        {
            var streamReader = new StreamReader(stream);
            var line = await streamReader.ReadLineAsync();
            var values = line.Split(',');

            return new InvocationDescriptor
            {
                Id = values[0].Substring(2),
                Method = values[1].Substring(1),
                Arguments = values.Skip(2).ToArray()
            };
        }

        public async Task WriteAsync(InvocationDescriptor value, Stream stream)
        {
            var msg = $"CI{value.Id},M{value.Method},{string.Join(",", value.Arguments.Select(a => a.ToString()))}\n";
            await WriteAsync(stream, msg);
            return;
        }

        private async Task WriteAsync(Stream stream, string msg)
        {
            var writer = new StreamWriter(stream);
            await writer.WriteAsync(msg);
            await writer.FlushAsync();
        }
    }

    /*
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
*/
}
