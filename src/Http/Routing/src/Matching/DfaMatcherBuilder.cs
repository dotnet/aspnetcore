// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Routing.Matching;

internal sealed class DfaMatcherBuilder : MatcherBuilder
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
        // DfaMatcherBuilder is a transient service. Each instance has its own cache of parameter policies.
        _parameterPolicyFactory = new CachingParameterPolicyFactory(parameterPolicyFactory);
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

    // Used in tests
    internal EndpointComparer Comparer => _comparer;

    public override void AddEndpoint(RouteEndpoint endpoint)
    {
        _endpoints.Add(endpoint);
    }

    public DfaNode BuildDfaTree(bool includeLabel = false)
    {
        // Since we're doing a BFS we will process each 'level' of the tree in stages
        // this list will hold the set of items we need to process at the current
        // stage.
        var work = new List<DfaBuilderWorkerWorkItem>(_endpoints.Count);

        var root = new DfaNode() { PathDepth = 0, Label = includeLabel ? "/" : null };

        // To prepare for this we need to compute the max depth, as well as
        // a seed list of items to process (entry, root).
        var maxDepth = 0;
        for (var i = 0; i < _endpoints.Count; i++)
        {
            var endpoint = _endpoints[i];
            var precedenceDigit = GetPrecedenceDigitAtDepth(endpoint, depth: 0);
            work.Add(new DfaBuilderWorkerWorkItem(endpoint, precedenceDigit, new List<DfaNode>() { root, }));
            maxDepth = Math.Max(maxDepth, endpoint.RoutePattern.PathSegments.Count);
        }

        // Sort work at each level by *PRECEDENCE OF THE CURRENT SEGMENT*.
        //
        // We build the tree by doing a BFS over the list of entries. This is important
        // because a 'parameter' node can also traverse the same paths that literal nodes
        // traverse. This means that we need to order the entries first, or else we will
        // miss possible edges in the DFA.
        //
        // We'll sort the matches again later using the *real* comparer once building the
        // precedence part of the DFA is over.
        var precedenceDigitComparer = Comparer<DfaBuilderWorkerWorkItem>.Create((x, y) =>
        {
            return x.PrecedenceDigit.CompareTo(y.PrecedenceDigit);
        });

        var dfaWorker = new DfaBuilderWorker(work, precedenceDigitComparer, includeLabel, _parameterPolicyFactory);

        // Now we process the entries a level at a time.
        for (var depth = 0; depth <= maxDepth; depth++)
        {
            dfaWorker.ProcessLevel(depth);
        }

        // Build the trees of policy nodes (like HTTP methods). Post-order traversal
        // means that we won't have infinite recursion.
        root.Visit(ApplyPolicies);

        return root;
    }

    private sealed class CachingParameterPolicyFactory : ParameterPolicyFactory
    {
        private readonly ParameterPolicyFactory _inner;
        private readonly Dictionary<string, IParameterPolicy> _cachedParameters;

        public CachingParameterPolicyFactory(ParameterPolicyFactory inner)
        {
            _inner = inner;
            _cachedParameters = new Dictionary<string, IParameterPolicy>(StringComparer.Ordinal);
        }

        public override IParameterPolicy Create(RoutePatternParameterPart parameter, string inlineText)
        {
            // Blindly check the cache to see if it contains a match.
            // Only cachable parameter policies are in the cache, so a match will only be available if the parameter policy key is configured to a cachable parameter policy.
            //
            // Note: Cache key is case sensitive. While the route prefix, e.g. "regex", is case-insensitive, the constraint could care about the case of the argument.
            if (_cachedParameters.TryGetValue(inlineText, out var parameterPolicy))
            {
                return _inner.Create(parameter, parameterPolicy);
            }

            parameterPolicy = _inner.Create(parameter, inlineText);

            // The created parameter policy can be wrapped in an OptionalRouteConstraint if RoutePatternParameterPart.IsOptional is true.
            var createdParameterPolicy = (parameterPolicy is OptionalRouteConstraint optionalRouteConstraint)
                ? optionalRouteConstraint.InnerConstraint
                : parameterPolicy;

            // Only cache policies in a known allow list. This is indicated by implementing ICachableParameterPolicy.
            // There is a chance that a user-defined constraint has state, such as an evaluation count. That would break if the constraint is shared between routes, so don't cache.
            if (createdParameterPolicy is ICachableParameterPolicy)
            {
                _cachedParameters[inlineText] = createdParameterPolicy;
            }

            return parameterPolicy;
        }

        public override IParameterPolicy Create(RoutePatternParameterPart parameter, IParameterPolicy parameterPolicy)
        {
            return _inner.Create(parameter, parameterPolicy);
        }
    }

    private sealed class DfaBuilderWorker
    {
        private List<DfaBuilderWorkerWorkItem> _previousWork;
        private List<DfaBuilderWorkerWorkItem> _work;
        private int _workCount;
        private readonly Comparer<DfaBuilderWorkerWorkItem> _precedenceDigitComparer;
        private readonly bool _includeLabel;
        private readonly ParameterPolicyFactory _parameterPolicyFactory;

        public DfaBuilderWorker(
            List<DfaBuilderWorkerWorkItem> work,
            Comparer<DfaBuilderWorkerWorkItem> precedenceDigitComparer,
            bool includeLabel,
            ParameterPolicyFactory parameterPolicyFactory)
        {
            _work = work;
            _previousWork = new List<DfaBuilderWorkerWorkItem>();
            _workCount = work.Count;
            _precedenceDigitComparer = precedenceDigitComparer;
            _includeLabel = includeLabel;
            _parameterPolicyFactory = parameterPolicyFactory;
        }

        // Each time we process a level of the DFA we keep a list of work items consisting on the nodes we need to evaluate
        // their precendence and their parent nodes. We sort nodes by precedence on each level, which means that nodes are
        // evaluated in the following order: (literals, constrained parameters/complex segments, parameters, constrainted catch-alls and catch-alls)
        // When we process a stage we build a list of the next set of workitems we need to evaluate. We also keep around the
        // list of workitems from the previous level so that we can reuse all the nested lists while we are evaluating the current level.
        internal void ProcessLevel(int depth)
        {
            // As we process items, collect the next set of items.
            var nextWork = _previousWork;
            var nextWorkCount = 0;

            // See comments on precedenceDigitComparer
            _work.Sort(0, _workCount, _precedenceDigitComparer);

            for (var i = 0; i < _workCount; i++)
            {
                var (endpoint, _, parents) = _work[i];

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
                    nextParents = nextWork[nextWorkCount].Parents;
                    nextParents.Clear();

                    var nextPrecedenceDigit = GetPrecedenceDigitAtDepth(endpoint, depth + 1);
                    nextWork[nextWorkCount] = new DfaBuilderWorkerWorkItem(endpoint, nextPrecedenceDigit, nextParents);
                }
                else
                {
                    nextParents = new List<DfaNode>();

                    // Add to the next set of work now so the list will be reused
                    // even if there are no parents
                    var nextPrecedenceDigit = GetPrecedenceDigitAtDepth(endpoint, depth + 1);
                    nextWork.Add(new DfaBuilderWorkerWorkItem(endpoint, nextPrecedenceDigit, nextParents));
                }

                var segment = GetCurrentSegment(endpoint, depth);
                if (segment == null)
                {
                    continue;
                }

                ProcessSegment(endpoint, parents, nextParents, segment);

                if (nextParents.Count > 0)
                {
                    nextWorkCount++;
                }
            }

            // Prepare to process the next stage.
            _previousWork = _work;
            _work = nextWork;
            _workCount = nextWorkCount;
        }

        private void ProcessSegment(
            RouteEndpoint endpoint,
            List<DfaNode> parents,
            List<DfaNode> nextParents,
            RoutePatternPathSegment segment)
        {
            for (var i = 0; i < parents.Count; i++)
            {
                var parent = parents[i];
                var part = segment.Parts[0];
                var parameterPart = part as RoutePatternParameterPart;
                if (segment.IsSimple && part is RoutePatternLiteralPart literalPart)
                {
                    AddLiteralNode(_includeLabel, nextParents, parent, literalPart.Content);
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
                            Label = _includeLabel ? parent.Label + "{*...}/" : null,
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

                    AddRequiredLiteralValue(endpoint, nextParents, parent, parameterPart, requiredValue);
                }
                else if (segment.IsSimple && parameterPart != null)
                {
                    if (parent.Parameters == null)
                    {
                        parent.Parameters = new DfaNode()
                        {
                            PathDepth = parent.PathDepth + 1,
                            Label = _includeLabel ? parent.Label + "{...}/" : null,
                        };
                    }

                    if (parent.Literals != null)
                    {
                        // If the parameter contains constraints, we can be smarter about it and evaluate them while we build the tree.
                        // If the literal doesn't match any of the constraints, we can prune the branch.
                        // For example, for a parameter in a route {lang:length(2)} and a parent literal "ABC", we can check that "ABC"
                        // doesn't meet the parameter constraint (length(2)) when building the tree, and avoid the extra nodes.
                        if (endpoint.RoutePattern.ParameterPolicies.TryGetValue(parameterPart.Name, out var parameterPolicyReferences))
                        {
                            // We filter out sibling literals that don't match one of the constraints in the segment to avoid adding nodes to the DFA
                            // that will never match a route and which will result in a much higher memory usage.
                            AddParentsWithMatchingLiteralConstraints(nextParents, parent, parameterPart, parameterPolicyReferences);
                        }
                        else
                        {
                            // This means the current parameter we are evaluating doesn't contain any constraint, so we need to
                            // traverse all literal nodes as well as the parameter node.
                            nextParents.AddRange(parent.Literals.Values);
                        }
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
                            Label = _includeLabel ? parent.Label + "{...}/" : null,
                        };
                    }

                    if (parent.Literals != null)
                    {
                        // For a complex segment like this, we can evaluate the literals and avoid adding extra nodes to
                        // the tree on cases where the literal won't ever be able to match the complex parameter.
                        // For example, if we have a complex parameter {a}-{b}.{c?} and a literal "Hello" we can guarantee
                        // that it will never be a match.

                        // We filter out sibling literals that don't match the complex parameter segment to avoid adding nodes to the DFA
                        // that will never match a route and which will result in a much higher memory usage.
                        AddParentsMatchingComplexSegment(endpoint, nextParents, segment, parent, parameterPart);
                    }
                    nextParents.Add(parent.Parameters);
                }
            }
        }

        private void AddParentsMatchingComplexSegment(RouteEndpoint endpoint, List<DfaNode> nextParents, RoutePatternPathSegment segment, DfaNode parent, RoutePatternParameterPart parameterPart)
        {
            var routeValues = new RouteValueDictionary();
            foreach (var literal in parent.Literals.Keys)
            {
                if (RoutePatternMatcher.MatchComplexSegment(segment, literal, routeValues))
                {
                    // If we got here (rare) it means that the literal matches the complex segment (for example the literal is something A-B)
                    // there is another thing we can try here, which is to evaluate the policies for the parts in case they have one (for example {a:length(4)}-{b:regex(\d+)})
                    // so that even if it maps closely to a complex parameter we have a chance to discard it and avoid adding the extra branches.
                    var passedAllPolicies = true;
                    for (var i = 0; i < segment.Parts.Count; i++)
                    {
                        var segmentPart = segment.Parts[i];
                        if (segmentPart is not RoutePatternParameterPart partParameter)
                        {
                            // We skip over the literals and the separator since we already checked against them
                            continue;
                        }

                        if (!routeValues.TryGetValue(partParameter.Name, out var parameterValue))
                        {
                            // We have a pattern like {a}-{b}.{part?} and a literal "a-b". Since we've matched the complex segment it means that the optional
                            // parameter was not specified, so we skip it.
                            Debug.Assert(i == segment.Parts.Count - 1 && partParameter.IsOptional);
                            continue;
                        }

                        if (endpoint.RoutePattern.ParameterPolicies.TryGetValue(partParameter.Name, out var parameterPolicyReferences))
                        {
                            for (var j = 0; j < parameterPolicyReferences.Count; j++)
                            {
                                var reference = parameterPolicyReferences[j];
                                var parameterPolicy = _parameterPolicyFactory.Create(parameterPart, reference);
                                if (parameterPolicy is IParameterLiteralNodeMatchingPolicy constraint && !constraint.MatchesLiteral(partParameter.Name, (string)parameterValue))
                                {
                                    passedAllPolicies = false;
                                    break;
                                }
                            }
                        }
                    }

                    if (passedAllPolicies)
                    {
                        nextParents.Add(parent.Literals[literal]);
                    }
                }

                routeValues.Clear();
            }
        }

        private void AddParentsWithMatchingLiteralConstraints(List<DfaNode> nextParents, DfaNode parent, RoutePatternParameterPart parameterPart, IReadOnlyList<RoutePatternParameterPolicyReference> parameterPolicyReferences)
        {
            // The list of parameters that fail to meet at least one IParameterLiteralNodeMatchingPolicy.
            var hasFailingPolicy = parent.Literals.Keys.Count < 32 ?
                (stackalloc bool[32]).Slice(0, parent.Literals.Keys.Count) :
                new bool[parent.Literals.Keys.Count];

            // Whether or not all parameters have failed to meet at least one constraint.
            for (var i = 0; i < parameterPolicyReferences.Count; i++)
            {
                var reference = parameterPolicyReferences[i];
                var parameterPolicy = _parameterPolicyFactory.Create(parameterPart, reference);
                if (parameterPolicy is IParameterLiteralNodeMatchingPolicy constraint)
                {
                    var literalIndex = 0;
                    var allFailed = true;
                    foreach (var literal in parent.Literals.Keys)
                    {
                        if (!hasFailingPolicy[literalIndex] && !constraint.MatchesLiteral(parameterPart.Name, literal))
                        {
                            hasFailingPolicy[literalIndex] = true;
                        }

                        allFailed &= hasFailingPolicy[literalIndex];

                        literalIndex++;
                    }

                    if (allFailed)
                    {
                        // If we get here it means that all literals have failed at least one policy, which means we can skip checking policies
                        // and return early. This will be a very common case when your constraints are things like "int,length or a regex".
                        return;
                    }
                }
            }

            var k = 0;
            foreach (var literal in parent.Literals.Values)
            {
                if (!hasFailingPolicy[k])
                {
                    nextParents.Add(literal);
                }
                k++;
            }
        }

        private void AddRequiredLiteralValue(RouteEndpoint endpoint, List<DfaNode> nextParents, DfaNode parent, RoutePatternParameterPart parameterPart, object requiredValue)
        {
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

            var literalValue = requiredValue?.ToString() ?? throw new InvalidOperationException($"Required value for literal '{parameterPart.Name}' must evaluate to a non-null string.");

            AddLiteralNode(_includeLabel, nextParents, parent, literalValue);
        }
    }

    private static void AddLiteralNode(bool includeLabel, List<DfaNode> nextParents, DfaNode parent, string literal)
    {
        if (parent.Literals == null ||
            !parent.Literals.TryGetValue(literal, out var next))
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

    private static RoutePatternPathSegment GetCurrentSegment(RouteEndpoint endpoint, int depth)
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

    private static int GetPrecedenceDigitAtDepth(RouteEndpoint endpoint, int depth)
    {
        var segment = GetCurrentSegment(endpoint, depth);
        if (segment is null)
        {
            // Treat "no segment" as high priority. it won't effect the algorithm, but we need to define a sort-order.
            return 0;
        }

        return RoutePrecedence.ComputeInboundPrecedenceDigit(endpoint.RoutePattern, segment);
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
            // This node has parameters but no catchall.
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
            // Use the final exit destination when building the policy state.
            // We don't want to use either of the current destinations because they refer routing states,
            // and a policy state should never transition back to a routing state.
            BuildPolicy(exitDestination, node.NodeBuilder, policyEntries));

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

        // We're done with the precedence based work. Sort the endpoints
        // before applying policies for simplicity in policy-related code.
        node.Matches.Sort(_comparer);

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

    public readonly struct DfaBuilderWorkerWorkItem
    {
        public RouteEndpoint Endpoint { get; }

        public int PrecedenceDigit { get; }

        public List<DfaNode> Parents { get; }

        public DfaBuilderWorkerWorkItem(RouteEndpoint endpoint, int precedenceDigit, List<DfaNode> parents)
        {
            Endpoint = endpoint;
            PrecedenceDigit = precedenceDigit;
            Parents = parents;
        }

        public void Deconstruct(out RouteEndpoint endpoint, out int precedenceDigit, out List<DfaNode> parents)
        {
            endpoint = Endpoint;
            precedenceDigit = PrecedenceDigit;
            parents = Parents;
        }
    }
}
