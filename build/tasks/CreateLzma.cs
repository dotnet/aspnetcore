// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.DotNet.Archive;

namespace RepoTasks
{
    public class CreateLzma : Task
    {
        [Required]
        public string OutputPath { get; set; }

        [Required]
        public string[] Sources { get; set; }

        public override bool Execute()
        {
            var progress = new ConsoleProgressReport();
            using (var  archive = new IndexedArchive())
            {
                foreach (var source in Sources)
                {
                    if (Directory.Exists(source))
                    {
                        var trimmedSource = source.TrimEnd(new []{ '\\', '/' });
                        Log.LogMessage(MessageImportance.High, $"Adding directory: {trimmedSource}");
                        archive.AddDirectory(trimmedSource, progress);
                    }
                    else
                    {
                        Log.LogMessage(MessageImportance.High, $"Adding file: {source}");
                        archive.AddFile(source, Path.GetFileName(source));
                    }
                }

                archive.Save(OutputPath, progress);
            }

            return true;
        }
    }
}
