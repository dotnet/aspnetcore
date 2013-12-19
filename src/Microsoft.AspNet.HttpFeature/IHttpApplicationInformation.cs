using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;

namespace Microsoft.AspNet.Interfaces
{
    public interface IHttpApplicationInformation
    {
        string AppName { get; set; }
        string AppMode { get; set; }
        CancellationToken OnAppDisposing { get; set; }
    }
}
