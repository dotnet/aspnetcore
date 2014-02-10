using System;
using System.Collections.Generic;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Hosting.Startup
{
    public interface IStartupLoader
    {
        Action<IBuilder> LoadStartup(string applicationName, IList<string> diagnosticMessages);
    }
}
