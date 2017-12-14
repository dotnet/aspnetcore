// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    internal class Assert : Xunit.Assert
    {
        public static void BuildPassed(MSBuildResult result)
        {
            NotNull(result);

            if (result.ExitCode != 0)
            {
                throw new BuildFailedException(result);
            }
        }

        public static void FileExists(MSBuildResult result, string filePath)
        {
            NotNull(result);
            NotNull(filePath);

            if (!File.Exists(Path.Combine(result.Project.DirectoryPath, filePath)))
            {
                throw new FileMissingException(result, filePath);
            }
        }

        private class BuildFailedException : Xunit.Sdk.XunitException
        {
            public BuildFailedException(MSBuildResult result)
            {
                Result = result;
            }

            public MSBuildResult Result { get; }

            public override string Message 
            {
                get 
                {
                    var message = new StringBuilder();
                    message.Append("Build failed: ");
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

        private class FileMissingException : Xunit.Sdk.XunitException
        {
            public FileMissingException(MSBuildResult result, string filePath)
            {
                Result = result;
                FilePath = filePath;
            }

            public string FilePath { get; }

            public MSBuildResult Result { get; }

            public override string Message
            {
                get
                {
                    var message = new StringBuilder();
                    message.Append($"File: '{FilePath}' was not found.");
                    message.Append(Result.FileName);
                    message.Append(" ");
                    message.Append(Result.Arguments);
                    message.AppendLine();
                    message.AppendLine();
                    message.Append(Result.Output);
                    return message.ToString();

                    ;
                }
            }
        }
    }
}