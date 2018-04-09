// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace RepoTools.BuildGraph
{
    internal class ExternalDependency
    {
        public string PackageId { get; set; }
        public string Version { get; set; }
        public bool IsReferenced { get; set; }
    }
}
