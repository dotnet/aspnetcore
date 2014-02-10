using System;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Hosting.Startup
{
    public interface IStartupManager
    {
        Action<IBuilder> LoadStartup(string applicationName);
    }
}