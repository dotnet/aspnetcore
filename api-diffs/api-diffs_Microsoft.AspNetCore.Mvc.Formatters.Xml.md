# Microsoft.AspNetCore.Mvc.Formatters.Xml

``` diff
 namespace Microsoft.AspNetCore.Mvc.Formatters.Xml {
     public class MvcXmlOptions : IEnumerable, IEnumerable<ICompatibilitySwitch> {
-        public bool AllowRfc7807CompliantProblemDetailsFormat { get; set; }

-        public IEnumerator<ICompatibilitySwitch> GetEnumerator();

+        IEnumerator<ICompatibilitySwitch> System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch>.GetEnumerator();
     }
-    public class ProblemDetails21Wrapper : IUnwrappable, IXmlSerializable {
 {
-        protected static readonly string EmptyKey;

-        public ProblemDetails21Wrapper();

-        public ProblemDetails21Wrapper(ProblemDetails problemDetails);

-        public XmlSchema GetSchema();

-        object Microsoft.AspNetCore.Mvc.Formatters.Xml.IUnwrappable.Unwrap(Type declaredType);

-        protected virtual void ReadValue(XmlReader reader, string name);

-        public virtual void ReadXml(XmlReader reader);

-        public virtual void WriteXml(XmlWriter writer);

-    }
-    public class ValidationProblemDetails21Wrapper : ProblemDetails21Wrapper, IUnwrappable {
 {
-        public ValidationProblemDetails21Wrapper();

-        public ValidationProblemDetails21Wrapper(ValidationProblemDetails problemDetails);

-        object Microsoft.AspNetCore.Mvc.Formatters.Xml.IUnwrappable.Unwrap(Type declaredType);

-        protected override void ReadValue(XmlReader reader, string name);

-        public override void WriteXml(XmlWriter writer);

-    }
 }
```

