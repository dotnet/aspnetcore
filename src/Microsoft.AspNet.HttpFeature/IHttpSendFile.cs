using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Interfaces
{
    public interface IHttpSendFile
    {
        Task SendFileAsync(string path, long offset, long? length, CancellationToken cancellation);
    }
}