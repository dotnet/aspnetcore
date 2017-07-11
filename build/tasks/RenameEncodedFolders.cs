// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace RepoTasks
{
    public class RenameEncodedFolders : Task
    {
        [Required]
        public string RootDirectory { get; set; }

        public override bool Execute()
        {
            foreach (var directory in Directory.EnumerateDirectories(RootDirectory, "*", SearchOption.AllDirectories))
            {
                var unescapedDirectory = Uri.UnescapeDataString(directory);
                if (!string.Equals(directory, unescapedDirectory, StringComparison.Ordinal))
                {
                    Log.LogMessage(MessageImportance.High, $"Moving {directory} to {unescapedDirectory}.");
                    Directory.Move(directory, unescapedDirectory);
                }
            }

            return true;
        }
    }
}
