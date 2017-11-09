// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace RepoTasks
{
    public class ReplaceInFile : Task
    {
        [Required]
        public string Filename { get; set; }

        [Required]
        public ITaskItem[] Items { get; set; }

        public override bool Execute()
        {
            var fileText = File.ReadAllText(Filename);

            foreach (var item in Items)
            {
                fileText = fileText.Replace(item.ItemSpec, item.GetMetadata("Replacement"));
            }

            File.WriteAllText(Filename, fileText);

            return true;
        }
    }
}
