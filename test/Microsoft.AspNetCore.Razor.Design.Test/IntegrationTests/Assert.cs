// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    internal class Assert : Xunit.Assert
    {
        public static void BuildPassed(MSBuildResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            };

            if (result.ExitCode != 0)
            {
                throw new BuildFailedException(result);
            }
        }

        public static void FileExists(MSBuildResult result, params string[] paths)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var filePath = Path.Combine(result.Project.DirectoryPath, Path.Combine(paths));
            if (!File.Exists(filePath))
            {
                throw new FileMissingException(result, filePath);
            }
        }

        public static void FileCountEquals(MSBuildResult result, int expected, string directoryPath, string searchPattern)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (directoryPath == null)
            {
                throw new ArgumentNullException(nameof(directoryPath));
            }

            if (searchPattern == null)
            {
                throw new ArgumentNullException(nameof(searchPattern));
            }

            directoryPath = Path.Combine(result.Project.DirectoryPath, directoryPath);

            var files = Directory.GetFiles(directoryPath, searchPattern, SearchOption.AllDirectories);
            if (files.Length != expected)
            {
                throw new FileCountException(result, expected, directoryPath, searchPattern, files);
            }
        }

        public static void FileDoesNotExist(MSBuildResult result, params string[] paths)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var filePath = Path.Combine(result.Project.DirectoryPath, Path.Combine(paths));
            if (File.Exists(filePath))
            {
                throw new FileFoundException(result, filePath);
            }
        }

        private abstract class MSBuildXunitException : Xunit.Sdk.XunitException
        {
            protected MSBuildXunitException(MSBuildResult result)
            {
                Result = result;
            }

            protected abstract string Heading { get; }

            public MSBuildResult Result { get; }

            public override string Message
            {
                get
                {
                    var message = new StringBuilder();
                    message.AppendLine(Heading);
                    message.Append(Result.FileName);
                    message.Append(" ");
                    message.Append(Result.Arguments);
                    message.AppendLine();
                    message.AppendLine();
                    message.Append(Result.Output);
                    return message.ToString();
                }
            }
        }

        private class BuildFailedException : MSBuildXunitException
        {
            public BuildFailedException(MSBuildResult result)
                : base(result)
            {
            }

            protected override string Heading => "Build failed:";
        }

        private class FileMissingException : MSBuildXunitException
        {
            public FileMissingException(MSBuildResult result, string filePath)
                : base(result)
            {
                FilePath = filePath;
            }

            public string FilePath { get; }

            protected override string Heading => $"File: '{FilePath}' was not found.";
        }

        private class FileCountException : MSBuildXunitException
        {
            public FileCountException(MSBuildResult result, int expected, string directoryPath, string searchPattern, string[] files)
                : base(result)
            {
                Expected = expected;
                DirectoryPath = directoryPath;
                SearchPattern = searchPattern;
                Files = files;
            }

            public string DirectoryPath { get; }

            public int Expected { get; }

            public string[] Files { get; }

            public string SearchPattern { get; }

            protected override string Heading
            {
                get
                {
                    var heading = new StringBuilder();
                    heading.AppendLine($"Expected {Expected} files matching {SearchPattern} in {DirectoryPath}, found {Files.Length}.");

                    if (Files.Length > 0)
                    {
                        heading.AppendLine("Files:");

                        foreach (var file in Files)
                        {
                            heading.Append("\t");
                            heading.Append(file);
                        }

                        heading.AppendLine();
                    }

                    return heading.ToString();
                }
            }
        }

        private class FileFoundException : MSBuildXunitException
        {
            public FileFoundException(MSBuildResult result, string filePath)
                : base(result)
            {
                FilePath = filePath;
            }

            public string FilePath { get; }

            protected override string Heading => $"File: '{FilePath}' was found, but should not exist.";
        }
    }
}