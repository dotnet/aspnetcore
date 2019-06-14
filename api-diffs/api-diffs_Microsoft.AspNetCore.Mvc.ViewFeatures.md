# Microsoft.AspNetCore.Mvc.ViewFeatures

``` diff
 namespace Microsoft.AspNetCore.Mvc.ViewFeatures {
     public class CookieTempDataProvider : ITempDataProvider {
-        public CookieTempDataProvider(IDataProtectionProvider dataProtectionProvider, ILoggerFactory loggerFactory, IOptions<CookieTempDataProviderOptions> options);

+        public CookieTempDataProvider(IDataProtectionProvider dataProtectionProvider, ILoggerFactory loggerFactory, IOptions<CookieTempDataProviderOptions> options, TempDataSerializer tempDataSerializer);
     }
     public class DefaultValidationHtmlAttributeProvider : ValidationHtmlAttributeProvider {
-        public DefaultValidationHtmlAttributeProvider(IOptions<MvcViewOptions> optionsAccessor, IModelMetadataProvider metadataProvider, ClientValidatorCache clientValidatorCache);

+        public DefaultValidationHtmlAttributeProvider(IOptions<MvcViewOptions> optionsAccessor, IModelMetadataProvider metadataProvider, ClientValidatorCache clientValidatorCache);
     }
     public class HtmlHelper : IHtmlHelper, IViewContextAware {
+        public HtmlHelper(IHtmlGenerator htmlGenerator, ICompositeViewEngine viewEngine, IModelMetadataProvider metadataProvider, IViewBufferScope bufferScope, HtmlEncoder htmlEncoder, UrlEncoder urlEncoder);
-        public HtmlHelper(IHtmlGenerator htmlGenerator, ICompositeViewEngine viewEngine, IModelMetadataProvider metadataProvider, IViewBufferScope bufferScope, HtmlEncoder htmlEncoder, UrlEncoder urlEncoder);

-        public IEnumerable<SelectListItem> GetEnumSelectList<TEnum>() where TEnum : struct, ValueType;
+        public IEnumerable<SelectListItem> GetEnumSelectList<TEnum>() where TEnum : struct;
     }
     public class HtmlHelper<TModel> : HtmlHelper, IHtmlHelper, IHtmlHelper<TModel> {
+        public HtmlHelper(IHtmlGenerator htmlGenerator, ICompositeViewEngine viewEngine, IModelMetadataProvider metadataProvider, IViewBufferScope bufferScope, HtmlEncoder htmlEncoder, UrlEncoder urlEncoder, ModelExpressionProvider modelExpressionProvider);
-        public HtmlHelper(IHtmlGenerator htmlGenerator, ICompositeViewEngine viewEngine, IModelMetadataProvider metadataProvider, IViewBufferScope bufferScope, HtmlEncoder htmlEncoder, UrlEncoder urlEncoder, ExpressionTextCache expressionTextCache);

     }
-    public class JsonHelper : IJsonHelper {
 {
-        public JsonHelper(JsonOutputFormatter jsonOutputFormatter, ArrayPool<char> charPool);

-        public IHtmlContent Serialize(object value);

-        public IHtmlContent Serialize(object value, JsonSerializerSettings serializerSettings);

-    }
     public class ModelExpressionProvider : IModelExpressionProvider {
+        public ModelExpressionProvider(IModelMetadataProvider modelMetadataProvider);
-        public ModelExpressionProvider(IModelMetadataProvider modelMetadataProvider, ExpressionTextCache expressionTextCache);

+        public ModelExpression CreateModelExpression<TModel>(ViewDataDictionary<TModel> viewData, string expression);
+        public string GetExpressionText<TModel, TValue>(Expression<Func<TModel, TValue>> expression);
     }
     public class PartialViewResultExecutor : ViewExecutor, IActionResultExecutor<PartialViewResult> {
+        public PartialViewResultExecutor(IOptions<MvcViewOptions> viewOptions, IHttpResponseStreamWriterFactory writerFactory, ICompositeViewEngine viewEngine, ITempDataDictionaryFactory tempDataFactory, DiagnosticListener diagnosticListener, ILoggerFactory loggerFactory, IModelMetadataProvider modelMetadataProvider);
-        public PartialViewResultExecutor(IOptions<MvcViewOptions> viewOptions, IHttpResponseStreamWriterFactory writerFactory, ICompositeViewEngine viewEngine, ITempDataDictionaryFactory tempDataFactory, DiagnosticSource diagnosticSource, ILoggerFactory loggerFactory, IModelMetadataProvider modelMetadataProvider);

     }
     public class SessionStateTempDataProvider : ITempDataProvider {
-        public SessionStateTempDataProvider();

+        public SessionStateTempDataProvider(TempDataSerializer tempDataSerializer);
     }
     public class ViewComponentResultExecutor : IActionResultExecutor<ViewComponentResult> {
+        public ViewComponentResultExecutor(IOptions<MvcViewOptions> mvcHelperOptions, ILoggerFactory loggerFactory, HtmlEncoder htmlEncoder, IModelMetadataProvider modelMetadataProvider, ITempDataDictionaryFactory tempDataDictionaryFactory, IHttpResponseStreamWriterFactory writerFactory);
     }
     public class ViewDataDictionaryControllerPropertyActivator : IControllerPropertyActivator
     public class ViewExecutor {
+        protected ViewExecutor(IHttpResponseStreamWriterFactory writerFactory, ICompositeViewEngine viewEngine, DiagnosticListener diagnosticListener);
-        protected ViewExecutor(IHttpResponseStreamWriterFactory writerFactory, ICompositeViewEngine viewEngine, DiagnosticSource diagnosticSource);

+        public ViewExecutor(IOptions<MvcViewOptions> viewOptions, IHttpResponseStreamWriterFactory writerFactory, ICompositeViewEngine viewEngine, ITempDataDictionaryFactory tempDataFactory, DiagnosticListener diagnosticListener, IModelMetadataProvider modelMetadataProvider);
-        public ViewExecutor(IOptions<MvcViewOptions> viewOptions, IHttpResponseStreamWriterFactory writerFactory, ICompositeViewEngine viewEngine, ITempDataDictionaryFactory tempDataFactory, DiagnosticSource diagnosticSource, IModelMetadataProvider modelMetadataProvider);

-        protected DiagnosticSource DiagnosticSource { get; }
+        protected DiagnosticListener DiagnosticSource { get; }
     }
     public class ViewResultExecutor : ViewExecutor, IActionResultExecutor<ViewResult> {
+        public ViewResultExecutor(IOptions<MvcViewOptions> viewOptions, IHttpResponseStreamWriterFactory writerFactory, ICompositeViewEngine viewEngine, ITempDataDictionaryFactory tempDataFactory, DiagnosticListener diagnosticListener, ILoggerFactory loggerFactory, IModelMetadataProvider modelMetadataProvider);
-        public ViewResultExecutor(IOptions<MvcViewOptions> viewOptions, IHttpResponseStreamWriterFactory writerFactory, ICompositeViewEngine viewEngine, ITempDataDictionaryFactory tempDataFactory, DiagnosticSource diagnosticSource, ILoggerFactory loggerFactory, IModelMetadataProvider modelMetadataProvider);

     }
 }
```

