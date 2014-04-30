using System.Threading;
using System.Threading.Tasks;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.HttpFeature
{
    [AssemblyNeutral]
    public interface IHttpSendFile
    {
        Task SendFileAsync(string path, long offset, long? length, CancellationToken cancellation);
    }
}