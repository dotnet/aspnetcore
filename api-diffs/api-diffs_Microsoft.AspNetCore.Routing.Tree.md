# Microsoft.AspNetCore.Routing.Tree

``` diff
 namespace Microsoft.AspNetCore.Routing.Tree {
     public class InboundMatch {
         public InboundMatch();
         public InboundRouteEntry Entry { get; set; }
         public TemplateMatcher TemplateMatcher { get; set; }
     }
     public class InboundRouteEntry {
         public InboundRouteEntry();
         public IDictionary<string, IRouteConstraint> Constraints { get; set; }
         public RouteValueDictionary Defaults { get; set; }
         public IRouter Handler { get; set; }
         public int Order { get; set; }
         public decimal Precedence { get; set; }
         public string RouteName { get; set; }
         public RouteTemplate RouteTemplate { get; set; }
     }
     public class OutboundMatch {
         public OutboundMatch();
         public OutboundRouteEntry Entry { get; set; }
         public TemplateBinder TemplateBinder { get; set; }
     }
     public class OutboundRouteEntry {
         public OutboundRouteEntry();
         public IDictionary<string, IRouteConstraint> Constraints { get; set; }
         public object Data { get; set; }
         public RouteValueDictionary Defaults { get; set; }
         public IRouter Handler { get; set; }
         public int Order { get; set; }
         public decimal Precedence { get; set; }
         public RouteValueDictionary RequiredLinkValues { get; set; }
         public string RouteName { get; set; }
         public RouteTemplate RouteTemplate { get; set; }
     }
     public class TreeRouteBuilder {
         public TreeRouteBuilder(ILoggerFactory loggerFactory, ObjectPool<UriBuildingContext> objectPool, IInlineConstraintResolver constraintResolver);
-        public TreeRouteBuilder(ILoggerFactory loggerFactory, UrlEncoder urlEncoder, ObjectPool<UriBuildingContext> objectPool, IInlineConstraintResolver constraintResolver);

         public IList<InboundRouteEntry> InboundEntries { get; }
         public IList<OutboundRouteEntry> OutboundEntries { get; }
         public TreeRouter Build();
         public TreeRouter Build(int version);
         public void Clear();
         public InboundRouteEntry MapInbound(IRouter handler, RouteTemplate routeTemplate, string routeName, int order);
         public OutboundRouteEntry MapOutbound(IRouter handler, RouteTemplate routeTemplate, RouteValueDictionary requiredLinkValues, string routeName, int order);
     }
     public class TreeRouter : IRouter {
         public static readonly string RouteGroupKey;
         public TreeRouter(UrlMatchingTree[] trees, IEnumerable<OutboundRouteEntry> linkGenerationEntries, UrlEncoder urlEncoder, ObjectPool<UriBuildingContext> objectPool, ILogger routeLogger, ILogger constraintLogger, int version);
         public int Version { get; }
         public VirtualPathData GetVirtualPath(VirtualPathContext context);
         public Task RouteAsync(RouteContext context);
     }
     public class UrlMatchingNode {
         public UrlMatchingNode(int length);
         public UrlMatchingNode CatchAlls { get; set; }
         public UrlMatchingNode ConstrainedCatchAlls { get; set; }
         public UrlMatchingNode ConstrainedParameters { get; set; }
         public int Depth { get; }
         public bool IsCatchAll { get; set; }
         public Dictionary<string, UrlMatchingNode> Literals { get; }
         public List<InboundMatch> Matches { get; }
         public UrlMatchingNode Parameters { get; set; }
     }
     public class UrlMatchingTree {
         public UrlMatchingTree(int order);
         public int Order { get; }
         public UrlMatchingNode Root { get; }
     }
 }
```

