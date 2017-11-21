// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace RepoTasks
{
    public class MapToFixedLengthPath : Task
    {
        [Required]
        public ITaskItem[] Files { get; set; }

        [Required]
        public string MapDirectory { get; set; }

        [Output]
        public ITaskItem[] Output { get; set; }

        public override bool Execute()
        {
            var output = new List<ITaskItem>();
            foreach (var file in Files)
            {
                var mappedPath = Path.Combine(MapDirectory, Path.GetRandomFileName() + Path.GetExtension(file.ItemSpec));
                file.SetMetadata("MappedPath", mappedPath);
                File.Copy(file.ItemSpec, mappedPath, overwrite: true);

                output.Add(file);
            }

            Output = output.ToArray();
            return true;
        }
    }
}
