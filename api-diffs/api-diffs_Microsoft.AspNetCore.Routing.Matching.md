# Microsoft.AspNetCore.Routing.Matching

``` diff
 namespace Microsoft.AspNetCore.Routing.Matching {
     public sealed class CandidateSet {
         public CandidateSet(Endpoint[] endpoints, RouteValueDictionary[] values, int[] scores);
         public int Count { get; }
         public ref CandidateState this[int index] { get; }
+        public void ExpandEndpoint(int index, IReadOnlyList<Endpoint> endpoints, IComparer<Endpoint> comparer);
         public bool IsValidCandidate(int index);
+        public void ReplaceEndpoint(int index, Endpoint endpoint, RouteValueDictionary values);
         public void SetValidity(int index, bool value);
     }
     public struct CandidateState {
         public Endpoint Endpoint { get; }
         public int Score { get; }
         public RouteValueDictionary Values { get; internal set; }
     }
+    public sealed class EndpointMetadataComparer : IComparer<Endpoint> {
+        int System.Collections.Generic.IComparer<Microsoft.AspNetCore.Http.Endpoint>.Compare(Endpoint x, Endpoint y);
+    }
     public abstract class EndpointMetadataComparer<TMetadata> : IComparer<Endpoint> where TMetadata : class {
         public static readonly EndpointMetadataComparer<TMetadata> Default;
         protected EndpointMetadataComparer();
         public int Compare(Endpoint x, Endpoint y);
         protected virtual int CompareMetadata(TMetadata x, TMetadata y);
         protected virtual TMetadata GetMetadata(Endpoint endpoint);
     }
     public abstract class EndpointSelector {
         protected EndpointSelector();
-        public abstract Task SelectAsync(HttpContext httpContext, EndpointSelectorContext context, CandidateSet candidates);

+        public abstract Task SelectAsync(HttpContext httpContext, CandidateSet candidates);
     }
+    public sealed class HostMatcherPolicy : MatcherPolicy, IEndpointComparerPolicy, IEndpointSelectorPolicy, INodeBuilderPolicy {
+        public HostMatcherPolicy();
+        public IComparer<Endpoint> Comparer { get; }
+        public override int Order { get; }
+        public Task ApplyAsync(HttpContext httpContext, CandidateSet candidates);
+        public PolicyJumpTable BuildJumpTable(int exitDestination, IReadOnlyList<PolicyJumpTableEdge> edges);
+        public IReadOnlyList<PolicyNodeEdge> GetEdges(IReadOnlyList<Endpoint> endpoints);
+        bool Microsoft.AspNetCore.Routing.Matching.IEndpointSelectorPolicy.AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints);
+        bool Microsoft.AspNetCore.Routing.Matching.INodeBuilderPolicy.AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints);
+    }
-    public sealed class HttpMethodMatcherPolicy : MatcherPolicy, IEndpointComparerPolicy, INodeBuilderPolicy {
+    public sealed class HttpMethodMatcherPolicy : MatcherPolicy, IEndpointComparerPolicy, IEndpointSelectorPolicy, INodeBuilderPolicy {
         public HttpMethodMatcherPolicy();
         public IComparer<Endpoint> Comparer { get; }
         public override int Order { get; }
-        public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints);

+        public Task ApplyAsync(HttpContext httpContext, CandidateSet candidates);
         public PolicyJumpTable BuildJumpTable(int exitDestination, IReadOnlyList<PolicyJumpTableEdge> edges);
         public IReadOnlyList<PolicyNodeEdge> GetEdges(IReadOnlyList<Endpoint> endpoints);
+        bool Microsoft.AspNetCore.Routing.Matching.IEndpointSelectorPolicy.AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints);
+        bool Microsoft.AspNetCore.Routing.Matching.INodeBuilderPolicy.AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints);
     }
     public interface IEndpointComparerPolicy {
         IComparer<Endpoint> Comparer { get; }
     }
     public interface IEndpointSelectorPolicy {
         bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints);
-        Task ApplyAsync(HttpContext httpContext, EndpointSelectorContext context, CandidateSet candidates);

+        Task ApplyAsync(HttpContext httpContext, CandidateSet candidates);
     }
     public interface INodeBuilderPolicy {
         bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints);
         PolicyJumpTable BuildJumpTable(int exitDestination, IReadOnlyList<PolicyJumpTableEdge> edges);
         IReadOnlyList<PolicyNodeEdge> GetEdges(IReadOnlyList<Endpoint> endpoints);
     }
     public abstract class PolicyJumpTable {
         protected PolicyJumpTable();
         public abstract int GetDestination(HttpContext httpContext);
     }
     public readonly struct PolicyJumpTableEdge {
         public PolicyJumpTableEdge(object state, int destination);
         public int Destination { get; }
         public object State { get; }
     }
     public readonly struct PolicyNodeEdge {
         public PolicyNodeEdge(object state, IReadOnlyList<Endpoint> endpoints);
         public IReadOnlyList<Endpoint> Endpoints { get; }
         public object State { get; }
     }
 }
```

