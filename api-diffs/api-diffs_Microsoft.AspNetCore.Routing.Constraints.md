# Microsoft.AspNetCore.Routing.Constraints

``` diff
 namespace Microsoft.AspNetCore.Routing.Constraints {
     public class AlphaRouteConstraint : RegexRouteConstraint {
         public AlphaRouteConstraint();
     }
     public class BoolRouteConstraint : IParameterPolicy, IRouteConstraint {
         public BoolRouteConstraint();
         public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection);
     }
     public class CompositeRouteConstraint : IParameterPolicy, IRouteConstraint {
         public CompositeRouteConstraint(IEnumerable<IRouteConstraint> constraints);
         public IEnumerable<IRouteConstraint> Constraints { get; private set; }
         public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection);
     }
     public class DateTimeRouteConstraint : IParameterPolicy, IRouteConstraint {
         public DateTimeRouteConstraint();
         public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection);
     }
     public class DecimalRouteConstraint : IParameterPolicy, IRouteConstraint {
         public DecimalRouteConstraint();
         public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection);
     }
     public class DoubleRouteConstraint : IParameterPolicy, IRouteConstraint {
         public DoubleRouteConstraint();
         public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection);
     }
+    public class FileNameRouteConstraint : IParameterPolicy, IRouteConstraint {
+        public FileNameRouteConstraint();
+        public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection);
+    }
     public class FloatRouteConstraint : IParameterPolicy, IRouteConstraint {
         public FloatRouteConstraint();
         public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection);
     }
     public class GuidRouteConstraint : IParameterPolicy, IRouteConstraint {
         public GuidRouteConstraint();
         public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection);
     }
     public class HttpMethodRouteConstraint : IParameterPolicy, IRouteConstraint {
         public HttpMethodRouteConstraint(params string[] allowedMethods);
         public IList<string> AllowedMethods { get; }
         public virtual bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection);
     }
     public class IntRouteConstraint : IParameterPolicy, IRouteConstraint {
         public IntRouteConstraint();
         public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection);
     }
     public class LengthRouteConstraint : IParameterPolicy, IRouteConstraint {
         public LengthRouteConstraint(int length);
         public LengthRouteConstraint(int minLength, int maxLength);
         public int MaxLength { get; }
         public int MinLength { get; }
         public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection);
     }
     public class LongRouteConstraint : IParameterPolicy, IRouteConstraint {
         public LongRouteConstraint();
         public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection);
     }
     public class MaxLengthRouteConstraint : IParameterPolicy, IRouteConstraint {
         public MaxLengthRouteConstraint(int maxLength);
         public int MaxLength { get; }
         public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection);
     }
     public class MaxRouteConstraint : IParameterPolicy, IRouteConstraint {
         public MaxRouteConstraint(long max);
         public long Max { get; private set; }
         public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection);
     }
     public class MinLengthRouteConstraint : IParameterPolicy, IRouteConstraint {
         public MinLengthRouteConstraint(int minLength);
         public int MinLength { get; private set; }
         public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection);
     }
     public class MinRouteConstraint : IParameterPolicy, IRouteConstraint {
         public MinRouteConstraint(long min);
         public long Min { get; }
         public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection);
     }
+    public class NonFileNameRouteConstraint : IParameterPolicy, IRouteConstraint {
+        public NonFileNameRouteConstraint();
+        public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection);
+    }
     public class OptionalRouteConstraint : IParameterPolicy, IRouteConstraint {
         public OptionalRouteConstraint(IRouteConstraint innerConstraint);
         public IRouteConstraint InnerConstraint { get; }
         public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection);
     }
     public class RangeRouteConstraint : IParameterPolicy, IRouteConstraint {
         public RangeRouteConstraint(long min, long max);
         public long Max { get; private set; }
         public long Min { get; private set; }
         public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection);
     }
     public class RegexInlineRouteConstraint : RegexRouteConstraint {
         public RegexInlineRouteConstraint(string regexPattern);
     }
     public class RegexRouteConstraint : IParameterPolicy, IRouteConstraint {
         public RegexRouteConstraint(string regexPattern);
         public RegexRouteConstraint(Regex regex);
         public Regex Constraint { get; private set; }
         public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection);
     }
     public class RequiredRouteConstraint : IParameterPolicy, IRouteConstraint {
         public RequiredRouteConstraint();
         public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection);
     }
     public class StringRouteConstraint : IParameterPolicy, IRouteConstraint {
         public StringRouteConstraint(string value);
         public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection);
     }
 }
```

