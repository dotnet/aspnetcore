// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.WebViewE2E.Test;

class Program
{
    // Yes, this is a Program.Main() inside of a test project! This project is a regular xUnit.net test project, but
    // some tests also launch this project as a regular executable to launch UI tests. To achieve this, the CSPROJ
    // has the <StartupObject> property set to indicate that _this_ is the Program.Main() to use when launching as
    // an executable.
    [STAThread]
    static void Main(string[] args)
    {
        try
        {
            // Future idea: To support multiple tests, the 'args' could specify which test to run, and this code could run
            // different types/methods/args to control variations of that. Then in WebViewManagerE2ETests the arg could
            // be specified for each variation when launching this executable. But, for now, we have only 1 test, so no need
            // for extra complexity.

            var basicBlazorHybridTest = new BasicBlazorHybridTest();
            basicBlazorHybridTest.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception while running {typeof(BasicBlazorHybridTest).FullName}: {ex}");
        }
    }
}
