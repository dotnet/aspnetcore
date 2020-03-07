// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching
{
    // Intermediate data structure used to build the DFA. Not used at runtime.
    [DebuggerDisplay("{DebuggerToString(),nq}")]
    internal class DfaNode
    {
        // The depth of the node. The depth indicates the number of segments
        // that must be processed to arrive at this node.
        //
        // This value is not computed for Policy nodes and will be set to -1.
        public int PathDepth { get; set; } = -1;

        // Just for diagnostics and debugging
        public string Label { get; set; }
        
        public List<Endpoint> Matches { get; private set; }

        public Dictionary<string, DfaNode> Literals { get; private set; }

        public DfaNode Parameters { get; set; }

        public DfaNode CatchAll { get; set; }

        public INodeBuilderPolicy NodeBuilder { get; set; }

        public Dictionary<object, DfaNode> PolicyEdges { get; private set; }

        public void AddPolicyEdge(object state, DfaNode node)
        {
            if (PolicyEdges == null)
            {
                PolicyEdges = new Dictionary<object, DfaNode>();
            }

            PolicyEdges.Add(state, node);
        }

        public void AddLiteral(string literal, DfaNode node)
        {
            if (Literals == null)
            {
                Literals = new Dictionary<string, DfaNode>(StringComparer.OrdinalIgnoreCase);
            }

            Literals.Add(literal, node);
        }

        public void AddMatch(Endpoint endpoint)
        {
            if (Matches == null)
            {
                Matches = new List<Endpoint>();
            }

            Matches.Add(endpoint);
        }

        public void AddMatches(IEnumerable<Endpoint> endpoints)
        {
            if (Matches == null)
            {
                Matches = new List<Endpoint>(endpoints);
            }
            else
            {
                Matches.AddRange(endpoints);
            }
        }

        public void Visit(Action<DfaNode> visitor)
        {
            if (Literals != null)
            {
                foreach (var kvp in Literals)
                {
                    kvp.Value.Visit(visitor);
                }
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

            if (PolicyEdges != null)
            {
                foreach (var kvp in PolicyEdges)
                {
                    kvp.Value.Visit(visitor);
                }
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
            builder.Append(Matches?.Count ?? 0);
            builder.Append(" c: ");
            if (Literals != null)
            {
                builder.AppendJoin(", ", Literals.Select(kvp => $"{kvp.Key}->({FormatNode(kvp.Value)})"));
            }
            return builder.ToString();
            
            // DfaNodes can be self-referential, don't traverse cycles.
            string FormatNode(DfaNode other)
            {
                return ReferenceEquals(this, other) ? "this" : other.DebuggerToString();
            }
        }
    }
}
