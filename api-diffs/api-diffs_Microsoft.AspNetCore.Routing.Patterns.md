# Microsoft.AspNetCore.Routing.Patterns

``` diff
 namespace Microsoft.AspNetCore.Routing.Patterns {
     public sealed class RoutePattern {
+        public static readonly object RequiredValueAny;
         public IReadOnlyDictionary<string, object> Defaults { get; }
         public decimal InboundPrecedence { get; }
         public decimal OutboundPrecedence { get; }
         public IReadOnlyDictionary<string, IReadOnlyList<RoutePatternParameterPolicyReference>> ParameterPolicies { get; }
         public IReadOnlyList<RoutePatternParameterPart> Parameters { get; }
         public IReadOnlyList<RoutePatternPathSegment> PathSegments { get; }
         public string RawText { get; }
+        public IReadOnlyDictionary<string, object> RequiredValues { get; }
         public RoutePatternParameterPart GetParameter(string name);
     }
     public sealed class RoutePatternException : Exception {
         public RoutePatternException(string pattern, string message);
         public string Pattern { get; }
         public override void GetObjectData(SerializationInfo info, StreamingContext context);
     }
     public static class RoutePatternFactory {
         public static RoutePatternParameterPolicyReference Constraint(IRouteConstraint constraint);
         public static RoutePatternParameterPolicyReference Constraint(object constraint);
         public static RoutePatternParameterPolicyReference Constraint(string constraint);
         public static RoutePatternLiteralPart LiteralPart(string content);
         public static RoutePatternParameterPart ParameterPart(string parameterName);
         public static RoutePatternParameterPart ParameterPart(string parameterName, object @default);
         public static RoutePatternParameterPart ParameterPart(string parameterName, object @default, RoutePatternParameterKind parameterKind);
         public static RoutePatternParameterPart ParameterPart(string parameterName, object @default, RoutePatternParameterKind parameterKind, params RoutePatternParameterPolicyReference[] parameterPolicies);
         public static RoutePatternParameterPart ParameterPart(string parameterName, object @default, RoutePatternParameterKind parameterKind, IEnumerable<RoutePatternParameterPolicyReference> parameterPolicies);
         public static RoutePatternParameterPolicyReference ParameterPolicy(IParameterPolicy parameterPolicy);
         public static RoutePatternParameterPolicyReference ParameterPolicy(string parameterPolicy);
         public static RoutePattern Parse(string pattern);
         public static RoutePattern Parse(string pattern, object defaults, object parameterPolicies);
+        public static RoutePattern Parse(string pattern, object defaults, object parameterPolicies, object requiredValues);
         public static RoutePattern Pattern(params RoutePatternPathSegment[] segments);
         public static RoutePattern Pattern(IEnumerable<RoutePatternPathSegment> segments);
         public static RoutePattern Pattern(object defaults, object parameterPolicies, params RoutePatternPathSegment[] segments);
         public static RoutePattern Pattern(object defaults, object parameterPolicies, IEnumerable<RoutePatternPathSegment> segments);
         public static RoutePattern Pattern(string rawText, params RoutePatternPathSegment[] segments);
         public static RoutePattern Pattern(string rawText, IEnumerable<RoutePatternPathSegment> segments);
         public static RoutePattern Pattern(string rawText, object defaults, object parameterPolicies, params RoutePatternPathSegment[] segments);
         public static RoutePattern Pattern(string rawText, object defaults, object parameterPolicies, IEnumerable<RoutePatternPathSegment> segments);
         public static RoutePatternPathSegment Segment(params RoutePatternPart[] parts);
         public static RoutePatternPathSegment Segment(IEnumerable<RoutePatternPart> parts);
         public static RoutePatternSeparatorPart SeparatorPart(string content);
     }
     public sealed class RoutePatternLiteralPart : RoutePatternPart {
         public string Content { get; }
     }
     public enum RoutePatternParameterKind {
         CatchAll = 2,
         Optional = 1,
         Standard = 0,
     }
     public sealed class RoutePatternParameterPart : RoutePatternPart {
         public object Default { get; }
         public bool EncodeSlashes { get; }
         public bool IsCatchAll { get; }
         public bool IsOptional { get; }
         public string Name { get; }
         public RoutePatternParameterKind ParameterKind { get; }
         public IReadOnlyList<RoutePatternParameterPolicyReference> ParameterPolicies { get; }
     }
     public sealed class RoutePatternParameterPolicyReference {
         public string Content { get; }
         public IParameterPolicy ParameterPolicy { get; }
     }
     public abstract class RoutePatternPart {
         public bool IsLiteral { get; }
         public bool IsParameter { get; }
         public bool IsSeparator { get; }
         public RoutePatternPartKind PartKind { get; }
     }
     public enum RoutePatternPartKind {
         Literal = 0,
         Parameter = 1,
         Separator = 2,
     }
     public sealed class RoutePatternPathSegment {
         public bool IsSimple { get; }
         public IReadOnlyList<RoutePatternPart> Parts { get; }
     }
     public sealed class RoutePatternSeparatorPart : RoutePatternPart {
         public string Content { get; }
     }
+    public abstract class RoutePatternTransformer {
+        protected RoutePatternTransformer();
+        public abstract RoutePattern SubstituteRequiredValues(RoutePattern original, object requiredValues);
+    }
 }
```

