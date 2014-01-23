using System.Threading;

namespace Microsoft.AspNet.HttpFeature
{
    public interface IHttpApplicationInformation
    {
        string AppName { get; set; }
        string AppMode { get; set; }
        CancellationToken OnAppDisposing { get; set; }
    }
}
