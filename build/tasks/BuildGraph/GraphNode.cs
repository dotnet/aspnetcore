// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace RepoTools.BuildGraph
{
    [DebuggerDisplay("{Repository.Name}")]
    public class GraphNode
    {
        public Repository Repository { get; set; }

        public ISet<GraphNode> Incoming { get; } = new HashSet<GraphNode>();

        public ISet<GraphNode> Outgoing { get; } = new HashSet<GraphNode>();
    }
}
