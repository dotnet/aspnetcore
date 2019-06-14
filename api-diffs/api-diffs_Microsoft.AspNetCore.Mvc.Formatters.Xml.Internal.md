# Microsoft.AspNetCore.Mvc.Formatters.Xml.Internal

``` diff
-namespace Microsoft.AspNetCore.Mvc.Formatters.Xml.Internal {
 {
-    public static class FormattingUtilities {
 {
-        public static readonly int DefaultMaxDepth;

-        public static readonly XsdDataContractExporter XsdDataContractExporter;

-        public static XmlDictionaryReaderQuotas GetDefaultXmlReaderQuotas();

-        public static XmlWriterSettings GetDefaultXmlWriterSettings();

-    }
-    public static class LoggerExtensions {
 {
-        public static void FailedToCreateDataContractSerializer(this ILogger logger, string typeName, Exception exception);

-        public static void FailedToCreateXmlSerializer(this ILogger logger, string typeName, Exception exception);

-    }
-}
```

