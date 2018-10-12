// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing.Internal
{
    /// <summary>
    /// <para>
    /// A singleton service that can be used to write the route table as a state machine
    /// in GraphViz DOT language https://www.graphviz.org/doc/info/lang.html
    /// </para>
    /// <para>
    /// You can use http://www.webgraphviz.com/ to visualize the results.
    /// </para>
    /// <para>
    /// This type has no support contract, and may be removed or changed at any time in
    /// a future release.
    /// </para>
    /// </summary>
    public class DfaGraphWriter
    {
        private readonly IServiceProvider _services;

        public DfaGraphWriter(IServiceProvider services)
        {
            _services = services;
        }

        public void Write(EndpointDataSource dataSource, TextWriter writer)
        {
            var builder = _services.GetRequiredService<DfaMatcherBuilder>();

            var endpoints = dataSource.Endpoints;
            for (var i = 0; i < endpoints.Count; i++)
            {
                if (endpoints[i] is RouteEndpoint endpoint && (endpoint.Metadata.GetMetadata<ISuppressMatchingMetadata>()?.SuppressMatching ?? false) == false)
                {
                    builder.AddEndpoint(endpoint);
                }
            }

            // Assign each node a sequential index.
            var visited = new Dictionary<DfaNode, int>();
            
            var tree = builder.BuildDfaTree(includeLabel: true);

            writer.WriteLine("digraph DFA {");
            tree.Visit(WriteNode);
            writer.WriteLine("}");

            void WriteNode(DfaNode node)
            {
                if (!visited.TryGetValue(node, out var label))
                {
                    label = visited.Count;
                    visited.Add(node, label);
                }

                // We can safely index into visited because this is a post-order traversal,
                // all of the children of this node are already in the dictionary.

                if (node.Literals != null)
                {
                    foreach (var literal in node.Literals)
                    {
                        writer.WriteLine($"{label} -> {visited[literal.Value]} [label=\"/{literal.Key}\"]");
                    }
                }

                if (node.Parameters != null)
                {
                    writer.WriteLine($"{label} -> {visited[node.Parameters]} [label=\"/*\"]");
                }

                if (node.CatchAll != null && node.Parameters != node.CatchAll)
                {
                    writer.WriteLine($"{label} -> {visited[node.CatchAll]} [label=\"/**\"]");
                }

                if (node.PolicyEdges != null)
                {
                    foreach (var policy in node.PolicyEdges)
                    {
                        writer.WriteLine($"{label} -> {visited[policy.Value]} [label=\"{policy.Key}\"]");
                    }
                }

                writer.WriteLine($"{label} [label=\"{node.Label}\"]");
            }
        }
    }
}
