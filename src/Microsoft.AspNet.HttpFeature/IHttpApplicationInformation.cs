using System.Threading;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.HttpFeature
{
    [AssemblyNeutral]
    public interface IHttpApplicationInformation
    {
        string AppName { get; set; }
        string AppMode { get; set; }
        CancellationToken OnAppDisposing { get; set; }
    }
}
