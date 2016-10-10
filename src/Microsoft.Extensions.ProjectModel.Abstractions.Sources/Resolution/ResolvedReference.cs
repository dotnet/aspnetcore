// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Extensions.ProjectModel.Resolution
{
    public class ResolvedReference
    {
        public ResolvedReference(string name,string resolvedPath)
        {
            Name = name;
            ResolvedPath = resolvedPath;
        }
        public string ResolvedPath { get; }
        public string Name { get; }
    }
}
