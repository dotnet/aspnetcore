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
        public static void EqualFileList(string root, IEnumerable<string> expectedFiles, FileSet actualFiles)
            => EqualFileList(root, expectedFiles, actualFiles.Select(f => f.FilePath));

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

        public static void EqualFileList(FileSet expectedFiles, FileSet actualFiles)
        {
            if (expectedFiles.Count != actualFiles.Count)
            {
                throw new AssertCollectionCountException(expectedFiles.Count, actualFiles.Count);
            }

            foreach (var expected in expectedFiles)
            {
                var actual = actualFiles.FirstOrDefault(f => Normalize(expected.FilePath) == Normalize(f.FilePath));

                if (actual.FilePath is null)
                {
                    throw new AssertActualExpectedException(
                        expected: $"Expected to find  {expected.FilePath}.",
                        actual: "\n" + string.Join("\n", actualFiles.Select(f => f.FilePath)),
                        userMessage: "File sets should be equal.");
                }

                if (expected.FileKind != actual.FileKind || expected.StaticWebAssetPath != actual.StaticWebAssetPath)
                {
                    throw new AssertActualExpectedException(
                        expected: $"FileKind: {expected.FileKind} StaticWebAssetPath {expected.StaticWebAssetPath}",
                        actual: $"FileKind: {actual.FileKind} StaticWebAssetPath {actual.StaticWebAssetPath}",
                        userMessage: "Flle sets should be equal.");
                }
            }

            static string Normalize(string file) => file.Replace('\\', '/');
        }
    }
}
