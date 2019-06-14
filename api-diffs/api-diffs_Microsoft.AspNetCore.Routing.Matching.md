# Microsoft.AspNetCore.Routing.Matching

``` diff
 namespace Microsoft.AspNetCore.Routing.Matching {
     public sealed class CandidateSet {
+        public void ExpandEndpoint(int index, IReadOnlyList<Endpoint> endpoints, IComparer<Endpoint> comparer);
+        public void ReplaceEndpoint(int index, Endpoint endpoint, RouteValueDictionary values);
     }
+    public sealed class EndpointMetadataComparer : IComparer<Endpoint> {
+        int System.Collections.Generic.IComparer<Microsoft.AspNetCore.Http.Endpoint>.Compare(Endpoint x, Endpoint y);
+    }
     public abstract class EndpointSelector {
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
-        public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints);

+        public Task ApplyAsync(HttpContext httpContext, CandidateSet candidates);
+        bool Microsoft.AspNetCore.Routing.Matching.IEndpointSelectorPolicy.AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints);
+        bool Microsoft.AspNetCore.Routing.Matching.INodeBuilderPolicy.AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints);
     }
     public interface IEndpointSelectorPolicy {
-        Task ApplyAsync(HttpContext httpContext, EndpointSelectorContext context, CandidateSet candidates);

+        Task ApplyAsync(HttpContext httpContext, CandidateSet candidates);
     }
 }
```

