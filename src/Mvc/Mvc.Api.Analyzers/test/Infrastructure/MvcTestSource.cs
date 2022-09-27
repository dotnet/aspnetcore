// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Analyzer.Testing;

namespace Microsoft.AspNetCore.Mvc;

public static class MvcTestSource
{
    // Test files are copied to both the bin/ and publish/ folders. Use BaseDirectory on or off Helix.
    private static readonly string ProjectDirectory = AppContext.BaseDirectory;

    public static TestSource Read(string testClassName, string testMethod)
    {
        var filePath = Path.Combine(ProjectDirectory, "TestFiles", testClassName, testMethod + ".cs");
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"TestFile {testMethod} could not be found at {filePath}.", filePath);
        }

        var fileContent = File.ReadAllText(filePath);
        return TestSource.Read(fileContent);
    }
}
