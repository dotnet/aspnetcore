// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Xunit.Sdk;

namespace Microsoft.AspNetCore
{
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
}
