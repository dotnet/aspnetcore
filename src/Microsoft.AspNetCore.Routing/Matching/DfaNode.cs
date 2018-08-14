// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.AspNetCore.Routing.Matching
{
    // Intermediate data structure used to build the DFA. Not used at runtime.
    [DebuggerDisplay("{DebuggerToString(),nq}")]
    internal class DfaNode
    {
        public DfaNode()
        {
            Literals = new Dictionary<string, DfaNode>(StringComparer.OrdinalIgnoreCase);
            Matches = new List<MatcherEndpoint>();
            PolicyEdges = new Dictionary<object, DfaNode>();
        }

        // The depth of the node. The depth indicates the number of segments
        // that must be processed to arrive at this node.
        //
        // This value is not computed for Policy nodes and will be set to -1.
        public int PathDepth { get; set; } = -1;

        // Just for diagnostics and debugging
        public string Label { get; set; }
        
        public List<MatcherEndpoint> Matches { get; }

        public Dictionary<string, DfaNode> Literals { get; }

        public DfaNode Parameters { get; set; }

        public DfaNode CatchAll { get; set; }

        public INodeBuilderPolicy NodeBuilder { get; set; }

        public Dictionary<object, DfaNode> PolicyEdges { get; }

        public void Visit(Action<DfaNode> visitor)
        {
            foreach (var kvp in Literals)
            {
                kvp.Value.Visit(visitor);
            }

            // Break cycles
            if (Parameters != null && !ReferenceEquals(this, Parameters))
            {
                Parameters.Visit(visitor);
            }

            // Break cycles
            if (CatchAll != null && !ReferenceEquals(this, CatchAll))
            {
                CatchAll.Visit(visitor);
            }

            foreach (var kvp in PolicyEdges)
            {
                kvp.Value.Visit(visitor);
            }

            visitor(this);
        }

        private string DebuggerToString()
        {
            var builder = new StringBuilder();
            builder.Append(Label);
            builder.Append(" d:");
            builder.Append(PathDepth);
            builder.Append(" m:");
            builder.Append(Matches.Count);
            builder.Append(" c: ");
            builder.Append(string.Join(", ", Literals.Select(kvp => $"{kvp.Key}->({FormatNode(kvp.Value)})")));
            return builder.ToString();
            
            // DfaNodes can be self-referential, don't traverse cycles.
            string FormatNode(DfaNode other)
            {
                return ReferenceEquals(this, other) ? "this" : other.DebuggerToString();
            }
        }
    }
}
