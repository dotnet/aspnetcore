# Microsoft.AspNetCore.Mvc.Internal

``` diff
-namespace Microsoft.AspNetCore.Mvc.Internal {
 {
-    public static class ActionAttributeRouteModel {
 {
-        public static IEnumerable<ValueTuple<AttributeRouteModel, SelectorModel, SelectorModel>> GetAttributeRoutes(ActionModel actionModel);

-    }
-    public class ActionConstraintCache {
 {
-        public ActionConstraintCache(IActionDescriptorCollectionProvider collectionProvider, IEnumerable<IActionConstraintProvider> actionConstraintProviders);

-        public IReadOnlyList<IActionConstraint> GetActionConstraints(HttpContext httpContext, ActionDescriptor action);

-    }
-    public class ActionInvokerFactory : IActionInvokerFactory {
 {
-        public ActionInvokerFactory(IEnumerable<IActionInvokerProvider> actionInvokerProviders);

-        public IActionInvoker CreateInvoker(ActionContext actionContext);

-    }
-    public class ActionResultTypeMapper : IActionResultTypeMapper {
 {
-        public ActionResultTypeMapper();

-        public IActionResult Convert(object value, Type returnType);

-        public Type GetResultDataType(Type returnType);

-    }
-    public class ActionSelector : IActionSelector {
 {
-        public ActionSelector(IActionDescriptorCollectionProvider actionDescriptorCollectionProvider, ActionConstraintCache actionConstraintCache, ILoggerFactory loggerFactory);

-        protected virtual IReadOnlyList<ActionDescriptor> SelectBestActions(IReadOnlyList<ActionDescriptor> actions);

-        public ActionDescriptor SelectBestCandidate(RouteContext context, IReadOnlyList<ActionDescriptor> candidates);

-        public IReadOnlyList<ActionDescriptor> SelectCandidates(RouteContext context);

-    }
-    public class AmbiguousActionException : InvalidOperationException {
 {
-        protected AmbiguousActionException(SerializationInfo info, StreamingContext context);

-        public AmbiguousActionException(string message);

-    }
-    public class ApiBehaviorOptionsSetup : ConfigureCompatibilityOptions<ApiBehaviorOptions>, IConfigureOptions<ApiBehaviorOptions> {
 {
-        public ApiBehaviorOptionsSetup(ILoggerFactory loggerFactory, IOptions<MvcCompatibilityOptions> compatibilityOptions);

-        protected override IReadOnlyDictionary<string, object> DefaultValues { get; }

-        public void Configure(ApiBehaviorOptions options);

-        public override void PostConfigure(string name, ApiBehaviorOptions options);

-    }
-    public class ApiDescriptionActionData {
 {
-        public ApiDescriptionActionData();

-        public string GroupName { get; set; }

-    }
-    public static class ApplicationModelConventions {
 {
-        public static void ApplyConventions(ApplicationModel applicationModel, IEnumerable<IApplicationModelConvention> conventions);

-    }
-    public class AttributeRoute : IRouter {
 {
-        public AttributeRoute(IActionDescriptorCollectionProvider actionDescriptorCollectionProvider, IServiceProvider services, Func<ActionDescriptor[], IRouter> handlerFactory);

-        public VirtualPathData GetVirtualPath(VirtualPathContext context);

-        public Task RouteAsync(RouteContext context);

-    }
-    public static class AttributeRouting {
 {
-        public static IRouter CreateAttributeMegaRoute(IServiceProvider services);

-    }
-    public class AuthorizationApplicationModelProvider : IApplicationModelProvider {
 {
-        public AuthorizationApplicationModelProvider(IAuthorizationPolicyProvider policyProvider);

-        public int Order { get; }

-        public static AuthorizeFilter GetFilter(IAuthorizationPolicyProvider policyProvider, IEnumerable<IAuthorizeData> authData);

-        public void OnProvidersExecuted(ApplicationModelProviderContext context);

-        public void OnProvidersExecuting(ApplicationModelProviderContext context);

-    }
-    public class ClientValidatorCache {
 {
-        public ClientValidatorCache();

-        public IReadOnlyList<IClientModelValidator> GetValidators(ModelMetadata metadata, IClientModelValidatorProvider validatorProvider);

-    }
-    public static class ControllerActionDescriptorBuilder {
 {
-        public static void AddRouteValues(ControllerActionDescriptor actionDescriptor, ControllerModel controller, ActionModel action);

-        public static IList<ControllerActionDescriptor> Build(ApplicationModel application);

-    }
-    public class ControllerActionDescriptorProvider : IActionDescriptorProvider {
 {
-        public ControllerActionDescriptorProvider(ApplicationPartManager partManager, IEnumerable<IApplicationModelProvider> applicationModelProviders, IOptions<MvcOptions> optionsAccessor);

-        public int Order { get; }

-        protected internal ApplicationModel BuildModel();

-        protected internal IEnumerable<ControllerActionDescriptor> GetDescriptors();

-        public void OnProvidersExecuted(ActionDescriptorProviderContext context);

-        public void OnProvidersExecuting(ActionDescriptorProviderContext context);

-    }
-    public class ControllerActionFilter : IAsyncActionFilter, IFilterMetadata, IOrderedFilter {
 {
-        public ControllerActionFilter();

-        public int Order { get; set; }

-        public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next);

-    }
-    public class ControllerActionInvoker : ResourceInvoker, IActionInvoker {
 {
-        protected override Task InvokeInnerFilterAsync();

-        protected override void ReleaseResources();

-    }
-    public class ControllerActionInvokerCache {
 {
-        public ControllerActionInvokerCache(IActionDescriptorCollectionProvider collectionProvider, ParameterBinder parameterBinder, IModelBinderFactory modelBinderFactory, IModelMetadataProvider modelMetadataProvider, IEnumerable<IFilterProvider> filterProviders, IControllerFactoryProvider factoryProvider, IOptions<MvcOptions> mvcOptions);

-        public ValueTuple<ControllerActionInvokerCacheEntry, IFilterMetadata[]> GetCachedResult(ControllerContext controllerContext);

-    }
-    public class ControllerActionInvokerCacheEntry {
 {
-        public FilterItem[] CachedFilters { get; }

-        public ControllerBinderDelegate ControllerBinderDelegate { get; }

-        public Func<ControllerContext, object> ControllerFactory { get; }

-        public Action<ControllerContext, object> ControllerReleaser { get; }

-    }
-    public class ControllerActionInvokerProvider : IActionInvokerProvider {
 {
-        public ControllerActionInvokerProvider(ControllerActionInvokerCache controllerActionInvokerCache, IOptions<MvcOptions> optionsAccessor, ILoggerFactory loggerFactory, DiagnosticSource diagnosticSource, IActionResultTypeMapper mapper);

-        public int Order { get; }

-        public void OnProvidersExecuted(ActionInvokerProviderContext context);

-        public void OnProvidersExecuting(ActionInvokerProviderContext context);

-    }
-    public delegate Task ControllerBinderDelegate(ControllerContext controllerContext, object controller, Dictionary<string, object> arguments);

-    public static class ControllerBinderDelegateProvider {
 {
-        public static ControllerBinderDelegate CreateBinderDelegate(ParameterBinder parameterBinder, IModelBinderFactory modelBinderFactory, IModelMetadataProvider modelMetadataProvider, ControllerActionDescriptor actionDescriptor, MvcOptions mvcOptions);

-    }
-    public class ControllerResultFilter : IAsyncResultFilter, IFilterMetadata, IOrderedFilter {
 {
-        public ControllerResultFilter();

-        public int Order { get; set; }

-        public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next);

-    }
-    public class CopyOnWriteList<T> : ICollection<T>, IEnumerable, IEnumerable<T>, IList<T> {
 {
-        public CopyOnWriteList(IReadOnlyList<T> source);

-        public int Count { get; }

-        public bool IsReadOnly { get; }

-        protected IReadOnlyList<T> Readable { get; }

-        public T this[int index] { get; set; }

-        protected List<T> Writable { get; }

-        public void Add(T item);

-        public void Clear();

-        public bool Contains(T item);

-        public void CopyTo(T[] array, int arrayIndex);

-        public IEnumerator<T> GetEnumerator();

-        public int IndexOf(T item);

-        public void Insert(int index, T item);

-        public bool Remove(T item);

-        public void RemoveAt(int index);

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-    }
-    public class DefaultActionConstraintProvider : IActionConstraintProvider {
 {
-        public DefaultActionConstraintProvider();

-        public int Order { get; }

-        public void OnProvidersExecuted(ActionConstraintProviderContext context);

-        public void OnProvidersExecuting(ActionConstraintProviderContext context);

-    }
-    public class DefaultApplicationModelProvider : IApplicationModelProvider {
 {
-        public DefaultApplicationModelProvider(IOptions<MvcOptions> mvcOptionsAccessor, IModelMetadataProvider modelMetadataProvider);

-        public int Order { get; }

-        protected virtual ActionModel CreateActionModel(TypeInfo typeInfo, MethodInfo methodInfo);

-        protected virtual ControllerModel CreateControllerModel(TypeInfo typeInfo);

-        protected virtual ParameterModel CreateParameterModel(ParameterInfo parameterInfo);

-        protected virtual PropertyModel CreatePropertyModel(PropertyInfo propertyInfo);

-        protected virtual bool IsAction(TypeInfo typeInfo, MethodInfo methodInfo);

-        public virtual void OnProvidersExecuted(ApplicationModelProviderContext context);

-        public virtual void OnProvidersExecuting(ApplicationModelProviderContext context);

-    }
-    public class DefaultBindingMetadataProvider : IBindingMetadataProvider, IMetadataDetailsProvider {
 {
-        public DefaultBindingMetadataProvider();

-        public void CreateBindingMetadata(BindingMetadataProviderContext context);

-    }
-    public class DefaultCollectionValidationStrategy : IValidationStrategy {
 {
-        public static readonly DefaultCollectionValidationStrategy Instance;

-        public IEnumerator<ValidationEntry> GetChildren(ModelMetadata metadata, string key, object model);

-        public IEnumerator GetEnumeratorForElementType(ModelMetadata metadata, object model);

-    }
-    public class DefaultComplexObjectValidationStrategy : IValidationStrategy {
 {
-        public static readonly IValidationStrategy Instance;

-        public IEnumerator<ValidationEntry> GetChildren(ModelMetadata metadata, string key, object model);

-    }
-    public class DefaultCompositeMetadataDetailsProvider : IBindingMetadataProvider, ICompositeMetadataDetailsProvider, IDisplayMetadataProvider, IMetadataDetailsProvider, IValidationMetadataProvider {
 {
-        public DefaultCompositeMetadataDetailsProvider(IEnumerable<IMetadataDetailsProvider> providers);

-        public virtual void CreateBindingMetadata(BindingMetadataProviderContext context);

-        public virtual void CreateDisplayMetadata(DisplayMetadataProviderContext context);

-        public virtual void CreateValidationMetadata(ValidationMetadataProviderContext context);

-    }
-    public class DefaultControllerPropertyActivator : IControllerPropertyActivator {
 {
-        public DefaultControllerPropertyActivator();

-        public void Activate(ControllerContext context, object controller);

-        public Action<ControllerContext, object> GetActivatorDelegate(ControllerActionDescriptor actionDescriptor);

-    }
-    public class DefaultFilterProvider : IFilterProvider {
 {
-        public DefaultFilterProvider();

-        public int Order { get; }

-        public void OnProvidersExecuted(FilterProviderContext context);

-        public void OnProvidersExecuting(FilterProviderContext context);

-        public virtual void ProvideFilter(FilterProviderContext context, FilterItem filterItem);

-    }
-    public class DefaultValidationMetadataProvider : IMetadataDetailsProvider, IValidationMetadataProvider {
 {
-        public DefaultValidationMetadataProvider();

-        public void CreateValidationMetadata(ValidationMetadataProviderContext context);

-    }
-    public class DisableRequestSizeLimitFilter : IAuthorizationFilter, IFilterMetadata, IRequestSizePolicy {
 {
-        public DisableRequestSizeLimitFilter(ILoggerFactory loggerFactory);

-        public void OnAuthorization(AuthorizationFilterContext context);

-    }
-    public class ElementalValueProvider : IValueProvider {
 {
-        public ElementalValueProvider(string key, string value, CultureInfo culture);

-        public CultureInfo Culture { get; }

-        public string Key { get; }

-        public string Value { get; }

-        public bool ContainsPrefix(string prefix);

-        public ValueProviderResult GetValue(string key);

-    }
-    public class ExplicitIndexCollectionValidationStrategy : IValidationStrategy {
 {
-        public ExplicitIndexCollectionValidationStrategy(IEnumerable<string> elementKeys);

-        public IEnumerable<string> ElementKeys { get; }

-        public IEnumerator<ValidationEntry> GetChildren(ModelMetadata metadata, string key, object model);

-    }
-    public struct FilterCursor {
 {
-        public FilterCursor(IFilterMetadata[] filters);

-        public FilterCursorItem<TFilter, TFilterAsync> GetNextFilter<TFilter, TFilterAsync>() where TFilter : class where TFilterAsync : class;

-        public void Reset();

-    }
-    public readonly struct FilterCursorItem<TFilter, TFilterAsync> {
 {
-        public FilterCursorItem(TFilter filter, TFilterAsync filterAsync);

-        public TFilter Filter { get; }

-        public TFilterAsync FilterAsync { get; }

-    }
-    public class FilterDescriptorOrderComparer : IComparer<FilterDescriptor> {
 {
-        public FilterDescriptorOrderComparer();

-        public static FilterDescriptorOrderComparer Comparer { get; }

-        public int Compare(FilterDescriptor x, FilterDescriptor y);

-    }
-    public static class FilterFactory {
 {
-        public static IFilterMetadata[] CreateUncachedFilters(IFilterProvider[] filterProviders, ActionContext actionContext, FilterItem[] cachedFilterItems);

-        public static FilterFactoryResult GetAllFilters(IFilterProvider[] filterProviders, ActionContext actionContext);

-    }
-    public readonly struct FilterFactoryResult {
 {
-        public FilterFactoryResult(FilterItem[] cacheableFilters, IFilterMetadata[] filters);

-        public FilterItem[] CacheableFilters { get; }

-        public IFilterMetadata[] Filters { get; }

-    }
-    public class HttpMethodActionConstraint : IActionConstraint, IActionConstraintMetadata {
 {
-        public static readonly int HttpMethodConstraintOrder;

-        public HttpMethodActionConstraint(IEnumerable<string> httpMethods);

-        public IEnumerable<string> HttpMethods { get; }

-        public int Order { get; }

-        public virtual bool Accept(ActionConstraintContext context);

-    }
-    public interface IConsumesActionConstraint : IActionConstraint, IActionConstraintMetadata

-    public interface IControllerPropertyActivator {
 {
-        void Activate(ControllerContext context, object controller);

-        Action<ControllerContext, object> GetActivatorDelegate(ControllerActionDescriptor actionDescriptor);

-    }
-    public interface IMiddlewareFilterFeature {
 {
-        ResourceExecutingContext ResourceExecutingContext { get; }

-        ResourceExecutionDelegate ResourceExecutionDelegate { get; }

-    }
-    public interface IResponseCacheFilter : IFilterMetadata

-    public interface ITypeActivatorCache {
 {
-        TInstance CreateInstance<TInstance>(IServiceProvider serviceProvider, Type optionType);

-    }
-    public class MemoryPoolHttpRequestStreamReaderFactory : IHttpRequestStreamReaderFactory {
 {
-        public static readonly int DefaultBufferSize;

-        public MemoryPoolHttpRequestStreamReaderFactory(ArrayPool<byte> bytePool, ArrayPool<char> charPool);

-        public TextReader CreateReader(Stream stream, Encoding encoding);

-    }
-    public class MemoryPoolHttpResponseStreamWriterFactory : IHttpResponseStreamWriterFactory {
 {
-        public static readonly int DefaultBufferSize;

-        public MemoryPoolHttpResponseStreamWriterFactory(ArrayPool<byte> bytePool, ArrayPool<char> charPool);

-        public TextWriter CreateWriter(Stream stream, Encoding encoding);

-    }
-    public class MiddlewareFilterBuilder {
 {
-        public MiddlewareFilterBuilder(MiddlewareFilterConfigurationProvider configurationProvider);

-        public IApplicationBuilder ApplicationBuilder { get; set; }

-        public RequestDelegate GetPipeline(Type configurationType);

-    }
-    public class MiddlewareFilterConfigurationProvider {
 {
-        public MiddlewareFilterConfigurationProvider();

-        public Action<IApplicationBuilder> CreateConfigureDelegate(Type configurationType);

-    }
-    public class MiddlewareFilterFeature : IMiddlewareFilterFeature {
 {
-        public MiddlewareFilterFeature();

-        public ResourceExecutingContext ResourceExecutingContext { get; set; }

-        public ResourceExecutionDelegate ResourceExecutionDelegate { get; set; }

-    }
-    public class MvcAttributeRouteHandler : IRouter {
 {
-        public MvcAttributeRouteHandler(IActionInvokerFactory actionInvokerFactory, IActionSelector actionSelector, DiagnosticSource diagnosticSource, ILoggerFactory loggerFactory);

-        public MvcAttributeRouteHandler(IActionInvokerFactory actionInvokerFactory, IActionSelector actionSelector, DiagnosticSource diagnosticSource, ILoggerFactory loggerFactory, IActionContextAccessor actionContextAccessor);

-        public ActionDescriptor[] Actions { get; set; }

-        public VirtualPathData GetVirtualPath(VirtualPathContext context);

-        public Task RouteAsync(RouteContext context);

-    }
-    public class MvcBuilder : IMvcBuilder {
 {
-        public MvcBuilder(IServiceCollection services, ApplicationPartManager manager);

-        public ApplicationPartManager PartManager { get; }

-        public IServiceCollection Services { get; }

-    }
-    public class MvcCoreBuilder : IMvcCoreBuilder {
 {
-        public MvcCoreBuilder(IServiceCollection services, ApplicationPartManager manager);

-        public ApplicationPartManager PartManager { get; }

-        public IServiceCollection Services { get; }

-    }
-    public static class MvcCoreDiagnosticSourceExtensions {
 {
-        public static void AfterAction(this DiagnosticSource diagnosticSource, ActionDescriptor actionDescriptor, HttpContext httpContext, RouteData routeData);

-        public static void AfterActionMethod(this DiagnosticSource diagnosticSource, ActionContext actionContext, IDictionary<string, object> actionArguments, object controller, IActionResult result);

-        public static void AfterActionResult(this DiagnosticSource diagnosticSource, ActionContext actionContext, IActionResult result);

-        public static void AfterOnActionExecuted(this DiagnosticSource diagnosticSource, ActionExecutedContext actionExecutedContext, IActionFilter filter);

-        public static void AfterOnActionExecuting(this DiagnosticSource diagnosticSource, ActionExecutingContext actionExecutingContext, IActionFilter filter);

-        public static void AfterOnActionExecution(this DiagnosticSource diagnosticSource, ActionExecutedContext actionExecutedContext, IAsyncActionFilter filter);

-        public static void AfterOnAuthorization(this DiagnosticSource diagnosticSource, AuthorizationFilterContext authorizationContext, IAuthorizationFilter filter);

-        public static void AfterOnAuthorizationAsync(this DiagnosticSource diagnosticSource, AuthorizationFilterContext authorizationContext, IAsyncAuthorizationFilter filter);

-        public static void AfterOnException(this DiagnosticSource diagnosticSource, ExceptionContext exceptionContext, IExceptionFilter filter);

-        public static void AfterOnExceptionAsync(this DiagnosticSource diagnosticSource, ExceptionContext exceptionContext, IAsyncExceptionFilter filter);

-        public static void AfterOnResourceExecuted(this DiagnosticSource diagnosticSource, ResourceExecutedContext resourceExecutedContext, IResourceFilter filter);

-        public static void AfterOnResourceExecuting(this DiagnosticSource diagnosticSource, ResourceExecutingContext resourceExecutingContext, IResourceFilter filter);

-        public static void AfterOnResourceExecution(this DiagnosticSource diagnosticSource, ResourceExecutedContext resourceExecutedContext, IAsyncResourceFilter filter);

-        public static void AfterOnResultExecuted(this DiagnosticSource diagnosticSource, ResultExecutedContext resultExecutedContext, IResultFilter filter);

-        public static void AfterOnResultExecuting(this DiagnosticSource diagnosticSource, ResultExecutingContext resultExecutingContext, IResultFilter filter);

-        public static void AfterOnResultExecution(this DiagnosticSource diagnosticSource, ResultExecutedContext resultExecutedContext, IAsyncResultFilter filter);

-        public static void BeforeAction(this DiagnosticSource diagnosticSource, ActionDescriptor actionDescriptor, HttpContext httpContext, RouteData routeData);

-        public static void BeforeActionMethod(this DiagnosticSource diagnosticSource, ActionContext actionContext, IDictionary<string, object> actionArguments, object controller);

-        public static void BeforeActionResult(this DiagnosticSource diagnosticSource, ActionContext actionContext, IActionResult result);

-        public static void BeforeOnActionExecuted(this DiagnosticSource diagnosticSource, ActionExecutedContext actionExecutedContext, IActionFilter filter);

-        public static void BeforeOnActionExecuting(this DiagnosticSource diagnosticSource, ActionExecutingContext actionExecutingContext, IActionFilter filter);

-        public static void BeforeOnActionExecution(this DiagnosticSource diagnosticSource, ActionExecutingContext actionExecutingContext, IAsyncActionFilter filter);

-        public static void BeforeOnAuthorization(this DiagnosticSource diagnosticSource, AuthorizationFilterContext authorizationContext, IAuthorizationFilter filter);

-        public static void BeforeOnAuthorizationAsync(this DiagnosticSource diagnosticSource, AuthorizationFilterContext authorizationContext, IAsyncAuthorizationFilter filter);

-        public static void BeforeOnException(this DiagnosticSource diagnosticSource, ExceptionContext exceptionContext, IExceptionFilter filter);

-        public static void BeforeOnExceptionAsync(this DiagnosticSource diagnosticSource, ExceptionContext exceptionContext, IAsyncExceptionFilter filter);

-        public static void BeforeOnResourceExecuted(this DiagnosticSource diagnosticSource, ResourceExecutedContext resourceExecutedContext, IResourceFilter filter);

-        public static void BeforeOnResourceExecuting(this DiagnosticSource diagnosticSource, ResourceExecutingContext resourceExecutingContext, IResourceFilter filter);

-        public static void BeforeOnResourceExecution(this DiagnosticSource diagnosticSource, ResourceExecutingContext resourceExecutingContext, IAsyncResourceFilter filter);

-        public static void BeforeOnResultExecuted(this DiagnosticSource diagnosticSource, ResultExecutedContext resultExecutedContext, IResultFilter filter);

-        public static void BeforeOnResultExecuting(this DiagnosticSource diagnosticSource, ResultExecutingContext resultExecutingContext, IResultFilter filter);

-        public static void BeforeOnResultExecution(this DiagnosticSource diagnosticSource, ResultExecutingContext resultExecutingContext, IAsyncResultFilter filter);

-    }
-    public class MvcCoreRouteOptionsSetup : IConfigureOptions<RouteOptions> {
 {
-        public MvcCoreRouteOptionsSetup();

-        public void Configure(RouteOptions options);

-    }
-    public class MvcMarkerService {
 {
-        public MvcMarkerService();

-    }
-    public static class MvcRazorPagesDiagnosticSourceExtensions {
 {
-        public static void AfterHandlerMethod(this DiagnosticSource diagnosticSource, ActionContext actionContext, HandlerMethodDescriptor handlerMethodDescriptor, IDictionary<string, object> arguments, object instance, IActionResult result);

-        public static void AfterOnPageHandlerExecuted(this DiagnosticSource diagnosticSource, PageHandlerExecutedContext handlerExecutedContext, IPageFilter filter);

-        public static void AfterOnPageHandlerExecuting(this DiagnosticSource diagnosticSource, PageHandlerExecutingContext handlerExecutingContext, IPageFilter filter);

-        public static void AfterOnPageHandlerExecution(this DiagnosticSource diagnosticSource, PageHandlerExecutedContext handlerExecutedContext, IAsyncPageFilter filter);

-        public static void AfterOnPageHandlerSelected(this DiagnosticSource diagnosticSource, PageHandlerSelectedContext handlerSelectedContext, IPageFilter filter);

-        public static void AfterOnPageHandlerSelection(this DiagnosticSource diagnosticSource, PageHandlerSelectedContext handlerSelectedContext, IAsyncPageFilter filter);

-        public static void BeforeHandlerMethod(this DiagnosticSource diagnosticSource, ActionContext actionContext, HandlerMethodDescriptor handlerMethodDescriptor, IDictionary<string, object> arguments, object instance);

-        public static void BeforeOnPageHandlerExecuted(this DiagnosticSource diagnosticSource, PageHandlerExecutedContext handlerExecutedContext, IPageFilter filter);

-        public static void BeforeOnPageHandlerExecuting(this DiagnosticSource diagnosticSource, PageHandlerExecutingContext handlerExecutingContext, IPageFilter filter);

-        public static void BeforeOnPageHandlerExecution(this DiagnosticSource diagnosticSource, PageHandlerExecutingContext handlerExecutionContext, IAsyncPageFilter filter);

-        public static void BeforeOnPageHandlerSelected(this DiagnosticSource diagnosticSource, PageHandlerSelectedContext handlerSelectedContext, IPageFilter filter);

-        public static void BeforeOnPageHandlerSelection(this DiagnosticSource diagnosticSource, PageHandlerSelectedContext handlerSelectedContext, IAsyncPageFilter filter);

-    }
-    public class MvcRouteHandler : IRouter {
 {
-        public MvcRouteHandler(IActionInvokerFactory actionInvokerFactory, IActionSelector actionSelector, DiagnosticSource diagnosticSource, ILoggerFactory loggerFactory);

-        public MvcRouteHandler(IActionInvokerFactory actionInvokerFactory, IActionSelector actionSelector, DiagnosticSource diagnosticSource, ILoggerFactory loggerFactory, IActionContextAccessor actionContextAccessor);

-        public VirtualPathData GetVirtualPath(VirtualPathContext context);

-        public Task RouteAsync(RouteContext context);

-    }
-    public class NonDisposableStream : Stream {
 {
-        public NonDisposableStream(Stream innerStream);

-        public override bool CanRead { get; }

-        public override bool CanSeek { get; }

-        public override bool CanTimeout { get; }

-        public override bool CanWrite { get; }

-        protected Stream InnerStream { get; }

-        public override long Length { get; }

-        public override long Position { get; set; }

-        public override int ReadTimeout { get; set; }

-        public override int WriteTimeout { get; set; }

-        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state);

-        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state);

-        public override void Close();

-        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken);

-        protected override void Dispose(bool disposing);

-        public override int EndRead(IAsyncResult asyncResult);

-        public override void EndWrite(IAsyncResult asyncResult);

-        public override void Flush();

-        public override Task FlushAsync(CancellationToken cancellationToken);

-        public override int Read(byte[] buffer, int offset, int count);

-        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);

-        public override int ReadByte();

-        public override long Seek(long offset, SeekOrigin origin);

-        public override void SetLength(long value);

-        public override void Write(byte[] buffer, int offset, int count);

-        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);

-        public override void WriteByte(byte value);

-    }
-    public class NoOpBinder : IModelBinder {
 {
-        public static readonly IModelBinder Instance;

-        public NoOpBinder();

-        public Task BindModelAsync(ModelBindingContext bindingContext);

-    }
-    public static class NormalizedRouteValue {
 {
-        public static string GetNormalizedRouteValue(ActionContext context, string key);

-    }
-    public static class ParameterDefaultValues {
 {
-        public static object[] GetParameterDefaultValues(MethodInfo methodInfo);

-        public static bool TryGetDeclaredParameterDefaultValue(ParameterInfo parameterInfo, out object defaultValue);

-    }
-    public class PlaceholderBinder : IModelBinder {
 {
-        public PlaceholderBinder();

-        public IModelBinder Inner { get; set; }

-        public Task BindModelAsync(ModelBindingContext bindingContext);

-    }
-    public class PrefixContainer {
 {
-        public PrefixContainer(ICollection<string> values);

-        public bool ContainsPrefix(string prefix);

-        public IDictionary<string, string> GetKeysFromPrefix(string prefix);

-    }
-    public static class PropertyValueSetter {
 {
-        public static void SetValue(ModelMetadata metadata, object instance, object value);

-    }
-    public class RequestFormLimitsFilter : IAuthorizationFilter, IFilterMetadata, IRequestFormLimitsPolicy {
 {
-        public RequestFormLimitsFilter(ILoggerFactory loggerFactory);

-        public FormOptions FormOptions { get; set; }

-        public void OnAuthorization(AuthorizationFilterContext context);

-    }
-    public class RequestSizeLimitFilter : IAuthorizationFilter, IFilterMetadata, IRequestSizePolicy {
 {
-        public RequestSizeLimitFilter(ILoggerFactory loggerFactory);

-        public long Bytes { get; set; }

-        public void OnAuthorization(AuthorizationFilterContext context);

-    }
-    public abstract class ResourceInvoker {
 {
-        protected readonly ActionContext _actionContext;

-        protected readonly IFilterMetadata[] _filters;

-        protected IActionResult _result;

-        protected readonly IActionResultTypeMapper _mapper;

-        protected FilterCursor _cursor;

-        protected readonly ILogger _logger;

-        protected readonly IList<IValueProviderFactory> _valueProviderFactories;

-        protected readonly DiagnosticSource _diagnosticSource;

-        protected object _instance;

-        public ResourceInvoker(DiagnosticSource diagnosticSource, ILogger logger, IActionResultTypeMapper mapper, ActionContext actionContext, IFilterMetadata[] filters, IList<IValueProviderFactory> valueProviderFactories);

-        public virtual Task InvokeAsync();

-        protected abstract Task InvokeInnerFilterAsync();

-        protected virtual Task InvokeResultAsync(IActionResult result);

-        protected abstract void ReleaseResources();

-    }
-    public class ResponseCacheFilter : IActionFilter, IFilterMetadata, IResponseCacheFilter {
 {
-        public ResponseCacheFilter(CacheProfile cacheProfile, ILoggerFactory loggerFactory);

-        public int Duration { get; set; }

-        public ResponseCacheLocation Location { get; set; }

-        public bool NoStore { get; set; }

-        public string VaryByHeader { get; set; }

-        public string[] VaryByQueryKeys { get; set; }

-        public void OnActionExecuted(ActionExecutedContext context);

-        public void OnActionExecuting(ActionExecutingContext context);

-    }
-    public class ResponseCacheFilterExecutor {
 {
-        public ResponseCacheFilterExecutor(CacheProfile cacheProfile);

-        public int Duration { get; set; }

-        public ResponseCacheLocation Location { get; set; }

-        public bool NoStore { get; set; }

-        public string VaryByHeader { get; set; }

-        public string[] VaryByQueryKeys { get; set; }

-        public void Execute(FilterContext context);

-    }
-    public static class ResponseContentTypeHelper {
 {
-        public static void ResolveContentTypeAndEncoding(string actionResultContentType, string httpResponseContentType, string defaultContentType, out string resolvedContentType, out Encoding resolvedContentTypeEncoding);

-    }
-    public class ShortFormDictionaryValidationStrategy<TKey, TValue> : IValidationStrategy {
 {
-        public ShortFormDictionaryValidationStrategy(IEnumerable<KeyValuePair<string, TKey>> keyMappings, ModelMetadata valueMetadata);

-        public IEnumerable<KeyValuePair<string, TKey>> KeyMappings { get; }

-        public IEnumerator<ValidationEntry> GetChildren(ModelMetadata metadata, string key, object model);

-    }
-    public class TypeActivatorCache : ITypeActivatorCache {
 {
-        public TypeActivatorCache();

-        public TInstance CreateInstance<TInstance>(IServiceProvider serviceProvider, Type implementationType);

-    }
-    public class ValidatorCache {
 {
-        public ValidatorCache();

-        public IReadOnlyList<IModelValidator> GetValidators(ModelMetadata metadata, IModelValidatorProvider validatorProvider);

-    }
-    public static class ViewEnginePath {
 {
-        public static readonly char[] PathSeparators;

-        public static string CombinePath(string first, string second);

-        public static string ResolvePath(string path);

-    }
-}
```

