// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit.Sdk;

namespace Microsoft.AspNetCore;

public class AssertEx
{
    public static void DirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            throw new XunitException($"Expected directory to exist at {path} but it did not");
        }
    }

    public static void FileExists(string path)
    {
        if (!File.Exists(path))
        {
            throw new XunitException($"Expected file to exist at {path} but it did not");
        }
    }

    public static void FileDoesNotExists(string path)
    {
        if (File.Exists(path))
        {
            throw new XunitException($"File should not exist at {path}");
        }
    }
}
