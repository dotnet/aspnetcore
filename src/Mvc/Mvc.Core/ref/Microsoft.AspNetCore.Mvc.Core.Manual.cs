// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Internal
{
    internal partial interface IResponseCacheFilter
    {
    }
}
namespace Microsoft.AspNetCore.Mvc
{
    internal partial class MvcCoreMvcOptionsSetup
    {
        private readonly Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.JsonOptions> _jsonOptions;
        private readonly Microsoft.Extensions.Logging.ILoggerFactory _loggerFactory;
        private readonly Microsoft.AspNetCore.Mvc.Infrastructure.IHttpRequestStreamReaderFactory _readerFactory;
        public MvcCoreMvcOptionsSetup(Microsoft.AspNetCore.Mvc.Infrastructure.IHttpRequestStreamReaderFactory readerFactory) { }
        public MvcCoreMvcOptionsSetup(Microsoft.AspNetCore.Mvc.Infrastructure.IHttpRequestStreamReaderFactory readerFactory, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.JsonOptions> jsonOptions) { }
        public void Configure(Microsoft.AspNetCore.Mvc.MvcOptions options) { }
        internal static void ConfigureAdditionalModelMetadataDetailsProviders(System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IMetadataDetailsProvider> modelMetadataDetailsProviders) { }
        public void PostConfigure(string name, Microsoft.AspNetCore.Mvc.MvcOptions options) { }
    }
}
namespace Microsoft.AspNetCore.Mvc.Controllers
{
    internal delegate System.Threading.Tasks.Task ControllerBinderDelegate(Microsoft.AspNetCore.Mvc.ControllerContext controllerContext, object controller, System.Collections.Generic.Dictionary<string, object> arguments);
}
namespace Microsoft.AspNetCore.Mvc.ActionConstraints
{
    internal partial class DefaultActionConstraintProvider : Microsoft.AspNetCore.Mvc.ActionConstraints.IActionConstraintProvider
    {
        public DefaultActionConstraintProvider() { }
        public int Order { get { throw null; } }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.ActionConstraints.ActionConstraintProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.ActionConstraints.ActionConstraintProviderContext context) { }
        private void ProvideConstraint(Microsoft.AspNetCore.Mvc.ActionConstraints.ActionConstraintItem item, System.IServiceProvider services) { }
    }
    internal partial class ActionConstraintCache
    {
        private readonly Microsoft.AspNetCore.Mvc.ActionConstraints.IActionConstraintProvider[] _actionConstraintProviders;
        private readonly Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider _collectionProvider;
        private volatile Microsoft.AspNetCore.Mvc.ActionConstraints.ActionConstraintCache.InnerCache _currentCache;
        public ActionConstraintCache(Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider collectionProvider, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ActionConstraints.IActionConstraintProvider> actionConstraintProviders) { }
        internal Microsoft.AspNetCore.Mvc.ActionConstraints.ActionConstraintCache.InnerCache CurrentCache { get { throw null; } }
        private void ExecuteProviders(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor action, System.Collections.Generic.List<Microsoft.AspNetCore.Mvc.ActionConstraints.ActionConstraintItem> items) { }
        private System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ActionConstraints.IActionConstraint> ExtractActionConstraints(System.Collections.Generic.List<Microsoft.AspNetCore.Mvc.ActionConstraints.ActionConstraintItem> items) { throw null; }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ActionConstraints.IActionConstraint> GetActionConstraints(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor action) { throw null; }
        private System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ActionConstraints.IActionConstraint> GetActionConstraintsFromEntry(Microsoft.AspNetCore.Mvc.ActionConstraints.ActionConstraintCache.CacheEntry entry, Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor action) { throw null; }
        internal readonly partial struct CacheEntry
        {
            private readonly object _dummy;
            public CacheEntry(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ActionConstraints.IActionConstraint> actionConstraints) { throw null; }
            public CacheEntry(System.Collections.Generic.List<Microsoft.AspNetCore.Mvc.ActionConstraints.ActionConstraintItem> items) { throw null; }
            public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ActionConstraints.IActionConstraint> ActionConstraints { get { throw null; } }
            public System.Collections.Generic.List<Microsoft.AspNetCore.Mvc.ActionConstraints.ActionConstraintItem> Items { get { throw null; } }
        }
        internal partial class InnerCache
        {
            private readonly Microsoft.AspNetCore.Mvc.Infrastructure.ActionDescriptorCollection _actions;
            private readonly System.Collections.Concurrent.ConcurrentDictionary<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor, Microsoft.AspNetCore.Mvc.ActionConstraints.ActionConstraintCache.CacheEntry> _Entries_k__BackingField;
            public InnerCache(Microsoft.AspNetCore.Mvc.Infrastructure.ActionDescriptorCollection actions) { }
            public System.Collections.Concurrent.ConcurrentDictionary<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor, Microsoft.AspNetCore.Mvc.ActionConstraints.ActionConstraintCache.CacheEntry> Entries { get { throw null; } }
            public int Version { get { throw null; } }
        }
    }
}
namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    internal partial class ApiBehaviorApplicationModelProvider : Microsoft.AspNetCore.Mvc.ApplicationModels.IApplicationModelProvider
    {
        private readonly System.Collections.Generic.List<Microsoft.AspNetCore.Mvc.ApplicationModels.IActionModelConvention> _ActionModelConventions_k__BackingField;
        public ApiBehaviorApplicationModelProvider(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions> apiBehaviorOptions, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider modelMetadataProvider, Microsoft.AspNetCore.Mvc.Infrastructure.IClientErrorFactory clientErrorFactory, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public System.Collections.Generic.List<Microsoft.AspNetCore.Mvc.ApplicationModels.IActionModelConvention> ActionModelConventions { get { throw null; } }
        public int Order { get { throw null; } }
        private static void EnsureActionIsAttributeRouted(Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel actionModel) { }
        private static bool IsApiController(Microsoft.AspNetCore.Mvc.ApplicationModels.ControllerModel controller) { throw null; }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelProviderContext context) { }
    }
    internal partial class ApplicationModelFactory
    {
        private readonly Microsoft.AspNetCore.Mvc.ApplicationModels.IApplicationModelProvider[] _applicationModelProviders;
        private readonly System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ApplicationModels.IApplicationModelConvention> _conventions;
        public ApplicationModelFactory(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ApplicationModels.IApplicationModelProvider> applicationModelProviders, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> options) { }
        private static void AddActionToMethodInfoMap(System.Collections.Generic.Dictionary<System.Reflection.MethodInfo, System.Collections.Generic.List<System.ValueTuple<Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel, Microsoft.AspNetCore.Mvc.ApplicationModels.SelectorModel>>> actionsByMethod, Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel action, Microsoft.AspNetCore.Mvc.ApplicationModels.SelectorModel selector) { }
        private static void AddActionToRouteNameMap(System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<System.ValueTuple<Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel, Microsoft.AspNetCore.Mvc.ApplicationModels.SelectorModel>>> actionsByRouteName, Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel action, Microsoft.AspNetCore.Mvc.ApplicationModels.SelectorModel selector) { }
        private static System.Collections.Generic.List<string> AddErrorNumbers(System.Collections.Generic.IEnumerable<string> namedRoutedErrors) { throw null; }
        public Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModel CreateApplicationModel(System.Collections.Generic.IEnumerable<System.Reflection.TypeInfo> controllerTypes) { throw null; }
        private static string CreateAttributeRoutingAggregateErrorMessage(System.Collections.Generic.IEnumerable<string> individualErrors) { throw null; }
        private static string CreateMixedRoutedActionDescriptorsErrorMessage(System.Reflection.MethodInfo method, System.Collections.Generic.List<System.ValueTuple<Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel, Microsoft.AspNetCore.Mvc.ApplicationModels.SelectorModel>> actions) { throw null; }
        public static System.Collections.Generic.List<TResult> Flatten<TResult>(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModel application, System.Func<Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModel, Microsoft.AspNetCore.Mvc.ApplicationModels.ControllerModel, Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel, Microsoft.AspNetCore.Mvc.ApplicationModels.SelectorModel, TResult> flattener) { throw null; }
        private static void ReplaceAttributeRouteTokens(Microsoft.AspNetCore.Mvc.ApplicationModels.ControllerModel controller, Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel action, Microsoft.AspNetCore.Mvc.ApplicationModels.SelectorModel selector, System.Collections.Generic.List<string> errors) { }
        private static void ValidateActionGroupConfiguration(System.Reflection.MethodInfo method, System.Collections.Generic.List<System.ValueTuple<Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel, Microsoft.AspNetCore.Mvc.ApplicationModels.SelectorModel>> actions, System.Collections.Generic.IDictionary<System.Reflection.MethodInfo, string> routingConfigurationErrors) { }
        private static System.Collections.Generic.List<string> ValidateNamedAttributeRoutedActions(System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<System.ValueTuple<Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel, Microsoft.AspNetCore.Mvc.ApplicationModels.SelectorModel>>> actionsByRouteName) { throw null; }
    }
    internal partial class ControllerActionDescriptorProvider
    {
        private readonly Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelFactory _applicationModelFactory;
        private readonly Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager _partManager;
        public ControllerActionDescriptorProvider(Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager partManager, Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelFactory applicationModelFactory) { }
        public int Order { get { throw null; } }
        private System.Collections.Generic.IEnumerable<System.Reflection.TypeInfo> GetControllerTypes() { throw null; }
        internal System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor> GetDescriptors() { throw null; }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptorProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptorProviderContext context) { }
    }
    internal partial class AuthorizationApplicationModelProvider : Microsoft.AspNetCore.Mvc.ApplicationModels.IApplicationModelProvider
    {
        private readonly Microsoft.AspNetCore.Mvc.MvcOptions _mvcOptions;
        private readonly Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider _policyProvider;
        public AuthorizationApplicationModelProvider(Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider policyProvider, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> mvcOptions) { }
        public int Order { get { throw null; } }
        public static Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter GetFilter(Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider policyProvider, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Authorization.IAuthorizeData> authData) { throw null; }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelProviderContext context) { }
    }
    internal partial class DefaultApplicationModelProvider : Microsoft.AspNetCore.Mvc.ApplicationModels.IApplicationModelProvider
    {
        private readonly Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider _modelMetadataProvider;
        private readonly Microsoft.AspNetCore.Mvc.MvcOptions _mvcOptions;
        private readonly System.Func<Microsoft.AspNetCore.Mvc.ActionContext, bool> _supportsAllRequests;
        private readonly System.Func<Microsoft.AspNetCore.Mvc.ActionContext, bool> _supportsNonGetRequests;
        public DefaultApplicationModelProvider(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> mvcOptionsAccessor, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider modelMetadataProvider) { }
        public int Order { get { throw null; } }
        private static void AddRange<T>(System.Collections.Generic.IList<T> list, System.Collections.Generic.IEnumerable<T> items) { }
        private string CanonicalizeActionName(string actionName) { throw null; }
        internal Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel CreateActionModel(System.Reflection.TypeInfo typeInfo, System.Reflection.MethodInfo methodInfo) { throw null; }
        internal Microsoft.AspNetCore.Mvc.ApplicationModels.ControllerModel CreateControllerModel(System.Reflection.TypeInfo typeInfo) { throw null; }
        internal Microsoft.AspNetCore.Mvc.ApplicationModels.ParameterModel CreateParameterModel(System.Reflection.ParameterInfo parameterInfo) { throw null; }
        internal Microsoft.AspNetCore.Mvc.ApplicationModels.PropertyModel CreatePropertyModel(System.Reflection.PropertyInfo propertyInfo) { throw null; }
        private static Microsoft.AspNetCore.Mvc.ApplicationModels.SelectorModel CreateSelectorModel(Microsoft.AspNetCore.Mvc.Routing.IRouteTemplateProvider route, System.Collections.Generic.IList<object> attributes) { throw null; }
        private System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ApplicationModels.SelectorModel> CreateSelectors(System.Collections.Generic.IList<object> attributes) { throw null; }
        private static bool InRouteProviders(System.Collections.Generic.List<Microsoft.AspNetCore.Mvc.Routing.IRouteTemplateProvider> routeProviders, object attribute) { throw null; }
        internal bool IsAction(System.Reflection.TypeInfo typeInfo, System.Reflection.MethodInfo methodInfo) { throw null; }
        private bool IsIDisposableMethod(System.Reflection.MethodInfo methodInfo) { throw null; }
        private bool IsSilentRouteAttribute(Microsoft.AspNetCore.Mvc.Routing.IRouteTemplateProvider routeTemplateProvider) { throw null; }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelProviderContext context) { }
    }
}
namespace Microsoft.AspNetCore.Mvc.Core
{
    internal static partial class Resources
    {
        private static System.Resources.ResourceManager s_resourceManager;
        private static System.Globalization.CultureInfo _Culture_k__BackingField;
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
        internal static System.Globalization.CultureInfo Culture { get { throw null; } set { } }
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
        internal static string GetResourceString(string resourceKey, string defaultValue = null) { throw null; }
        private static string GetResourceString(string resourceKey, string[] formatterNames) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.Controllers
{
    internal partial class DefaultControllerPropertyActivator : Microsoft.AspNetCore.Mvc.Controllers.IControllerPropertyActivator
    {
        private System.Collections.Concurrent.ConcurrentDictionary<System.Type, Microsoft.Extensions.Internal.PropertyActivator<Microsoft.AspNetCore.Mvc.ControllerContext>[]> _activateActions;
        private static readonly System.Func<System.Type, Microsoft.Extensions.Internal.PropertyActivator<Microsoft.AspNetCore.Mvc.ControllerContext>[]> _getPropertiesToActivate;
        private bool _initialized;
        private object _initializeLock;
        public DefaultControllerPropertyActivator() { }
        public void Activate(Microsoft.AspNetCore.Mvc.ControllerContext context, object controller) { }
        public System.Action<Microsoft.AspNetCore.Mvc.ControllerContext, object> GetActivatorDelegate(Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor actionDescriptor) { throw null; }
        private static Microsoft.Extensions.Internal.PropertyActivator<Microsoft.AspNetCore.Mvc.ControllerContext>[] GetPropertiesToActivate(System.Type type) { throw null; }
    }
    internal partial interface IControllerPropertyActivator
    {
        void Activate(Microsoft.AspNetCore.Mvc.ControllerContext context, object controller);
        System.Action<Microsoft.AspNetCore.Mvc.ControllerContext, object> GetActivatorDelegate(Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor actionDescriptor);
    }
}
namespace Microsoft.AspNetCore.Mvc.Filters
{
    internal partial class DefaultFilterProvider
    {
        public DefaultFilterProvider() { }
        public int Order { get { throw null; } }
        private void ApplyFilterToContainer(object actualFilter, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filterMetadata) { }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.Filters.FilterProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.Filters.FilterProviderContext context) { }
        public void ProvideFilter(Microsoft.AspNetCore.Mvc.Filters.FilterProviderContext context, Microsoft.AspNetCore.Mvc.Filters.FilterItem filterItem) { }
    }
    internal readonly partial struct FilterFactoryResult
    {
        private readonly object _dummy;
        public FilterFactoryResult(Microsoft.AspNetCore.Mvc.Filters.FilterItem[] cacheableFilters, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata[] filters) { throw null; }
        public Microsoft.AspNetCore.Mvc.Filters.FilterItem[] CacheableFilters { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata[] Filters { get { throw null; } }
    }
    internal static partial class FilterFactory
    {
        public static Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata[] CreateUncachedFilters(Microsoft.AspNetCore.Mvc.Filters.IFilterProvider[] filterProviders, Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.Filters.FilterItem[] cachedFilterItems) { throw null; }
        private static Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata[] CreateUncachedFiltersCore(Microsoft.AspNetCore.Mvc.Filters.IFilterProvider[] filterProviders, Microsoft.AspNetCore.Mvc.ActionContext actionContext, System.Collections.Generic.List<Microsoft.AspNetCore.Mvc.Filters.FilterItem> filterItems) { throw null; }
        public static Microsoft.AspNetCore.Mvc.Filters.FilterFactoryResult GetAllFilters(Microsoft.AspNetCore.Mvc.Filters.IFilterProvider[] filterProviders, Microsoft.AspNetCore.Mvc.ActionContext actionContext) { throw null; }
    }
    internal partial interface IResponseCacheFilter : Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata
    {
    }
    internal partial struct FilterCursor
    {
        private object _dummy;
        private int _dummyPrimitive;
        public FilterCursor(Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata[] filters) { throw null; }
        public Microsoft.AspNetCore.Mvc.Filters.FilterCursorItem<TFilter, TFilterAsync> GetNextFilter<TFilter, TFilterAsync>() where TFilter : class where TFilterAsync : class { throw null; }
        public void Reset() { }
    }
    internal partial class ResponseCacheFilterExecutor
    {
        private int? _cacheDuration;
        private Microsoft.AspNetCore.Mvc.ResponseCacheLocation? _cacheLocation;
        private bool? _cacheNoStore;
        private readonly Microsoft.AspNetCore.Mvc.CacheProfile _cacheProfile;
        private string _cacheVaryByHeader;
        private string[] _cacheVaryByQueryKeys;
        public ResponseCacheFilterExecutor(Microsoft.AspNetCore.Mvc.CacheProfile cacheProfile) { }
        public int Duration { get { throw null; } set { } }
        public Microsoft.AspNetCore.Mvc.ResponseCacheLocation Location { get { throw null; } set { } }
        public bool NoStore { get { throw null; } set { } }
        public string VaryByHeader { get { throw null; } set { } }
        public string[] VaryByQueryKeys { get { throw null; } set { } }
        public void Execute(Microsoft.AspNetCore.Mvc.Filters.FilterContext context) { }
    }
    internal readonly partial struct FilterCursorItem<TFilter, TFilterAsync>
    {
        private readonly TFilter _Filter_k__BackingField;
        private readonly TFilterAsync _FilterAsync_k__BackingField;
        private readonly int _dummyPrimitive;
        public FilterCursorItem(TFilter filter, TFilterAsync filterAsync) { throw null; }
        public TFilter Filter { get { throw null; } }
        public TFilterAsync FilterAsync { get { throw null; } }
    }
}
namespace Microsoft.AspNetCore.Mvc.Formatters
{
    internal static partial class MediaTypeHeaderValues
    {
        public static readonly Microsoft.Net.Http.Headers.MediaTypeHeaderValue ApplicationAnyJsonSyntax;
        public static readonly Microsoft.Net.Http.Headers.MediaTypeHeaderValue ApplicationAnyXmlSyntax;
        public static readonly Microsoft.Net.Http.Headers.MediaTypeHeaderValue ApplicationJson;
        public static readonly Microsoft.Net.Http.Headers.MediaTypeHeaderValue ApplicationJsonPatch;
        public static readonly Microsoft.Net.Http.Headers.MediaTypeHeaderValue ApplicationXml;
        public static readonly Microsoft.Net.Http.Headers.MediaTypeHeaderValue TextJson;
        public static readonly Microsoft.Net.Http.Headers.MediaTypeHeaderValue TextXml;
    }
    internal static partial class ResponseContentTypeHelper
    {
        public static void ResolveContentTypeAndEncoding(string actionResultContentType, string httpResponseContentType, string defaultContentType, out string resolvedContentType, out System.Text.Encoding resolvedContentTypeEncoding) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    internal abstract partial class ActionMethodExecutor
    {
        private static readonly Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor[] Executors;
        protected ActionMethodExecutor() { }
        protected abstract bool CanExecute(Microsoft.Extensions.Internal.ObjectMethodExecutor executor);
        private Microsoft.AspNetCore.Mvc.IActionResult ConvertToActionResult(Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper mapper, object returnValue, System.Type declaredType) { throw null; }
        private static void EnsureActionResultNotNull(Microsoft.Extensions.Internal.ObjectMethodExecutor executor, Microsoft.AspNetCore.Mvc.IActionResult actionResult) { }
        public abstract System.Threading.Tasks.ValueTask<Microsoft.AspNetCore.Mvc.IActionResult> Execute(Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper mapper, Microsoft.Extensions.Internal.ObjectMethodExecutor executor, object controller, object[] arguments);
        public static Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor GetExecutor(Microsoft.Extensions.Internal.ObjectMethodExecutor executor) { throw null; }
        private partial class AwaitableObjectResultExecutor : Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor
        {
            public AwaitableObjectResultExecutor() { }
            protected override bool CanExecute(Microsoft.Extensions.Internal.ObjectMethodExecutor executor) { throw null; }
            public override System.Threading.Tasks.ValueTask<Microsoft.AspNetCore.Mvc.IActionResult> Execute(Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper mapper, Microsoft.Extensions.Internal.ObjectMethodExecutor executor, object controller, object[] arguments) { throw null; }
        }
        private partial class AwaitableResultExecutor : Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor
        {
            public AwaitableResultExecutor() { }
            protected override bool CanExecute(Microsoft.Extensions.Internal.ObjectMethodExecutor executor) { throw null; }
            public override System.Threading.Tasks.ValueTask<Microsoft.AspNetCore.Mvc.IActionResult> Execute(Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper mapper, Microsoft.Extensions.Internal.ObjectMethodExecutor executor, object controller, object[] arguments) { throw null; }
        }
        private partial class SyncActionResultExecutor : Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor
        {
            public SyncActionResultExecutor() { }
            protected override bool CanExecute(Microsoft.Extensions.Internal.ObjectMethodExecutor executor) { throw null; }
            public override System.Threading.Tasks.ValueTask<Microsoft.AspNetCore.Mvc.IActionResult> Execute(Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper mapper, Microsoft.Extensions.Internal.ObjectMethodExecutor executor, object controller, object[] arguments) { throw null; }
        }
        private partial class SyncObjectResultExecutor : Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor
        {
            public SyncObjectResultExecutor() { }
            protected override bool CanExecute(Microsoft.Extensions.Internal.ObjectMethodExecutor executor) { throw null; }
            public override System.Threading.Tasks.ValueTask<Microsoft.AspNetCore.Mvc.IActionResult> Execute(Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper mapper, Microsoft.Extensions.Internal.ObjectMethodExecutor executor, object controller, object[] arguments) { throw null; }
        }
        private partial class TaskOfActionResultExecutor : Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor
        {
            public TaskOfActionResultExecutor() { }
            protected override bool CanExecute(Microsoft.Extensions.Internal.ObjectMethodExecutor executor) { throw null; }
            public override System.Threading.Tasks.ValueTask<Microsoft.AspNetCore.Mvc.IActionResult> Execute(Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper mapper, Microsoft.Extensions.Internal.ObjectMethodExecutor executor, object controller, object[] arguments) { throw null; }
        }
        private partial class TaskOfIActionResultExecutor : Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor
        {
            public TaskOfIActionResultExecutor() { }
            protected override bool CanExecute(Microsoft.Extensions.Internal.ObjectMethodExecutor executor) { throw null; }
            public override System.Threading.Tasks.ValueTask<Microsoft.AspNetCore.Mvc.IActionResult> Execute(Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper mapper, Microsoft.Extensions.Internal.ObjectMethodExecutor executor, object controller, object[] arguments) { throw null; }
        }
        private partial class TaskResultExecutor : Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor
        {
            public TaskResultExecutor() { }
            protected override bool CanExecute(Microsoft.Extensions.Internal.ObjectMethodExecutor executor) { throw null; }
            public override System.Threading.Tasks.ValueTask<Microsoft.AspNetCore.Mvc.IActionResult> Execute(Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper mapper, Microsoft.Extensions.Internal.ObjectMethodExecutor executor, object controller, object[] arguments) { throw null; }
        }
        private partial class VoidResultExecutor : Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor
        {
            public VoidResultExecutor() { }
            protected override bool CanExecute(Microsoft.Extensions.Internal.ObjectMethodExecutor executor) { throw null; }
            public override System.Threading.Tasks.ValueTask<Microsoft.AspNetCore.Mvc.IActionResult> Execute(Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper mapper, Microsoft.Extensions.Internal.ObjectMethodExecutor executor, object controller, object[] arguments) { throw null; }
        }
    }
    internal partial class ControllerActionInvokerCacheEntry
    {
        private readonly Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor _ActionMethodExecutor_k__BackingField;
        private readonly Microsoft.AspNetCore.Mvc.Filters.FilterItem[] _CachedFilters_k__BackingField;
        private readonly Microsoft.AspNetCore.Mvc.Controllers.ControllerBinderDelegate _ControllerBinderDelegate_k__BackingField;
        private readonly System.Func<Microsoft.AspNetCore.Mvc.ControllerContext, object> _ControllerFactory_k__BackingField;
        private readonly System.Action<Microsoft.AspNetCore.Mvc.ControllerContext, object> _ControllerReleaser_k__BackingField;
        private readonly Microsoft.Extensions.Internal.ObjectMethodExecutor _ObjectMethodExecutor_k__BackingField;
        internal ControllerActionInvokerCacheEntry(Microsoft.AspNetCore.Mvc.Filters.FilterItem[] cachedFilters, System.Func<Microsoft.AspNetCore.Mvc.ControllerContext, object> controllerFactory, System.Action<Microsoft.AspNetCore.Mvc.ControllerContext, object> controllerReleaser, Microsoft.AspNetCore.Mvc.Controllers.ControllerBinderDelegate controllerBinderDelegate, Microsoft.Extensions.Internal.ObjectMethodExecutor objectMethodExecutor, Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor actionMethodExecutor) { }
        internal Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor ActionMethodExecutor { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.FilterItem[] CachedFilters { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Controllers.ControllerBinderDelegate ControllerBinderDelegate { get { throw null; } }
        public System.Func<Microsoft.AspNetCore.Mvc.ControllerContext, object> ControllerFactory { get { throw null; } }
        public System.Action<Microsoft.AspNetCore.Mvc.ControllerContext, object> ControllerReleaser { get { throw null; } }
        internal Microsoft.Extensions.Internal.ObjectMethodExecutor ObjectMethodExecutor { get { throw null; } }
    }
    internal partial class ControllerActionInvokerCache
    {
        private readonly Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider _collectionProvider;
        private readonly Microsoft.AspNetCore.Mvc.Controllers.IControllerFactoryProvider _controllerFactoryProvider;
        private volatile Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvokerCache.InnerCache _currentCache;
        private readonly Microsoft.AspNetCore.Mvc.Filters.IFilterProvider[] _filterProviders;
        private readonly Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderFactory _modelBinderFactory;
        private readonly Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider _modelMetadataProvider;
        private readonly Microsoft.AspNetCore.Mvc.MvcOptions _mvcOptions;
        private readonly Microsoft.AspNetCore.Mvc.ModelBinding.ParameterBinder _parameterBinder;
        public ControllerActionInvokerCache(Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider collectionProvider, Microsoft.AspNetCore.Mvc.ModelBinding.ParameterBinder parameterBinder, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderFactory modelBinderFactory, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider modelMetadataProvider, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Filters.IFilterProvider> filterProviders, Microsoft.AspNetCore.Mvc.Controllers.IControllerFactoryProvider factoryProvider, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> mvcOptions) { }
        private Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvokerCache.InnerCache CurrentCache { get { throw null; } }
        public (Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvokerCacheEntry cacheEntry, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata[] filters) GetCachedResult(Microsoft.AspNetCore.Mvc.ControllerContext controllerContext) { throw null; }
        private partial class InnerCache
        {
            private readonly System.Collections.Concurrent.ConcurrentDictionary<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor, Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvokerCacheEntry> _Entries_k__BackingField;
            private readonly int _Version_k__BackingField;
            public InnerCache(int version) { }
            public System.Collections.Concurrent.ConcurrentDictionary<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor, Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvokerCacheEntry> Entries { get { throw null; } }
            public int Version { get { throw null; } }
        }
    }
    internal partial class MvcOptionsConfigureCompatibilityOptions : Microsoft.AspNetCore.Mvc.Infrastructure.ConfigureCompatibilityOptions<Microsoft.AspNetCore.Mvc.MvcOptions>
    {
        public MvcOptionsConfigureCompatibilityOptions(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.Infrastructure.MvcCompatibilityOptions> compatibilityOptions) : base(loggerFactory, compatibilityOptions) { }
        protected override System.Collections.Generic.IReadOnlyDictionary<string, object> DefaultValues { get { throw null; } }
    }
    internal partial class DefaultActionDescriptorCollectionProvider : Microsoft.AspNetCore.Mvc.Infrastructure.ActionDescriptorCollectionProvider
    {
        private readonly Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorChangeProvider[] _actionDescriptorChangeProviders;
        private readonly Microsoft.AspNetCore.Mvc.Abstractions.IActionDescriptorProvider[] _actionDescriptorProviders;
        private System.Threading.CancellationTokenSource _cancellationTokenSource;
        private Microsoft.Extensions.Primitives.IChangeToken _changeToken;
        private Microsoft.AspNetCore.Mvc.Infrastructure.ActionDescriptorCollection _collection;
        private readonly object _lock;
        private int _version;
        public DefaultActionDescriptorCollectionProvider(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Abstractions.IActionDescriptorProvider> actionDescriptorProviders, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorChangeProvider> actionDescriptorChangeProviders) { }
        public override Microsoft.AspNetCore.Mvc.Infrastructure.ActionDescriptorCollection ActionDescriptors { get { throw null; } }
        public override Microsoft.Extensions.Primitives.IChangeToken GetChangeToken() { throw null; }
        private Microsoft.Extensions.Primitives.IChangeToken GetCompositeChangeToken() { throw null; }
        private void Initialize() { }
        private void UpdateCollection() { }
    }
    internal partial class ControllerActionInvokerProvider
    {
        private readonly Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor _actionContextAccessor;
        private readonly Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvokerCache _controllerActionInvokerCache;
        private readonly System.Diagnostics.DiagnosticListener _diagnosticListener;
        private readonly Microsoft.Extensions.Logging.ILogger _logger;
        private readonly Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper _mapper;
        private readonly int _maxModelValidationErrors;
        private readonly System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ModelBinding.IValueProviderFactory> _valueProviderFactories;
        public ControllerActionInvokerProvider(Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvokerCache controllerActionInvokerCache, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> optionsAccessor, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper mapper) { }
        public ControllerActionInvokerProvider(Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvokerCache controllerActionInvokerCache, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> optionsAccessor, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper mapper, Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor actionContextAccessor) { }
        public int Order { get { throw null; } }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.Abstractions.ActionInvokerProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.Abstractions.ActionInvokerProviderContext context) { }
    }
    internal partial class ActionSelector : Microsoft.AspNetCore.Mvc.Infrastructure.IActionSelector
    {
        private readonly Microsoft.AspNetCore.Mvc.ActionConstraints.ActionConstraintCache _actionConstraintCache;
        private readonly Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;
        private Microsoft.AspNetCore.Mvc.Infrastructure.ActionSelectionTable<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor> _cache;
        private readonly Microsoft.Extensions.Logging.ILogger _logger;
        public ActionSelector(Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider actionDescriptorCollectionProvider, Microsoft.AspNetCore.Mvc.ActionConstraints.ActionConstraintCache actionConstraintCache, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        private Microsoft.AspNetCore.Mvc.Infrastructure.ActionSelectionTable<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor> Current { get { throw null; } }
        private System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor> EvaluateActionConstraints(Microsoft.AspNetCore.Routing.RouteContext context, System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor> actions) { throw null; }
        private System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ActionConstraints.ActionSelectorCandidate> EvaluateActionConstraintsCore(Microsoft.AspNetCore.Routing.RouteContext context, System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ActionConstraints.ActionSelectorCandidate> candidates, int? startingOrder) { throw null; }
        private System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor> SelectBestActions(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor> actions) { throw null; }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor SelectBestCandidate(Microsoft.AspNetCore.Routing.RouteContext context, System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor> candidates) { throw null; }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor> SelectCandidates(Microsoft.AspNetCore.Routing.RouteContext context) { throw null; }
    }
    internal partial class CopyOnWriteList<T> : System.Collections.Generic.IList<T>
    {
        private System.Collections.Generic.List<T> _copy;
        private readonly System.Collections.Generic.IReadOnlyList<T> _source;
        public CopyOnWriteList(System.Collections.Generic.IReadOnlyList<T> source) { }
        public int Count { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public T this[int index] { get { throw null; } set { } }
        private System.Collections.Generic.IReadOnlyList<T> Readable { get { throw null; } }
        private System.Collections.Generic.List<T> Writable { get { throw null; } }
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
    public partial class ActionContextAccessor : Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor
    {
        internal static readonly Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor Null;
    }
    internal static partial class ParameterDefaultValues
    {
        private static object GetParameterDefaultValue(System.Reflection.ParameterInfo parameterInfo) { throw null; }
        public static object[] GetParameterDefaultValues(System.Reflection.MethodInfo methodInfo) { throw null; }
        public static bool TryGetDeclaredParameterDefaultValue(System.Reflection.ParameterInfo parameterInfo, out object defaultValue) { throw null; }
    }
    internal partial interface ITypeActivatorCache
    {
        TInstance CreateInstance<TInstance>(System.IServiceProvider serviceProvider, System.Type optionType);
    }
    internal partial class NonDisposableStream : System.IO.Stream
    {
        private readonly System.IO.Stream _innerStream;
        public NonDisposableStream(System.IO.Stream innerStream) { }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanTimeout { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        private System.IO.Stream InnerStream { get { throw null; } }
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
    internal partial class ActionSelectionTable<TItem>
    {
        private readonly System.Collections.Generic.Dictionary<string[], System.Collections.Generic.List<TItem>> _OrdinalEntries_k__BackingField;
        private readonly System.Collections.Generic.Dictionary<string[], System.Collections.Generic.List<TItem>> _OrdinalIgnoreCaseEntries_k__BackingField;
        private readonly string[] _RouteKeys_k__BackingField;
        private readonly int _Version_k__BackingField;
        private ActionSelectionTable(int version, string[] routeKeys, System.Collections.Generic.Dictionary<string[], System.Collections.Generic.List<TItem>> ordinalEntries, System.Collections.Generic.Dictionary<string[], System.Collections.Generic.List<TItem>> ordinalIgnoreCaseEntries) { }
        private System.Collections.Generic.Dictionary<string[], System.Collections.Generic.List<TItem>> OrdinalEntries { get { throw null; } }
        private System.Collections.Generic.Dictionary<string[], System.Collections.Generic.List<TItem>> OrdinalIgnoreCaseEntries { get { throw null; } }
        private string[] RouteKeys { get { throw null; } }
        public int Version { get { throw null; } }
        public static Microsoft.AspNetCore.Mvc.Infrastructure.ActionSelectionTable<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor> Create(Microsoft.AspNetCore.Mvc.Infrastructure.ActionDescriptorCollection actions) { throw null; }
        public static Microsoft.AspNetCore.Mvc.Infrastructure.ActionSelectionTable<Microsoft.AspNetCore.Http.Endpoint> Create(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Http.Endpoint> endpoints) { throw null; }
        private static Microsoft.AspNetCore.Mvc.Infrastructure.ActionSelectionTable<T> CreateCore<T>(int version, System.Collections.Generic.IEnumerable<T> items, System.Func<T, System.Collections.Generic.IEnumerable<string>> getRouteKeys, System.Func<T, string, string> getRouteValue) { throw null; }
        public System.Collections.Generic.IReadOnlyList<TItem> Select(Microsoft.AspNetCore.Routing.RouteValueDictionary values) { throw null; }
    }
    internal abstract partial class ResourceInvoker
    {
        protected readonly Microsoft.AspNetCore.Mvc.ActionContext _actionContext;
        protected readonly Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor _actionContextAccessor;
        private Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.AuthorizationFilterContextSealed _authorizationContext;
        protected Microsoft.AspNetCore.Mvc.Filters.FilterCursor _cursor;
        protected readonly System.Diagnostics.DiagnosticListener _diagnosticListener;
        private Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.ExceptionContextSealed _exceptionContext;
        protected readonly Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata[] _filters;
        protected object _instance;
        protected readonly Microsoft.Extensions.Logging.ILogger _logger;
        protected readonly Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper _mapper;
        private Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.ResourceExecutedContextSealed _resourceExecutedContext;
        private Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.ResourceExecutingContextSealed _resourceExecutingContext;
        protected Microsoft.AspNetCore.Mvc.IActionResult _result;
        private Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.ResultExecutedContextSealed _resultExecutedContext;
        private Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.ResultExecutingContextSealed _resultExecutingContext;
        protected readonly System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.IValueProviderFactory> _valueProviderFactories;
        public ResourceInvoker(System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor actionContextAccessor, Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper mapper, Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata[] filters, System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.IValueProviderFactory> valueProviderFactories) { }
        private System.Threading.Tasks.Task InvokeAlwaysRunResultFilters() { throw null; }
        public virtual System.Threading.Tasks.Task InvokeAsync() { throw null; }
        private System.Threading.Tasks.Task InvokeFilterPipelineAsync() { throw null; }
        protected abstract System.Threading.Tasks.Task InvokeInnerFilterAsync();
        private System.Threading.Tasks.Task InvokeNextExceptionFilterAsync() { throw null; }
        private System.Threading.Tasks.Task InvokeNextResourceFilter() { throw null; }
        private System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.Filters.ResourceExecutedContext> InvokeNextResourceFilterAwaitedAsync() { throw null; }
        private System.Threading.Tasks.Task InvokeNextResultFilterAsync<TFilter, TFilterAsync>() where TFilter : class, Microsoft.AspNetCore.Mvc.Filters.IResultFilter where TFilterAsync : class, Microsoft.AspNetCore.Mvc.Filters.IAsyncResultFilter { throw null; }
        private System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.Filters.ResultExecutedContext> InvokeNextResultFilterAwaitedAsync<TFilter, TFilterAsync>() where TFilter : class, Microsoft.AspNetCore.Mvc.Filters.IResultFilter where TFilterAsync : class, Microsoft.AspNetCore.Mvc.Filters.IAsyncResultFilter { throw null; }
        protected virtual System.Threading.Tasks.Task InvokeResultAsync(Microsoft.AspNetCore.Mvc.IActionResult result) { throw null; }
        private System.Threading.Tasks.Task InvokeResultFilters() { throw null; }
        private System.Threading.Tasks.Task Next(ref Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.State next, ref Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.Scope scope, ref object state, ref bool isCompleted) { throw null; }
        protected abstract void ReleaseResources();
        private System.Threading.Tasks.Task ResultNext<TFilter, TFilterAsync>(ref Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.State next, ref Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.Scope scope, ref object state, ref bool isCompleted) where TFilter : class, Microsoft.AspNetCore.Mvc.Filters.IResultFilter where TFilterAsync : class, Microsoft.AspNetCore.Mvc.Filters.IAsyncResultFilter { throw null; }
        private static void Rethrow(Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.ExceptionContextSealed context) { }
        private static void Rethrow(Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.ResourceExecutedContextSealed context) { }
        private static void Rethrow(Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.ResultExecutedContextSealed context) { }
        private sealed partial class AuthorizationFilterContextSealed
        {
            public AuthorizationFilterContextSealed(Microsoft.AspNetCore.Mvc.ActionContext actionContext, System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata> filters) { }
        }
        private sealed partial class ExceptionContextSealed
        {
            public ExceptionContextSealed(Microsoft.AspNetCore.Mvc.ActionContext actionContext, System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata> filters) { }
        }
        private static partial class FilterTypeConstants
        {
            public const string ActionFilter = "Action Filter";
            public const string AlwaysRunResultFilter = "Always Run Result Filter";
            public const string AuthorizationFilter = "Authorization Filter";
            public const string ExceptionFilter = "Exception Filter";
            public const string ResourceFilter = "Resource Filter";
            public const string ResultFilter = "Result Filter";
        }
        private sealed partial class ResourceExecutedContextSealed
        {
            public ResourceExecutedContextSealed(Microsoft.AspNetCore.Mvc.ActionContext actionContext, System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata> filters) { }
        }
        private sealed partial class ResourceExecutingContextSealed
        {
            public ResourceExecutingContextSealed(Microsoft.AspNetCore.Mvc.ActionContext actionContext, System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata> filters, System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.IValueProviderFactory> valueProviderFactories) { }
        }
        private sealed partial class ResultExecutedContextSealed
        {
            public ResultExecutedContextSealed(Microsoft.AspNetCore.Mvc.ActionContext actionContext, System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata> filters, Microsoft.AspNetCore.Mvc.IActionResult result, object controller) { }
        }
        private sealed partial class ResultExecutingContextSealed
        {
            public ResultExecutingContextSealed(Microsoft.AspNetCore.Mvc.ActionContext actionContext, System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata> filters, Microsoft.AspNetCore.Mvc.IActionResult result, object controller) { }
        }
        private enum Scope
        {
            Invoker = 0,
            Resource = 1,
            Exception = 2,
            Result = 3,
        }
        private enum State
        {
            InvokeBegin = 0,
            AuthorizationBegin = 1,
            AuthorizationNext = 2,
            AuthorizationAsyncBegin = 3,
            AuthorizationAsyncEnd = 4,
            AuthorizationSync = 5,
            AuthorizationShortCircuit = 6,
            AuthorizationEnd = 7,
            ResourceBegin = 8,
            ResourceNext = 9,
            ResourceAsyncBegin = 10,
            ResourceAsyncEnd = 11,
            ResourceSyncBegin = 12,
            ResourceSyncEnd = 13,
            ResourceShortCircuit = 14,
            ResourceInside = 15,
            ResourceInsideEnd = 16,
            ResourceEnd = 17,
            ExceptionBegin = 18,
            ExceptionNext = 19,
            ExceptionAsyncBegin = 20,
            ExceptionAsyncResume = 21,
            ExceptionAsyncEnd = 22,
            ExceptionSyncBegin = 23,
            ExceptionSyncEnd = 24,
            ExceptionInside = 25,
            ExceptionHandled = 26,
            ExceptionEnd = 27,
            ActionBegin = 28,
            ActionEnd = 29,
            ResultBegin = 30,
            ResultNext = 31,
            ResultAsyncBegin = 32,
            ResultAsyncEnd = 33,
            ResultSyncBegin = 34,
            ResultSyncEnd = 35,
            ResultInside = 36,
            ResultEnd = 37,
            InvokeEnd = 38,
        }
    }
}
namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    internal static partial class PropertyValueSetter
    {
        private static readonly System.Reflection.MethodInfo CallPropertyAddRangeOpenGenericMethod;
        private static void CallPropertyAddRange<TElement>(object target, object source) { }
        public static void SetValue(Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata metadata, object instance, object value) { }
    }
    internal static partial class ModelBindingHelper
    {
        public static bool CanGetCompatibleCollection<T>(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { throw null; }
        internal static TModel CastOrDefault<TModel>(object model) { throw null; }
        public static void ClearValidationStateForModel(Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata modelMetadata, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState, string modelKey) { }
        public static void ClearValidationStateForModel(System.Type modelType, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, string modelKey) { }
        private static object ConvertSimpleType(object value, System.Type destinationType, System.Globalization.CultureInfo culture) { throw null; }
        public static object ConvertTo(object value, System.Type type, System.Globalization.CultureInfo culture) { throw null; }
        public static T ConvertTo<T>(object value, System.Globalization.CultureInfo culture) { throw null; }
        private static System.Collections.Generic.List<T> CreateList<T>(int? capacity) { throw null; }
        public static System.Collections.Generic.ICollection<T> GetCompatibleCollection<T>(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext) { throw null; }
        public static System.Collections.Generic.ICollection<T> GetCompatibleCollection<T>(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext, int capacity) { throw null; }
        private static System.Collections.Generic.ICollection<T> GetCompatibleCollection<T>(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext bindingContext, int? capacity) { throw null; }
        private static System.Linq.Expressions.Expression<System.Func<Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata, bool>> GetPredicateExpression<TModel>(System.Linq.Expressions.Expression<System.Func<TModel, object>> expression) { throw null; }
        public static System.Linq.Expressions.Expression<System.Func<Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata, bool>> GetPropertyFilterExpression<TModel>(System.Linq.Expressions.Expression<System.Func<TModel, object>>[] expressions) { throw null; }
        internal static string GetPropertyName(System.Linq.Expressions.Expression expression) { throw null; }
        public static System.Threading.Tasks.Task<bool> TryUpdateModelAsync(object model, System.Type modelType, string prefix, Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderFactory modelBinderFactory, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider valueProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IObjectModelValidator objectModelValidator) { throw null; }
        public static System.Threading.Tasks.Task<bool> TryUpdateModelAsync(object model, System.Type modelType, string prefix, Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderFactory modelBinderFactory, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider valueProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IObjectModelValidator objectModelValidator, System.Func<Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata, bool> propertyFilter) { throw null; }
        public static System.Threading.Tasks.Task<bool> TryUpdateModelAsync<TModel>(TModel model, string prefix, Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderFactory modelBinderFactory, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider valueProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IObjectModelValidator objectModelValidator) where TModel : class { throw null; }
        public static System.Threading.Tasks.Task<bool> TryUpdateModelAsync<TModel>(TModel model, string prefix, Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderFactory modelBinderFactory, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider valueProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IObjectModelValidator objectModelValidator, System.Func<Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata, bool> propertyFilter) where TModel : class { throw null; }
        public static System.Threading.Tasks.Task<bool> TryUpdateModelAsync<TModel>(TModel model, string prefix, Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderFactory modelBinderFactory, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider valueProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IObjectModelValidator objectModelValidator, params System.Linq.Expressions.Expression<System.Func<TModel, object>>[] includeExpressions) where TModel : class { throw null; }
        private static System.Type UnwrapNullableType(System.Type destinationType) { throw null; }
        private static object UnwrapPossibleArrayType(object value, System.Type destinationType, System.Globalization.CultureInfo culture) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation
{
    internal partial class DefaultModelValidatorProvider : Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IMetadataBasedModelValidatorProvider
    {
        public DefaultModelValidatorProvider() { }
        public void CreateValidators(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ModelValidatorProviderContext context) { }
        public bool HasValidators(System.Type modelType, System.Collections.Generic.IList<object> validatorMetadata) { throw null; }
    }
    internal partial class HasValidatorsValidationMetadataProvider : Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IMetadataDetailsProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IValidationMetadataProvider
    {
        private readonly bool _hasOnlyMetadataBasedValidators;
        private readonly Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IMetadataBasedModelValidatorProvider[] _validatorProviders;
        public HasValidatorsValidationMetadataProvider(System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IModelValidatorProvider> modelValidatorProviders) { }
        public void CreateValidationMetadata(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ValidationMetadataProviderContext context) { }
    }
}
namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata
{internal partial class DefaultBindingMetadataProvider : Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IBindingMetadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IMetadataDetailsProvider
    {
        public DefaultBindingMetadataProvider() { }
        public void CreateBindingMetadata(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.BindingMetadataProviderContext context) { }
        private static Microsoft.AspNetCore.Mvc.ModelBinding.BindingBehaviorAttribute FindBindingBehavior(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.BindingMetadataProviderContext context) { throw null; }
        private partial class CompositePropertyFilterProvider
        {
            private readonly System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ModelBinding.IPropertyFilterProvider> _providers;
            public CompositePropertyFilterProvider(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ModelBinding.IPropertyFilterProvider> providers) { }
            public System.Func<Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata, bool> PropertyFilter { get { throw null; } }
            private System.Func<Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata, bool> CreatePropertyFilter() { throw null; }
        }
    }
    internal partial class DefaultCompositeMetadataDetailsProvider : Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IBindingMetadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ICompositeMetadataDetailsProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IDisplayMetadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IMetadataDetailsProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IValidationMetadataProvider
    {
        private readonly System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IMetadataDetailsProvider> _providers;
        public DefaultCompositeMetadataDetailsProvider(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IMetadataDetailsProvider> providers) { }
        public void CreateBindingMetadata(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.BindingMetadataProviderContext context) { }
        public void CreateDisplayMetadata(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.DisplayMetadataProviderContext context) { }
        public void CreateValidationMetadata(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ValidationMetadataProviderContext context) { }
    }
    internal partial class DefaultValidationMetadataProvider : Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IMetadataDetailsProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IValidationMetadataProvider
    {
        public DefaultValidationMetadataProvider() { }
        public void CreateValidationMetadata(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ValidationMetadataProviderContext context) { }
    }
}
namespace Microsoft.AspNetCore.Internal
{
    internal partial class ChunkingCookieManager
    {
        private const string ChunkCountPrefix = "chunks-";
        private const string ChunkKeySuffix = "C";
        public const int DefaultChunkSize = 4050;
        private int? _ChunkSize_k__BackingField;
        private bool _ThrowForPartialCookies_k__BackingField;
        public ChunkingCookieManager() { }
        public int? ChunkSize { get { throw null; } set { } }
        public bool ThrowForPartialCookies { get { throw null; } set { } }
        public void AppendResponseCookie(Microsoft.AspNetCore.Http.HttpContext context, string key, string value, Microsoft.AspNetCore.Http.CookieOptions options) { }
        public void DeleteCookie(Microsoft.AspNetCore.Http.HttpContext context, string key, Microsoft.AspNetCore.Http.CookieOptions options) { }
        public string GetRequestCookie(Microsoft.AspNetCore.Http.HttpContext context, string key) { throw null; }
        private static int ParseChunksCount(string value) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc
{
    internal partial class ApiDescriptionActionData
    {
        private string _GroupName_k__BackingField;
        public ApiDescriptionActionData() { }
        public string GroupName { get { throw null; } set { } }
    }
}
namespace Microsoft.AspNetCore.Mvc.Routing
{
    internal static partial class ViewEnginePath
    {
        private const string CurrentDirectoryToken = ".";
        private const string ParentDirectoryToken = "..";
        public static readonly char[] PathSeparators;
        public static string CombinePath(string first, string second) { throw null; }
        public static string ResolvePath(string path) { throw null; }
    }
    internal static partial class NormalizedRouteValue
    {
        public static string GetNormalizedRouteValue(Microsoft.AspNetCore.Mvc.ActionContext context, string key) { throw null; }
    }
    internal partial class ControllerActionEndpointDataSource : Microsoft.AspNetCore.Mvc.Routing.ActionEndpointDataSourceBase
    {
        private readonly Microsoft.AspNetCore.Mvc.Routing.ActionEndpointFactory _endpointFactory;
        private int _order;
        private readonly System.Collections.Generic.List<Microsoft.AspNetCore.Mvc.Routing.ConventionalRouteEntry> _routes;
        private bool _CreateInertEndpoints_k__BackingField;
        private readonly Microsoft.AspNetCore.Builder.ControllerActionEndpointConventionBuilder _DefaultBuilder_k__BackingField;
        public ControllerActionEndpointDataSource(Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider actions, Microsoft.AspNetCore.Mvc.Routing.ActionEndpointFactory endpointFactory) : base (default(Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider)) { }
        public bool CreateInertEndpoints { get { throw null; } set { } }
        public Microsoft.AspNetCore.Builder.ControllerActionEndpointConventionBuilder DefaultBuilder { get { throw null; } }
        public Microsoft.AspNetCore.Builder.ControllerActionEndpointConventionBuilder AddRoute(string routeName, string pattern, Microsoft.AspNetCore.Routing.RouteValueDictionary defaults, System.Collections.Generic.IDictionary<string, object> constraints, Microsoft.AspNetCore.Routing.RouteValueDictionary dataTokens) { throw null; }
        protected override System.Collections.Generic.List<Microsoft.AspNetCore.Http.Endpoint> CreateEndpoints(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor> actions, System.Collections.Generic.IReadOnlyList<System.Action<Microsoft.AspNetCore.Builder.EndpointBuilder>> conventions) { throw null; }
    }
    internal readonly partial struct ConventionalRouteEntry
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public ConventionalRouteEntry(string routeName, string pattern, Microsoft.AspNetCore.Routing.RouteValueDictionary defaults, System.Collections.Generic.IDictionary<string, object> constraints, Microsoft.AspNetCore.Routing.RouteValueDictionary dataTokens, int order, System.Collections.Generic.List<System.Action<Microsoft.AspNetCore.Builder.EndpointBuilder>> conventions) { throw null; }
    }
    internal partial class ActionConstraintMatcherPolicy : Microsoft.AspNetCore.Routing.MatcherPolicy, Microsoft.AspNetCore.Routing.Matching.IEndpointSelectorPolicy
    {
        private static readonly System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> EmptyEndpoints;
        internal static readonly Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor NonAction;
        private readonly Microsoft.AspNetCore.Mvc.ActionConstraints.ActionConstraintCache _actionConstraintCache;
        public ActionConstraintMatcherPolicy(Microsoft.AspNetCore.Mvc.ActionConstraints.ActionConstraintCache actionConstraintCache) { }
        public override int Order { get { throw null; } }
        public bool AppliesToEndpoints(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> endpoints) { throw null; }
        public System.Threading.Tasks.Task ApplyAsync(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.Matching.CandidateSet candidateSet) { throw null; }
        private System.Collections.Generic.IReadOnlyList<System.ValueTuple<int, Microsoft.AspNetCore.Mvc.ActionConstraints.ActionSelectorCandidate>> EvaluateActionConstraints(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.Matching.CandidateSet candidateSet) { throw null; }
        private System.Collections.Generic.IReadOnlyList<System.ValueTuple<int, Microsoft.AspNetCore.Mvc.ActionConstraints.ActionSelectorCandidate>> EvaluateActionConstraintsCore(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.Matching.CandidateSet candidateSet, System.Collections.Generic.IReadOnlyList<System.ValueTuple<int, Microsoft.AspNetCore.Mvc.ActionConstraints.ActionSelectorCandidate>> items, int? startingOrder) { throw null; }
    }
    internal abstract partial class ActionEndpointDataSourceBase : Microsoft.AspNetCore.Routing.EndpointDataSource, System.IDisposable
    {
        protected readonly System.Collections.Generic.List<System.Action<Microsoft.AspNetCore.Builder.EndpointBuilder>> Conventions;
        protected readonly object Lock;
        private readonly Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider _actions;
        private System.Threading.CancellationTokenSource _cancellationTokenSource;
        private Microsoft.Extensions.Primitives.IChangeToken _changeToken;
        private System.IDisposable _disposable;
        private System.Collections.Generic.List<Microsoft.AspNetCore.Http.Endpoint> _endpoints;
        public ActionEndpointDataSourceBase(Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider actions) { }
        public override System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> Endpoints { get { throw null; } }
        protected abstract System.Collections.Generic.List<Microsoft.AspNetCore.Http.Endpoint> CreateEndpoints(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor> actions, System.Collections.Generic.IReadOnlyList<System.Action<Microsoft.AspNetCore.Builder.EndpointBuilder>> conventions);
        public void Dispose() { }
        public override Microsoft.Extensions.Primitives.IChangeToken GetChangeToken() { throw null; }
        private void Initialize() { }
        protected void Subscribe() { }
        private void UpdateEndpoints() { }
    }
    internal partial class ActionEndpointFactory
    {
        private readonly Microsoft.AspNetCore.Http.RequestDelegate _requestDelegate;
        private readonly Microsoft.AspNetCore.Routing.Patterns.RoutePatternTransformer _routePatternTransformer;
        public ActionEndpointFactory(Microsoft.AspNetCore.Routing.Patterns.RoutePatternTransformer routePatternTransformer) { }
        private void AddActionDataToBuilder(Microsoft.AspNetCore.Builder.EndpointBuilder builder, System.Collections.Generic.HashSet<string> routeNames, Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor action, string routeName, Microsoft.AspNetCore.Routing.RouteValueDictionary dataTokens, bool suppressLinkGeneration, bool suppressPathMatching, System.Collections.Generic.IReadOnlyList<System.Action<Microsoft.AspNetCore.Builder.EndpointBuilder>> conventions, System.Collections.Generic.IReadOnlyList<System.Action<Microsoft.AspNetCore.Builder.EndpointBuilder>> perRouteConventions) { }
        public void AddConventionalLinkGenerationRoute(System.Collections.Generic.List<Microsoft.AspNetCore.Http.Endpoint> endpoints, System.Collections.Generic.HashSet<string> routeNames, System.Collections.Generic.HashSet<string> keys, Microsoft.AspNetCore.Mvc.Routing.ConventionalRouteEntry route, System.Collections.Generic.IReadOnlyList<System.Action<Microsoft.AspNetCore.Builder.EndpointBuilder>> conventions) { }
        public void AddEndpoints(System.Collections.Generic.List<Microsoft.AspNetCore.Http.Endpoint> endpoints, System.Collections.Generic.HashSet<string> routeNames, Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor action, System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.Routing.ConventionalRouteEntry> routes, System.Collections.Generic.IReadOnlyList<System.Action<Microsoft.AspNetCore.Builder.EndpointBuilder>> conventions, bool createInertEndpoints) { }
        private static Microsoft.AspNetCore.Http.RequestDelegate CreateRequestDelegate() { throw null; }
        private static (Microsoft.AspNetCore.Routing.Patterns.RoutePattern resolvedRoutePattern, System.Collections.Generic.IDictionary<string, string> resolvedRequiredValues) ResolveDefaultsAndRequiredValues(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor action, Microsoft.AspNetCore.Routing.Patterns.RoutePattern attributeRoutePattern) { throw null; }
        private partial class InertEndpointBuilder : Microsoft.AspNetCore.Builder.EndpointBuilder
        {
            public InertEndpointBuilder() { }
            public override Microsoft.AspNetCore.Http.Endpoint Build() { throw null; }
        }
    }
    internal partial class DynamicControllerEndpointSelector
    {
        private readonly Microsoft.AspNetCore.Routing.DataSourceDependentCache<Microsoft.AspNetCore.Mvc.Infrastructure.ActionSelectionTable<Microsoft.AspNetCore.Http.Endpoint>> _cache;
        private readonly Microsoft.AspNetCore.Routing.EndpointDataSource _dataSource;
        public DynamicControllerEndpointSelector(Microsoft.AspNetCore.Mvc.Routing.ControllerActionEndpointDataSource dataSource) { }
        protected DynamicControllerEndpointSelector(Microsoft.AspNetCore.Routing.EndpointDataSource dataSource) { }
        private Microsoft.AspNetCore.Mvc.Infrastructure.ActionSelectionTable<Microsoft.AspNetCore.Http.Endpoint> Table { get { throw null; } }
        public void Dispose() { }
        private static Microsoft.AspNetCore.Mvc.Infrastructure.ActionSelectionTable<Microsoft.AspNetCore.Http.Endpoint> Initialize(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> endpoints) { throw null; }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> SelectEndpoints(Microsoft.AspNetCore.Routing.RouteValueDictionary values) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Routing
{
    internal sealed partial class DataSourceDependentCache<T> where T : class
    {
        private readonly Microsoft.AspNetCore.Routing.EndpointDataSource _dataSource;
        private System.IDisposable _disposable;
        private bool _disposed;
        private readonly System.Func<System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint>, T> _initializeCore;
        private bool _initialized;
        private readonly System.Func<T> _initializer;
        private readonly System.Action<object> _initializerWithState;
        private object _lock;
        private T _value;
        public DataSourceDependentCache(Microsoft.AspNetCore.Routing.EndpointDataSource dataSource, System.Func<System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint>, T> initialize) { }
        public T Value { get { throw null; } }
        public void Dispose() { }
        public T EnsureInitialized() { throw null; }
        private T Initialize() { throw null; }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    internal partial class ApiBehaviorOptionsSetup
    {
        private Microsoft.AspNetCore.Mvc.Infrastructure.ProblemDetailsFactory _problemDetailsFactory;
        public ApiBehaviorOptionsSetup() { }
        public void Configure(Microsoft.AspNetCore.Mvc.ApiBehaviorOptions options) { }
        internal static void ConfigureClientErrorMapping(Microsoft.AspNetCore.Mvc.ApiBehaviorOptions options) { }
        internal static Microsoft.AspNetCore.Mvc.IActionResult ProblemDetailsInvalidModelStateResponse(Microsoft.AspNetCore.Mvc.Infrastructure.ProblemDetailsFactory problemDetailsFactory, Microsoft.AspNetCore.Mvc.ActionContext context) { throw null; }
    }
    internal partial class MvcCoreRouteOptionsSetup
    {
        public MvcCoreRouteOptionsSetup() { }
        public void Configure(Microsoft.AspNetCore.Routing.RouteOptions options) { }
    }
    internal partial class MvcCoreBuilder : Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder
    {
        private readonly Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager _PartManager_k__BackingField;
        private readonly Microsoft.Extensions.DependencyInjection.IServiceCollection _Services_k__BackingField;
        public MvcCoreBuilder(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager manager) { }
        public Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager PartManager { get { throw null; } }
        public Microsoft.Extensions.DependencyInjection.IServiceCollection Services { get { throw null; } }
    }
    internal partial class MvcBuilder : Microsoft.Extensions.DependencyInjection.IMvcBuilder
    {
        private readonly Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager _PartManager_k__BackingField;
        private readonly Microsoft.Extensions.DependencyInjection.IServiceCollection _Services_k__BackingField;
        public MvcBuilder(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager manager) { }
        public Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager PartManager { get { throw null; } }
        public Microsoft.Extensions.DependencyInjection.IServiceCollection Services { get { throw null; } }
    }
}
namespace Microsoft.Extensions.Internal
{
    internal partial class CopyOnWriteDictionary<TKey, TValue> : System.Collections.Generic.IDictionary<TKey, TValue>
    {
        private readonly System.Collections.Generic.IEqualityComparer<TKey> _comparer;
        private System.Collections.Generic.IDictionary<TKey, TValue> _innerDictionary;
        private readonly System.Collections.Generic.IDictionary<TKey, TValue> _sourceDictionary;
        public CopyOnWriteDictionary(System.Collections.Generic.IDictionary<TKey, TValue> sourceDictionary, System.Collections.Generic.IEqualityComparer<TKey> comparer) { }
        public virtual int Count { get { throw null; } }
        public virtual bool IsReadOnly { get { throw null; } }
        public virtual TValue this[TKey key] { get { throw null; } set { } }
        public virtual System.Collections.Generic.ICollection<TKey> Keys { get { throw null; } }
        private System.Collections.Generic.IDictionary<TKey, TValue> ReadDictionary { get { throw null; } }
        public virtual System.Collections.Generic.ICollection<TValue> Values { get { throw null; } }
        private System.Collections.Generic.IDictionary<TKey, TValue> WriteDictionary { get { throw null; } }
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
    internal readonly partial struct AwaitableInfo
    {
        private readonly object _dummy;
        public AwaitableInfo(System.Type awaiterType, System.Reflection.PropertyInfo awaiterIsCompletedProperty, System.Reflection.MethodInfo awaiterGetResultMethod, System.Reflection.MethodInfo awaiterOnCompletedMethod, System.Reflection.MethodInfo awaiterUnsafeOnCompletedMethod, System.Type resultType, System.Reflection.MethodInfo getAwaiterMethod) { throw null; }
        public System.Reflection.MethodInfo AwaiterGetResultMethod { get { throw null; } }
        public System.Reflection.PropertyInfo AwaiterIsCompletedProperty { get { throw null; } }
        public System.Reflection.MethodInfo AwaiterOnCompletedMethod { get { throw null; } }
        public System.Type AwaiterType { get { throw null; } }
        public System.Reflection.MethodInfo AwaiterUnsafeOnCompletedMethod { get { throw null; } }
        public System.Reflection.MethodInfo GetAwaiterMethod { get { throw null; } }
        public System.Type ResultType { get { throw null; } }
        public static bool IsTypeAwaitable(System.Type type, out Microsoft.Extensions.Internal.AwaitableInfo awaitableInfo) { throw null; }
    }
    internal readonly partial struct CoercedAwaitableInfo
    {
        private readonly object _dummy;
        public CoercedAwaitableInfo(Microsoft.Extensions.Internal.AwaitableInfo awaitableInfo) { throw null; }
        public CoercedAwaitableInfo(System.Linq.Expressions.Expression coercerExpression, System.Type coercerResultType, Microsoft.Extensions.Internal.AwaitableInfo coercedAwaitableInfo) { throw null; }
        public Microsoft.Extensions.Internal.AwaitableInfo AwaitableInfo { get { throw null; } }
        public System.Linq.Expressions.Expression CoercerExpression { get { throw null; } }
        public System.Type CoercerResultType { get { throw null; } }
        public bool RequiresCoercion { get { throw null; } }
        public static bool IsTypeAwaitable(System.Type type, out Microsoft.Extensions.Internal.CoercedAwaitableInfo info) { throw null; }
    }
    internal partial class ObjectMethodExecutor
    {
        private readonly Microsoft.Extensions.Internal.ObjectMethodExecutor.MethodExecutor _executor;
        private readonly Microsoft.Extensions.Internal.ObjectMethodExecutor.MethodExecutorAsync _executorAsync;
        private static readonly System.Reflection.ConstructorInfo _objectMethodExecutorAwaitableConstructor;
        private readonly object[] _parameterDefaultValues;
        private readonly System.Type _AsyncResultType_k__BackingField;
        private readonly bool _IsMethodAsync_k__BackingField;
        private readonly System.Reflection.MethodInfo _MethodInfo_k__BackingField;
        private readonly System.Reflection.ParameterInfo[] _MethodParameters_k__BackingField;
        private System.Type _MethodReturnType_k__BackingField;
        private readonly System.Reflection.TypeInfo _TargetTypeInfo_k__BackingField;
        private ObjectMethodExecutor(System.Reflection.MethodInfo methodInfo, System.Reflection.TypeInfo targetTypeInfo, object[] parameterDefaultValues) { }
        public System.Type AsyncResultType { get { throw null; } }
        public bool IsMethodAsync { get { throw null; } }
        public System.Reflection.MethodInfo MethodInfo { get { throw null; } }
        public System.Reflection.ParameterInfo[] MethodParameters { get { throw null; } }
        public System.Type MethodReturnType { get { throw null; } internal set { } }
        public System.Reflection.TypeInfo TargetTypeInfo { get { throw null; } }
        public static Microsoft.Extensions.Internal.ObjectMethodExecutor Create(System.Reflection.MethodInfo methodInfo, System.Reflection.TypeInfo targetTypeInfo) { throw null; }
        public static Microsoft.Extensions.Internal.ObjectMethodExecutor Create(System.Reflection.MethodInfo methodInfo, System.Reflection.TypeInfo targetTypeInfo, object[] parameterDefaultValues) { throw null; }
        public object Execute(object target, object[] parameters) { throw null; }
        public Microsoft.Extensions.Internal.ObjectMethodExecutorAwaitable ExecuteAsync(object target, object[] parameters) { throw null; }
        public object GetDefaultValueForParameter(int index) { throw null; }
        private static Microsoft.Extensions.Internal.ObjectMethodExecutor.MethodExecutor GetExecutor(System.Reflection.MethodInfo methodInfo, System.Reflection.TypeInfo targetTypeInfo) { throw null; }
        private static Microsoft.Extensions.Internal.ObjectMethodExecutor.MethodExecutorAsync GetExecutorAsync(System.Reflection.MethodInfo methodInfo, System.Reflection.TypeInfo targetTypeInfo, Microsoft.Extensions.Internal.CoercedAwaitableInfo coercedAwaitableInfo) { throw null; }
        private static Microsoft.Extensions.Internal.ObjectMethodExecutor.MethodExecutor WrapVoidMethod(Microsoft.Extensions.Internal.ObjectMethodExecutor.VoidMethodExecutor executor) { throw null; }
        private delegate object MethodExecutor(object target, object[] parameters);
        private delegate Microsoft.Extensions.Internal.ObjectMethodExecutorAwaitable MethodExecutorAsync(object target, object[] parameters);
        private delegate void VoidMethodExecutor(object target, object[] parameters);
    }
    internal readonly partial struct ObjectMethodExecutorAwaitable
    {
        private readonly object _dummy;
        public ObjectMethodExecutorAwaitable(object customAwaitable, System.Func<object, object> getAwaiterMethod, System.Func<object, bool> isCompletedMethod, System.Func<object, object> getResultMethod, System.Action<object, System.Action> onCompletedMethod, System.Action<object, System.Action> unsafeOnCompletedMethod) { throw null; }
        public Microsoft.Extensions.Internal.ObjectMethodExecutorAwaitable.Awaiter GetAwaiter() { throw null; }
        public readonly partial struct Awaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion
        {
            private readonly object _dummy;
            public Awaiter(object customAwaiter, System.Func<object, bool> isCompletedMethod, System.Func<object, object> getResultMethod, System.Action<object, System.Action> onCompletedMethod, System.Action<object, System.Action> unsafeOnCompletedMethod) { throw null; }
            public bool IsCompleted { get { throw null; } }
            public object GetResult() { throw null; }
            public void OnCompleted(System.Action continuation) { }
            public void UnsafeOnCompleted(System.Action continuation) { }
        }
    }
    internal partial class PropertyActivator<TContext>
    {
        private readonly System.Action<object, object> _fastPropertySetter;
        private readonly System.Func<TContext, object> _valueAccessor;
        private System.Reflection.PropertyInfo _PropertyInfo_k__BackingField;
        public PropertyActivator(System.Reflection.PropertyInfo propertyInfo, System.Func<TContext, object> valueAccessor) { }
        public System.Reflection.PropertyInfo PropertyInfo { get { throw null; } private set { } }
        public object Activate(object instance, TContext context) { throw null; }
        public static Microsoft.Extensions.Internal.PropertyActivator<TContext>[] GetPropertiesToActivate(System.Type type, System.Type activateAttributeType, System.Func<System.Reflection.PropertyInfo, Microsoft.Extensions.Internal.PropertyActivator<TContext>> createActivateInfo) { throw null; }
        public static Microsoft.Extensions.Internal.PropertyActivator<TContext>[] GetPropertiesToActivate(System.Type type, System.Type activateAttributeType, System.Func<System.Reflection.PropertyInfo, Microsoft.Extensions.Internal.PropertyActivator<TContext>> createActivateInfo, bool includeNonPublic) { throw null; }
    }
    internal partial class PropertyHelper
    {
        private static readonly System.Reflection.MethodInfo CallNullSafePropertyGetterByReferenceOpenGenericMethod;
        private static readonly System.Reflection.MethodInfo CallNullSafePropertyGetterOpenGenericMethod;
        private static readonly System.Reflection.MethodInfo CallPropertyGetterByReferenceOpenGenericMethod;
        private static readonly System.Reflection.MethodInfo CallPropertyGetterOpenGenericMethod;
        private static readonly System.Reflection.MethodInfo CallPropertySetterOpenGenericMethod;
        private static readonly System.Type IsByRefLikeAttribute;
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<System.Type, Microsoft.Extensions.Internal.PropertyHelper[]> PropertiesCache;
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<System.Type, Microsoft.Extensions.Internal.PropertyHelper[]> VisiblePropertiesCache;
        private System.Func<object, object> _valueGetter;
        private System.Action<object, object> _valueSetter;
        private string _Name_k__BackingField;
        private readonly System.Reflection.PropertyInfo _Property_k__BackingField;
        public PropertyHelper(System.Reflection.PropertyInfo property) { }
        public virtual string Name { get { throw null; } protected set { } }
        public System.Reflection.PropertyInfo Property { get { throw null; } }
        public System.Func<object, object> ValueGetter { get { throw null; } }
        public System.Action<object, object> ValueSetter { get { throw null; } }
        private static object CallNullSafePropertyGetterByReference<TDeclaringType, TValue>(Microsoft.Extensions.Internal.PropertyHelper.ByRefFunc<TDeclaringType, TValue> getter, object target) { throw null; }
        private static object CallNullSafePropertyGetter<TDeclaringType, TValue>(System.Func<TDeclaringType, TValue> getter, object target) { throw null; }
        private static object CallPropertyGetterByReference<TDeclaringType, TValue>(Microsoft.Extensions.Internal.PropertyHelper.ByRefFunc<TDeclaringType, TValue> getter, object target) { throw null; }
        private static object CallPropertyGetter<TDeclaringType, TValue>(System.Func<TDeclaringType, TValue> getter, object target) { throw null; }
        private static void CallPropertySetter<TDeclaringType, TValue>(System.Action<TDeclaringType, TValue> setter, object target, object value) { }
        private static Microsoft.Extensions.Internal.PropertyHelper CreateInstance(System.Reflection.PropertyInfo property) { throw null; }
        public static Microsoft.Extensions.Internal.PropertyHelper[] GetProperties(System.Reflection.TypeInfo typeInfo) { throw null; }
        public static Microsoft.Extensions.Internal.PropertyHelper[] GetProperties(System.Type type) { throw null; }
        protected static Microsoft.Extensions.Internal.PropertyHelper[] GetProperties(System.Type type, System.Func<System.Reflection.PropertyInfo, Microsoft.Extensions.Internal.PropertyHelper> createPropertyHelper, System.Collections.Concurrent.ConcurrentDictionary<System.Type, Microsoft.Extensions.Internal.PropertyHelper[]> cache) { throw null; }
        public object GetValue(object instance) { throw null; }
        public static Microsoft.Extensions.Internal.PropertyHelper[] GetVisibleProperties(System.Reflection.TypeInfo typeInfo) { throw null; }
        public static Microsoft.Extensions.Internal.PropertyHelper[] GetVisibleProperties(System.Type type) { throw null; }
        protected static Microsoft.Extensions.Internal.PropertyHelper[] GetVisibleProperties(System.Type type, System.Func<System.Reflection.PropertyInfo, Microsoft.Extensions.Internal.PropertyHelper> createPropertyHelper, System.Collections.Concurrent.ConcurrentDictionary<System.Type, Microsoft.Extensions.Internal.PropertyHelper[]> allPropertiesCache, System.Collections.Concurrent.ConcurrentDictionary<System.Type, Microsoft.Extensions.Internal.PropertyHelper[]> visiblePropertiesCache) { throw null; }
        private static bool IsInterestingProperty(System.Reflection.PropertyInfo property) { throw null; }
        private static bool IsRefStructProperty(System.Reflection.PropertyInfo property) { throw null; }
        public static System.Func<object, object> MakeFastPropertyGetter(System.Reflection.PropertyInfo propertyInfo) { throw null; }
        private static System.Func<object, object> MakeFastPropertyGetter(System.Reflection.PropertyInfo propertyInfo, System.Reflection.MethodInfo propertyGetterWrapperMethod, System.Reflection.MethodInfo propertyGetterByRefWrapperMethod) { throw null; }
        private static System.Func<object, object> MakeFastPropertyGetter(System.Type openGenericDelegateType, System.Reflection.MethodInfo propertyGetMethod, System.Reflection.MethodInfo openGenericWrapperMethod) { throw null; }
        public static System.Action<object, object> MakeFastPropertySetter(System.Reflection.PropertyInfo propertyInfo) { throw null; }
        public static System.Func<object, object> MakeNullSafeFastPropertyGetter(System.Reflection.PropertyInfo propertyInfo) { throw null; }
        public static System.Collections.Generic.IDictionary<string, object> ObjectToDictionary(object value) { throw null; }
        public void SetValue(object instance, object value) { }
        private delegate TValue ByRefFunc<TDeclaringType, TValue>(ref TDeclaringType arg);
    }
}
namespace System.Text.Json
{
    internal static partial class JsonSerializerOptionsCopyConstructor
    {
        public static System.Text.Json.JsonSerializerOptions Copy(this System.Text.Json.JsonSerializerOptions serializerOptions, System.Text.Encodings.Web.JavaScriptEncoder encoder) { throw null; }
    }
}