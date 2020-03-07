// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace RepoTasks
{
    public class GenerateGuid : Task
    {
        [Output]
        public string Guid { get; private set; }

        [Required]
        public string NamespaceGuid { get; set; }

        [Required]
        public ITaskItem[] Values { get; set; }

        public override bool Execute()
        {
            try
            {
                var value = string.Join(",", Values.Select(o => o.ItemSpec).ToArray()).ToLowerInvariant();

                Guid = Uuid.Create(new Guid(NamespaceGuid), value).ToString();
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e);
            }

            return !Log.HasLoggedErrors;
        }
    }
}
