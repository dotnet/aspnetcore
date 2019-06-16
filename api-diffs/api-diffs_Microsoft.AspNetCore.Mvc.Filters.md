# Microsoft.AspNetCore.Mvc.Filters

``` diff
 namespace Microsoft.AspNetCore.Mvc.Filters {
     public class ActionExecutedContext : FilterContext {
         public ActionExecutedContext(ActionContext actionContext, IList<IFilterMetadata> filters, object controller);
         public virtual bool Canceled { get; set; }
         public virtual object Controller { get; }
         public virtual Exception Exception { get; set; }
         public virtual ExceptionDispatchInfo ExceptionDispatchInfo { get; set; }
         public virtual bool ExceptionHandled { get; set; }
         public virtual IActionResult Result { get; set; }
     }
     public class ActionExecutingContext : FilterContext {
         public ActionExecutingContext(ActionContext actionContext, IList<IFilterMetadata> filters, IDictionary<string, object> actionArguments, object controller);
         public virtual IDictionary<string, object> ActionArguments { get; }
         public virtual object Controller { get; }
         public virtual IActionResult Result { get; set; }
     }
     public delegate Task<ActionExecutedContext> ActionExecutionDelegate();
     public abstract class ActionFilterAttribute : Attribute, IActionFilter, IAsyncActionFilter, IAsyncResultFilter, IFilterMetadata, IOrderedFilter, IResultFilter {
         protected ActionFilterAttribute();
         public int Order { get; set; }
         public virtual void OnActionExecuted(ActionExecutedContext context);
         public virtual void OnActionExecuting(ActionExecutingContext context);
         public virtual Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next);
         public virtual void OnResultExecuted(ResultExecutedContext context);
         public virtual void OnResultExecuting(ResultExecutingContext context);
         public virtual Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next);
     }
     public class AuthorizationFilterContext : FilterContext {
         public AuthorizationFilterContext(ActionContext actionContext, IList<IFilterMetadata> filters);
         public virtual IActionResult Result { get; set; }
     }
     public class ExceptionContext : FilterContext {
         public ExceptionContext(ActionContext actionContext, IList<IFilterMetadata> filters);
         public virtual Exception Exception { get; set; }
         public virtual ExceptionDispatchInfo ExceptionDispatchInfo { get; set; }
         public virtual bool ExceptionHandled { get; set; }
         public virtual IActionResult Result { get; set; }
     }
     public abstract class ExceptionFilterAttribute : Attribute, IAsyncExceptionFilter, IExceptionFilter, IFilterMetadata, IOrderedFilter {
         protected ExceptionFilterAttribute();
         public int Order { get; set; }
         public virtual void OnException(ExceptionContext context);
         public virtual Task OnExceptionAsync(ExceptionContext context);
     }
     public class FilterCollection : Collection<IFilterMetadata> {
         public FilterCollection();
         public IFilterMetadata Add(Type filterType);
         public IFilterMetadata Add(Type filterType, int order);
         public IFilterMetadata Add<TFilterType>() where TFilterType : IFilterMetadata;
         public IFilterMetadata Add<TFilterType>(int order) where TFilterType : IFilterMetadata;
         public IFilterMetadata AddService(Type filterType);
         public IFilterMetadata AddService(Type filterType, int order);
         public IFilterMetadata AddService<TFilterType>() where TFilterType : IFilterMetadata;
         public IFilterMetadata AddService<TFilterType>(int order) where TFilterType : IFilterMetadata;
     }
     public abstract class FilterContext : ActionContext {
         public FilterContext(ActionContext actionContext, IList<IFilterMetadata> filters);
         public virtual IList<IFilterMetadata> Filters { get; }
         public TMetadata FindEffectivePolicy<TMetadata>() where TMetadata : IFilterMetadata;
         public bool IsEffectivePolicy<TMetadata>(TMetadata policy) where TMetadata : IFilterMetadata;
     }
     public class FilterDescriptor {
         public FilterDescriptor(IFilterMetadata filter, int filterScope);
         public IFilterMetadata Filter { get; }
         public int Order { get; set; }
         public int Scope { get; }
     }
     public class FilterItem {
         public FilterItem(FilterDescriptor descriptor);
         public FilterItem(FilterDescriptor descriptor, IFilterMetadata filter);
         public FilterDescriptor Descriptor { get; }
         public IFilterMetadata Filter { get; set; }
         public bool IsReusable { get; set; }
     }
     public class FilterProviderContext {
         public FilterProviderContext(ActionContext actionContext, IList<FilterItem> items);
         public ActionContext ActionContext { get; set; }
         public IList<FilterItem> Results { get; set; }
     }
     public static class FilterScope {
         public static readonly int Action;
         public static readonly int Controller;
         public static readonly int First;
         public static readonly int Global;
         public static readonly int Last;
     }
     public interface IActionFilter : IFilterMetadata {
         void OnActionExecuted(ActionExecutedContext context);
         void OnActionExecuting(ActionExecutingContext context);
     }
     public interface IAlwaysRunResultFilter : IFilterMetadata, IResultFilter
     public interface IAsyncActionFilter : IFilterMetadata {
         Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next);
     }
     public interface IAsyncAlwaysRunResultFilter : IAsyncResultFilter, IFilterMetadata
     public interface IAsyncAuthorizationFilter : IFilterMetadata {
         Task OnAuthorizationAsync(AuthorizationFilterContext context);
     }
     public interface IAsyncExceptionFilter : IFilterMetadata {
         Task OnExceptionAsync(ExceptionContext context);
     }
     public interface IAsyncPageFilter : IFilterMetadata {
         Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next);
         Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context);
     }
     public interface IAsyncResourceFilter : IFilterMetadata {
         Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next);
     }
     public interface IAsyncResultFilter : IFilterMetadata {
         Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next);
     }
     public interface IAuthorizationFilter : IFilterMetadata {
         void OnAuthorization(AuthorizationFilterContext context);
     }
     public interface IExceptionFilter : IFilterMetadata {
         void OnException(ExceptionContext context);
     }
     public interface IFilterContainer {
         IFilterMetadata FilterDefinition { get; set; }
     }
     public interface IFilterFactory : IFilterMetadata {
         bool IsReusable { get; }
         IFilterMetadata CreateInstance(IServiceProvider serviceProvider);
     }
     public interface IFilterMetadata
     public interface IFilterProvider {
         int Order { get; }
         void OnProvidersExecuted(FilterProviderContext context);
         void OnProvidersExecuting(FilterProviderContext context);
     }
     public interface IOrderedFilter : IFilterMetadata {
         int Order { get; }
     }
     public interface IPageFilter : IFilterMetadata {
         void OnPageHandlerExecuted(PageHandlerExecutedContext context);
         void OnPageHandlerExecuting(PageHandlerExecutingContext context);
         void OnPageHandlerSelected(PageHandlerSelectedContext context);
     }
     public interface IResourceFilter : IFilterMetadata {
         void OnResourceExecuted(ResourceExecutedContext context);
         void OnResourceExecuting(ResourceExecutingContext context);
     }
     public interface IResultFilter : IFilterMetadata {
         void OnResultExecuted(ResultExecutedContext context);
         void OnResultExecuting(ResultExecutingContext context);
     }
     public class PageHandlerExecutedContext : FilterContext {
         public PageHandlerExecutedContext(PageContext pageContext, IList<IFilterMetadata> filters, HandlerMethodDescriptor handlerMethod, object handlerInstance);
         public virtual new CompiledPageActionDescriptor ActionDescriptor { get; }
         public virtual bool Canceled { get; set; }
         public virtual Exception Exception { get; set; }
         public virtual ExceptionDispatchInfo ExceptionDispatchInfo { get; set; }
         public virtual bool ExceptionHandled { get; set; }
         public virtual object HandlerInstance { get; }
         public virtual HandlerMethodDescriptor HandlerMethod { get; }
         public virtual IActionResult Result { get; set; }
     }
     public class PageHandlerExecutingContext : FilterContext {
         public PageHandlerExecutingContext(PageContext pageContext, IList<IFilterMetadata> filters, HandlerMethodDescriptor handlerMethod, IDictionary<string, object> handlerArguments, object handlerInstance);
         public virtual new CompiledPageActionDescriptor ActionDescriptor { get; }
         public virtual IDictionary<string, object> HandlerArguments { get; }
         public virtual object HandlerInstance { get; }
         public virtual HandlerMethodDescriptor HandlerMethod { get; }
         public virtual IActionResult Result { get; set; }
     }
     public delegate Task<PageHandlerExecutedContext> PageHandlerExecutionDelegate();
     public class PageHandlerSelectedContext : FilterContext {
         public PageHandlerSelectedContext(PageContext pageContext, IList<IFilterMetadata> filters, object handlerInstance);
         public virtual new CompiledPageActionDescriptor ActionDescriptor { get; }
         public virtual object HandlerInstance { get; }
         public virtual HandlerMethodDescriptor HandlerMethod { get; set; }
     }
     public class ResourceExecutedContext : FilterContext {
         public ResourceExecutedContext(ActionContext actionContext, IList<IFilterMetadata> filters);
         public virtual bool Canceled { get; set; }
         public virtual Exception Exception { get; set; }
         public virtual ExceptionDispatchInfo ExceptionDispatchInfo { get; set; }
         public virtual bool ExceptionHandled { get; set; }
         public virtual IActionResult Result { get; set; }
     }
     public class ResourceExecutingContext : FilterContext {
         public ResourceExecutingContext(ActionContext actionContext, IList<IFilterMetadata> filters, IList<IValueProviderFactory> valueProviderFactories);
         public virtual IActionResult Result { get; set; }
         public IList<IValueProviderFactory> ValueProviderFactories { get; }
     }
     public delegate Task<ResourceExecutedContext> ResourceExecutionDelegate();
     public class ResultExecutedContext : FilterContext {
         public ResultExecutedContext(ActionContext actionContext, IList<IFilterMetadata> filters, IActionResult result, object controller);
         public virtual bool Canceled { get; set; }
         public virtual object Controller { get; }
         public virtual Exception Exception { get; set; }
         public virtual ExceptionDispatchInfo ExceptionDispatchInfo { get; set; }
         public virtual bool ExceptionHandled { get; set; }
         public virtual IActionResult Result { get; }
     }
     public class ResultExecutingContext : FilterContext {
         public ResultExecutingContext(ActionContext actionContext, IList<IFilterMetadata> filters, IActionResult result, object controller);
         public virtual bool Cancel { get; set; }
         public virtual object Controller { get; }
         public virtual IActionResult Result { get; set; }
     }
     public delegate Task<ResultExecutedContext> ResultExecutionDelegate();
     public abstract class ResultFilterAttribute : Attribute, IAsyncResultFilter, IFilterMetadata, IOrderedFilter, IResultFilter {
         protected ResultFilterAttribute();
         public int Order { get; set; }
         public virtual void OnResultExecuted(ResultExecutedContext context);
         public virtual void OnResultExecuting(ResultExecutingContext context);
         public virtual Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next);
     }
 }
```

