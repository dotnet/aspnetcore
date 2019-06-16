# Microsoft.AspNetCore.Mvc.Formatters.Xml

``` diff
 namespace Microsoft.AspNetCore.Mvc.Formatters.Xml {
     public class DelegatingEnumerable<TWrapped, TDeclared> : IEnumerable, IEnumerable<TWrapped> {
         public DelegatingEnumerable();
         public DelegatingEnumerable(IEnumerable<TDeclared> source, IWrapperProvider elementWrapperProvider);
         public void Add(object item);
         public IEnumerator<TWrapped> GetEnumerator();
         IEnumerator System.Collections.IEnumerable.GetEnumerator();
     }
     public class DelegatingEnumerator<TWrapped, TDeclared> : IDisposable, IEnumerator, IEnumerator<TWrapped> {
         public DelegatingEnumerator(IEnumerator<TDeclared> inner, IWrapperProvider wrapperProvider);
         public TWrapped Current { get; }
         object System.Collections.IEnumerator.Current { get; }
         public void Dispose();
         public bool MoveNext();
         public void Reset();
     }
     public class EnumerableWrapperProvider : IWrapperProvider {
         public EnumerableWrapperProvider(Type sourceEnumerableOfT, IWrapperProvider elementWrapperProvider);
         public Type WrappingType { get; }
         public object Wrap(object original);
     }
     public class EnumerableWrapperProviderFactory : IWrapperProviderFactory {
         public EnumerableWrapperProviderFactory(IEnumerable<IWrapperProviderFactory> wrapperProviderFactories);
         public IWrapperProvider GetProvider(WrapperProviderContext context);
     }
     public interface IUnwrappable {
         object Unwrap(Type declaredType);
     }
     public interface IWrapperProvider {
         Type WrappingType { get; }
         object Wrap(object original);
     }
     public interface IWrapperProviderFactory {
         IWrapperProvider GetProvider(WrapperProviderContext context);
     }
     public class MvcXmlOptions : IEnumerable, IEnumerable<ICompatibilitySwitch> {
         public MvcXmlOptions();
-        public bool AllowRfc7807CompliantProblemDetailsFormat { get; set; }

-        public IEnumerator<ICompatibilitySwitch> GetEnumerator();

+        IEnumerator<ICompatibilitySwitch> System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch>.GetEnumerator();
         IEnumerator System.Collections.IEnumerable.GetEnumerator();
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
     public class ProblemDetailsWrapper : IUnwrappable, IXmlSerializable {
         protected static readonly string EmptyKey;
         public ProblemDetailsWrapper();
         public ProblemDetailsWrapper(ProblemDetails problemDetails);
         public XmlSchema GetSchema();
         object Microsoft.AspNetCore.Mvc.Formatters.Xml.IUnwrappable.Unwrap(Type declaredType);
         protected virtual void ReadValue(XmlReader reader, string name);
         public virtual void ReadXml(XmlReader reader);
         public virtual void WriteXml(XmlWriter writer);
     }
     public sealed class SerializableErrorWrapper : IUnwrappable, IXmlSerializable {
         public SerializableErrorWrapper();
         public SerializableErrorWrapper(SerializableError error);
         public SerializableError SerializableError { get; }
         public XmlSchema GetSchema();
         public void ReadXml(XmlReader reader);
         public object Unwrap(Type declaredType);
         public void WriteXml(XmlWriter writer);
     }
     public class SerializableErrorWrapperProvider : IWrapperProvider {
         public SerializableErrorWrapperProvider();
         public Type WrappingType { get; }
         public object Wrap(object original);
     }
     public class SerializableErrorWrapperProviderFactory : IWrapperProviderFactory {
         public SerializableErrorWrapperProviderFactory();
         public IWrapperProvider GetProvider(WrapperProviderContext context);
     }
-    public class ValidationProblemDetails21Wrapper : ProblemDetails21Wrapper, IUnwrappable {
 {
-        public ValidationProblemDetails21Wrapper();

-        public ValidationProblemDetails21Wrapper(ValidationProblemDetails problemDetails);

-        object Microsoft.AspNetCore.Mvc.Formatters.Xml.IUnwrappable.Unwrap(Type declaredType);

-        protected override void ReadValue(XmlReader reader, string name);

-        public override void WriteXml(XmlWriter writer);

-    }
     public class ValidationProblemDetailsWrapper : ProblemDetailsWrapper, IUnwrappable {
         public ValidationProblemDetailsWrapper();
         public ValidationProblemDetailsWrapper(ValidationProblemDetails problemDetails);
         object Microsoft.AspNetCore.Mvc.Formatters.Xml.IUnwrappable.Unwrap(Type declaredType);
         protected override void ReadValue(XmlReader reader, string name);
         public override void WriteXml(XmlWriter writer);
     }
     public class WrapperProviderContext {
         public WrapperProviderContext(Type declaredType, bool isSerialization);
         public Type DeclaredType { get; }
         public bool IsSerialization { get; }
     }
     public static class WrapperProviderFactoriesExtensions {
         public static IWrapperProvider GetWrapperProvider(this IEnumerable<IWrapperProviderFactory> wrapperProviderFactories, WrapperProviderContext wrapperProviderContext);
     }
 }
```

