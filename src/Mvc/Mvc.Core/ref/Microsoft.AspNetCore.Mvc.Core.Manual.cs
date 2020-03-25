// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public sealed partial class ControllerActionEndpointConventionBuilder : Microsoft.AspNetCore.Builder.IEndpointConventionBuilder
    {
        internal ControllerActionEndpointConventionBuilder(object @lock, System.Collections.Generic.List<System.Action<Microsoft.AspNetCore.Builder.EndpointBuilder>> conventions) { }
    }
}
namespace Microsoft.AspNetCore.Internal
{
    internal partial class ChunkingCookieManager
    {
        public const int DefaultChunkSize = 4050;
        public ChunkingCookieManager() { }
        public int? ChunkSize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool ThrowForPartialCookies { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public void AppendResponseCookie(Microsoft.AspNetCore.Http.HttpContext context, string key, string value, Microsoft.AspNetCore.Http.CookieOptions options) { }
        public void DeleteCookie(Microsoft.AspNetCore.Http.HttpContext context, string key, Microsoft.AspNetCore.Http.CookieOptions options) { }
        public string GetRequestCookie(Microsoft.AspNetCore.Http.HttpContext context, string key) { throw null; }
    }
    internal static partial class RangeHelper
    {
        internal static Microsoft.Net.Http.Headers.RangeItemHeaderValue NormalizeRange(Microsoft.Net.Http.Headers.RangeItemHeaderValue range, long length) { throw null; }
        public static (bool isRangeRequest, Microsoft.Net.Http.Headers.RangeItemHeaderValue range) ParseRange(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Http.Headers.RequestHeaders requestHeaders, long length, Microsoft.Extensions.Logging.ILogger logger) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc
{
    public sealed partial class ApiConventionMethodAttribute : System.Attribute
    {
        internal System.Reflection.MethodInfo Method { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public sealed partial class ApiConventionTypeAttribute : System.Attribute
    {
        internal static void EnsureValid(System.Type conventionType) { }
    }
    internal static partial class MvcCoreDiagnosticListenerExtensions
    {
        public static void AfterAction(this System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor actionDescriptor, Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.RouteData routeData) { }
        public static void AfterActionResult(this System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.IActionResult result) { }
        public static void AfterControllerActionMethod(this System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.ActionContext actionContext, System.Collections.Generic.IReadOnlyDictionary<string, object> actionArguments, object controller, Microsoft.AspNetCore.Mvc.IActionResult result) { }
        public static void AfterOnActionExecuted(this System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext actionExecutedContext, Microsoft.AspNetCore.Mvc.Filters.IActionFilter filter) { }
        public static void AfterOnActionExecuting(this System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext actionExecutingContext, Microsoft.AspNetCore.Mvc.Filters.IActionFilter filter) { }
        public static void AfterOnActionExecution(this System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext actionExecutedContext, Microsoft.AspNetCore.Mvc.Filters.IAsyncActionFilter filter) { }
        public static void AfterOnAuthorization(this System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext authorizationContext, Microsoft.AspNetCore.Mvc.Filters.IAuthorizationFilter filter) { }
        public static void AfterOnAuthorizationAsync(this System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext authorizationContext, Microsoft.AspNetCore.Mvc.Filters.IAsyncAuthorizationFilter filter) { }
        public static void AfterOnException(this System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Filters.ExceptionContext exceptionContext, Microsoft.AspNetCore.Mvc.Filters.IExceptionFilter filter) { }
        public static void AfterOnExceptionAsync(this System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Filters.ExceptionContext exceptionContext, Microsoft.AspNetCore.Mvc.Filters.IAsyncExceptionFilter filter) { }
        public static void AfterOnResourceExecuted(this System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Filters.ResourceExecutedContext resourceExecutedContext, Microsoft.AspNetCore.Mvc.Filters.IResourceFilter filter) { }
        public static void AfterOnResourceExecuting(this System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Filters.ResourceExecutingContext resourceExecutingContext, Microsoft.AspNetCore.Mvc.Filters.IResourceFilter filter) { }
        public static void AfterOnResourceExecution(this System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Filters.ResourceExecutedContext resourceExecutedContext, Microsoft.AspNetCore.Mvc.Filters.IAsyncResourceFilter filter) { }
        public static void AfterOnResultExecuted(this System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Filters.ResultExecutedContext resultExecutedContext, Microsoft.AspNetCore.Mvc.Filters.IResultFilter filter) { }
        public static void AfterOnResultExecuting(this System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Filters.ResultExecutingContext resultExecutingContext, Microsoft.AspNetCore.Mvc.Filters.IResultFilter filter) { }
        public static void AfterOnResultExecution(this System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Filters.ResultExecutedContext resultExecutedContext, Microsoft.AspNetCore.Mvc.Filters.IAsyncResultFilter filter) { }
        public static void BeforeAction(this System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor actionDescriptor, Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.RouteData routeData) { }
        public static void BeforeActionResult(this System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.IActionResult result) { }
        public static void BeforeControllerActionMethod(this System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.ActionContext actionContext, System.Collections.Generic.IReadOnlyDictionary<string, object> actionArguments, object controller) { }
        public static void BeforeOnActionExecuted(this System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext actionExecutedContext, Microsoft.AspNetCore.Mvc.Filters.IActionFilter filter) { }
        public static void BeforeOnActionExecuting(this System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext actionExecutingContext, Microsoft.AspNetCore.Mvc.Filters.IActionFilter filter) { }
        public static void BeforeOnActionExecution(this System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext actionExecutingContext, Microsoft.AspNetCore.Mvc.Filters.IAsyncActionFilter filter) { }
        public static void BeforeOnAuthorization(this System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext authorizationContext, Microsoft.AspNetCore.Mvc.Filters.IAuthorizationFilter filter) { }
        public static void BeforeOnAuthorizationAsync(this System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext authorizationContext, Microsoft.AspNetCore.Mvc.Filters.IAsyncAuthorizationFilter filter) { }
        public static void BeforeOnException(this System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Filters.ExceptionContext exceptionContext, Microsoft.AspNetCore.Mvc.Filters.IExceptionFilter filter) { }
        public static void BeforeOnExceptionAsync(this System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Filters.ExceptionContext exceptionContext, Microsoft.AspNetCore.Mvc.Filters.IAsyncExceptionFilter filter) { }
        public static void BeforeOnResourceExecuted(this System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Filters.ResourceExecutedContext resourceExecutedContext, Microsoft.AspNetCore.Mvc.Filters.IResourceFilter filter) { }
        public static void BeforeOnResourceExecuting(this System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Filters.ResourceExecutingContext resourceExecutingContext, Microsoft.AspNetCore.Mvc.Filters.IResourceFilter filter) { }
        public static void BeforeOnResourceExecution(this System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Filters.ResourceExecutingContext resourceExecutingContext, Microsoft.AspNetCore.Mvc.Filters.IAsyncResourceFilter filter) { }
        public static void BeforeOnResultExecuted(this System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Filters.ResultExecutedContext resultExecutedContext, Microsoft.AspNetCore.Mvc.Filters.IResultFilter filter) { }
        public static void BeforeOnResultExecuting(this System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Filters.ResultExecutingContext resultExecutingContext, Microsoft.AspNetCore.Mvc.Filters.IResultFilter filter) { }
        public static void BeforeOnResultExecution(this System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Filters.ResultExecutingContext resultExecutingContext, Microsoft.AspNetCore.Mvc.Filters.IAsyncResultFilter filter) { }
    }
    internal static partial class MvcCoreLoggerExtensions
    {
        public const string ActionFilter = "Action Filter";
        public static void ActionDoesNotExplicitlySpecifyContentTypes(this Microsoft.Extensions.Logging.ILogger logger) { }
        public static void ActionDoesNotSupportFormatFilterContentType(this Microsoft.Extensions.Logging.ILogger logger, string format, Microsoft.AspNetCore.Mvc.Formatters.MediaTypeCollection supportedMediaTypes) { }
        public static void ActionFiltersExecutionPlan(this Microsoft.Extensions.Logging.ILogger logger, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata> filters) { }
        public static void ActionFilterShortCircuited(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filter) { }
        public static void ActionMethodExecuted(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.ControllerContext context, Microsoft.AspNetCore.Mvc.IActionResult result, System.TimeSpan timeSpan) { }
        public static void ActionMethodExecuting(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.ControllerContext context, object[] arguments) { }
#nullable enable
        public static System.IDisposable ActionScope(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor action) { throw new System.ArgumentException(); }
#nullable restore
        public static void AfterExecutingActionResult(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.IActionResult actionResult) { }
        public static void AfterExecutingMethodOnFilter(this Microsoft.Extensions.Logging.ILogger logger, string filterType, string methodName, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filter) { }
        public static void AmbiguousActions(this Microsoft.Extensions.Logging.ILogger logger, string actionNames) { }
        public static void AppliedRequestFormLimits(this Microsoft.Extensions.Logging.ILogger logger) { }
        public static void AttemptingToBindCollectionUsingIndices(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { }
        public static void AttemptingToBindModel(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { }
        public static void AttemptingToBindParameterOrProperty(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.Abstractions.ParameterDescriptor parameter, Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata modelMetadata) { }
        public static void AttemptingToValidateParameterOrProperty(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.Abstractions.ParameterDescriptor parameter, Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata modelMetadata) { }
        public static void AuthorizationFailure(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filter) { }
        public static void AuthorizationFiltersExecutionPlan(this Microsoft.Extensions.Logging.ILogger logger, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata> filters) { }
        public static void BeforeExecutingActionResult(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.IActionResult actionResult) { }
        public static void BeforeExecutingMethodOnFilter(this Microsoft.Extensions.Logging.ILogger logger, string filterType, string methodName, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filter) { }
        public static void CannotApplyFormatFilterContentType(this Microsoft.Extensions.Logging.ILogger logger, string format) { }
        public static void CannotApplyRequestFormLimits(this Microsoft.Extensions.Logging.ILogger logger) { }
        public static void CannotBindToComplexType(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { }
        public static void CannotBindToFilesCollectionDueToUnsupportedContentType(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { }
        public static void CannotCreateHeaderModelBinder(this Microsoft.Extensions.Logging.ILogger logger, System.Type modelType) { }
        public static void CannotCreateHeaderModelBinderCompatVersion_2_0(this Microsoft.Extensions.Logging.ILogger logger, System.Type modelType) { }
        public static void ChallengeResultExecuting(this Microsoft.Extensions.Logging.ILogger logger, System.Collections.Generic.IList<string> schemes) { }
        public static void ConstraintMismatch(this Microsoft.Extensions.Logging.ILogger logger, string actionName, string actionId, Microsoft.AspNetCore.Mvc.ActionConstraints.IActionConstraint actionConstraint) { }
        public static void ContentResultExecuting(this Microsoft.Extensions.Logging.ILogger logger, string contentType) { }
        public static void DoneAttemptingToBindModel(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { }
        public static void DoneAttemptingToBindParameterOrProperty(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.Abstractions.ParameterDescriptor parameter, Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata modelMetadata) { }
        public static void DoneAttemptingToValidateParameterOrProperty(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.Abstractions.ParameterDescriptor parameter, Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata modelMetadata) { }
        public static void ExceptionFiltersExecutionPlan(this Microsoft.Extensions.Logging.ILogger logger, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata> filters) { }
        public static void ExceptionFilterShortCircuited(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filter) { }
        public static void ExecutedAction(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor action, System.TimeSpan timeSpan) { }
        public static void ExecutedControllerFactory(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.ControllerContext context) { }
        public static void ExecutingAction(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor action) { }
        public static void ExecutingControllerFactory(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.ControllerContext context) { }
        public static void ExecutingFileResult(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.FileResult fileResult) { }
        public static void ExecutingFileResult(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.FileResult fileResult, string fileName) { }
        public static void FeatureIsReadOnly(this Microsoft.Extensions.Logging.ILogger logger) { }
        public static void FeatureNotFound(this Microsoft.Extensions.Logging.ILogger logger) { }
        public static void ForbidResultExecuting(this Microsoft.Extensions.Logging.ILogger logger, System.Collections.Generic.IList<string> authenticationSchemes) { }
        public static void FormatterSelected(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.Formatters.IOutputFormatter outputFormatter, Microsoft.AspNetCore.Mvc.Formatters.OutputFormatterCanWriteContext context) { }
        public static void FoundNoValueInRequest(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { }
        public static void HttpStatusCodeResultExecuting(this Microsoft.Extensions.Logging.ILogger logger, int statusCode) { }
        public static void IfMatchPreconditionFailed(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.Net.Http.Headers.EntityTagHeaderValue etag) { }
        public static void IfRangeETagPreconditionFailed(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.Net.Http.Headers.EntityTagHeaderValue currentETag, Microsoft.Net.Http.Headers.EntityTagHeaderValue ifRangeTag) { }
        public static void IfRangeLastModifiedPreconditionFailed(this Microsoft.Extensions.Logging.ILogger logger, System.DateTimeOffset? lastModified, System.DateTimeOffset? ifRangeLastModifiedDate) { }
        public static void IfUnmodifiedSincePreconditionFailed(this Microsoft.Extensions.Logging.ILogger logger, System.DateTimeOffset? lastModified, System.DateTimeOffset? ifUnmodifiedSinceDate) { }
        public static void InferredParameterBindingSource(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.ApplicationModels.ParameterModel parameterModel, Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource bindingSource) { }
        public static void InputFormatterRejected(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.Formatters.IInputFormatter inputFormatter, Microsoft.AspNetCore.Mvc.Formatters.InputFormatterContext formatterContext) { }
        public static void InputFormatterSelected(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.Formatters.IInputFormatter inputFormatter, Microsoft.AspNetCore.Mvc.Formatters.InputFormatterContext formatterContext) { }
        public static void LocalRedirectResultExecuting(this Microsoft.Extensions.Logging.ILogger logger, string destination) { }
        public static void MaxRequestBodySizeSet(this Microsoft.Extensions.Logging.ILogger logger, string requestSize) { }
        public static void ModelStateInvalidFilterExecuting(this Microsoft.Extensions.Logging.ILogger logger) { }
        public static void NoAcceptForNegotiation(this Microsoft.Extensions.Logging.ILogger logger) { }
        public static void NoActionsMatched(this Microsoft.Extensions.Logging.ILogger logger, System.Collections.Generic.IDictionary<string, object> routeValueDictionary) { }
        public static void NoFilesFoundInRequest(this Microsoft.Extensions.Logging.ILogger logger) { }
        public static void NoFormatter(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.Formatters.OutputFormatterCanWriteContext context, Microsoft.AspNetCore.Mvc.Formatters.MediaTypeCollection contentTypes) { }
        public static void NoFormatterFromNegotiation(this Microsoft.Extensions.Logging.ILogger logger, System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Formatters.MediaTypeSegmentWithQuality> acceptTypes) { }
        public static void NoInputFormatterSelected(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.Formatters.InputFormatterContext formatterContext) { }
        public static void NoKeyValueFormatForDictionaryModelBinder(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { }
        public static void NoNonIndexBasedFormatFoundForCollection(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { }
        public static void NoPublicSettableProperties(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { }
        public static void NotEnabledForRangeProcessing(this Microsoft.Extensions.Logging.ILogger logger) { }
        public static void NotMostEffectiveFilter(this Microsoft.Extensions.Logging.ILogger logger, System.Type overridenFilter, System.Type overridingFilter, System.Type policyType) { }
        public static void ObjectResultExecuting(this Microsoft.Extensions.Logging.ILogger logger, object value) { }
        public static void ParameterBinderRequestPredicateShortCircuit(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.Abstractions.ParameterDescriptor parameter, Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata modelMetadata) { }
        public static void RedirectResultExecuting(this Microsoft.Extensions.Logging.ILogger logger, string destination) { }
        public static void RedirectToActionResultExecuting(this Microsoft.Extensions.Logging.ILogger logger, string destination) { }
        public static void RedirectToPageResultExecuting(this Microsoft.Extensions.Logging.ILogger logger, string page) { }
        public static void RedirectToRouteResultExecuting(this Microsoft.Extensions.Logging.ILogger logger, string destination, string routeName) { }
        public static void RegisteredModelBinderProviders(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderProvider[] providers) { }
        public static void RegisteredOutputFormatters(this Microsoft.Extensions.Logging.ILogger logger, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Formatters.IOutputFormatter> outputFormatters) { }
        public static void RequestBodySizeLimitDisabled(this Microsoft.Extensions.Logging.ILogger logger) { }
        public static void ResourceFiltersExecutionPlan(this Microsoft.Extensions.Logging.ILogger logger, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata> filters) { }
        public static void ResourceFilterShortCircuited(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filter) { }
        public static void ResultFiltersExecutionPlan(this Microsoft.Extensions.Logging.ILogger logger, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata> filters) { }
        public static void ResultFilterShortCircuited(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filter) { }
        public static void SelectFirstCanWriteFormatter(this Microsoft.Extensions.Logging.ILogger logger) { }
        public static void SelectingOutputFormatterUsingAcceptHeader(this Microsoft.Extensions.Logging.ILogger logger, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Formatters.MediaTypeSegmentWithQuality> acceptHeader) { }
        public static void SelectingOutputFormatterUsingAcceptHeaderAndExplicitContentTypes(this Microsoft.Extensions.Logging.ILogger logger, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Formatters.MediaTypeSegmentWithQuality> acceptHeader, Microsoft.AspNetCore.Mvc.Formatters.MediaTypeCollection mediaTypeCollection) { }
        public static void SelectingOutputFormatterUsingContentTypes(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.Formatters.MediaTypeCollection mediaTypeCollection) { }
        public static void SelectingOutputFormatterWithoutUsingContentTypes(this Microsoft.Extensions.Logging.ILogger logger) { }
        public static void SignInResultExecuting(this Microsoft.Extensions.Logging.ILogger logger, string authenticationScheme, System.Security.Claims.ClaimsPrincipal principal) { }
        public static void SignOutResultExecuting(this Microsoft.Extensions.Logging.ILogger logger, System.Collections.Generic.IList<string> authenticationSchemes) { }
        public static void SkippedContentNegotiation(this Microsoft.Extensions.Logging.ILogger logger, string contentType) { }
        public static void TransformingClientError(this Microsoft.Extensions.Logging.ILogger logger, System.Type initialType, System.Type replacedType, int? statusCode) { }
        public static void UnsupportedFormatFilterContentType(this Microsoft.Extensions.Logging.ILogger logger, string format) { }
        public static void WritingRangeToBody(this Microsoft.Extensions.Logging.ILogger logger) { }
    }
    internal partial class MvcCoreMvcOptionsSetup : Microsoft.Extensions.Options.IConfigureOptions<Microsoft.AspNetCore.Mvc.MvcOptions>, Microsoft.Extensions.Options.IPostConfigureOptions<Microsoft.AspNetCore.Mvc.MvcOptions>
    {
        public MvcCoreMvcOptionsSetup(Microsoft.AspNetCore.Mvc.Infrastructure.IHttpRequestStreamReaderFactory readerFactory) { }
        public MvcCoreMvcOptionsSetup(Microsoft.AspNetCore.Mvc.Infrastructure.IHttpRequestStreamReaderFactory readerFactory, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.JsonOptions> jsonOptions) { }
        public void Configure(Microsoft.AspNetCore.Mvc.MvcOptions options) { }
        internal static void ConfigureAdditionalModelMetadataDetailsProviders(System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IMetadataDetailsProvider> modelMetadataDetailsProviders) { }
        public void PostConfigure(string name, Microsoft.AspNetCore.Mvc.MvcOptions options) { }
    }
    public partial class MvcOptions : System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch>, System.Collections.IEnumerable
    {
        internal const int DefaultMaxModelBindingCollectionSize = 1024;
        internal const int DefaultMaxModelBindingRecursionDepth = 32;
    }
    public partial class RequestFormLimitsAttribute : System.Attribute, Microsoft.AspNetCore.Mvc.Filters.IFilterFactory, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter
    {
        internal Microsoft.AspNetCore.Http.Features.FormOptions FormOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
}
namespace Microsoft.AspNetCore.Mvc.ActionConstraints
{
    internal partial class ActionConstraintCache
    {
        public ActionConstraintCache(Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider collectionProvider, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ActionConstraints.IActionConstraintProvider> actionConstraintProviders) { }
        internal Microsoft.AspNetCore.Mvc.ActionConstraints.ActionConstraintCache.InnerCache CurrentCache { get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ActionConstraints.IActionConstraint> GetActionConstraints(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor action) { throw null; }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        internal readonly partial struct CacheEntry
        {
            private readonly object _dummy;
            public CacheEntry(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ActionConstraints.IActionConstraint> actionConstraints) { throw null; }
            public CacheEntry(System.Collections.Generic.List<Microsoft.AspNetCore.Mvc.ActionConstraints.ActionConstraintItem> items) { throw null; }
            public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ActionConstraints.IActionConstraint> ActionConstraints { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
            public System.Collections.Generic.List<Microsoft.AspNetCore.Mvc.ActionConstraints.ActionConstraintItem> Items { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        }
        internal partial class InnerCache
        {
            public InnerCache(Microsoft.AspNetCore.Mvc.Infrastructure.ActionDescriptorCollection actions) { }
            public System.Collections.Concurrent.ConcurrentDictionary<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor, Microsoft.AspNetCore.Mvc.ActionConstraints.ActionConstraintCache.CacheEntry> Entries { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
            public int Version { get { throw null; } }
        }
    }
    internal partial class DefaultActionConstraintProvider : Microsoft.AspNetCore.Mvc.ActionConstraints.IActionConstraintProvider
    {
        public DefaultActionConstraintProvider() { }
        public int Order { get { throw null; } }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.ActionConstraints.ActionConstraintProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.ActionConstraints.ActionConstraintProviderContext context) { }
    }
    internal partial interface IConsumesActionConstraint : Microsoft.AspNetCore.Mvc.ActionConstraints.IActionConstraint, Microsoft.AspNetCore.Mvc.ActionConstraints.IActionConstraintMetadata
    {
    }
}
namespace Microsoft.AspNetCore.Mvc.ApiExplorer
{
    internal static partial class ApiConventionMatcher
    {
        internal static Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchBehavior GetNameMatchBehavior(System.Reflection.ICustomAttributeProvider attributeProvider) { throw null; }
        internal static Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionTypeMatchBehavior GetTypeMatchBehavior(System.Reflection.ICustomAttributeProvider attributeProvider) { throw null; }
        internal static bool IsMatch(System.Reflection.MethodInfo methodInfo, System.Reflection.MethodInfo conventionMethod) { throw null; }
        internal static bool IsNameMatch(string name, string conventionName, Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchBehavior nameMatchBehavior) { throw null; }
        internal static bool IsTypeMatch(System.Type type, System.Type conventionType, Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionTypeMatchBehavior typeMatchBehavior) { throw null; }
    }
    public sealed partial class ApiConventionResult
    {
        internal static bool TryGetApiConvention(System.Reflection.MethodInfo method, Microsoft.AspNetCore.Mvc.ApiConventionTypeAttribute[] apiConventionAttributes, out Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionResult result) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    internal static partial class ActionAttributeRouteModel
    {
        public static System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ApplicationModels.SelectorModel> FlattenSelectors(Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel actionModel) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.ValueTuple<Microsoft.AspNetCore.Mvc.ApplicationModels.AttributeRouteModel, Microsoft.AspNetCore.Mvc.ApplicationModels.SelectorModel, Microsoft.AspNetCore.Mvc.ApplicationModels.SelectorModel>> GetAttributeRoutes(Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel actionModel) { throw null; }
    }
    internal partial class ApiBehaviorApplicationModelProvider : Microsoft.AspNetCore.Mvc.ApplicationModels.IApplicationModelProvider
    {
        public ApiBehaviorApplicationModelProvider(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions> apiBehaviorOptions, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider modelMetadataProvider, Microsoft.AspNetCore.Mvc.Infrastructure.IClientErrorFactory clientErrorFactory, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public System.Collections.Generic.List<Microsoft.AspNetCore.Mvc.ApplicationModels.IActionModelConvention> ActionModelConventions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public int Order { get { throw null; } }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelProviderContext context) { }
    }
    internal static partial class ApplicationModelConventions
    {
        public static void ApplyConventions(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModel applicationModel, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ApplicationModels.IApplicationModelConvention> conventions) { }
    }
    internal partial class ApplicationModelFactory
    {
        public ApplicationModelFactory(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ApplicationModels.IApplicationModelProvider> applicationModelProviders, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> options) { }
        public Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModel CreateApplicationModel(System.Collections.Generic.IEnumerable<System.Reflection.TypeInfo> controllerTypes) { throw null; }
        public static System.Collections.Generic.List<TResult> Flatten<TResult>(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModel application, System.Func<Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModel, Microsoft.AspNetCore.Mvc.ApplicationModels.ControllerModel, Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel, Microsoft.AspNetCore.Mvc.ApplicationModels.SelectorModel, TResult> flattener) { throw null; }
    }
    internal partial class AuthorizationApplicationModelProvider : Microsoft.AspNetCore.Mvc.ApplicationModels.IApplicationModelProvider
    {
        public AuthorizationApplicationModelProvider(Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider policyProvider, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> mvcOptions) { }
        public int Order { get { throw null; } }
        public static Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter GetFilter(Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider policyProvider, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Authorization.IAuthorizeData> authData) { throw null; }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelProviderContext context) { }
    }
    public partial class ConsumesConstraintForFormFileParameterConvention : Microsoft.AspNetCore.Mvc.ApplicationModels.IActionModelConvention
    {
        internal void AddMultipartFormDataConsumesAttribute(Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel action) { }
    }
    internal static partial class ControllerActionDescriptorBuilder
    {
        public static void AddRouteValues(Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.ApplicationModels.ControllerModel controller, Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel action) { }
        public static System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor> Build(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModel application) { throw null; }
    }
    internal partial class ControllerActionDescriptorProvider : Microsoft.AspNetCore.Mvc.Abstractions.IActionDescriptorProvider
    {
        public ControllerActionDescriptorProvider(Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager partManager, Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelFactory applicationModelFactory) { }
        public int Order { get { throw null; } }
        internal System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor> GetDescriptors() { throw null; }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptorProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptorProviderContext context) { }
    }
    internal partial class DefaultApplicationModelProvider : Microsoft.AspNetCore.Mvc.ApplicationModels.IApplicationModelProvider
    {
        public DefaultApplicationModelProvider(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> mvcOptionsAccessor, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider modelMetadataProvider) { }
        public int Order { get { throw null; } }
        internal Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel CreateActionModel(System.Reflection.TypeInfo typeInfo, System.Reflection.MethodInfo methodInfo) { throw null; }
        internal Microsoft.AspNetCore.Mvc.ApplicationModels.ControllerModel CreateControllerModel(System.Reflection.TypeInfo typeInfo) { throw null; }
        internal Microsoft.AspNetCore.Mvc.ApplicationModels.ParameterModel CreateParameterModel(System.Reflection.ParameterInfo parameterInfo) { throw null; }
        internal Microsoft.AspNetCore.Mvc.ApplicationModels.PropertyModel CreatePropertyModel(System.Reflection.PropertyInfo propertyInfo) { throw null; }
        internal bool IsAction(System.Reflection.TypeInfo typeInfo, System.Reflection.MethodInfo methodInfo) { throw null; }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelProviderContext context) { }
    }
    public partial class InferParameterBindingInfoConvention : Microsoft.AspNetCore.Mvc.ApplicationModels.IActionModelConvention
    {
        internal Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource InferBindingSourceForParameter(Microsoft.AspNetCore.Mvc.ApplicationModels.ParameterModel parameter) { throw null; }
        internal void InferParameterBindingSources(Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel action) { }
    }
}
namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    public partial class ApplicationPartManager
    {
        internal void PopulateDefaultParts(string entryAssemblyName) { }
    }
    public sealed partial class RelatedAssemblyAttribute : System.Attribute
    {
        internal static string GetAssemblyLocation(System.Reflection.Assembly assembly) { throw null; }
        internal static System.Collections.Generic.IReadOnlyList<System.Reflection.Assembly> GetRelatedAssemblies(System.Reflection.Assembly assembly, bool throwOnError, System.Func<string, bool> fileExists, System.Func<string, System.Reflection.Assembly> loadFile) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.Authorization
{
    public partial class AuthorizeFilter : Microsoft.AspNetCore.Mvc.Filters.IAsyncAuthorizationFilter, Microsoft.AspNetCore.Mvc.Filters.IFilterFactory, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata
    {
        [System.Diagnostics.DebuggerStepThroughAttribute]
        internal System.Threading.Tasks.Task<Microsoft.AspNetCore.Authorization.AuthorizationPolicy> GetEffectivePolicyAsync(Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext context) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.Controllers
{
    internal delegate System.Threading.Tasks.Task ControllerBinderDelegate(Microsoft.AspNetCore.Mvc.ControllerContext controllerContext, object controller, System.Collections.Generic.Dictionary<string, object> arguments);
    internal static partial class ControllerBinderDelegateProvider
    {
        public static Microsoft.AspNetCore.Mvc.Controllers.ControllerBinderDelegate CreateBinderDelegate(Microsoft.AspNetCore.Mvc.ModelBinding.ParameterBinder parameterBinder, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderFactory modelBinderFactory, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider modelMetadataProvider, Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.MvcOptions mvcOptions) { throw null; }
    }
    internal partial class ControllerFactoryProvider : Microsoft.AspNetCore.Mvc.Controllers.IControllerFactoryProvider
    {
        public ControllerFactoryProvider(Microsoft.AspNetCore.Mvc.Controllers.IControllerActivatorProvider activatorProvider, Microsoft.AspNetCore.Mvc.Controllers.IControllerFactory controllerFactory, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Controllers.IControllerPropertyActivator> propertyActivators) { }
        public System.Func<Microsoft.AspNetCore.Mvc.ControllerContext, object> CreateControllerFactory(Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor descriptor) { throw null; }
        public System.Action<Microsoft.AspNetCore.Mvc.ControllerContext, object> CreateControllerReleaser(Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor descriptor) { throw null; }
    }
    internal partial class DefaultControllerActivator : Microsoft.AspNetCore.Mvc.Controllers.IControllerActivator
    {
        public DefaultControllerActivator(Microsoft.AspNetCore.Mvc.Infrastructure.ITypeActivatorCache typeActivatorCache) { }
        public object Create(Microsoft.AspNetCore.Mvc.ControllerContext controllerContext) { throw null; }
        public void Release(Microsoft.AspNetCore.Mvc.ControllerContext context, object controller) { }
    }
    internal partial class DefaultControllerFactory : Microsoft.AspNetCore.Mvc.Controllers.IControllerFactory
    {
        public DefaultControllerFactory(Microsoft.AspNetCore.Mvc.Controllers.IControllerActivator controllerActivator, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Controllers.IControllerPropertyActivator> propertyActivators) { }
        public object CreateController(Microsoft.AspNetCore.Mvc.ControllerContext context) { throw null; }
        public void ReleaseController(Microsoft.AspNetCore.Mvc.ControllerContext context, object controller) { }
    }
    internal partial class DefaultControllerPropertyActivator : Microsoft.AspNetCore.Mvc.Controllers.IControllerPropertyActivator
    {
        public DefaultControllerPropertyActivator() { }
        public void Activate(Microsoft.AspNetCore.Mvc.ControllerContext context, object controller) { }
        public System.Action<Microsoft.AspNetCore.Mvc.ControllerContext, object> GetActivatorDelegate(Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor actionDescriptor) { throw null; }
    }
    internal partial interface IControllerPropertyActivator
    {
        void Activate(Microsoft.AspNetCore.Mvc.ControllerContext context, object controller);
        System.Action<Microsoft.AspNetCore.Mvc.ControllerContext, object> GetActivatorDelegate(Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor actionDescriptor);
    }
}
namespace Microsoft.AspNetCore.Mvc.Core
{
    internal static partial class Resources
    {
        internal static string AcceptHeaderParser_ParseAcceptHeader_InvalidValues { get { throw null; } }
        internal static string ActionDescriptorMustBeBasedOnControllerAction { get { throw null; } }
        internal static string ActionExecutor_UnexpectedTaskInstance { get { throw null; } }
        internal static string ActionExecutor_WrappedTaskInstance { get { throw null; } }
        internal static string ActionInvokerFactory_CouldNotCreateInvoker { get { throw null; } }
        internal static string ActionResult_ActionReturnValueCannotBeNull { get { throw null; } }
        internal static string ApiController_AttributeRouteRequired { get { throw null; } }
        internal static string ApiController_MultipleBodyParametersFound { get { throw null; } }
        internal static string ApiConventionMethod_AmbiguousMethodName { get { throw null; } }
        internal static string ApiConventionMethod_NoMethodFound { get { throw null; } }
        internal static string ApiConventionMustBeStatic { get { throw null; } }
        internal static string ApiConventions_Title_400 { get { throw null; } }
        internal static string ApiConventions_Title_401 { get { throw null; } }
        internal static string ApiConventions_Title_403 { get { throw null; } }
        internal static string ApiConventions_Title_404 { get { throw null; } }
        internal static string ApiConventions_Title_406 { get { throw null; } }
        internal static string ApiConventions_Title_409 { get { throw null; } }
        internal static string ApiConventions_Title_415 { get { throw null; } }
        internal static string ApiConventions_Title_422 { get { throw null; } }
        internal static string ApiConventions_Title_500 { get { throw null; } }
        internal static string ApiConvention_UnsupportedAttributesOnConvention { get { throw null; } }
        internal static string ApiExplorer_UnsupportedAction { get { throw null; } }
        internal static string ApplicationAssembliesProvider_DuplicateRelatedAssembly { get { throw null; } }
        internal static string ApplicationAssembliesProvider_RelatedAssemblyCannotDefineAdditional { get { throw null; } }
        internal static string ApplicationPartFactory_InvalidFactoryType { get { throw null; } }
        internal static string ArgumentCannotBeNullOrEmpty { get { throw null; } }
        internal static string Argument_InvalidOffsetLength { get { throw null; } }
        internal static string AsyncActionFilter_InvalidShortCircuit { get { throw null; } }
        internal static string AsyncResourceFilter_InvalidShortCircuit { get { throw null; } }
        internal static string AsyncResultFilter_InvalidShortCircuit { get { throw null; } }
        internal static string AttributeRoute_AggregateErrorMessage { get { throw null; } }
        internal static string AttributeRoute_AggregateErrorMessage_ErrorNumber { get { throw null; } }
        internal static string AttributeRoute_CannotContainParameter { get { throw null; } }
        internal static string AttributeRoute_DuplicateNames { get { throw null; } }
        internal static string AttributeRoute_DuplicateNames_Item { get { throw null; } }
        internal static string AttributeRoute_IndividualErrorMessage { get { throw null; } }
        internal static string AttributeRoute_MixedAttributeAndConventionallyRoutedActions_ForMethod { get { throw null; } }
        internal static string AttributeRoute_MixedAttributeAndConventionallyRoutedActions_ForMethod_Item { get { throw null; } }
        internal static string AttributeRoute_NullTemplateRepresentation { get { throw null; } }
        internal static string AttributeRoute_TokenReplacement_EmptyTokenNotAllowed { get { throw null; } }
        internal static string AttributeRoute_TokenReplacement_ImbalancedSquareBrackets { get { throw null; } }
        internal static string AttributeRoute_TokenReplacement_InvalidSyntax { get { throw null; } }
        internal static string AttributeRoute_TokenReplacement_ReplacementValueNotFound { get { throw null; } }
        internal static string AttributeRoute_TokenReplacement_UnclosedToken { get { throw null; } }
        internal static string AttributeRoute_TokenReplacement_UnescapedBraceInToken { get { throw null; } }
        internal static string AuthorizeFilter_AuthorizationPolicyCannotBeCreated { get { throw null; } }
        internal static string BinderType_MustBeIModelBinder { get { throw null; } }
        internal static string BindingSource_CannotBeComposite { get { throw null; } }
        internal static string BindingSource_CannotBeGreedy { get { throw null; } }
        internal static string CacheProfileNotFound { get { throw null; } }
        internal static string CandidateResolver_DifferentCasedReference { get { throw null; } }
        internal static string Common_PropertyNotFound { get { throw null; } }
        internal static string ComplexTypeModelBinder_NoParameterlessConstructor_ForParameter { get { throw null; } }
        internal static string ComplexTypeModelBinder_NoParameterlessConstructor_ForProperty { get { throw null; } }
        internal static string ComplexTypeModelBinder_NoParameterlessConstructor_ForType { get { throw null; } }
        internal static string CouldNotCreateIModelBinder { get { throw null; } }
        internal static System.Globalization.CultureInfo Culture { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        internal static string DefaultActionSelector_AmbiguousActions { get { throw null; } }
        internal static string FileResult_InvalidPath { get { throw null; } }
        internal static string FileResult_PathNotRooted { get { throw null; } }
        internal static string FilterFactoryAttribute_TypeMustImplementIFilter { get { throw null; } }
        internal static string FormatFormatterMappings_GetMediaTypeMappingForFormat_InvalidFormat { get { throw null; } }
        internal static string FormatterMappings_NotValidMediaType { get { throw null; } }
        internal static string Formatter_NoMediaTypes { get { throw null; } }
        internal static string Format_NotValid { get { throw null; } }
        internal static string FormCollectionModelBinder_CannotBindToFormCollection { get { throw null; } }
        internal static string HtmlGeneration_NonPropertyValueMustBeNumber { get { throw null; } }
        internal static string HtmlGeneration_ValueIsInvalid { get { throw null; } }
        internal static string HtmlGeneration_ValueMustBeNumber { get { throw null; } }
        internal static string InputFormatterNoEncoding { get { throw null; } }
        internal static string InputFormattersAreRequired { get { throw null; } }
        internal static string InvalidTypeTForActionResultOfT { get { throw null; } }
        internal static string Invalid_IncludePropertyExpression { get { throw null; } }
        internal static string JQueryFormValueProviderFactory_MissingClosingBracket { get { throw null; } }
        internal static string KeyValuePair_BothKeyAndValueMustBePresent { get { throw null; } }
        internal static string MatchAllContentTypeIsNotAllowed { get { throw null; } }
        internal static string MiddewareFilter_ConfigureMethodOverload { get { throw null; } }
        internal static string MiddewareFilter_NoConfigureMethod { get { throw null; } }
        internal static string MiddlewareFilterBuilder_NoMiddlewareFeature { get { throw null; } }
        internal static string MiddlewareFilterBuilder_NullApplicationBuilder { get { throw null; } }
        internal static string MiddlewareFilterConfigurationProvider_CreateConfigureDelegate_CannotCreateType { get { throw null; } }
        internal static string MiddlewareFilter_InvalidConfigureReturnType { get { throw null; } }
        internal static string MiddlewareFilter_ServiceResolutionFail { get { throw null; } }
        internal static string ModelBinderProvidersAreRequired { get { throw null; } }
        internal static string ModelBinderUtil_ModelCannotBeNull { get { throw null; } }
        internal static string ModelBinderUtil_ModelInstanceIsWrong { get { throw null; } }
        internal static string ModelBinderUtil_ModelMetadataCannotBeNull { get { throw null; } }
        internal static string ModelBinding_ExceededMaxModelBindingCollectionSize { get { throw null; } }
        internal static string ModelBinding_ExceededMaxModelBindingRecursionDepth { get { throw null; } }
        internal static string ModelBinding_MissingBindRequiredMember { get { throw null; } }
        internal static string ModelBinding_MissingRequestBodyRequiredMember { get { throw null; } }
        internal static string ModelBinding_NullValueNotValid { get { throw null; } }
        internal static string ModelState_AttemptedValueIsInvalid { get { throw null; } }
        internal static string ModelState_NonPropertyAttemptedValueIsInvalid { get { throw null; } }
        internal static string ModelState_NonPropertyUnknownValueIsInvalid { get { throw null; } }
        internal static string ModelState_UnknownValueIsInvalid { get { throw null; } }
        internal static string ModelType_WrongType { get { throw null; } }
        internal static string NoRoutesMatched { get { throw null; } }
        internal static string NoRoutesMatchedForPage { get { throw null; } }
        internal static string ObjectResultExecutor_MaxEnumerationExceeded { get { throw null; } }
        internal static string ObjectResult_MatchAllContentType { get { throw null; } }
        internal static string OutputFormatterNoMediaType { get { throw null; } }
        internal static string OutputFormattersAreRequired { get { throw null; } }
        internal static string PropertyOfTypeCannotBeNull { get { throw null; } }
        internal static string Property_MustBeInstanceOfType { get { throw null; } }
        internal static string ReferenceToNewtonsoftJsonRequired { get { throw null; } }
        internal static string RelatedAssemblyAttribute_AssemblyCannotReferenceSelf { get { throw null; } }
        internal static string RelatedAssemblyAttribute_CouldNotBeFound { get { throw null; } }
        internal static System.Resources.ResourceManager ResourceManager { get { throw null; } }
        internal static string ResponseCache_SpecifyDuration { get { throw null; } }
        internal static string SerializableError_DefaultError { get { throw null; } }
        internal static string TextInputFormatter_SupportedEncodingsMustNotBeEmpty { get { throw null; } }
        internal static string TextOutputFormatter_SupportedEncodingsMustNotBeEmpty { get { throw null; } }
        internal static string TextOutputFormatter_WriteResponseBodyAsyncNotSupported { get { throw null; } }
        internal static string TypeMethodMustReturnNotNullValue { get { throw null; } }
        internal static string TypeMustDeriveFromType { get { throw null; } }
        internal static string UnableToFindServices { get { throw null; } }
        internal static string UnexpectedJsonEnd { get { throw null; } }
        internal static string UnsupportedContentType { get { throw null; } }
        internal static string UrlHelper_RelativePagePathIsNotSupported { get { throw null; } }
        internal static string UrlNotLocal { get { throw null; } }
        internal static string ValidationProblemDescription_Title { get { throw null; } }
        internal static string ValidationVisitor_ExceededMaxDepth { get { throw null; } }
        internal static string ValidationVisitor_ExceededMaxDepthFix { get { throw null; } }
        internal static string ValidationVisitor_ExceededMaxPropertyDepth { get { throw null; } }
        internal static string ValueInterfaceAbstractOrOpenGenericTypesCannotBeActivated { get { throw null; } }
        internal static string ValueProviderResult_NoConverterExists { get { throw null; } }
        internal static string VaryByQueryKeys_Requires_ResponseCachingMiddleware { get { throw null; } }
        internal static string VirtualFileResultExecutor_NoFileProviderConfigured { get { throw null; } }
        internal static string FormatAcceptHeaderParser_ParseAcceptHeader_InvalidValues(object p0) { throw null; }
        internal static string FormatActionDescriptorMustBeBasedOnControllerAction(object p0) { throw null; }
        internal static string FormatActionExecutor_UnexpectedTaskInstance(object p0, object p1) { throw null; }
        internal static string FormatActionExecutor_WrappedTaskInstance(object p0, object p1, object p2) { throw null; }
        internal static string FormatActionInvokerFactory_CouldNotCreateInvoker(object p0) { throw null; }
        internal static string FormatActionResult_ActionReturnValueCannotBeNull(object p0) { throw null; }
        internal static string FormatApiController_AttributeRouteRequired(object p0, object p1) { throw null; }
        internal static string FormatApiController_MultipleBodyParametersFound(object p0, object p1, object p2, object p3) { throw null; }
        internal static string FormatApiConventionMethod_AmbiguousMethodName(object p0, object p1) { throw null; }
        internal static string FormatApiConventionMethod_NoMethodFound(object p0, object p1) { throw null; }
        internal static string FormatApiConventionMustBeStatic(object p0) { throw null; }
        internal static string FormatApiConvention_UnsupportedAttributesOnConvention(object p0, object p1, object p2) { throw null; }
        internal static string FormatApiExplorer_UnsupportedAction(object p0) { throw null; }
        internal static string FormatApplicationAssembliesProvider_DuplicateRelatedAssembly(object p0) { throw null; }
        internal static string FormatApplicationAssembliesProvider_RelatedAssemblyCannotDefineAdditional(object p0, object p1) { throw null; }
        internal static string FormatApplicationPartFactory_InvalidFactoryType(object p0, object p1, object p2) { throw null; }
        internal static string FormatArgument_InvalidOffsetLength(object p0, object p1) { throw null; }
        internal static string FormatAsyncActionFilter_InvalidShortCircuit(object p0, object p1, object p2, object p3) { throw null; }
        internal static string FormatAsyncResourceFilter_InvalidShortCircuit(object p0, object p1, object p2, object p3) { throw null; }
        internal static string FormatAsyncResultFilter_InvalidShortCircuit(object p0, object p1, object p2, object p3) { throw null; }
        internal static string FormatAttributeRoute_AggregateErrorMessage(object p0, object p1) { throw null; }
        internal static string FormatAttributeRoute_AggregateErrorMessage_ErrorNumber(object p0, object p1, object p2) { throw null; }
        internal static string FormatAttributeRoute_CannotContainParameter(object p0, object p1, object p2) { throw null; }
        internal static string FormatAttributeRoute_DuplicateNames(object p0, object p1, object p2) { throw null; }
        internal static string FormatAttributeRoute_DuplicateNames_Item(object p0, object p1) { throw null; }
        internal static string FormatAttributeRoute_IndividualErrorMessage(object p0, object p1, object p2) { throw null; }
        internal static string FormatAttributeRoute_MixedAttributeAndConventionallyRoutedActions_ForMethod(object p0, object p1, object p2) { throw null; }
        internal static string FormatAttributeRoute_MixedAttributeAndConventionallyRoutedActions_ForMethod_Item(object p0, object p1, object p2) { throw null; }
        internal static string FormatAttributeRoute_TokenReplacement_InvalidSyntax(object p0, object p1) { throw null; }
        internal static string FormatAttributeRoute_TokenReplacement_ReplacementValueNotFound(object p0, object p1, object p2) { throw null; }
        internal static string FormatAuthorizeFilter_AuthorizationPolicyCannotBeCreated(object p0, object p1) { throw null; }
        internal static string FormatBinderType_MustBeIModelBinder(object p0, object p1) { throw null; }
        internal static string FormatBindingSource_CannotBeComposite(object p0, object p1) { throw null; }
        internal static string FormatBindingSource_CannotBeGreedy(object p0, object p1) { throw null; }
        internal static string FormatCacheProfileNotFound(object p0) { throw null; }
        internal static string FormatCandidateResolver_DifferentCasedReference(object p0) { throw null; }
        internal static string FormatCommon_PropertyNotFound(object p0, object p1) { throw null; }
        internal static string FormatComplexTypeModelBinder_NoParameterlessConstructor_ForParameter(object p0, object p1) { throw null; }
        internal static string FormatComplexTypeModelBinder_NoParameterlessConstructor_ForProperty(object p0, object p1, object p2) { throw null; }
        internal static string FormatComplexTypeModelBinder_NoParameterlessConstructor_ForType(object p0) { throw null; }
        internal static string FormatCouldNotCreateIModelBinder(object p0) { throw null; }
        internal static string FormatDefaultActionSelector_AmbiguousActions(object p0, object p1) { throw null; }
        internal static string FormatFileResult_InvalidPath(object p0) { throw null; }
        internal static string FormatFileResult_PathNotRooted(object p0) { throw null; }
        internal static string FormatFilterFactoryAttribute_TypeMustImplementIFilter(object p0, object p1) { throw null; }
        internal static string FormatFormatFormatterMappings_GetMediaTypeMappingForFormat_InvalidFormat(object p0) { throw null; }
        internal static string FormatFormatterMappings_NotValidMediaType(object p0) { throw null; }
        internal static string FormatFormatter_NoMediaTypes(object p0, object p1) { throw null; }
        internal static string FormatFormat_NotValid(object p0) { throw null; }
        internal static string FormatFormCollectionModelBinder_CannotBindToFormCollection(object p0, object p1, object p2) { throw null; }
        internal static string FormatHtmlGeneration_ValueIsInvalid(object p0) { throw null; }
        internal static string FormatHtmlGeneration_ValueMustBeNumber(object p0) { throw null; }
        internal static string FormatInputFormatterNoEncoding(object p0) { throw null; }
        internal static string FormatInputFormattersAreRequired(object p0, object p1, object p2) { throw null; }
        internal static string FormatInvalidTypeTForActionResultOfT(object p0, object p1) { throw null; }
        internal static string FormatInvalid_IncludePropertyExpression(object p0) { throw null; }
        internal static string FormatJQueryFormValueProviderFactory_MissingClosingBracket(object p0) { throw null; }
        internal static string FormatMatchAllContentTypeIsNotAllowed(object p0) { throw null; }
        internal static string FormatMiddewareFilter_ConfigureMethodOverload(object p0) { throw null; }
        internal static string FormatMiddewareFilter_NoConfigureMethod(object p0, object p1) { throw null; }
        internal static string FormatMiddlewareFilterBuilder_NoMiddlewareFeature(object p0) { throw null; }
        internal static string FormatMiddlewareFilterBuilder_NullApplicationBuilder(object p0) { throw null; }
        internal static string FormatMiddlewareFilterConfigurationProvider_CreateConfigureDelegate_CannotCreateType(object p0, object p1) { throw null; }
        internal static string FormatMiddlewareFilter_InvalidConfigureReturnType(object p0, object p1, object p2) { throw null; }
        internal static string FormatMiddlewareFilter_ServiceResolutionFail(object p0, object p1, object p2, object p3) { throw null; }
        internal static string FormatModelBinderProvidersAreRequired(object p0, object p1, object p2) { throw null; }
        internal static string FormatModelBinderUtil_ModelCannotBeNull(object p0) { throw null; }
        internal static string FormatModelBinderUtil_ModelInstanceIsWrong(object p0, object p1) { throw null; }
        internal static string FormatModelBinding_ExceededMaxModelBindingCollectionSize(object p0, object p1, object p2, object p3, object p4) { throw null; }
        internal static string FormatModelBinding_ExceededMaxModelBindingRecursionDepth(object p0, object p1, object p2, object p3) { throw null; }
        internal static string FormatModelBinding_MissingBindRequiredMember(object p0) { throw null; }
        internal static string FormatModelBinding_NullValueNotValid(object p0) { throw null; }
        internal static string FormatModelState_AttemptedValueIsInvalid(object p0, object p1) { throw null; }
        internal static string FormatModelState_NonPropertyAttemptedValueIsInvalid(object p0) { throw null; }
        internal static string FormatModelState_UnknownValueIsInvalid(object p0) { throw null; }
        internal static string FormatModelType_WrongType(object p0, object p1) { throw null; }
        internal static string FormatNoRoutesMatchedForPage(object p0) { throw null; }
        internal static string FormatObjectResultExecutor_MaxEnumerationExceeded(object p0, object p1) { throw null; }
        internal static string FormatObjectResult_MatchAllContentType(object p0, object p1) { throw null; }
        internal static string FormatOutputFormatterNoMediaType(object p0) { throw null; }
        internal static string FormatOutputFormattersAreRequired(object p0, object p1, object p2) { throw null; }
        internal static string FormatPropertyOfTypeCannotBeNull(object p0, object p1) { throw null; }
        internal static string FormatProperty_MustBeInstanceOfType(object p0, object p1, object p2) { throw null; }
        internal static string FormatReferenceToNewtonsoftJsonRequired(object p0, object p1, object p2, object p3, object p4) { throw null; }
        internal static string FormatRelatedAssemblyAttribute_AssemblyCannotReferenceSelf(object p0, object p1) { throw null; }
        internal static string FormatRelatedAssemblyAttribute_CouldNotBeFound(object p0, object p1, object p2) { throw null; }
        internal static string FormatResponseCache_SpecifyDuration(object p0, object p1) { throw null; }
        internal static string FormatTextInputFormatter_SupportedEncodingsMustNotBeEmpty(object p0) { throw null; }
        internal static string FormatTextOutputFormatter_SupportedEncodingsMustNotBeEmpty(object p0) { throw null; }
        internal static string FormatTextOutputFormatter_WriteResponseBodyAsyncNotSupported(object p0, object p1, object p2) { throw null; }
        internal static string FormatTypeMethodMustReturnNotNullValue(object p0, object p1) { throw null; }
        internal static string FormatTypeMustDeriveFromType(object p0, object p1) { throw null; }
        internal static string FormatUnableToFindServices(object p0, object p1, object p2) { throw null; }
        internal static string FormatUnsupportedContentType(object p0) { throw null; }
        internal static string FormatUrlHelper_RelativePagePathIsNotSupported(object p0, object p1, object p2) { throw null; }
        internal static string FormatValidationVisitor_ExceededMaxDepth(object p0, object p1, object p2) { throw null; }
        internal static string FormatValidationVisitor_ExceededMaxDepthFix(object p0, object p1) { throw null; }
        internal static string FormatValidationVisitor_ExceededMaxPropertyDepth(object p0, object p1, object p2, object p3) { throw null; }
        internal static string FormatValueInterfaceAbstractOrOpenGenericTypesCannotBeActivated(object p0, object p1) { throw null; }
        internal static string FormatValueProviderResult_NoConverterExists(object p0, object p1) { throw null; }
        internal static string FormatVaryByQueryKeys_Requires_ResponseCachingMiddleware(object p0) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]internal static string GetResourceString(string resourceKey, string defaultValue = null) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.Filters
{
    internal partial class ControllerActionFilter : Microsoft.AspNetCore.Mvc.Filters.IAsyncActionFilter, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter
    {
        public ControllerActionFilter() { }
        public int Order { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Threading.Tasks.Task OnActionExecutionAsync(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context, Microsoft.AspNetCore.Mvc.Filters.ActionExecutionDelegate next) { throw null; }
    }
    internal partial class ControllerResultFilter : Microsoft.AspNetCore.Mvc.Filters.IAsyncResultFilter, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter
    {
        public ControllerResultFilter() { }
        public int Order { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Threading.Tasks.Task OnResultExecutionAsync(Microsoft.AspNetCore.Mvc.Filters.ResultExecutingContext context, Microsoft.AspNetCore.Mvc.Filters.ResultExecutionDelegate next) { throw null; }
    }
    internal partial class DefaultFilterProvider : Microsoft.AspNetCore.Mvc.Filters.IFilterProvider
    {
        public DefaultFilterProvider() { }
        public int Order { get { throw null; } }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.Filters.FilterProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.Filters.FilterProviderContext context) { }
        public void ProvideFilter(Microsoft.AspNetCore.Mvc.Filters.FilterProviderContext context, Microsoft.AspNetCore.Mvc.Filters.FilterItem filterItem) { }
    }
    internal partial class DisableRequestSizeLimitFilter : Microsoft.AspNetCore.Mvc.Filters.IAuthorizationFilter, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.IRequestSizePolicy
    {
        public DisableRequestSizeLimitFilter(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public void OnAuthorization(Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext context) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal partial struct FilterCursor
    {
        private object _dummy;
        private int _dummyPrimitive;
        public FilterCursor(Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata[] filters) { throw null; }
        public Microsoft.AspNetCore.Mvc.Filters.FilterCursorItem<TFilter, TFilterAsync> GetNextFilter<TFilter, TFilterAsync>() where TFilter : class where TFilterAsync : class { throw null; }
        public void Reset() { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal readonly partial struct FilterCursorItem<TFilter, TFilterAsync>
    {
        [System.Diagnostics.DebuggerBrowsableAttribute(System.Diagnostics.DebuggerBrowsableState.Never)]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        private readonly TFilter _Filter_k__BackingField;
        [System.Diagnostics.DebuggerBrowsableAttribute(System.Diagnostics.DebuggerBrowsableState.Never)]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        private readonly TFilterAsync _FilterAsync_k__BackingField;
        private readonly int _dummyPrimitive;
        public FilterCursorItem(TFilter filter, TFilterAsync filterAsync) { throw null; }
        public TFilter Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TFilterAsync FilterAsync { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    internal partial class FilterDescriptorOrderComparer : System.Collections.Generic.IComparer<Microsoft.AspNetCore.Mvc.Filters.FilterDescriptor>
    {
        public FilterDescriptorOrderComparer() { }
        public static Microsoft.AspNetCore.Mvc.Filters.FilterDescriptorOrderComparer Comparer { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public int Compare(Microsoft.AspNetCore.Mvc.Filters.FilterDescriptor x, Microsoft.AspNetCore.Mvc.Filters.FilterDescriptor y) { throw null; }
    }
    internal static partial class FilterFactory
    {
        public static Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata[] CreateUncachedFilters(Microsoft.AspNetCore.Mvc.Filters.IFilterProvider[] filterProviders, Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.Filters.FilterItem[] cachedFilterItems) { throw null; }
        public static Microsoft.AspNetCore.Mvc.Filters.FilterFactoryResult GetAllFilters(Microsoft.AspNetCore.Mvc.Filters.IFilterProvider[] filterProviders, Microsoft.AspNetCore.Mvc.ActionContext actionContext) { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal readonly partial struct FilterFactoryResult
    {
        private readonly object _dummy;
        public FilterFactoryResult(Microsoft.AspNetCore.Mvc.Filters.FilterItem[] cacheableFilters, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata[] filters) { throw null; }
        public Microsoft.AspNetCore.Mvc.Filters.FilterItem[] CacheableFilters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata[] Filters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    internal partial interface IMiddlewareFilterFeature
    {
        Microsoft.AspNetCore.Mvc.Filters.ResourceExecutingContext ResourceExecutingContext { get; }
        Microsoft.AspNetCore.Mvc.Filters.ResourceExecutionDelegate ResourceExecutionDelegate { get; }
    }
    internal partial interface IResponseCacheFilter : Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata
    {
    }
    internal partial class MiddlewareFilter : Microsoft.AspNetCore.Mvc.Filters.IAsyncResourceFilter, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata
    {
        public MiddlewareFilter(Microsoft.AspNetCore.Http.RequestDelegate middlewarePipeline) { }
        public System.Threading.Tasks.Task OnResourceExecutionAsync(Microsoft.AspNetCore.Mvc.Filters.ResourceExecutingContext context, Microsoft.AspNetCore.Mvc.Filters.ResourceExecutionDelegate next) { throw null; }
    }
    internal partial class MiddlewareFilterBuilder
    {
        public MiddlewareFilterBuilder(Microsoft.AspNetCore.Mvc.Filters.MiddlewareFilterConfigurationProvider configurationProvider) { }
        public Microsoft.AspNetCore.Builder.IApplicationBuilder ApplicationBuilder { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Http.RequestDelegate GetPipeline(System.Type configurationType) { throw null; }
    }
    internal partial class MiddlewareFilterBuilderStartupFilter : Microsoft.AspNetCore.Hosting.IStartupFilter
    {
        public MiddlewareFilterBuilderStartupFilter() { }
        public System.Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> Configure(System.Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> next) { throw null; }
    }
    internal partial class MiddlewareFilterConfigurationProvider
    {
        public MiddlewareFilterConfigurationProvider() { }
        public System.Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> CreateConfigureDelegate(System.Type configurationType) { throw null; }
    }
    internal partial class MiddlewareFilterFeature : Microsoft.AspNetCore.Mvc.Filters.IMiddlewareFilterFeature
    {
        public MiddlewareFilterFeature() { }
        public Microsoft.AspNetCore.Mvc.Filters.ResourceExecutingContext ResourceExecutingContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Mvc.Filters.ResourceExecutionDelegate ResourceExecutionDelegate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    internal partial class RequestFormLimitsFilter : Microsoft.AspNetCore.Mvc.Filters.IAuthorizationFilter, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.IRequestFormLimitsPolicy
    {
        public RequestFormLimitsFilter(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public Microsoft.AspNetCore.Http.Features.FormOptions FormOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public void OnAuthorization(Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext context) { }
    }
    internal partial class RequestSizeLimitFilter : Microsoft.AspNetCore.Mvc.Filters.IAuthorizationFilter, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.IRequestSizePolicy
    {
        public RequestSizeLimitFilter(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public long Bytes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public void OnAuthorization(Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext context) { }
    }
    internal partial class ResponseCacheFilter : Microsoft.AspNetCore.Mvc.Filters.IActionFilter, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IResponseCacheFilter
    {
        public ResponseCacheFilter(Microsoft.AspNetCore.Mvc.CacheProfile cacheProfile, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public int Duration { get { throw null; } set { } }
        public Microsoft.AspNetCore.Mvc.ResponseCacheLocation Location { get { throw null; } set { } }
        public bool NoStore { get { throw null; } set { } }
        public string VaryByHeader { get { throw null; } set { } }
        public string[] VaryByQueryKeys { get { throw null; } set { } }
        public void OnActionExecuted(Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext context) { }
        public void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context) { }
    }
    internal partial class ResponseCacheFilterExecutor
    {
        public ResponseCacheFilterExecutor(Microsoft.AspNetCore.Mvc.CacheProfile cacheProfile) { }
        public int Duration { get { throw null; } set { } }
        public Microsoft.AspNetCore.Mvc.ResponseCacheLocation Location { get { throw null; } set { } }
        public bool NoStore { get { throw null; } set { } }
        public string VaryByHeader { get { throw null; } set { } }
        public string[] VaryByQueryKeys { get { throw null; } set { } }
        public void Execute(Microsoft.AspNetCore.Mvc.Filters.FilterContext context) { }
    }
}
namespace Microsoft.AspNetCore.Mvc.Formatters
{
    internal static partial class AcceptHeaderParser
    {
        public static System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Formatters.MediaTypeSegmentWithQuality> ParseAcceptHeader(System.Collections.Generic.IList<string> acceptHeaders) { throw null; }
        public static void ParseAcceptHeader(System.Collections.Generic.IList<string> acceptHeaders, System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Formatters.MediaTypeSegmentWithQuality> parsedValues) { }
    }
    internal enum HttpParseResult
    {
        Parsed = 0,
        NotParsed = 1,
        InvalidFormat = 2,
    }
    internal static partial class HttpTokenParsingRules
    {
        internal const char CR = '\r';
        internal static readonly System.Text.Encoding DefaultHttpEncoding;
        internal const char LF = '\n';
        internal const int MaxInt32Digits = 10;
        internal const int MaxInt64Digits = 19;
        internal const char SP = ' ';
        internal const char Tab = '\t';
        internal static Microsoft.AspNetCore.Mvc.Formatters.HttpParseResult GetQuotedPairLength(string input, int startIndex, out int length) { throw null; }
        internal static Microsoft.AspNetCore.Mvc.Formatters.HttpParseResult GetQuotedStringLength(string input, int startIndex, out int length) { throw null; }
        internal static int GetTokenLength(string input, int startIndex) { throw null; }
        internal static int GetWhitespaceLength(string input, int startIndex) { throw null; }
        internal static bool IsTokenChar(char character) { throw null; }
    }
    internal partial interface IFormatFilter : Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata
    {
        string GetFormat(Microsoft.AspNetCore.Mvc.ActionContext context);
    }
    internal static partial class MediaTypeHeaderValues
    {
        public static readonly Microsoft.Net.Http.Headers.MediaTypeHeaderValue ApplicationAnyJsonSyntax;
        public static readonly Microsoft.Net.Http.Headers.MediaTypeHeaderValue ApplicationAnyXmlSyntax;
        public static readonly Microsoft.Net.Http.Headers.MediaTypeHeaderValue ApplicationJson;
        public static readonly Microsoft.Net.Http.Headers.MediaTypeHeaderValue ApplicationXml;
        public static readonly Microsoft.Net.Http.Headers.MediaTypeHeaderValue TextJson;
        public static readonly Microsoft.Net.Http.Headers.MediaTypeHeaderValue TextXml;
    }
    internal static partial class ResponseContentTypeHelper
    {
        public static void ResolveContentTypeAndEncoding(string actionResultContentType, string httpResponseContentType, string defaultContentType, out string resolvedContentType, out System.Text.Encoding resolvedContentTypeEncoding) { throw null; }
    }
    public partial class SystemTextJsonOutputFormatter : Microsoft.AspNetCore.Mvc.Formatters.TextOutputFormatter
    {
        internal static Microsoft.AspNetCore.Mvc.Formatters.SystemTextJsonOutputFormatter CreateFormatter(Microsoft.AspNetCore.Mvc.JsonOptions jsonOptions) { throw null; }
    }
    public abstract partial class TextOutputFormatter : Microsoft.AspNetCore.Mvc.Formatters.OutputFormatter
    {
        internal static System.Collections.Generic.IList<Microsoft.Net.Http.Headers.StringWithQualityHeaderValue> GetAcceptCharsetHeaderValues(Microsoft.AspNetCore.Mvc.Formatters.OutputFormatterWriteContext context) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.Formatters.Json
{
    internal sealed partial class TranscodingReadStream : System.IO.Stream
    {
        internal const int MaxByteBufferSize = 4096;
        internal const int MaxCharBufferSize = 12288;
        public TranscodingReadStream(System.IO.Stream input, System.Text.Encoding sourceEncoding) { }
        internal int ByteBufferCount { get { throw null; } }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        internal int CharBufferCount { get { throw null; } }
        public override long Length { get { throw null; } }
        internal int OverflowCount { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        protected override void Dispose(bool disposing) { }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
    }
    internal sealed partial class TranscodingWriteStream : System.IO.Stream
    {
        internal const int MaxByteBufferSize = 16384;
        internal const int MaxCharBufferSize = 4096;
        public TranscodingWriteStream(System.IO.Stream stream, System.Text.Encoding targetEncoding) { }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public override long Length { get { throw null; } }
        public override long Position { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        protected override void Dispose(bool disposing) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task FinalWriteAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public override void Flush() { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public override int Read(byte[] buffer, int offset, int count) { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    public partial class ActionContextAccessor : Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor
    {
        internal static readonly Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor Null;
    }
    internal partial class ActionInvokerFactory : Microsoft.AspNetCore.Mvc.Infrastructure.IActionInvokerFactory
    {
        public ActionInvokerFactory(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Abstractions.IActionInvokerProvider> actionInvokerProviders) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.IActionInvoker CreateInvoker(Microsoft.AspNetCore.Mvc.ActionContext actionContext) { throw null; }
    }
    internal abstract partial class ActionMethodExecutor
    {
        protected ActionMethodExecutor() { }
        protected abstract bool CanExecute(Microsoft.Extensions.Internal.ObjectMethodExecutor executor);
        public abstract System.Threading.Tasks.ValueTask<Microsoft.AspNetCore.Mvc.IActionResult> Execute(Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper mapper, Microsoft.Extensions.Internal.ObjectMethodExecutor executor, object controller, object[] arguments);
        public static Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor GetExecutor(Microsoft.Extensions.Internal.ObjectMethodExecutor executor) { throw null; }
    }
    internal partial class ActionResultTypeMapper : Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper
    {
        public ActionResultTypeMapper() { }
        public Microsoft.AspNetCore.Mvc.IActionResult Convert(object value, System.Type returnType) { throw null; }
        public System.Type GetResultDataType(System.Type returnType) { throw null; }
    }
    internal partial class ActionSelectionTable<TItem>
    {
        public int Version { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public static Microsoft.AspNetCore.Mvc.Infrastructure.ActionSelectionTable<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor> Create(Microsoft.AspNetCore.Mvc.Infrastructure.ActionDescriptorCollection actions) { throw null; }
        public static Microsoft.AspNetCore.Mvc.Infrastructure.ActionSelectionTable<Microsoft.AspNetCore.Http.Endpoint> Create(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Http.Endpoint> endpoints) { throw null; }
        public System.Collections.Generic.IReadOnlyList<TItem> Select(Microsoft.AspNetCore.Routing.RouteValueDictionary values) { throw null; }
    }
    internal partial class ActionSelector : Microsoft.AspNetCore.Mvc.Infrastructure.IActionSelector
    {
        public ActionSelector(Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider actionDescriptorCollectionProvider, Microsoft.AspNetCore.Mvc.ActionConstraints.ActionConstraintCache actionConstraintCache, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor SelectBestCandidate(Microsoft.AspNetCore.Routing.RouteContext context, System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor> candidates) { throw null; }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor> SelectCandidates(Microsoft.AspNetCore.Routing.RouteContext context) { throw null; }
    }
    internal sealed partial class AsyncEnumerableReader
    {
        public AsyncEnumerableReader(Microsoft.AspNetCore.Mvc.MvcOptions mvcOptions) { }
        public bool TryGetReader(System.Type type, out System.Func<object, System.Threading.Tasks.Task<System.Collections.ICollection>> reader) { throw null; }
    }
    internal partial class ClientErrorResultFilter : Microsoft.AspNetCore.Mvc.Filters.IAlwaysRunResultFilter, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter, Microsoft.AspNetCore.Mvc.Filters.IResultFilter
    {
        internal const int FilterOrder = -2000;
        public ClientErrorResultFilter(Microsoft.AspNetCore.Mvc.Infrastructure.IClientErrorFactory clientErrorFactory, Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Mvc.Infrastructure.ClientErrorResultFilter> logger) { }
        public int Order { get { throw null; } }
        public void OnResultExecuted(Microsoft.AspNetCore.Mvc.Filters.ResultExecutedContext context) { }
        public void OnResultExecuting(Microsoft.AspNetCore.Mvc.Filters.ResultExecutingContext context) { }
    }
    internal sealed partial class ClientErrorResultFilterFactory : Microsoft.AspNetCore.Mvc.Filters.IFilterFactory, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter
    {
        public ClientErrorResultFilterFactory() { }
        public bool IsReusable { get { throw null; } }
        public int Order { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata CreateInstance(System.IServiceProvider serviceProvider) { throw null; }
    }
    internal partial class ControllerActionInvoker : Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker, Microsoft.AspNetCore.Mvc.Abstractions.IActionInvoker
    {
        internal ControllerActionInvoker(Microsoft.Extensions.Logging.ILogger logger, System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor actionContextAccessor, Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper mapper, Microsoft.AspNetCore.Mvc.ControllerContext controllerContext, Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvokerCacheEntry cacheEntry, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata[] filters) : base (default(System.Diagnostics.DiagnosticListener), default(Microsoft.Extensions.Logging.ILogger), default(Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor), default(Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper), default(Microsoft.AspNetCore.Mvc.ActionContext), default(Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata[]), default(System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.IValueProviderFactory>)) { }
        internal Microsoft.AspNetCore.Mvc.ControllerContext ControllerContext { get { throw null; } }
        protected override System.Threading.Tasks.Task InvokeInnerFilterAsync() { throw null; }
        protected override void ReleaseResources() { }
    }
    internal partial class ControllerActionInvokerCache
    {
        public ControllerActionInvokerCache(Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider collectionProvider, Microsoft.AspNetCore.Mvc.ModelBinding.ParameterBinder parameterBinder, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderFactory modelBinderFactory, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider modelMetadataProvider, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Filters.IFilterProvider> filterProviders, Microsoft.AspNetCore.Mvc.Controllers.IControllerFactoryProvider factoryProvider, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> mvcOptions) { }
        public (Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvokerCacheEntry cacheEntry, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata[] filters) GetCachedResult(Microsoft.AspNetCore.Mvc.ControllerContext controllerContext) { throw null; }
    }
    internal partial class ControllerActionInvokerCacheEntry
    {
        internal ControllerActionInvokerCacheEntry(Microsoft.AspNetCore.Mvc.Filters.FilterItem[] cachedFilters, System.Func<Microsoft.AspNetCore.Mvc.ControllerContext, object> controllerFactory, System.Action<Microsoft.AspNetCore.Mvc.ControllerContext, object> controllerReleaser, Microsoft.AspNetCore.Mvc.Controllers.ControllerBinderDelegate controllerBinderDelegate, Microsoft.Extensions.Internal.ObjectMethodExecutor objectMethodExecutor, Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor actionMethodExecutor) { }
        internal Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor ActionMethodExecutor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.FilterItem[] CachedFilters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Controllers.ControllerBinderDelegate ControllerBinderDelegate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Func<Microsoft.AspNetCore.Mvc.ControllerContext, object> ControllerFactory { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Action<Microsoft.AspNetCore.Mvc.ControllerContext, object> ControllerReleaser { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        internal Microsoft.Extensions.Internal.ObjectMethodExecutor ObjectMethodExecutor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    internal partial class ControllerActionInvokerProvider : Microsoft.AspNetCore.Mvc.Abstractions.IActionInvokerProvider
    {
        public ControllerActionInvokerProvider(Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvokerCache controllerActionInvokerCache, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> optionsAccessor, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper mapper) { }
        public ControllerActionInvokerProvider(Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvokerCache controllerActionInvokerCache, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> optionsAccessor, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper mapper, Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor actionContextAccessor) { }
        public int Order { get { throw null; } }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.Abstractions.ActionInvokerProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.Abstractions.ActionInvokerProviderContext context) { }
    }
    internal partial class CopyOnWriteList<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IList<T>, System.Collections.IEnumerable
    {
        public CopyOnWriteList(System.Collections.Generic.IReadOnlyList<T> source) { }
        public int Count { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public T this[int index] { get { throw null; } set { } }
        public void Add(T item) { }
        public void Clear() { }
        public bool Contains(T item) { throw null; }
        public void CopyTo(T[] array, int arrayIndex) { }
        public System.Collections.Generic.IEnumerator<T> GetEnumerator() { throw null; }
        public int IndexOf(T item) { throw null; }
        public void Insert(int index, T item) { }
        public bool Remove(T item) { throw null; }
        public void RemoveAt(int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    internal partial class DefaultActionDescriptorCollectionProvider : Microsoft.AspNetCore.Mvc.Infrastructure.ActionDescriptorCollectionProvider
    {
        public DefaultActionDescriptorCollectionProvider(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Abstractions.IActionDescriptorProvider> actionDescriptorProviders, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorChangeProvider> actionDescriptorChangeProviders) { }
        public override Microsoft.AspNetCore.Mvc.Infrastructure.ActionDescriptorCollection ActionDescriptors { get { throw null; } }
        public override Microsoft.Extensions.Primitives.IChangeToken GetChangeToken() { throw null; }
    }
    internal sealed partial class DefaultProblemDetailsFactory : Microsoft.AspNetCore.Mvc.Infrastructure.ProblemDetailsFactory
    {
        public DefaultProblemDetailsFactory(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions> options) { }
        public override Microsoft.AspNetCore.Mvc.ProblemDetails CreateProblemDetails(Microsoft.AspNetCore.Http.HttpContext httpContext, int? statusCode = default(int?), string title = null, string type = null, string detail = null, string instance = null) { throw null; }
        public override Microsoft.AspNetCore.Mvc.ValidationProblemDetails CreateValidationProblemDetails(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelStateDictionary, int? statusCode = default(int?), string title = null, string type = null, string detail = null, string instance = null) { throw null; }
    }
    public partial class FileResultExecutorBase
    {
        internal Microsoft.AspNetCore.Mvc.Infrastructure.FileResultExecutorBase.PreconditionState GetPreconditionState(Microsoft.AspNetCore.Http.Headers.RequestHeaders httpRequestHeaders, System.DateTimeOffset? lastModified = default(System.DateTimeOffset?), Microsoft.Net.Http.Headers.EntityTagHeaderValue etag = null) { throw null; }
        internal bool IfRangeValid(Microsoft.AspNetCore.Http.Headers.RequestHeaders httpRequestHeaders, System.DateTimeOffset? lastModified = default(System.DateTimeOffset?), Microsoft.Net.Http.Headers.EntityTagHeaderValue etag = null) { throw null; }
        internal enum PreconditionState
        {
            Unspecified = 0,
            NotModified = 1,
            ShouldProcess = 2,
            PreconditionFailed = 3,
        }
    }
    internal partial interface ITypeActivatorCache
    {
        TInstance CreateInstance<TInstance>(System.IServiceProvider serviceProvider, System.Type optionType);
    }
    internal partial class MemoryPoolHttpRequestStreamReaderFactory : Microsoft.AspNetCore.Mvc.Infrastructure.IHttpRequestStreamReaderFactory
    {
        public static readonly int DefaultBufferSize;
        public MemoryPoolHttpRequestStreamReaderFactory(System.Buffers.ArrayPool<byte> bytePool, System.Buffers.ArrayPool<char> charPool) { }
        public System.IO.TextReader CreateReader(System.IO.Stream stream, System.Text.Encoding encoding) { throw null; }
    }
    internal partial class MemoryPoolHttpResponseStreamWriterFactory : Microsoft.AspNetCore.Mvc.Infrastructure.IHttpResponseStreamWriterFactory
    {
        public static readonly int DefaultBufferSize;
        public MemoryPoolHttpResponseStreamWriterFactory(System.Buffers.ArrayPool<byte> bytePool, System.Buffers.ArrayPool<char> charPool) { }
        public System.IO.TextWriter CreateWriter(System.IO.Stream stream, System.Text.Encoding encoding) { throw null; }
    }
    public partial class ModelStateInvalidFilter : Microsoft.AspNetCore.Mvc.Filters.IActionFilter, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter
    {
        internal const int FilterOrder = -2000;
    }
    internal partial class ModelStateInvalidFilterFactory : Microsoft.AspNetCore.Mvc.Filters.IFilterFactory, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter
    {
        public ModelStateInvalidFilterFactory() { }
        public bool IsReusable { get { throw null; } }
        public int Order { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata CreateInstance(System.IServiceProvider serviceProvider) { throw null; }
    }
    internal partial class MvcOptionsConfigureCompatibilityOptions : Microsoft.AspNetCore.Mvc.Infrastructure.ConfigureCompatibilityOptions<Microsoft.AspNetCore.Mvc.MvcOptions>
    {
        public MvcOptionsConfigureCompatibilityOptions(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.Infrastructure.MvcCompatibilityOptions> compatibilityOptions) : base (default(Microsoft.Extensions.Logging.ILoggerFactory), default(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.Infrastructure.MvcCompatibilityOptions>)) { }
        protected override System.Collections.Generic.IReadOnlyDictionary<string, object> DefaultValues { get { throw null; } }
    }
    internal partial class NonDisposableStream : System.IO.Stream
    {
        public NonDisposableStream(System.IO.Stream innerStream) { }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanTimeout { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public override long Length { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        public override int ReadTimeout { get { throw null; } set { } }
        public override int WriteTimeout { get { throw null; } set { } }
        public override System.IAsyncResult BeginRead(byte[] buffer, int offset, int count, System.AsyncCallback callback, object state) { throw null; }
        public override System.IAsyncResult BeginWrite(byte[] buffer, int offset, int count, System.AsyncCallback callback, object state) { throw null; }
        public override void Close() { }
        public override System.Threading.Tasks.Task CopyToAsync(System.IO.Stream destination, int bufferSize, System.Threading.CancellationToken cancellationToken) { throw null; }
        protected override void Dispose(bool disposing) { }
        public override int EndRead(System.IAsyncResult asyncResult) { throw null; }
        public override void EndWrite(System.IAsyncResult asyncResult) { }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public override int Read(byte[] buffer, int offset, int count) { throw null; }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override int ReadByte() { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override void WriteByte(byte value) { }
    }
    internal partial class NullableCompatibilitySwitch<TValue> : Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch where TValue : struct
    {
        public NullableCompatibilitySwitch(string name) { }
        public bool IsValueSet { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        object Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch.Value { get { throw null; } set { } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TValue? Value { get { throw null; } set { } }
    }
    internal static partial class ParameterDefaultValues
    {
        public static object[] GetParameterDefaultValues(System.Reflection.MethodInfo methodInfo) { throw null; }
        public static bool TryGetDeclaredParameterDefaultValue(System.Reflection.ParameterInfo parameterInfo, out object defaultValue) { throw null; }
    }
    internal partial class ProblemDetailsClientErrorFactory : Microsoft.AspNetCore.Mvc.Infrastructure.IClientErrorFactory
    {
        public ProblemDetailsClientErrorFactory(Microsoft.AspNetCore.Mvc.Infrastructure.ProblemDetailsFactory problemDetailsFactory) { }
        public Microsoft.AspNetCore.Mvc.IActionResult GetClientError(Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.Infrastructure.IClientErrorActionResult clientError) { throw null; }
    }
    internal partial class ProblemDetailsJsonConverter : System.Text.Json.Serialization.JsonConverter<Microsoft.AspNetCore.Mvc.ProblemDetails>
    {
        public ProblemDetailsJsonConverter() { }
        public override Microsoft.AspNetCore.Mvc.ProblemDetails Read(ref System.Text.Json.Utf8JsonReader reader, System.Type typeToConvert, System.Text.Json.JsonSerializerOptions options) { throw null; }
        internal static void ReadValue(ref System.Text.Json.Utf8JsonReader reader, Microsoft.AspNetCore.Mvc.ProblemDetails value, System.Text.Json.JsonSerializerOptions options) { }
        internal static bool TryReadStringProperty(ref System.Text.Json.Utf8JsonReader reader, System.Text.Json.JsonEncodedText propertyName, out string value) { throw null; }
        public override void Write(System.Text.Json.Utf8JsonWriter writer, Microsoft.AspNetCore.Mvc.ProblemDetails value, System.Text.Json.JsonSerializerOptions options) { }
        internal static void WriteProblemDetails(System.Text.Json.Utf8JsonWriter writer, Microsoft.AspNetCore.Mvc.ProblemDetails value, System.Text.Json.JsonSerializerOptions options) { }
    }
#nullable enable
    internal abstract partial class ResourceInvoker
    {
        protected readonly Microsoft.AspNetCore.Mvc.ActionContext _actionContext;
        protected readonly Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor _actionContextAccessor;
        protected Microsoft.AspNetCore.Mvc.Filters.FilterCursor _cursor;
        protected readonly System.Diagnostics.DiagnosticListener _diagnosticListener;
        protected readonly Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata[] _filters;
        protected object? _instance;
        protected readonly Microsoft.Extensions.Logging.ILogger _logger;
        protected readonly Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper _mapper;
        protected Microsoft.AspNetCore.Mvc.IActionResult? _result;
        protected readonly System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.IValueProviderFactory> _valueProviderFactories;
        public ResourceInvoker(System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor actionContextAccessor, Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper mapper, Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata[] filters, System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.IValueProviderFactory> valueProviderFactories)
        {
            _actionContext = actionContext;
            _actionContextAccessor = actionContextAccessor;
            _diagnosticListener = diagnosticListener;
            _filters = filters;
            _logger = logger;
            _mapper = mapper;
            _valueProviderFactories = valueProviderFactories;
        }
        public virtual System.Threading.Tasks.Task InvokeAsync() { throw new System.ArgumentException(); }
        protected abstract System.Threading.Tasks.Task InvokeInnerFilterAsync();
        protected virtual System.Threading.Tasks.Task InvokeResultAsync(Microsoft.AspNetCore.Mvc.IActionResult result) { throw new System.ArgumentException(); }
        protected abstract void ReleaseResources();
    }
#nullable restore
    internal partial class StringArrayComparer : System.Collections.Generic.IEqualityComparer<string[]>
    {
        public static readonly Microsoft.AspNetCore.Mvc.Infrastructure.StringArrayComparer Ordinal;
        public static readonly Microsoft.AspNetCore.Mvc.Infrastructure.StringArrayComparer OrdinalIgnoreCase;
        public bool Equals(string[] x, string[] y) { throw null; }
        public int GetHashCode(string[] obj) { throw null; }
    }
    internal sealed partial class SystemTextJsonResultExecutor : Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultExecutor<Microsoft.AspNetCore.Mvc.JsonResult>
    {
        public SystemTextJsonResultExecutor(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.JsonOptions> options, Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Mvc.Infrastructure.SystemTextJsonResultExecutor> logger, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> mvcOptions) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task ExecuteAsync(Microsoft.AspNetCore.Mvc.ActionContext context, Microsoft.AspNetCore.Mvc.JsonResult result) { throw null; }
    }
    internal partial class TypeActivatorCache : Microsoft.AspNetCore.Mvc.Infrastructure.ITypeActivatorCache
    {
        public TypeActivatorCache() { }
        public TInstance CreateInstance<TInstance>(System.IServiceProvider serviceProvider, System.Type implementationType) { throw null; }
    }
    internal partial class ValidationProblemDetailsJsonConverter : System.Text.Json.Serialization.JsonConverter<Microsoft.AspNetCore.Mvc.ValidationProblemDetails>
    {
        public ValidationProblemDetailsJsonConverter() { }
        public override Microsoft.AspNetCore.Mvc.ValidationProblemDetails Read(ref System.Text.Json.Utf8JsonReader reader, System.Type typeToConvert, System.Text.Json.JsonSerializerOptions options) { throw null; }
        public override void Write(System.Text.Json.Utf8JsonWriter writer, Microsoft.AspNetCore.Mvc.ValidationProblemDetails value, System.Text.Json.JsonSerializerOptions options) { }
    }
}
namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public partial class CompositeValueProvider : System.Collections.ObjectModel.Collection<Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider>, Microsoft.AspNetCore.Mvc.ModelBinding.IBindingSourceValueProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IEnumerableValueProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IKeyRewriterValueProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider
    {
        [System.Diagnostics.DebuggerStepThroughAttribute]
        internal static System.Threading.Tasks.ValueTask<System.ValueTuple<bool, Microsoft.AspNetCore.Mvc.ModelBinding.CompositeValueProvider>> TryCreateAsync(Microsoft.AspNetCore.Mvc.ActionContext actionContext, System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.IValueProviderFactory> factories) { throw null; }
    }
    internal partial class ElementalValueProvider : Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider
    {
        public ElementalValueProvider(string key, string value, System.Globalization.CultureInfo culture) { }
        public System.Globalization.CultureInfo Culture { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Key { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool ContainsPrefix(string prefix) { throw null; }
        public Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderResult GetValue(string key) { throw null; }
    }
    public partial class ModelAttributes
    {
        internal ModelAttributes(System.Collections.Generic.IEnumerable<object> typeAttributes, System.Collections.Generic.IEnumerable<object> propertyAttributes, System.Collections.Generic.IEnumerable<object> parameterAttributes) { }
    }
    internal static partial class ModelBindingHelper
    {
        public static bool CanGetCompatibleCollection<T>(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { throw null; }
        internal static TModel CastOrDefault<TModel>(object model) { throw null; }
        public static void ClearValidationStateForModel(Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata modelMetadata, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState, string modelKey) { }
        public static void ClearValidationStateForModel(System.Type modelType, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, string modelKey) { }
        public static object ConvertTo(object value, System.Type type, System.Globalization.CultureInfo culture) { throw null; }
        public static T ConvertTo<T>(object value, System.Globalization.CultureInfo culture) { throw null; }
        public static System.Collections.Generic.ICollection<T> GetCompatibleCollection<T>(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { throw null; }
        public static System.Collections.Generic.ICollection<T> GetCompatibleCollection<T>(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext, int capacity) { throw null; }
        public static System.Linq.Expressions.Expression<System.Func<Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata, bool>> GetPropertyFilterExpression<TModel>(System.Linq.Expressions.Expression<System.Func<TModel, object>>[] expressions) { throw null; }
        internal static string GetPropertyName(System.Linq.Expressions.Expression expression) { throw null; }
        public static System.Threading.Tasks.Task<bool> TryUpdateModelAsync(object model, System.Type modelType, string prefix, Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderFactory modelBinderFactory, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider valueProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IObjectModelValidator objectModelValidator) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public static System.Threading.Tasks.Task<bool> TryUpdateModelAsync(object model, System.Type modelType, string prefix, Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderFactory modelBinderFactory, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider valueProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IObjectModelValidator objectModelValidator, System.Func<Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata, bool> propertyFilter) { throw null; }
        public static System.Threading.Tasks.Task<bool> TryUpdateModelAsync<TModel>(TModel model, string prefix, Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderFactory modelBinderFactory, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider valueProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IObjectModelValidator objectModelValidator) where TModel : class { throw null; }
        public static System.Threading.Tasks.Task<bool> TryUpdateModelAsync<TModel>(TModel model, string prefix, Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderFactory modelBinderFactory, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider valueProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IObjectModelValidator objectModelValidator, System.Func<Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata, bool> propertyFilter) where TModel : class { throw null; }
        public static System.Threading.Tasks.Task<bool> TryUpdateModelAsync<TModel>(TModel model, string prefix, Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderFactory modelBinderFactory, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider valueProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IObjectModelValidator objectModelValidator, params System.Linq.Expressions.Expression<System.Func<TModel, object>>[] includeExpressions) where TModel : class { throw null; }
    }
    internal partial class NoOpBinder : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder
    {
        public static readonly Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder Instance;
        public NoOpBinder() { }
        public System.Threading.Tasks.Task BindModelAsync(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { throw null; }
    }
    internal partial class PlaceholderBinder : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder
    {
        public PlaceholderBinder() { }
        public Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder Inner { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Threading.Tasks.Task BindModelAsync(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { throw null; }
    }
    internal static partial class PropertyValueSetter
    {
        public static void SetValue(Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata metadata, object instance, object value) { }
    }
    internal partial class ReferenceEqualityComparer : System.Collections.Generic.IEqualityComparer<object>
    {
        public ReferenceEqualityComparer() { }
        public static Microsoft.AspNetCore.Mvc.ModelBinding.ReferenceEqualityComparer Instance { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public new bool Equals(object x, object y) { throw null; }
        public int GetHashCode(object obj) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    public partial class CollectionModelBinder<TElement> : Microsoft.AspNetCore.Mvc.ModelBinding.ICollectionModelBinder, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder
    {
        internal bool AllowValidatingTopLevelNodes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        internal System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.ModelBinding.Binders.CollectionModelBinder<TElement>.CollectionResult> BindComplexCollectionFromIndexes(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext, System.Collections.Generic.IEnumerable<string> indexNames) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        internal System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.ModelBinding.Binders.CollectionModelBinder<TElement>.CollectionResult> BindSimpleCollection(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext, Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderResult values) { throw null; }
        internal partial class CollectionResult
        {
            public CollectionResult() { }
            public System.Collections.Generic.IEnumerable<TElement> Model { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IValidationStrategy ValidationStrategy { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        }
    }
    public partial class ComplexTypeModelBinder : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder
    {
        internal const int GreedyPropertiesMayHaveData = 1;
        internal const int NoDataAvailable = 0;
        internal const int ValueProviderDataAvailable = 2;
        internal int CanCreateModel(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { throw null; }
        internal static bool CanUpdatePropertyInternal(Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata propertyMetadata) { throw null; }
    }
    public partial class FloatingPointTypeModelBinderProvider : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderProvider
    {
        internal static readonly System.Globalization.NumberStyles SupportedStyles;
    }
    public partial class HeaderModelBinder : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder
    {
        internal Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder InnerModelBinder { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public partial class KeyValuePairModelBinder<TKey, TValue> : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder
    {
        [System.Diagnostics.DebuggerStepThroughAttribute]
        internal System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingResult> TryBindStrongModel<TModel>(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder binder, string propertyName, string propertyModelName) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata
{
    internal partial class DefaultBindingMetadataProvider : Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IBindingMetadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IMetadataDetailsProvider
    {
        public DefaultBindingMetadataProvider() { }
        public void CreateBindingMetadata(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.BindingMetadataProviderContext context) { }
    }
    internal partial class DefaultCompositeMetadataDetailsProvider : Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IBindingMetadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ICompositeMetadataDetailsProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IDisplayMetadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IMetadataDetailsProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IValidationMetadataProvider
    {
        public DefaultCompositeMetadataDetailsProvider(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IMetadataDetailsProvider> providers) { }
        public void CreateBindingMetadata(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.BindingMetadataProviderContext context) { }
        public void CreateDisplayMetadata(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.DisplayMetadataProviderContext context) { }
        public void CreateValidationMetadata(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ValidationMetadataProviderContext context) { }
    }
    public partial class DefaultModelMetadata : Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata
    {
        internal static bool CalculateHasValidators(System.Collections.Generic.HashSet<Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.DefaultModelMetadata> visited, Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata metadata) { throw null; }
    }
    internal partial class DefaultValidationMetadataProvider : Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IMetadataDetailsProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IValidationMetadataProvider
    {
        public DefaultValidationMetadataProvider() { }
        public void CreateValidationMetadata(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ValidationMetadataProviderContext context) { }
    }
}
namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation
{
    internal partial class DefaultCollectionValidationStrategy : Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IValidationStrategy
    {
        public static readonly Microsoft.AspNetCore.Mvc.ModelBinding.Validation.DefaultCollectionValidationStrategy Instance;
        public System.Collections.Generic.IEnumerator<Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidationEntry> GetChildren(Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata metadata, string key, object model) { throw null; }
        public System.Collections.IEnumerator GetEnumeratorForElementType(Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata metadata, object model) { throw null; }
    }
    internal partial class DefaultComplexObjectValidationStrategy : Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IValidationStrategy
    {
        public static readonly Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IValidationStrategy Instance;
        public System.Collections.Generic.IEnumerator<Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidationEntry> GetChildren(Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata metadata, string key, object model) { throw null; }
    }
    internal partial class DefaultModelValidatorProvider : Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IMetadataBasedModelValidatorProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IModelValidatorProvider
    {
        public DefaultModelValidatorProvider() { }
        public void CreateValidators(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ModelValidatorProviderContext context) { }
        public bool HasValidators(System.Type modelType, System.Collections.Generic.IList<object> validatorMetadata) { throw null; }
    }
    internal partial class DefaultObjectValidator : Microsoft.AspNetCore.Mvc.ModelBinding.ObjectModelValidator
    {
        public DefaultObjectValidator(Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider modelMetadataProvider, System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IModelValidatorProvider> validatorProviders, Microsoft.AspNetCore.Mvc.MvcOptions mvcOptions) : base (default(Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider), default(System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IModelValidatorProvider>)) { }
        public override Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidationVisitor GetValidationVisitor(Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IModelValidatorProvider validatorProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidatorCache validatorCache, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidationStateDictionary validationState) { throw null; }
    }
    internal partial class ExplicitIndexCollectionValidationStrategy : Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IValidationStrategy
    {
        public ExplicitIndexCollectionValidationStrategy(System.Collections.Generic.IEnumerable<string> elementKeys) { }
        public System.Collections.Generic.IEnumerable<string> ElementKeys { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Collections.Generic.IEnumerator<Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidationEntry> GetChildren(Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata metadata, string key, object model) { throw null; }
    }
    internal partial class HasValidatorsValidationMetadataProvider : Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IMetadataDetailsProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IValidationMetadataProvider
    {
        public HasValidatorsValidationMetadataProvider(System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IModelValidatorProvider> modelValidatorProviders) { }
        public void CreateValidationMetadata(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ValidationMetadataProviderContext context) { }
    }
    internal partial class ShortFormDictionaryValidationStrategy<TKey, TValue> : Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IValidationStrategy
    {
        public ShortFormDictionaryValidationStrategy(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, TKey>> keyMappings, Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata valueMetadata) { }
        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, TKey>> KeyMappings { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Collections.Generic.IEnumerator<Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidationEntry> GetChildren(Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata metadata, string key, object model) { throw null; }
    }
    internal partial class ValidationStack
    {
        internal const int CutOff = 20;
        public ValidationStack() { }
        public int Count { get { throw null; } }
        internal System.Collections.Generic.HashSet<object> HashSet { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        internal System.Collections.Generic.List<object> List { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public void Pop(object model) { }
        public bool Push(object model) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.Routing
{
    internal partial class ActionConstraintMatcherPolicy : Microsoft.AspNetCore.Routing.MatcherPolicy, Microsoft.AspNetCore.Routing.Matching.IEndpointSelectorPolicy
    {
        internal static readonly Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor NonAction;
        public ActionConstraintMatcherPolicy(Microsoft.AspNetCore.Mvc.ActionConstraints.ActionConstraintCache actionConstraintCache) { }
        public override int Order { get { throw null; } }
        public bool AppliesToEndpoints(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> endpoints) { throw null; }
        public System.Threading.Tasks.Task ApplyAsync(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.Matching.CandidateSet candidateSet) { throw null; }
    }
    internal abstract partial class ActionEndpointDataSourceBase : Microsoft.AspNetCore.Routing.EndpointDataSource, System.IDisposable
    {
        protected readonly System.Collections.Generic.List<System.Action<Microsoft.AspNetCore.Builder.EndpointBuilder>> Conventions;
        protected readonly object Lock;
        public ActionEndpointDataSourceBase(Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider actions) { }
        public override System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> Endpoints { get { throw null; } }
        protected abstract System.Collections.Generic.List<Microsoft.AspNetCore.Http.Endpoint> CreateEndpoints(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor> actions, System.Collections.Generic.IReadOnlyList<System.Action<Microsoft.AspNetCore.Builder.EndpointBuilder>> conventions);
        public void Dispose() { }
        public override Microsoft.Extensions.Primitives.IChangeToken GetChangeToken() { throw null; }
        protected void Subscribe() { }
    }
    internal partial class ActionEndpointFactory
    {
        public ActionEndpointFactory(Microsoft.AspNetCore.Routing.Patterns.RoutePatternTransformer routePatternTransformer) { }
        public void AddConventionalLinkGenerationRoute(System.Collections.Generic.List<Microsoft.AspNetCore.Http.Endpoint> endpoints, System.Collections.Generic.HashSet<string> routeNames, System.Collections.Generic.HashSet<string> keys, Microsoft.AspNetCore.Mvc.Routing.ConventionalRouteEntry route, System.Collections.Generic.IReadOnlyList<System.Action<Microsoft.AspNetCore.Builder.EndpointBuilder>> conventions) { }
        public void AddEndpoints(System.Collections.Generic.List<Microsoft.AspNetCore.Http.Endpoint> endpoints, System.Collections.Generic.HashSet<string> routeNames, Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor action, System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.Routing.ConventionalRouteEntry> routes, System.Collections.Generic.IReadOnlyList<System.Action<Microsoft.AspNetCore.Builder.EndpointBuilder>> conventions, bool createInertEndpoints) { }
    }
    internal partial class AttributeRoute : Microsoft.AspNetCore.Routing.IRouter
    {
        public AttributeRoute(Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider actionDescriptorCollectionProvider, System.IServiceProvider services, System.Func<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor[], Microsoft.AspNetCore.Routing.IRouter> handlerFactory) { }
        internal void AddEntries(Microsoft.AspNetCore.Routing.Tree.TreeRouteBuilder builder, Microsoft.AspNetCore.Mvc.Infrastructure.ActionDescriptorCollection actions) { }
        public Microsoft.AspNetCore.Routing.VirtualPathData GetVirtualPath(Microsoft.AspNetCore.Routing.VirtualPathContext context) { throw null; }
        public System.Threading.Tasks.Task RouteAsync(Microsoft.AspNetCore.Routing.RouteContext context) { throw null; }
    }
    internal static partial class AttributeRouting
    {
        public static Microsoft.AspNetCore.Routing.IRouter CreateAttributeMegaRoute(System.IServiceProvider services) { throw null; }
    }
    internal partial class ConsumesMatcherPolicy : Microsoft.AspNetCore.Routing.MatcherPolicy, Microsoft.AspNetCore.Routing.Matching.IEndpointComparerPolicy, Microsoft.AspNetCore.Routing.Matching.IEndpointSelectorPolicy, Microsoft.AspNetCore.Routing.Matching.INodeBuilderPolicy
    {
        internal const string AnyContentType = "*/*";
        internal const string Http415EndpointDisplayName = "415 HTTP Unsupported Media Type";
        public ConsumesMatcherPolicy() { }
        public System.Collections.Generic.IComparer<Microsoft.AspNetCore.Http.Endpoint> Comparer { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public override int Order { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Threading.Tasks.Task ApplyAsync(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.Matching.CandidateSet candidates) { throw null; }
        public Microsoft.AspNetCore.Routing.Matching.PolicyJumpTable BuildJumpTable(int exitDestination, System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Routing.Matching.PolicyJumpTableEdge> edges) { throw null; }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Routing.Matching.PolicyNodeEdge> GetEdges(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> endpoints) { throw null; }
        bool Microsoft.AspNetCore.Routing.Matching.IEndpointSelectorPolicy.AppliesToEndpoints(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> endpoints) { throw null; }
        bool Microsoft.AspNetCore.Routing.Matching.INodeBuilderPolicy.AppliesToEndpoints(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> endpoints) { throw null; }
    }
    internal partial class ConsumesMetadata : Microsoft.AspNetCore.Mvc.Routing.IConsumesMetadata
    {
        public ConsumesMetadata(string[] contentTypes) { }
        public System.Collections.Generic.IReadOnlyList<string> ContentTypes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    internal partial class ControllerActionEndpointDataSource : Microsoft.AspNetCore.Mvc.Routing.ActionEndpointDataSourceBase
    {
        public ControllerActionEndpointDataSource(Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider actions, Microsoft.AspNetCore.Mvc.Routing.ActionEndpointFactory endpointFactory) : base (default(Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider)) { }
        public bool CreateInertEndpoints { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Builder.ControllerActionEndpointConventionBuilder DefaultBuilder { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Builder.ControllerActionEndpointConventionBuilder AddRoute(string routeName, string pattern, Microsoft.AspNetCore.Routing.RouteValueDictionary defaults, System.Collections.Generic.IDictionary<string, object> constraints, Microsoft.AspNetCore.Routing.RouteValueDictionary dataTokens) { throw null; }
        protected override System.Collections.Generic.List<Microsoft.AspNetCore.Http.Endpoint> CreateEndpoints(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor> actions, System.Collections.Generic.IReadOnlyList<System.Action<Microsoft.AspNetCore.Builder.EndpointBuilder>> conventions) { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal readonly partial struct ConventionalRouteEntry
    {
        public readonly Microsoft.AspNetCore.Routing.Patterns.RoutePattern Pattern;
        public readonly string RouteName;
        public readonly Microsoft.AspNetCore.Routing.RouteValueDictionary DataTokens;
        public readonly int Order;
        public readonly System.Collections.Generic.IReadOnlyList<System.Action<Microsoft.AspNetCore.Builder.EndpointBuilder>> Conventions;
        public ConventionalRouteEntry(string routeName, string pattern, Microsoft.AspNetCore.Routing.RouteValueDictionary defaults, System.Collections.Generic.IDictionary<string, object> constraints, Microsoft.AspNetCore.Routing.RouteValueDictionary dataTokens, int order, System.Collections.Generic.List<System.Action<Microsoft.AspNetCore.Builder.EndpointBuilder>> conventions) { throw null; }
    }
    internal partial class DynamicControllerEndpointMatcherPolicy : Microsoft.AspNetCore.Routing.MatcherPolicy, Microsoft.AspNetCore.Routing.Matching.IEndpointSelectorPolicy
    {
        public DynamicControllerEndpointMatcherPolicy(Microsoft.AspNetCore.Mvc.Routing.DynamicControllerEndpointSelector selector, Microsoft.AspNetCore.Routing.Matching.EndpointMetadataComparer comparer) { }
        public override int Order { get { throw null; } }
        public bool AppliesToEndpoints(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> endpoints) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task ApplyAsync(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.Matching.CandidateSet candidates) { throw null; }
    }
    internal partial class DynamicControllerEndpointSelector : System.IDisposable
    {
        public DynamicControllerEndpointSelector(Microsoft.AspNetCore.Mvc.Routing.ControllerActionEndpointDataSource dataSource) { }
        protected DynamicControllerEndpointSelector(Microsoft.AspNetCore.Routing.EndpointDataSource dataSource) { }
        public void Dispose() { }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> SelectEndpoints(Microsoft.AspNetCore.Routing.RouteValueDictionary values) { throw null; }
    }
    internal partial class DynamicControllerMetadata : Microsoft.AspNetCore.Routing.IDynamicEndpointMetadata
    {
        public DynamicControllerMetadata(Microsoft.AspNetCore.Routing.RouteValueDictionary values) { }
        public bool IsDynamic { get { throw null; } }
        public Microsoft.AspNetCore.Routing.RouteValueDictionary Values { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    internal partial class DynamicControllerRouteValueTransformerMetadata : Microsoft.AspNetCore.Routing.IDynamicEndpointMetadata
    {
        public DynamicControllerRouteValueTransformerMetadata(System.Type selectorType) { }
        public bool IsDynamic { get { throw null; } }
        public System.Type SelectorType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    internal partial class EndpointRoutingUrlHelper : Microsoft.AspNetCore.Mvc.Routing.UrlHelperBase
    {
        public EndpointRoutingUrlHelper(Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Routing.LinkGenerator linkGenerator, Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Mvc.Routing.EndpointRoutingUrlHelper> logger) : base (default(Microsoft.AspNetCore.Mvc.ActionContext)) { }
        public override string Action(Microsoft.AspNetCore.Mvc.Routing.UrlActionContext urlActionContext) { throw null; }
        public override string RouteUrl(Microsoft.AspNetCore.Mvc.Routing.UrlRouteContext routeContext) { throw null; }
    }
    internal partial interface IConsumesMetadata
    {
        System.Collections.Generic.IReadOnlyList<string> ContentTypes { get; }
    }
    internal partial class MvcAttributeRouteHandler : Microsoft.AspNetCore.Routing.IRouter
    {
        public MvcAttributeRouteHandler(Microsoft.AspNetCore.Mvc.Infrastructure.IActionInvokerFactory actionInvokerFactory, Microsoft.AspNetCore.Mvc.Infrastructure.IActionSelector actionSelector, System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor[] Actions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Routing.VirtualPathData GetVirtualPath(Microsoft.AspNetCore.Routing.VirtualPathContext context) { throw null; }
        public System.Threading.Tasks.Task RouteAsync(Microsoft.AspNetCore.Routing.RouteContext context) { throw null; }
    }
    internal partial class MvcRouteHandler : Microsoft.AspNetCore.Routing.IRouter
    {
        public MvcRouteHandler(Microsoft.AspNetCore.Mvc.Infrastructure.IActionInvokerFactory actionInvokerFactory, Microsoft.AspNetCore.Mvc.Infrastructure.IActionSelector actionSelector, System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public Microsoft.AspNetCore.Routing.VirtualPathData GetVirtualPath(Microsoft.AspNetCore.Routing.VirtualPathContext context) { throw null; }
        public System.Threading.Tasks.Task RouteAsync(Microsoft.AspNetCore.Routing.RouteContext context) { throw null; }
    }
    internal static partial class NormalizedRouteValue
    {
        public static string GetNormalizedRouteValue(Microsoft.AspNetCore.Mvc.ActionContext context, string key) { throw null; }
    }
    internal partial class NullRouter : Microsoft.AspNetCore.Routing.IRouter
    {
        public static Microsoft.AspNetCore.Routing.IRouter Instance;
        public Microsoft.AspNetCore.Routing.VirtualPathData GetVirtualPath(Microsoft.AspNetCore.Routing.VirtualPathContext context) { throw null; }
        public System.Threading.Tasks.Task RouteAsync(Microsoft.AspNetCore.Routing.RouteContext context) { throw null; }
    }
    internal static partial class RoutePatternWriter
    {
        public static void WriteString(System.Text.StringBuilder sb, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Routing.Patterns.RoutePatternPathSegment> routeSegments) { }
    }
    public abstract partial class UrlHelperBase : Microsoft.AspNetCore.Mvc.IUrlHelper
    {
        internal static void AppendPathAndFragment(System.Text.StringBuilder builder, Microsoft.AspNetCore.Http.PathString pathBase, string virtualPath, string fragment) { }
        internal static void NormalizeRouteValuesForAction(string action, string controller, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteValueDictionary ambientValues) { }
        internal static void NormalizeRouteValuesForPage(Microsoft.AspNetCore.Mvc.ActionContext context, string page, string handler, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteValueDictionary ambientValues) { }
    }
    internal static partial class ViewEnginePath
    {
        public static readonly char[] PathSeparators;
        public static string CombinePath(string first, string second) { throw null; }
        public static string ResolvePath(string path) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Routing
{
    internal sealed partial class DataSourceDependentCache<T> : System.IDisposable where T : class
    {
        public DataSourceDependentCache(Microsoft.AspNetCore.Routing.EndpointDataSource dataSource, System.Func<System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint>, T> initialize) { }
        public T Value { get { throw null; } }
        public void Dispose() { }
        public T EnsureInitialized() { throw null; }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    internal partial class ApiBehaviorOptionsSetup : Microsoft.Extensions.Options.IConfigureOptions<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>
    {
        public ApiBehaviorOptionsSetup() { }
        public void Configure(Microsoft.AspNetCore.Mvc.ApiBehaviorOptions options) { }
        internal static void ConfigureClientErrorMapping(Microsoft.AspNetCore.Mvc.ApiBehaviorOptions options) { }
        internal static Microsoft.AspNetCore.Mvc.IActionResult ProblemDetailsInvalidModelStateResponse(Microsoft.AspNetCore.Mvc.Infrastructure.ProblemDetailsFactory problemDetailsFactory, Microsoft.AspNetCore.Mvc.ActionContext context) { throw null; }
    }
    internal partial class MvcBuilder : Microsoft.Extensions.DependencyInjection.IMvcBuilder
    {
        public MvcBuilder(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager manager) { }
        public Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager PartManager { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.Extensions.DependencyInjection.IServiceCollection Services { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    internal partial class MvcCoreBuilder : Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder
    {
        public MvcCoreBuilder(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager manager) { }
        public Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager PartManager { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.Extensions.DependencyInjection.IServiceCollection Services { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public static partial class MvcCoreMvcCoreBuilderExtensions
    {
        internal static void AddAuthorizationServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services) { }
        internal static void AddFormatterMappingsServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services) { }
    }
    internal partial class MvcCoreRouteOptionsSetup : Microsoft.Extensions.Options.IConfigureOptions<Microsoft.AspNetCore.Routing.RouteOptions>
    {
        public MvcCoreRouteOptionsSetup() { }
        public void Configure(Microsoft.AspNetCore.Routing.RouteOptions options) { }
    }
    public static partial class MvcCoreServiceCollectionExtensions
    {
        internal static void AddMvcCoreServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services) { }
    }
    internal partial class MvcMarkerService
    {
        public MvcMarkerService() { }
    }
}
namespace Microsoft.Extensions.Internal
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal readonly partial struct AwaitableInfo
    {
        private readonly object _dummy;
        public AwaitableInfo(System.Type awaiterType, System.Reflection.PropertyInfo awaiterIsCompletedProperty, System.Reflection.MethodInfo awaiterGetResultMethod, System.Reflection.MethodInfo awaiterOnCompletedMethod, System.Reflection.MethodInfo awaiterUnsafeOnCompletedMethod, System.Type resultType, System.Reflection.MethodInfo getAwaiterMethod) { throw null; }
        public System.Reflection.MethodInfo AwaiterGetResultMethod { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Reflection.PropertyInfo AwaiterIsCompletedProperty { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Reflection.MethodInfo AwaiterOnCompletedMethod { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Type AwaiterType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Reflection.MethodInfo AwaiterUnsafeOnCompletedMethod { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Reflection.MethodInfo GetAwaiterMethod { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Type ResultType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public static bool IsTypeAwaitable(System.Type type, out Microsoft.Extensions.Internal.AwaitableInfo awaitableInfo) { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal readonly partial struct CoercedAwaitableInfo
    {
        private readonly object _dummy;
        public CoercedAwaitableInfo(Microsoft.Extensions.Internal.AwaitableInfo awaitableInfo) { throw null; }
        public CoercedAwaitableInfo(System.Linq.Expressions.Expression coercerExpression, System.Type coercerResultType, Microsoft.Extensions.Internal.AwaitableInfo coercedAwaitableInfo) { throw null; }
        public Microsoft.Extensions.Internal.AwaitableInfo AwaitableInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Linq.Expressions.Expression CoercerExpression { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Type CoercerResultType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool RequiresCoercion { get { throw null; } }
        public static bool IsTypeAwaitable(System.Type type, out Microsoft.Extensions.Internal.CoercedAwaitableInfo info) { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal partial struct CopyOnWriteDictionaryHolder<TKey, TValue>
    {
        private object _dummy;
        public CopyOnWriteDictionaryHolder(Microsoft.Extensions.Internal.CopyOnWriteDictionaryHolder<TKey, TValue> source) { throw null; }
        public CopyOnWriteDictionaryHolder(System.Collections.Generic.Dictionary<TKey, TValue> source) { throw null; }
        public int Count { get { throw null; } }
        public bool HasBeenCopied { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public TValue this[TKey key] { get { throw null; } set { } }
        public System.Collections.Generic.Dictionary<TKey, TValue>.KeyCollection Keys { get { throw null; } }
        public System.Collections.Generic.Dictionary<TKey, TValue> ReadDictionary { get { throw null; } }
        public System.Collections.Generic.Dictionary<TKey, TValue>.ValueCollection Values { get { throw null; } }
        public System.Collections.Generic.Dictionary<TKey, TValue> WriteDictionary { get { throw null; } }
        public void Add(System.Collections.Generic.KeyValuePair<TKey, TValue> item) { }
        public void Add(TKey key, TValue value) { }
        public void Clear() { }
        public bool Contains(System.Collections.Generic.KeyValuePair<TKey, TValue> item) { throw null; }
        public bool ContainsKey(TKey key) { throw null; }
        public void CopyTo(System.Collections.Generic.KeyValuePair<TKey, TValue>[] array, int arrayIndex) { }
        public System.Collections.Generic.Dictionary<TKey, TValue>.Enumerator GetEnumerator() { throw null; }
        public bool Remove(System.Collections.Generic.KeyValuePair<TKey, TValue> item) { throw null; }
        public bool Remove(TKey key) { throw null; }
        public bool TryGetValue(TKey key, out TValue value) { throw null; }
    }
    internal partial class CopyOnWriteDictionary<TKey, TValue> : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IDictionary<TKey, TValue>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.IEnumerable
    {
        public CopyOnWriteDictionary(System.Collections.Generic.IDictionary<TKey, TValue> sourceDictionary, System.Collections.Generic.IEqualityComparer<TKey> comparer) { }
        public virtual int Count { get { throw null; } }
        public virtual bool IsReadOnly { get { throw null; } }
        public virtual TValue this[TKey key] { get { throw null; } set { } }
        public virtual System.Collections.Generic.ICollection<TKey> Keys { get { throw null; } }
        public virtual System.Collections.Generic.ICollection<TValue> Values { get { throw null; } }
        public virtual void Add(System.Collections.Generic.KeyValuePair<TKey, TValue> item) { }
        public virtual void Add(TKey key, TValue value) { }
        public virtual void Clear() { }
        public virtual bool Contains(System.Collections.Generic.KeyValuePair<TKey, TValue> item) { throw null; }
        public virtual bool ContainsKey(TKey key) { throw null; }
        public virtual void CopyTo(System.Collections.Generic.KeyValuePair<TKey, TValue>[] array, int arrayIndex) { }
        public virtual System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>> GetEnumerator() { throw null; }
        public bool Remove(System.Collections.Generic.KeyValuePair<TKey, TValue> item) { throw null; }
        public virtual bool Remove(TKey key) { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public virtual bool TryGetValue(TKey key, out TValue value) { throw null; }
    }
    internal partial class ObjectMethodExecutor
    {
        public System.Type AsyncResultType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool IsMethodAsync { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Reflection.MethodInfo MethodInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Reflection.ParameterInfo[] MethodParameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Type MethodReturnType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]internal set { } }
        public System.Reflection.TypeInfo TargetTypeInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public static Microsoft.Extensions.Internal.ObjectMethodExecutor Create(System.Reflection.MethodInfo methodInfo, System.Reflection.TypeInfo targetTypeInfo) { throw null; }
        public static Microsoft.Extensions.Internal.ObjectMethodExecutor Create(System.Reflection.MethodInfo methodInfo, System.Reflection.TypeInfo targetTypeInfo, object[] parameterDefaultValues) { throw null; }
        public object Execute(object target, object[] parameters) { throw null; }
        public Microsoft.Extensions.Internal.ObjectMethodExecutorAwaitable ExecuteAsync(object target, object[] parameters) { throw null; }
        public object GetDefaultValueForParameter(int index) { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal readonly partial struct ObjectMethodExecutorAwaitable
    {
        private readonly object _dummy;
        public ObjectMethodExecutorAwaitable(object customAwaitable, System.Func<object, object> getAwaiterMethod, System.Func<object, bool> isCompletedMethod, System.Func<object, object> getResultMethod, System.Action<object, System.Action> onCompletedMethod, System.Action<object, System.Action> unsafeOnCompletedMethod) { throw null; }
        public Microsoft.Extensions.Internal.ObjectMethodExecutorAwaitable.Awaiter GetAwaiter() { throw null; }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public readonly partial struct Awaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion
        {
            private readonly object _dummy;
            public Awaiter(object customAwaiter, System.Func<object, bool> isCompletedMethod, System.Func<object, object> getResultMethod, System.Action<object, System.Action> onCompletedMethod, System.Action<object, System.Action> unsafeOnCompletedMethod) { throw null; }
            public bool IsCompleted { get { throw null; } }
            public object GetResult() { throw null; }
            public void OnCompleted(System.Action continuation) { }
            public void UnsafeOnCompleted(System.Action continuation) { }
        }
    }
    internal static partial class ObjectMethodExecutorFSharpSupport
    {
        public static bool TryBuildCoercerFromFSharpAsyncToAwaitable(System.Type possibleFSharpAsyncType, out System.Linq.Expressions.Expression coerceToAwaitableExpression, out System.Type awaitableType) { throw null; }
    }
    internal partial class PropertyActivator<TContext>
    {
        public PropertyActivator(System.Reflection.PropertyInfo propertyInfo, System.Func<TContext, object> valueAccessor) { }
        public System.Reflection.PropertyInfo PropertyInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public object Activate(object instance, TContext context) { throw null; }
        public static Microsoft.Extensions.Internal.PropertyActivator<TContext>[] GetPropertiesToActivate(System.Type type, System.Type activateAttributeType, System.Func<System.Reflection.PropertyInfo, Microsoft.Extensions.Internal.PropertyActivator<TContext>> createActivateInfo) { throw null; }
        public static Microsoft.Extensions.Internal.PropertyActivator<TContext>[] GetPropertiesToActivate(System.Type type, System.Type activateAttributeType, System.Func<System.Reflection.PropertyInfo, Microsoft.Extensions.Internal.PropertyActivator<TContext>> createActivateInfo, bool includeNonPublic) { throw null; }
    }
    internal partial class PropertyHelper
    {
        public PropertyHelper(System.Reflection.PropertyInfo property) { }
        public virtual string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]protected set { } }
        public System.Reflection.PropertyInfo Property { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Func<object, object> ValueGetter { get { throw null; } }
        public System.Action<object, object> ValueSetter { get { throw null; } }
        public static Microsoft.Extensions.Internal.PropertyHelper[] GetProperties(System.Reflection.TypeInfo typeInfo) { throw null; }
        public static Microsoft.Extensions.Internal.PropertyHelper[] GetProperties(System.Type type) { throw null; }
        protected static Microsoft.Extensions.Internal.PropertyHelper[] GetProperties(System.Type type, System.Func<System.Reflection.PropertyInfo, Microsoft.Extensions.Internal.PropertyHelper> createPropertyHelper, System.Collections.Concurrent.ConcurrentDictionary<System.Type, Microsoft.Extensions.Internal.PropertyHelper[]> cache) { throw null; }
        public object GetValue(object instance) { throw null; }
        public static Microsoft.Extensions.Internal.PropertyHelper[] GetVisibleProperties(System.Reflection.TypeInfo typeInfo) { throw null; }
        public static Microsoft.Extensions.Internal.PropertyHelper[] GetVisibleProperties(System.Type type) { throw null; }
        protected static Microsoft.Extensions.Internal.PropertyHelper[] GetVisibleProperties(System.Type type, System.Func<System.Reflection.PropertyInfo, Microsoft.Extensions.Internal.PropertyHelper> createPropertyHelper, System.Collections.Concurrent.ConcurrentDictionary<System.Type, Microsoft.Extensions.Internal.PropertyHelper[]> allPropertiesCache, System.Collections.Concurrent.ConcurrentDictionary<System.Type, Microsoft.Extensions.Internal.PropertyHelper[]> visiblePropertiesCache) { throw null; }
        public static System.Func<object, object> MakeFastPropertyGetter(System.Reflection.PropertyInfo propertyInfo) { throw null; }
        public static System.Action<object, object> MakeFastPropertySetter(System.Reflection.PropertyInfo propertyInfo) { throw null; }
        public static System.Func<object, object> MakeNullSafeFastPropertyGetter(System.Reflection.PropertyInfo propertyInfo) { throw null; }
        public static System.Collections.Generic.IDictionary<string, object> ObjectToDictionary(object value) { throw null; }
        public void SetValue(object instance, object value) { }
    }
    internal static partial class SecurityHelper
    {
        public static System.Security.Claims.ClaimsPrincipal MergeUserPrincipal(System.Security.Claims.ClaimsPrincipal existingPrincipal, System.Security.Claims.ClaimsPrincipal additionalPrincipal) { throw null; }
    }
}
namespace System.Text.Json
{
    internal static partial class JsonSerializerOptionsCopyConstructor
    {
        public static System.Text.Json.JsonSerializerOptions Copy(this System.Text.Json.JsonSerializerOptions serializerOptions, System.Text.Encodings.Web.JavaScriptEncoder encoder) { throw null; }
    }
}
