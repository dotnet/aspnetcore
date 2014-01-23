using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.HttpFeature
{
    public interface IHttpSendFile
    {
        Task SendFileAsync(string path, long offset, long? length, CancellationToken cancellation);
    }
}