# Microsoft.AspNetCore.Routing

``` diff
 namespace Microsoft.AspNetCore.Routing {
     public sealed class CompositeEndpointDataSource : EndpointDataSource {
+        public CompositeEndpointDataSource(IEnumerable<EndpointDataSource> endpointDataSources);
+        public IEnumerable<EndpointDataSource> DataSources { get; }
     }
     public class DefaultInlineConstraintResolver : IInlineConstraintResolver {
-        public DefaultInlineConstraintResolver(IOptions<RouteOptions> routeOptions);

     }
-    public sealed class EndpointSelectorContext : IEndpointFeature, IRouteValuesFeature, IRoutingFeature {
 {
-        public EndpointSelectorContext();

-        public Endpoint Endpoint { get; set; }

-        RouteData Microsoft.AspNetCore.Routing.IRoutingFeature.RouteData { get; set; }

-        public RouteValueDictionary RouteValues { get; set; }

-    }
+    public sealed class HostAttribute : Attribute, IHostMetadata {
+        public HostAttribute(string host);
+        public HostAttribute(params string[] hosts);
+        public IReadOnlyList<string> Hosts { get; }
+    }
+    public interface IDynamicEndpointMetadata {
+        bool IsDynamic { get; }
+    }
+    public interface IEndpointRouteBuilder {
+        ICollection<EndpointDataSource> DataSources { get; }
+        IServiceProvider ServiceProvider { get; }
+        IApplicationBuilder CreateApplicationBuilder();
+    }
+    public interface IHostMetadata {
+        IReadOnlyList<string> Hosts { get; }
+    }
+    public interface IRouteNameMetadata {
+        string RouteName { get; }
+    }
-    public interface IRouteValuesAddressMetadata {
 {
-        IReadOnlyDictionary<string, object> RequiredValues { get; }

-        string RouteName { get; }

-    }
+    public abstract class LinkParser {
+        protected LinkParser();
+        public abstract RouteValueDictionary ParsePathByAddress<TAddress>(TAddress address, PathString path);
+    }
+    public static class LinkParserEndpointNameAddressExtensions {
+        public static RouteValueDictionary ParsePathByEndpointName(this LinkParser parser, string endpointName, PathString path);
+    }
     public abstract class MatcherPolicy {
+        protected static bool ContainsDynamicEndpoints(IReadOnlyList<Endpoint> endpoints);
     }
     public class RouteData {
-        public struct RouteDataSnapshot
+        public readonly struct RouteDataSnapshot
     }
+    public sealed class RouteEndpointBuilder : EndpointBuilder {
+        public RouteEndpointBuilder(RequestDelegate requestDelegate, RoutePattern routePattern, int order);
+        public int Order { get; set; }
+        public RoutePattern RoutePattern { get; set; }
+        public override Endpoint Build();
+    }
+    public sealed class RouteNameMetadata : IRouteNameMetadata {
+        public RouteNameMetadata(string routeName);
+        public string RouteName { get; }
+    }
     public class RouteOptions {
+        public bool SuppressCheckForUnhandledSecurityMetadata { get; set; }
     }
     public class RouteValueEqualityComparer : IEqualityComparer<object> {
+        public static readonly RouteValueEqualityComparer Default;
     }
-    public sealed class RouteValuesAddressMetadata : IRouteValuesAddressMetadata {
 {
-        public RouteValuesAddressMetadata(IReadOnlyDictionary<string, object> requiredValues);

-        public RouteValuesAddressMetadata(string routeName);

-        public RouteValuesAddressMetadata(string routeName, IReadOnlyDictionary<string, object> requiredValues);

-        public IReadOnlyDictionary<string, object> RequiredValues { get; }

-        public string RouteName { get; }

-    }
 }
```

