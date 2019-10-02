// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Routing.Matching
{
    internal class DfaMatcherBuilder : MatcherBuilder
    {
        private readonly List<RouteEndpoint> _endpoints = new List<RouteEndpoint>();

        private readonly ILoggerFactory _loggerFactory;
        private readonly ParameterPolicyFactory _parameterPolicyFactory;
        private readonly EndpointSelector _selector;
        private readonly IEndpointSelectorPolicy[] _endpointSelectorPolicies;
        private readonly INodeBuilderPolicy[] _nodeBuilders;
        private readonly EndpointComparer _comparer;

        // These collections are reused when building candidates
        private readonly Dictionary<string, int> _assignments;
        private readonly List<KeyValuePair<string, object>> _slots;
        private readonly List<(string parameterName, int segmentIndex, int slotIndex)> _captures;
        private readonly List<(RoutePatternPathSegment pathSegment, int segmentIndex)> _complexSegments;
        private readonly List<KeyValuePair<string, IRouteConstraint>> _constraints;

        private int _stateIndex;

        public DfaMatcherBuilder(
            ILoggerFactory loggerFactory,
            ParameterPolicyFactory parameterPolicyFactory,
            EndpointSelector selector,
            IEnumerable<MatcherPolicy> policies)
        {
            _loggerFactory = loggerFactory;
            _parameterPolicyFactory = parameterPolicyFactory;
            _selector = selector;

            var (nodeBuilderPolicies, endpointComparerPolicies, endpointSelectorPolicies) = ExtractPolicies(policies.OrderBy(p => p.Order));
            _endpointSelectorPolicies = endpointSelectorPolicies;
            _nodeBuilders = nodeBuilderPolicies;
            _comparer = new EndpointComparer(endpointComparerPolicies);

            _assignments = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _slots = new List<KeyValuePair<string, object>>();
            _captures = new List<(string parameterName, int segmentIndex, int slotIndex)>();
            _complexSegments = new List<(RoutePatternPathSegment pathSegment, int segmentIndex)>();
            _constraints = new List<KeyValuePair<string, IRouteConstraint>>();
        }

        public override void AddEndpoint(RouteEndpoint endpoint)
        {
            _endpoints.Add(endpoint);
        }

        public DfaNode BuildDfaTree(bool includeLabel = false)
        {
            // We build the tree by doing a BFS over the list of entries. This is important
            // because a 'parameter' node can also traverse the same paths that literal nodes
            // traverse. This means that we need to order the entries first, or else we will
            // miss possible edges in the DFA.
            _endpoints.Sort(_comparer);

            // Since we're doing a BFS we will process each 'level' of the tree in stages
            // this list will hold the set of items we need to process at the current
            // stage.
            var work = new List<(RouteEndpoint endpoint, List<DfaNode> parents)>(_endpoints.Count);
            List<(RouteEndpoint endpoint, List<DfaNode> parents)> previousWork = null;

            var root = new DfaNode() { PathDepth = 0, Label = includeLabel ? "/" : null };

            // To prepare for this we need to compute the max depth, as well as
            // a seed list of items to process (entry, root).
            var maxDepth = 0;
            for (var i = 0; i < _endpoints.Count; i++)
            {
                var endpoint = _endpoints[i];
                maxDepth = Math.Max(maxDepth, endpoint.RoutePattern.PathSegments.Count);

                work.Add((endpoint, new List<DfaNode>() { root, }));
            }
            var workCount = work.Count;

            // Now we process the entries a level at a time.
            for (var depth = 0; depth <= maxDepth; depth++)
            {
                // As we process items, collect the next set of items.
                List<(RouteEndpoint endpoint, List<DfaNode> parents)> nextWork;
                var nextWorkCount = 0;
                if (previousWork == null)
                {
                    nextWork = new List<(RouteEndpoint endpoint, List<DfaNode> parents)>();
                }
                else
                {
                    // Reuse previous collection for the next collection
                    // Don't clear the list so nested lists can be reused
                    nextWork = previousWork;
                }

                for (var i = 0; i < workCount; i++)
                {
                    var (endpoint, parents) = work[i];

                    if (!HasAdditionalRequiredSegments(endpoint, depth))
                    {
                        for (var j = 0; j < parents.Count; j++)
                        {
                            var parent = parents[j];
                            parent.AddMatch(endpoint);
                        }
                    }

                    // Find the parents of this edge at the current depth
                    List<DfaNode> nextParents;
                    if (nextWorkCount < nextWork.Count)
                    {
                        nextParents = nextWork[nextWorkCount].parents;
                        nextParents.Clear();

                        nextWork[nextWorkCount] = (endpoint, nextParents);
                    }
                    else
                    {
                        nextParents = new List<DfaNode>();

                        // Add to the next set of work now so the list will be reused
                        // even if there are no parents
                        nextWork.Add((endpoint, nextParents));
                    }

                    var segment = GetCurrentSegment(endpoint, depth);
                    if (segment == null)
                    {
                        continue;
                    }

                    for (var j = 0; j < parents.Count; j++)
                    {
                        var parent = parents[j];
                        var part = segment.Parts[0];
                        var parameterPart = part as RoutePatternParameterPart;
                        if (segment.IsSimple && part is RoutePatternLiteralPart literalPart)
                        {
                            AddLiteralNode(includeLabel, nextParents, parent, literalPart.Content);
                        }
                        else if (segment.IsSimple && parameterPart != null && parameterPart.IsCatchAll)
                        {
                            // A catch all should traverse all literal nodes as well as parameter nodes
                            // we don't need to create the parameter node here because of ordering
                            // all catchalls will be processed after all parameters.
                            if (parent.Literals != null)
                            {
                                nextParents.AddRange(parent.Literals.Values);
                            }
                            if (parent.Parameters != null)
                            {
                                nextParents.Add(parent.Parameters);
                            }

                            // We also create a 'catchall' here. We don't do further traversals
                            // on the catchall node because only catchalls can end up here. The
                            // catchall node allows us to capture an unlimited amount of segments
                            // and also to match a zero-length segment, which a parameter node
                            // doesn't allow.
                            if (parent.CatchAll == null)
                            {
                                parent.CatchAll = new DfaNode()
                                {
                                    PathDepth = parent.PathDepth + 1,
                                    Label = includeLabel ? parent.Label + "{*...}/" : null,
                                };

                                // The catchall node just loops.
                                parent.CatchAll.Parameters = parent.CatchAll;
                                parent.CatchAll.CatchAll = parent.CatchAll;
                            }

                            parent.CatchAll.AddMatch(endpoint);
                        }
                        else if (segment.IsSimple && parameterPart != null && TryGetRequiredValue(endpoint.RoutePattern, parameterPart, out var requiredValue))
                        {
                            // If the parameter has a matching required value, replace the parameter with the required value
                            // as a literal. This should use the parameter's transformer (if present)
                            // e.g. Template: Home/{action}, Required values: { action = "Index" }, Result: Home/Index

                            if (endpoint.RoutePattern.ParameterPolicies.TryGetValue(parameterPart.Name, out var parameterPolicyReferences))
                            {
                                for (var k = 0; k < parameterPolicyReferences.Count; k++)
                                {
                                    var reference = parameterPolicyReferences[k];
                                    var parameterPolicy = _parameterPolicyFactory.Create(parameterPart, reference);
                                    if (parameterPolicy is IOutboundParameterTransformer parameterTransformer)
                                    {
                                        requiredValue = parameterTransformer.TransformOutbound(requiredValue);
                                        break;
                                    }
                                }
                            }

                            AddLiteralNode(includeLabel, nextParents, parent, requiredValue.ToString());
                        }
                        else if (segment.IsSimple && parameterPart != null)
                        {
                            if (parent.Parameters == null)
                            {
                                parent.Parameters = new DfaNode()
                                {
                                    PathDepth = parent.PathDepth + 1,
                                    Label = includeLabel ? parent.Label + "{...}/" : null,
                                };
                            }

                            // A parameter should traverse all literal nodes as well as the parameter node
                            if (parent.Literals != null)
                            {
                                nextParents.AddRange(parent.Literals.Values);
                            }
                            nextParents.Add(parent.Parameters);
                        }
                        else
                        {
                            // Complex segment - we treat these are parameters here and do the
                            // expensive processing later. We don't want to spend time processing
                            // complex segments unless they are the best match, and treating them
                            // like parameters in the DFA allows us to do just that.
                            if (parent.Parameters == null)
                            {
                                parent.Parameters = new DfaNode()
                                {
                                    PathDepth = parent.PathDepth + 1,
                                    Label = includeLabel ? parent.Label + "{...}/" : null,
                                };
                            }

                            if (parent.Literals != null)
                            {
                                nextParents.AddRange(parent.Literals.Values);
                            }
                            nextParents.Add(parent.Parameters);
                        }
                    }

                    if (nextParents.Count > 0)
                    {
                        nextWorkCount++;
                    }
                }

                // Prepare the process the next stage.
                previousWork = work;
                work = nextWork;
                workCount = nextWorkCount;
            }

            // Build the trees of policy nodes (like HTTP methods). Post-order traversal
            // means that we won't have infinite recursion.
            root.Visit(ApplyPolicies);

            return root;
        }

        private static void AddLiteralNode(bool includeLabel, List<DfaNode> nextParents, DfaNode parent, string literal)
        {
            DfaNode next = null;
            if (parent.Literals == null ||
                !parent.Literals.TryGetValue(literal, out next))
            {
                next = new DfaNode()
                {
                    PathDepth = parent.PathDepth + 1,
                    Label = includeLabel ? parent.Label + literal + "/" : null,
                };
                parent.AddLiteral(literal, next);
            }

            nextParents.Add(next);
        }

        private RoutePatternPathSegment GetCurrentSegment(RouteEndpoint endpoint, int depth)
        {
            if (depth < endpoint.RoutePattern.PathSegments.Count)
            {
                return endpoint.RoutePattern.PathSegments[depth];
            }

            if (endpoint.RoutePattern.PathSegments.Count == 0)
            {
                return null;
            }

            var lastSegment = endpoint.RoutePattern.PathSegments[endpoint.RoutePattern.PathSegments.Count - 1];
            if (lastSegment.IsSimple && lastSegment.Parts[0] is RoutePatternParameterPart parameterPart && parameterPart.IsCatchAll)
            {
                return lastSegment;
            }

            return null;
        }

        public override Matcher Build()
        {
#if DEBUG
            var includeLabel = true;
#else
            var includeLabel = false;
#endif

            var root = BuildDfaTree(includeLabel);

            // State count is the number of nodes plus an exit state
            var stateCount = 1;
            var maxSegmentCount = 0;
            root.Visit((node) =>
            {
                stateCount++;
                maxSegmentCount = Math.Max(maxSegmentCount, node.PathDepth);
            });
            _stateIndex = 0;

            // The max segment count is the maximum path-node-depth +1. We need
            // the +1 to capture any additional content after the 'last' segment.
            maxSegmentCount++;

            var states = new DfaState[stateCount];
            var exitDestination = stateCount - 1;
            AddNode(root, states, exitDestination);

            // The root state only has a jump table.
            states[exitDestination] = new DfaState(
                Array.Empty<Candidate>(),
                Array.Empty<IEndpointSelectorPolicy>(),
                JumpTableBuilder.Build(exitDestination, exitDestination, null),
                null);

            return new DfaMatcher(_loggerFactory.CreateLogger<DfaMatcher>(), _selector, states, maxSegmentCount);
        }

        private int AddNode(
            DfaNode node,
            DfaState[] states,
            int exitDestination)
        {
            node.Matches?.Sort(_comparer);

            var currentStateIndex = _stateIndex;

            var currentDefaultDestination = exitDestination;
            var currentExitDestination = exitDestination;
            (string text, int destination)[] pathEntries = null;
            PolicyJumpTableEdge[] policyEntries = null;

            if (node.Literals != null)
            {
                pathEntries = new (string text, int destination)[node.Literals.Count];

                var index = 0;
                foreach (var kvp in node.Literals)
                {
                    var transition = Transition(kvp.Value);
                    pathEntries[index++] = (kvp.Key, transition);
                }
            }

            if (node.Parameters != null &&
                node.CatchAll != null &&
                ReferenceEquals(node.Parameters, node.CatchAll))
            {
                // This node has a single transition to but it should accept zero-width segments
                // this can happen when a node only has catchall parameters.
                currentExitDestination = currentDefaultDestination = Transition(node.Parameters);
            }
            else if (node.Parameters != null && node.CatchAll != null)
            {
                // This node has a separate transition for zero-width segments
                // this can happen when a node has both parameters and catchall parameters.
                currentDefaultDestination = Transition(node.Parameters);
                currentExitDestination = Transition(node.CatchAll);
            }
            else if (node.Parameters != null)
            {
                // This node has paramters but no catchall.
                currentDefaultDestination = Transition(node.Parameters);
            }
            else if (node.CatchAll != null)
            {
                // This node has a catchall but no parameters
                currentExitDestination = currentDefaultDestination = Transition(node.CatchAll);
            }

            if (node.PolicyEdges != null && node.PolicyEdges.Count > 0)
            {
                policyEntries = new PolicyJumpTableEdge[node.PolicyEdges.Count];

                var index = 0;
                foreach (var kvp in node.PolicyEdges)
                {
                    policyEntries[index++] = new PolicyJumpTableEdge(kvp.Key, Transition(kvp.Value));
                }
            }

            var candidates = CreateCandidates(node.Matches);

            // Perf: most of the time there aren't any endpoint selector policies, create
            // this lazily.
            List<IEndpointSelectorPolicy> endpointSelectorPolicies = null;
            if (node.Matches?.Count > 0)
            {
                for (var i = 0; i < _endpointSelectorPolicies.Length; i++)
                {
                    var endpointSelectorPolicy = _endpointSelectorPolicies[i];
                    if (endpointSelectorPolicy.AppliesToEndpoints(node.Matches))
                    {
                        if (endpointSelectorPolicies == null)
                        {
                            endpointSelectorPolicies = new List<IEndpointSelectorPolicy>();
                        }

                        endpointSelectorPolicies.Add(endpointSelectorPolicy);
                    }
                }
            }

            states[currentStateIndex] = new DfaState(
                candidates,
                endpointSelectorPolicies?.ToArray() ?? Array.Empty<IEndpointSelectorPolicy>(),
                JumpTableBuilder.Build(currentDefaultDestination, currentExitDestination, pathEntries),
                BuildPolicy(currentExitDestination, node.NodeBuilder, policyEntries));

            return currentStateIndex;

            int Transition(DfaNode next)
            {
                // Break cycles
                if (ReferenceEquals(node, next))
                {
                    return _stateIndex;
                }
                else
                {
                    _stateIndex++;
                    return AddNode(next, states, exitDestination);
                }
            }
        }

        private static PolicyJumpTable BuildPolicy(int exitDestination, INodeBuilderPolicy nodeBuilder, PolicyJumpTableEdge[] policyEntries)
        {
            if (policyEntries == null)
            {
                return null;
            }

            return nodeBuilder.BuildJumpTable(exitDestination, policyEntries);
        }

        // Builds an array of candidates for a node, assigns a 'score' for each
        // endpoint.
        internal Candidate[] CreateCandidates(IReadOnlyList<Endpoint> endpoints)
        {
            if (endpoints == null || endpoints.Count == 0)
            {
                return Array.Empty<Candidate>();
            }

            var candiates = new Candidate[endpoints.Count];

            var score = 0;
            var examplar = endpoints[0];
            candiates[0] = CreateCandidate(examplar, score);

            for (var i = 1; i < endpoints.Count; i++)
            {
                var endpoint = endpoints[i];
                if (!_comparer.Equals(examplar, endpoint))
                {
                    // This endpoint doesn't have the same priority.
                    examplar = endpoint;
                    score++;
                }

                candiates[i] = CreateCandidate(endpoint, score);
            }

            return candiates;
        }

        // internal for tests
        internal Candidate CreateCandidate(Endpoint endpoint, int score)
        {
            (string parameterName, int segmentIndex, int slotIndex) catchAll = default;

            if (endpoint is RouteEndpoint routeEndpoint)
            {
                _assignments.Clear();
                _slots.Clear();
                _captures.Clear();
                _complexSegments.Clear();
                _constraints.Clear();

                foreach (var kvp in routeEndpoint.RoutePattern.Defaults)
                {
                    _assignments.Add(kvp.Key, _assignments.Count);
                    _slots.Add(kvp);
                }

                for (var i = 0; i < routeEndpoint.RoutePattern.PathSegments.Count; i++)
                {
                    var segment = routeEndpoint.RoutePattern.PathSegments[i];
                    if (!segment.IsSimple)
                    {
                        continue;
                    }

                    var parameterPart = segment.Parts[0] as RoutePatternParameterPart;
                    if (parameterPart == null)
                    {
                        continue;
                    }

                    if (!_assignments.TryGetValue(parameterPart.Name, out var slotIndex))
                    {
                        slotIndex = _assignments.Count;
                        _assignments.Add(parameterPart.Name, slotIndex);

                        // A parameter can have a required value, default value/catch all, or be a normal parameter
                        // Add the required value or default value as the slot's initial value
                        if (TryGetRequiredValue(routeEndpoint.RoutePattern, parameterPart, out var requiredValue))
                        {
                            _slots.Add(new KeyValuePair<string, object>(parameterPart.Name, requiredValue));
                        }
                        else
                        {
                            var hasDefaultValue = parameterPart.Default != null || parameterPart.IsCatchAll;
                            _slots.Add(hasDefaultValue ? new KeyValuePair<string, object>(parameterPart.Name, parameterPart.Default) : default);
                        }
                    }

                    if (TryGetRequiredValue(routeEndpoint.RoutePattern, parameterPart, out _))
                    {
                        // Don't capture a parameter if it has a required value
                        // There is no need because a parameter with a required value is matched as a literal
                    }
                    else if (parameterPart.IsCatchAll)
                    {
                        catchAll = (parameterPart.Name, i, slotIndex);
                    }
                    else
                    {
                        _captures.Add((parameterPart.Name, i, slotIndex));
                    }
                }

                for (var i = 0; i < routeEndpoint.RoutePattern.PathSegments.Count; i++)
                {
                    var segment = routeEndpoint.RoutePattern.PathSegments[i];
                    if (segment.IsSimple)
                    {
                        continue;
                    }

                    _complexSegments.Add((segment, i));
                }

                foreach (var kvp in routeEndpoint.RoutePattern.ParameterPolicies)
                {
                    var parameter = routeEndpoint.RoutePattern.GetParameter(kvp.Key); // may be null, that's ok
                    var parameterPolicyReferences = kvp.Value;
                    for (var i = 0; i < parameterPolicyReferences.Count; i++)
                    {
                        var reference = parameterPolicyReferences[i];
                        var parameterPolicy = _parameterPolicyFactory.Create(parameter, reference);
                        if (parameterPolicy is IRouteConstraint routeConstraint)
                        {
                            _constraints.Add(new KeyValuePair<string, IRouteConstraint>(kvp.Key, routeConstraint));
                        }
                    }
                }

                return new Candidate(
                    endpoint,
                    score,
                    _slots.ToArray(),
                    _captures.ToArray(),
                    catchAll,
                    _complexSegments.ToArray(),
                    _constraints.ToArray());
            }
            else
            {
                return new Candidate(
                    endpoint,
                    score,
                    Array.Empty<KeyValuePair<string, object>>(),
                    Array.Empty<(string parameterName, int segmentIndex, int slotIndex)>(),
                    catchAll,
                    Array.Empty<(RoutePatternPathSegment pathSegment, int segmentIndex)>(),
                    Array.Empty<KeyValuePair<string, IRouteConstraint>>());
            }
        }

        private int[] GetGroupLengths(DfaNode node)
        {
            var nodeMatches = node.Matches;
            if (nodeMatches == null || nodeMatches.Count == 0)
            {
                return Array.Empty<int>();
            }

            var groups = new List<int>();

            var length = 1;
            var exemplar = nodeMatches[0];

            for (var i = 1; i < nodeMatches.Count; i++)
            {
                if (!_comparer.Equals(exemplar, nodeMatches[i]))
                {
                    groups.Add(length);
                    length = 0;

                    exemplar = nodeMatches[i];
                }

                length++;
            }

            groups.Add(length);

            return groups.ToArray();
        }

        private static bool HasAdditionalRequiredSegments(RouteEndpoint endpoint, int depth)
        {
            for (var i = depth; i < endpoint.RoutePattern.PathSegments.Count; i++)
            {
                var segment = endpoint.RoutePattern.PathSegments[i];
                if (!segment.IsSimple)
                {
                    // Complex segments always require more processing
                    return true;
                }

                var parameterPart = segment.Parts[0] as RoutePatternParameterPart;
                if (parameterPart == null)
                {
                    // It's a literal
                    return true;
                }

                if (!parameterPart.IsOptional &&
                    !parameterPart.IsCatchAll &&
                    parameterPart.Default == null)
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplyPolicies(DfaNode node)
        {
            if (node.Matches == null || node.Matches.Count == 0)
            {
                return;
            }

            // Start with the current node as the root.
            var work = new List<DfaNode>() { node, };
            List<DfaNode> previousWork = null;
            for (var i = 0; i < _nodeBuilders.Length; i++)
            {
                var nodeBuilder = _nodeBuilders[i];

                // Build a list of each 
                List<DfaNode> nextWork;
                if (previousWork == null)
                {
                    nextWork = new List<DfaNode>();
                }
                else
                {
                    // Reuse previous collection for the next collection
                    previousWork.Clear();
                    nextWork = previousWork;
                }

                for (var j = 0; j < work.Count; j++)
                {
                    var parent = work[j];
                    if (!nodeBuilder.AppliesToEndpoints(parent.Matches ?? (IReadOnlyList<Endpoint>)Array.Empty<Endpoint>()))
                    {
                        // This node-builder doesn't care about this node, so add it to the list
                        // to be processed by the next node-builder.
                        nextWork.Add(parent);
                        continue;
                    }

                    // This node-builder does apply to this node, so we need to create new nodes for each edge,
                    // and then attach them to the parent.
                    var edges = nodeBuilder.GetEdges(parent.Matches ?? (IReadOnlyList<Endpoint>)Array.Empty<Endpoint>());
                    for (var k = 0; k < edges.Count; k++)
                    {
                        var edge = edges[k];

                        var next = new DfaNode()
                        {
                            // If parent label is null then labels are not being included
                            Label = (parent.Label != null) ? parent.Label + " " + edge.State.ToString() : null,
                        };

                        if (edge.Endpoints.Count > 0)
                        {
                            next.AddMatches(edge.Endpoints);
                        }
                        nextWork.Add(next);

                        parent.AddPolicyEdge(edge.State, next);
                    }

                    // Associate the node-builder so we can build a jump table later.
                    parent.NodeBuilder = nodeBuilder;

                    // The parent no longer has matches, it's not considered a terminal node.
                    parent.Matches?.Clear();
                }

                previousWork = work;
                work = nextWork;
            }
        }

        private static (INodeBuilderPolicy[] nodeBuilderPolicies, IEndpointComparerPolicy[] endpointComparerPolicies, IEndpointSelectorPolicy[] endpointSelectorPolicies) ExtractPolicies(IEnumerable<MatcherPolicy> policies)
        {
            var nodeBuilderPolicies = new List<INodeBuilderPolicy>();
            var endpointComparerPolicies = new List<IEndpointComparerPolicy>();
            var endpointSelectorPolicies = new List<IEndpointSelectorPolicy>();

            foreach (var policy in policies)
            {
                if (policy is INodeBuilderPolicy nodeBuilderPolicy)
                {
                    nodeBuilderPolicies.Add(nodeBuilderPolicy);
                }

                if (policy is IEndpointComparerPolicy endpointComparerPolicy)
                {
                    endpointComparerPolicies.Add(endpointComparerPolicy);
                }

                if (policy is IEndpointSelectorPolicy endpointSelectorPolicy)
                {
                    endpointSelectorPolicies.Add(endpointSelectorPolicy);
                }
            }

            return (nodeBuilderPolicies.ToArray(), endpointComparerPolicies.ToArray(), endpointSelectorPolicies.ToArray());
        }

        private static bool TryGetRequiredValue(RoutePattern routePattern, RoutePatternParameterPart parameterPart, out object value)
        {
            if (!routePattern.RequiredValues.TryGetValue(parameterPart.Name, out value))
            {
                return false;
            }

            return !RouteValueEqualityComparer.Default.Equals(value, string.Empty);
        }
    }
}
