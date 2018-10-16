// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using RepoTasks.Utilities;

namespace RepoTasks
{
    public class ResolveSymbolsRecursivePath : Task
    {
        [Required]
        [Output]
        public ITaskItem[] Symbols { get; set; }

        public override bool Execute()
        {
            foreach (var symbol in Symbols)
            {
                var fullPath = symbol.GetMetadata("PortablePDB");
                symbol.SetMetadata("SymbolsRecursivePath", fullPath.Substring(fullPath.IndexOf($"{Path.DirectorySeparatorChar}lib{Path.DirectorySeparatorChar}")));
            }

            return true;
        }
    }
}
