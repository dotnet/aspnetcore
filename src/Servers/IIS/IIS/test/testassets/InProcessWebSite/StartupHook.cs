using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestSite;

internal class StartupHook
{
    public static void Initialize()
    {
        Startup.StartupHookCalled = true;
    }
}
