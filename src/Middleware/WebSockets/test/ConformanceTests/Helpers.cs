// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.WebSockets.ConformanceTest;

public class Helpers
{
    public static string GetApplicationPath(string projectName)
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, projectName));
    }
}
