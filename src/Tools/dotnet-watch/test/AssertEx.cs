// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit.Sdk;

namespace Microsoft.DotNet.Watcher.Tools.Tests
{
    public static class AssertEx
    {
        public static void EqualFileList(string root, IEnumerable<string> expectedFiles, IEnumerable<string> actualFiles)
        {
            var expected = expectedFiles.Select(p => Path.Combine(root, p));
            EqualFileList(expected, actualFiles);
        }

        public static void EqualFileList(IEnumerable<string> expectedFiles, IEnumerable<string> actualFiles)
        {
            string normalize(string p) => p.Replace('\\', '/');
            var expected = new HashSet<string>(expectedFiles.Select(normalize));
            var actual = new HashSet<string>(actualFiles.Where(p => !string.IsNullOrEmpty(p)).Select(normalize));
            if (!expected.SetEquals(actual))
            {
                throw new AssertActualExpectedException(
                    expected: "\n" + string.Join("\n", expected),
                    actual:  "\n" + string.Join("\n", actual),
                    userMessage: "File sets should be equal");
            }
        }
    }
}
