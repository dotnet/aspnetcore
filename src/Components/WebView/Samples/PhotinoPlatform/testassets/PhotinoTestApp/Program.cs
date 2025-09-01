// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace PhotinoTestApp;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            var sample = new LaunchSample();
            sample.Run();
            return;
        }
        var testScenario = args[0];
        if (testScenario == "--basic-test")
        {
            var basic = new BasicTest();
            basic.Run();
        }
        else
        {
            throw new ArgumentException($"Scenario {testScenario} is unknown.", nameof(args));
        }
    }
}
