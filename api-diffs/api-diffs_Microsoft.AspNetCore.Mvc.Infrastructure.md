# Microsoft.AspNetCore.Mvc.Infrastructure

``` diff
 namespace Microsoft.AspNetCore.Mvc.Infrastructure {
     public class ActionContextAccessor : IActionContextAccessor {
         public ActionContextAccessor();
         public ActionContext ActionContext { get; set; }
     }
     public class ActionDescriptorCollection {
         public ActionDescriptorCollection(IReadOnlyList<ActionDescriptor> items, int version);
         public IReadOnlyList<ActionDescriptor> Items { get; }
         public int Version { get; }
     }
     public abstract class ActionDescriptorCollectionProvider : IActionDescriptorCollectionProvider {
         protected ActionDescriptorCollectionProvider();
         public abstract ActionDescriptorCollection ActionDescriptors { get; }
         public abstract IChangeToken GetChangeToken();
     }
+    public sealed class ActionResultObjectValueAttribute : Attribute {
+        public ActionResultObjectValueAttribute();
+    }
+    public sealed class ActionResultStatusCodeAttribute : Attribute {
+        public ActionResultStatusCodeAttribute();
+    }
-    public class CompatibilitySwitch<TValue> : ICompatibilitySwitch where TValue : struct, ValueType {
+    public class CompatibilitySwitch<TValue> : ICompatibilitySwitch where TValue : struct {
         public CompatibilitySwitch(string name);
         public CompatibilitySwitch(string name, TValue initialValue);
         public bool IsValueSet { get; private set; }
         object Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch.Value { get; set; }
         public string Name { get; }
         public TValue Value { get; set; }
     }
     public abstract class ConfigureCompatibilityOptions<TOptions> : IPostConfigureOptions<TOptions> where TOptions : class, IEnumerable<ICompatibilitySwitch> {
         protected ConfigureCompatibilityOptions(ILoggerFactory loggerFactory, IOptions<MvcCompatibilityOptions> compatibilityOptions);
         protected abstract IReadOnlyDictionary<string, object> DefaultValues { get; }
         protected CompatibilityVersion Version { get; }
         public virtual void PostConfigure(string name, TOptions options);
     }
     public class ContentResultExecutor : IActionResultExecutor<ContentResult> {
         public ContentResultExecutor(ILogger<ContentResultExecutor> logger, IHttpResponseStreamWriterFactory httpResponseStreamWriterFactory);
         public virtual Task ExecuteAsync(ActionContext context, ContentResult result);
     }
     public class DefaultOutputFormatterSelector : OutputFormatterSelector {
         public DefaultOutputFormatterSelector(IOptions<MvcOptions> options, ILoggerFactory loggerFactory);
         public override IOutputFormatter SelectFormatter(OutputFormatterCanWriteContext context, IList<IOutputFormatter> formatters, MediaTypeCollection contentTypes);
     }
     public sealed class DefaultStatusCodeAttribute : Attribute {
         public DefaultStatusCodeAttribute(int statusCode);
         public int StatusCode { get; }
     }
     public class FileContentResultExecutor : FileResultExecutorBase, IActionResultExecutor<FileContentResult> {
         public FileContentResultExecutor(ILoggerFactory loggerFactory);
         public virtual Task ExecuteAsync(ActionContext context, FileContentResult result);
         protected virtual Task WriteFileAsync(ActionContext context, FileContentResult result, RangeItemHeaderValue range, long rangeLength);
     }
     public class FileResultExecutorBase {
         protected const int BufferSize = 65536;
         public FileResultExecutorBase(ILogger logger);
         protected ILogger Logger { get; }
         protected static ILogger CreateLogger<T>(ILoggerFactory factory);
         protected virtual ValueTuple<RangeItemHeaderValue, long, bool> SetHeadersAndLog(ActionContext context, FileResult result, Nullable<long> fileLength, bool enableRangeProcessing, Nullable<DateTimeOffset> lastModified = default(Nullable<DateTimeOffset>), EntityTagHeaderValue etag = null);
         protected static Task WriteFileAsync(HttpContext context, Stream fileStream, RangeItemHeaderValue range, long rangeLength);
     }
     public class FileStreamResultExecutor : FileResultExecutorBase, IActionResultExecutor<FileStreamResult> {
         public FileStreamResultExecutor(ILoggerFactory loggerFactory);
         public virtual Task ExecuteAsync(ActionContext context, FileStreamResult result);
         protected virtual Task WriteFileAsync(ActionContext context, FileStreamResult result, RangeItemHeaderValue range, long rangeLength);
     }
     public interface IActionContextAccessor {
         ActionContext ActionContext { get; set; }
     }
     public interface IActionDescriptorChangeProvider {
         IChangeToken GetChangeToken();
     }
     public interface IActionDescriptorCollectionProvider {
         ActionDescriptorCollection ActionDescriptors { get; }
     }
     public interface IActionInvokerFactory {
         IActionInvoker CreateInvoker(ActionContext actionContext);
     }
     public interface IActionResultExecutor<in TResult> where TResult : IActionResult {
         Task ExecuteAsync(ActionContext context, TResult result);
     }
     public interface IActionResultTypeMapper {
         IActionResult Convert(object value, Type returnType);
         Type GetResultDataType(Type returnType);
     }
     public interface IActionSelector {
         ActionDescriptor SelectBestCandidate(RouteContext context, IReadOnlyList<ActionDescriptor> candidates);
         IReadOnlyList<ActionDescriptor> SelectCandidates(RouteContext context);
     }
+    public interface IApiBehaviorMetadata : IFilterMetadata
     public interface IClientErrorActionResult : IActionResult, IStatusCodeActionResult
     public interface IClientErrorFactory {
         IActionResult GetClientError(ActionContext actionContext, IClientErrorActionResult clientError);
     }
     public interface ICompatibilitySwitch {
         bool IsValueSet { get; }
         string Name { get; }
         object Value { get; set; }
     }
     public interface IConvertToActionResult {
         IActionResult Convert();
     }
     public interface IHttpRequestStreamReaderFactory {
         TextReader CreateReader(Stream stream, Encoding encoding);
     }
     public interface IHttpResponseStreamWriterFactory {
         TextWriter CreateWriter(Stream stream, Encoding encoding);
     }
     public interface IParameterInfoParameterDescriptor {
         ParameterInfo ParameterInfo { get; }
     }
     public interface IPropertyInfoParameterDescriptor {
         PropertyInfo PropertyInfo { get; }
     }
     public interface IStatusCodeActionResult : IActionResult {
         Nullable<int> StatusCode { get; }
     }
     public class LocalRedirectResultExecutor : IActionResultExecutor<LocalRedirectResult> {
         public LocalRedirectResultExecutor(ILoggerFactory loggerFactory, IUrlHelperFactory urlHelperFactory);
         public virtual Task ExecuteAsync(ActionContext context, LocalRedirectResult result);
     }
     public class ModelStateInvalidFilter : IActionFilter, IFilterMetadata, IOrderedFilter {
         public ModelStateInvalidFilter(ApiBehaviorOptions apiBehaviorOptions, ILogger logger);
         public bool IsReusable { get; }
         public int Order { get; }
         public void OnActionExecuted(ActionExecutedContext context);
         public void OnActionExecuting(ActionExecutingContext context);
     }
     public class MvcCompatibilityOptions {
         public MvcCompatibilityOptions();
         public CompatibilityVersion CompatibilityVersion { get; set; }
     }
     public class ObjectResultExecutor : IActionResultExecutor<ObjectResult> {
         public ObjectResultExecutor(OutputFormatterSelector formatterSelector, IHttpResponseStreamWriterFactory writerFactory, ILoggerFactory loggerFactory);
+        public ObjectResultExecutor(OutputFormatterSelector formatterSelector, IHttpResponseStreamWriterFactory writerFactory, ILoggerFactory loggerFactory, IOptions<MvcOptions> mvcOptions);
         protected OutputFormatterSelector FormatterSelector { get; }
         protected ILogger Logger { get; }
         protected Func<Stream, Encoding, TextWriter> WriterFactory { get; }
         public virtual Task ExecuteAsync(ActionContext context, ObjectResult result);
     }
     public abstract class OutputFormatterSelector {
         protected OutputFormatterSelector();
         public abstract IOutputFormatter SelectFormatter(OutputFormatterCanWriteContext context, IList<IOutputFormatter> formatters, MediaTypeCollection mediaTypes);
     }
     public class PhysicalFileResultExecutor : FileResultExecutorBase, IActionResultExecutor<PhysicalFileResult> {
         public PhysicalFileResultExecutor(ILoggerFactory loggerFactory);
         public virtual Task ExecuteAsync(ActionContext context, PhysicalFileResult result);
         protected virtual PhysicalFileResultExecutor.FileMetadata GetFileInfo(string path);
         protected virtual Stream GetFileStream(string path);
         protected virtual Task WriteFileAsync(ActionContext context, PhysicalFileResult result, RangeItemHeaderValue range, long rangeLength);
         protected class FileMetadata {
             public FileMetadata();
             public bool Exists { get; set; }
             public DateTimeOffset LastModified { get; set; }
             public long Length { get; set; }
         }
     }
     public class RedirectResultExecutor : IActionResultExecutor<RedirectResult> {
         public RedirectResultExecutor(ILoggerFactory loggerFactory, IUrlHelperFactory urlHelperFactory);
         public virtual Task ExecuteAsync(ActionContext context, RedirectResult result);
     }
     public class RedirectToActionResultExecutor : IActionResultExecutor<RedirectToActionResult> {
         public RedirectToActionResultExecutor(ILoggerFactory loggerFactory, IUrlHelperFactory urlHelperFactory);
         public virtual Task ExecuteAsync(ActionContext context, RedirectToActionResult result);
     }
     public class RedirectToPageResultExecutor : IActionResultExecutor<RedirectToPageResult> {
         public RedirectToPageResultExecutor(ILoggerFactory loggerFactory, IUrlHelperFactory urlHelperFactory);
         public virtual Task ExecuteAsync(ActionContext context, RedirectToPageResult result);
     }
     public class RedirectToRouteResultExecutor : IActionResultExecutor<RedirectToRouteResult> {
         public RedirectToRouteResultExecutor(ILoggerFactory loggerFactory, IUrlHelperFactory urlHelperFactory);
         public virtual Task ExecuteAsync(ActionContext context, RedirectToRouteResult result);
     }
     public class VirtualFileResultExecutor : FileResultExecutorBase, IActionResultExecutor<VirtualFileResult> {
-        public VirtualFileResultExecutor(ILoggerFactory loggerFactory, IHostingEnvironment hostingEnvironment);

+        public VirtualFileResultExecutor(ILoggerFactory loggerFactory, IWebHostEnvironment hostingEnvironment);
         public virtual Task ExecuteAsync(ActionContext context, VirtualFileResult result);
         protected virtual Stream GetFileStream(IFileInfo fileInfo);
         protected virtual Task WriteFileAsync(ActionContext context, VirtualFileResult result, IFileInfo fileInfo, RangeItemHeaderValue range, long rangeLength);
     }
 }
```

