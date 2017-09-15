// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace RepoTasks.ProjectModel
{
    internal class DotNetCliReferenceInfo
    {
        public DotNetCliReferenceInfo(string id, string version)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException(nameof(id));
            }

            Id = id;
            Version = version;
        }

        public string Id { get; }
        public string Version { get; }
    }
}
