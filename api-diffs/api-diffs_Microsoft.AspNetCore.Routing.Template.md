# Microsoft.AspNetCore.Routing.Template

``` diff
 namespace Microsoft.AspNetCore.Routing.Template {
     public class InlineConstraint {
         public InlineConstraint(RoutePatternParameterPolicyReference other);
         public InlineConstraint(string constraint);
         public string Constraint { get; }
     }
     public static class RoutePrecedence {
         public static decimal ComputeInbound(RouteTemplate template);
         public static decimal ComputeOutbound(RouteTemplate template);
     }
     public class RouteTemplate {
         public RouteTemplate(RoutePattern other);
         public RouteTemplate(string template, List<TemplateSegment> segments);
         public IList<TemplatePart> Parameters { get; }
         public IList<TemplateSegment> Segments { get; }
         public string TemplateText { get; }
         public TemplatePart GetParameter(string name);
         public TemplateSegment GetSegment(int index);
         public RoutePattern ToRoutePattern();
     }
     public class TemplateBinder {
         public TemplateBinder(UrlEncoder urlEncoder, ObjectPool<UriBuildingContext> pool, RoutePattern pattern, RouteValueDictionary defaults, IEnumerable<string> requiredKeys, IEnumerable<ValueTuple<string, IParameterPolicy>> parameterPolicies);
         public TemplateBinder(UrlEncoder urlEncoder, ObjectPool<UriBuildingContext> pool, RouteTemplate template, RouteValueDictionary defaults);
         public string BindValues(RouteValueDictionary acceptedValues);
         public TemplateValuesResult GetValues(RouteValueDictionary ambientValues, RouteValueDictionary values);
         public static bool RoutePartsEqual(object a, object b);
         public bool TryProcessConstraints(HttpContext httpContext, RouteValueDictionary combinedValues, out string parameterName, out IRouteConstraint constraint);
     }
+    public abstract class TemplateBinderFactory {
+        protected TemplateBinderFactory();
+        public abstract TemplateBinder Create(RoutePattern pattern);
+        public abstract TemplateBinder Create(RouteTemplate template, RouteValueDictionary defaults);
+    }
     public class TemplateMatcher {
         public TemplateMatcher(RouteTemplate template, RouteValueDictionary defaults);
         public RouteValueDictionary Defaults { get; }
         public RouteTemplate Template { get; }
         public bool TryMatch(PathString path, RouteValueDictionary values);
     }
     public static class TemplateParser {
         public static RouteTemplate Parse(string routeTemplate);
     }
     public class TemplatePart {
         public TemplatePart();
         public TemplatePart(RoutePatternPart other);
         public object DefaultValue { get; private set; }
         public IEnumerable<InlineConstraint> InlineConstraints { get; private set; }
         public bool IsCatchAll { get; private set; }
         public bool IsLiteral { get; private set; }
         public bool IsOptional { get; private set; }
         public bool IsOptionalSeperator { get; set; }
         public bool IsParameter { get; private set; }
         public string Name { get; private set; }
         public string Text { get; private set; }
         public static TemplatePart CreateLiteral(string text);
         public static TemplatePart CreateParameter(string name, bool isCatchAll, bool isOptional, object defaultValue, IEnumerable<InlineConstraint> inlineConstraints);
         public RoutePatternPart ToRoutePatternPart();
     }
     public class TemplateSegment {
         public TemplateSegment();
         public TemplateSegment(RoutePatternPathSegment other);
         public bool IsSimple { get; }
         public List<TemplatePart> Parts { get; }
         public RoutePatternPathSegment ToRoutePatternPathSegment();
     }
     public class TemplateValuesResult {
         public TemplateValuesResult();
         public RouteValueDictionary AcceptedValues { get; set; }
         public RouteValueDictionary CombinedValues { get; set; }
     }
 }
```

