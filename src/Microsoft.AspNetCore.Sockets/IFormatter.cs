using System.IO;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Sockets
{
    // TODO: Is this name too generic?
    public interface IFormatter
    {
        Task<T> ReadAsync<T>(Stream stream);
        Task WriteAsync<T>(T value, Stream stream);
    }
}
