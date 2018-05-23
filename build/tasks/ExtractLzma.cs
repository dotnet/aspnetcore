// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.DotNet.Archive;

namespace RepoTasks
{
    public class ExtractLzma : Task
    {
        [Required]
        public string InputArchive { get; set; }

        [Required]
        public string OutputPath { get; set; }

        public override bool Execute()
        {
            Log.LogMessage(MessageImportance.High, $"Extracting LZMA: {InputArchive}");
            new IndexedArchive().Extract(InputArchive, OutputPath, new ConsoleProgressReport());
            return true;
        }
    }
}
