using System;
using System.Collections.Generic;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Hosting.Startup
{
    public class NullStartupLoader : IStartupLoader
    {
        static NullStartupLoader()
        {
            Instance = new NullStartupLoader();
        }

        public static IStartupLoader Instance { get; private set; }

        public Action<IBuilder> LoadStartup(string applicationName, IList<string> diagnosticMessages)
        {
            return null;
        }
    }
}