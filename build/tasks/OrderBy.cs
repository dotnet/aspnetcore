// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace RepoTasks
{
    public class OrderBy : Task
    {
        [Required]
        [Output]
        public ITaskItem[] Items { get; set; }

        public string Key { get; set; }

        public override bool Execute()
        {
            var key = string.IsNullOrEmpty(Key)
                ? "Identity"
                : Key;
            Items = Items.OrderBy(k => k.GetMetadata(key)).ToArray();
            return true;
        }
    }
}
