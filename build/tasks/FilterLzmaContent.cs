// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using RepoTasks.Utilities;

namespace RepoTasks
{
    public class FilterLzmaContent : Task
    {
        public ITaskItem[] PreviousLzmaContent { get; set; }

        [Required]
        public ITaskItem[] InputLzmaContent { get; set; }

        [Output]
        public ITaskItem[] FilteredLzmaContent { get; set; }


        public override bool Execute()
        {
            var previousContent = new HashSet<ITaskItem>(new ITaskItemComparer());
            var inputContent = new HashSet<ITaskItem>(new ITaskItemComparer());
            // Keeping dlls separate for use to trim xml
            var inputDlls = new HashSet<ITaskItem>(new ITaskItemComparer());
            var newContent = new List<ITaskItem>();

            if (PreviousLzmaContent != null)
            {
                foreach (var item in PreviousLzmaContent)
                {
                    // To round trip correctly, overwrite RecursiveDir with RelativeDir
                    item.SetMetadata("RecursiveDir", item.GetMetadata("RelativeDir"));
                    previousContent.Add(item);
                }
            }
            foreach (var item in InputLzmaContent)
            {
                var extension = item.GetExtension();

                inputContent.Add(item);
                if (string.Equals(".dll", extension, StringComparison.OrdinalIgnoreCase))
                {
                    inputDlls.Add(item);
                }
            }

            foreach (var item in inputContent)
            {
                // skip if contained in the previous LZMA
                if (previousContent.Contains(item))
                {
                    continue;
                }

                // skip if the file is an .xml that is matched with a .dll file
                if (string.Equals(".xml", item.GetExtension(), StringComparison.OrdinalIgnoreCase)
                    && inputDlls.Any(dll =>
                        // Match by filename
                        string.Equals(item.GetFileName(), dll.GetFileName(), StringComparison.OrdinalIgnoreCase)
                        // Match by folder structure (.xml must be under .dll's folder)
                        && item.GetRecursiveDir().StartsWith(dll.GetRecursiveDir())))
                {
                    continue;
                }

                newContent.Add(item);
            }

            FilteredLzmaContent = newContent.ToArray();

            return true;
        }

        private class ITaskItemComparer : IEqualityComparer<ITaskItem>
        {
            public bool Equals(ITaskItem x, ITaskItem y)
                => string.Equals(x.GetRecursiveDir(), y.GetRecursiveDir(), StringComparison.OrdinalIgnoreCase)
                    && string.Equals(x.GetFileName(), y.GetFileName(), StringComparison.OrdinalIgnoreCase)
                    && string.Equals(x.GetExtension(), y.GetExtension(), StringComparison.OrdinalIgnoreCase);

            public int GetHashCode(ITaskItem obj)
                => $"{obj.GetRecursiveDir()}{obj.GetFileName()}{obj.GetExtension()}".GetHashCode();
        }
    }
}
