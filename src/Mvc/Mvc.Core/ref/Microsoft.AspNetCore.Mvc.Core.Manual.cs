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
    }
    internal partial class ActionConstraintCache
    {
        public ActionConstraintCache(Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider collectionProvider, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ActionConstraints.IActionConstraintProvider> actionConstraintProviders) { }
        internal Microsoft.AspNetCore.Mvc.ActionConstraints.ActionConstraintCache.InnerCache CurrentCache { get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ActionConstraints.IActionConstraint> GetActionConstraints(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor action) { throw null; }
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
        public ApiBehaviorApplicationModelProvider(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions> apiBehaviorOptions, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider modelMetadataProvider, Microsoft.AspNetCore.Mvc.Infrastructure.IClientErrorFactory clientErrorFactory, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public System.Collections.Generic.List<Microsoft.AspNetCore.Mvc.ApplicationModels.IActionModelConvention> ActionModelConventions { get { throw null; } }
        public int Order { get { throw null; } }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelProviderContext context) { }
    }
    internal partial class ApplicationModelFactory
    {
        public ApplicationModelFactory(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ApplicationModels.IApplicationModelProvider> applicationModelProviders, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> options) { }
        public Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModel CreateApplicationModel(System.Collections.Generic.IEnumerable<System.Reflection.TypeInfo> controllerTypes) { throw null; }
        public static System.Collections.Generic.List<TResult> Flatten<TResult>(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModel application, System.Func<Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModel, Microsoft.AspNetCore.Mvc.ApplicationModels.ControllerModel, Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel, Microsoft.AspNetCore.Mvc.ApplicationModels.SelectorModel, TResult> flattener) { throw null; }
    }
    internal partial class ControllerActionDescriptorProvider
    {
        public ControllerActionDescriptorProvider(Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager partManager, Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelFactory applicationModelFactory) { }
        public int Order { get { throw null; } }
        internal System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor> GetDescriptors() { throw null; }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptorProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptorProviderContext context) { }
    }
    internal partial class AuthorizationApplicationModelProvider : Microsoft.AspNetCore.Mvc.ApplicationModels.IApplicationModelProvider
    {
        public AuthorizationApplicationModelProvider(Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider policyProvider, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> mvcOptions) { }
        public int Order { get { throw null; } }
        public static Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter GetFilter(Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider policyProvider, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Authorization.IAuthorizeData> authData) { throw null; }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelProviderContext context) { }
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
    }
}
namespace Microsoft.AspNetCore.Mvc.Controllers
{
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
namespace Microsoft.AspNetCore.Mvc.Filters
{
    internal partial class DefaultFilterProvider
    {
        public DefaultFilterProvider() { }
        public int Order { get { throw null; } }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.Filters.FilterProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.Filters.FilterProviderContext context) { }
        public void ProvideFilter(Microsoft.AspNetCore.Mvc.Filters.FilterProviderContext context, Microsoft.AspNetCore.Mvc.Filters.FilterItem filterItem) { }
    }
    internal readonly partial struct FilterFactoryResult
    {
        public FilterFactoryResult(Microsoft.AspNetCore.Mvc.Filters.FilterItem[] cacheableFilters, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata[] filters) { throw null; }
        public Microsoft.AspNetCore.Mvc.Filters.FilterItem[] CacheableFilters { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata[] Filters { get { throw null; } }
    }
    internal static partial class FilterFactory
    {
        public static Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata[] CreateUncachedFilters(Microsoft.AspNetCore.Mvc.Filters.IFilterProvider[] filterProviders, Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.Filters.FilterItem[] cachedFilterItems) { throw null; }
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
        protected ActionMethodExecutor() { }
        protected abstract bool CanExecute(Microsoft.Extensions.Internal.ObjectMethodExecutor executor);
        public abstract System.Threading.Tasks.ValueTask<Microsoft.AspNetCore.Mvc.IActionResult> Execute(Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper mapper, Microsoft.Extensions.Internal.ObjectMethodExecutor executor, object controller, object[] arguments);
        public static Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor GetExecutor(Microsoft.Extensions.Internal.ObjectMethodExecutor executor) { throw null; }
    }
    internal partial class ControllerActionInvokerCacheEntry
    {
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
        public ControllerActionInvokerCache(Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider collectionProvider, Microsoft.AspNetCore.Mvc.ModelBinding.ParameterBinder parameterBinder, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderFactory modelBinderFactory, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider modelMetadataProvider, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Filters.IFilterProvider> filterProviders, Microsoft.AspNetCore.Mvc.Controllers.IControllerFactoryProvider factoryProvider, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> mvcOptions) { }
        public (Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvokerCacheEntry cacheEntry, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata[] filters) GetCachedResult(Microsoft.AspNetCore.Mvc.ControllerContext controllerContext) { throw null; }
    }
    internal partial class MvcOptionsConfigureCompatibilityOptions : Microsoft.AspNetCore.Mvc.Infrastructure.ConfigureCompatibilityOptions<Microsoft.AspNetCore.Mvc.MvcOptions>
    {
        public MvcOptionsConfigureCompatibilityOptions(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.Infrastructure.MvcCompatibilityOptions> compatibilityOptions) : base(loggerFactory, compatibilityOptions) { }
        protected override System.Collections.Generic.IReadOnlyDictionary<string, object> DefaultValues { get { throw null; } }
    }
    internal partial class DefaultActionDescriptorCollectionProvider : Microsoft.AspNetCore.Mvc.Infrastructure.ActionDescriptorCollectionProvider
    {
        public DefaultActionDescriptorCollectionProvider(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Abstractions.IActionDescriptorProvider> actionDescriptorProviders, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorChangeProvider> actionDescriptorChangeProviders) { }
        public override Microsoft.AspNetCore.Mvc.Infrastructure.ActionDescriptorCollection ActionDescriptors { get { throw null; } }
        public override Microsoft.Extensions.Primitives.IChangeToken GetChangeToken() { throw null; }
    }
    internal partial class ControllerActionInvokerProvider
    {
        public ControllerActionInvokerProvider(Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvokerCache controllerActionInvokerCache, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> optionsAccessor, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper mapper) { }
        public ControllerActionInvokerProvider(Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvokerCache controllerActionInvokerCache, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> optionsAccessor, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper mapper, Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor actionContextAccessor) { }
        public int Order { get { throw null; } }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.Abstractions.ActionInvokerProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.Abstractions.ActionInvokerProviderContext context) { }
    }
    internal partial class ActionSelector : Microsoft.AspNetCore.Mvc.Infrastructure.IActionSelector
    {
        public ActionSelector(Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider actionDescriptorCollectionProvider, Microsoft.AspNetCore.Mvc.ActionConstraints.ActionConstraintCache actionConstraintCache, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor SelectBestCandidate(Microsoft.AspNetCore.Routing.RouteContext context, System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor> candidates) { throw null; }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor> SelectCandidates(Microsoft.AspNetCore.Routing.RouteContext context) { throw null; }
    }
    internal partial class CopyOnWriteList<T> : System.Collections.Generic.IList<T>
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
    public partial class ActionContextAccessor : Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor
    {
        internal static readonly Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor Null;
    }
    internal static partial class ParameterDefaultValues
    {
        public static object[] GetParameterDefaultValues(System.Reflection.MethodInfo methodInfo) { throw null; }
        public static bool TryGetDeclaredParameterDefaultValue(System.Reflection.ParameterInfo parameterInfo, out object defaultValue) { throw null; }
    }
    internal partial interface ITypeActivatorCache
    {
        TInstance CreateInstance<TInstance>(System.IServiceProvider serviceProvider, System.Type optionType);
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
    internal partial class ActionSelectionTable<TItem>
    {
        public int Version { get { throw null; } }
        public static Microsoft.AspNetCore.Mvc.Infrastructure.ActionSelectionTable<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor> Create(Microsoft.AspNetCore.Mvc.Infrastructure.ActionDescriptorCollection actions) { throw null; }
        public static Microsoft.AspNetCore.Mvc.Infrastructure.ActionSelectionTable<Microsoft.AspNetCore.Http.Endpoint> Create(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Http.Endpoint> endpoints) { throw null; }
        public System.Collections.Generic.IReadOnlyList<TItem> Select(Microsoft.AspNetCore.Routing.RouteValueDictionary values) { throw null; }
    }
    internal abstract partial class ResourceInvoker
    {
        protected readonly Microsoft.AspNetCore.Mvc.ActionContext _actionContext;
        protected readonly Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor _actionContextAccessor;
        protected Microsoft.AspNetCore.Mvc.Filters.FilterCursor _cursor;
        protected readonly System.Diagnostics.DiagnosticListener _diagnosticListener;
        protected readonly Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata[] _filters;
        protected object _instance;
        protected readonly Microsoft.Extensions.Logging.ILogger _logger;
        protected readonly Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper _mapper;
        protected Microsoft.AspNetCore.Mvc.IActionResult _result;
        protected readonly System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.IValueProviderFactory> _valueProviderFactories;
        public ResourceInvoker(System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.Extensions.Logging.ILogger logger, Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor actionContextAccessor, Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper mapper, Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata[] filters, System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.IValueProviderFactory> valueProviderFactories) { }
        public virtual System.Threading.Tasks.Task InvokeAsync() { throw null; }
        protected abstract System.Threading.Tasks.Task InvokeInnerFilterAsync();
        protected virtual System.Threading.Tasks.Task InvokeResultAsync(Microsoft.AspNetCore.Mvc.IActionResult result) { throw null; }
        protected abstract void ReleaseResources();
    }
}
namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    internal static partial class PropertyValueSetter
    {
        public static void SetValue(Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata metadata, object instance, object value) { }
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
        public static System.Threading.Tasks.Task<bool> TryUpdateModelAsync(object model, System.Type modelType, string prefix, Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderFactory modelBinderFactory, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider valueProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IObjectModelValidator objectModelValidator, System.Func<Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata, bool> propertyFilter) { throw null; }
        public static System.Threading.Tasks.Task<bool> TryUpdateModelAsync<TModel>(TModel model, string prefix, Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderFactory modelBinderFactory, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider valueProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IObjectModelValidator objectModelValidator) where TModel : class { throw null; }
        public static System.Threading.Tasks.Task<bool> TryUpdateModelAsync<TModel>(TModel model, string prefix, Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderFactory modelBinderFactory, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider valueProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IObjectModelValidator objectModelValidator, System.Func<Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata, bool> propertyFilter) where TModel : class { throw null; }
        public static System.Threading.Tasks.Task<bool> TryUpdateModelAsync<TModel>(TModel model, string prefix, Microsoft.AspNetCore.Mvc.ActionContext actionContext, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderFactory modelBinderFactory, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider valueProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IObjectModelValidator objectModelValidator, params System.Linq.Expressions.Expression<System.Func<TModel, object>>[] includeExpressions) where TModel : class { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    public partial class ComplexTypeModelBinder : IModelBinder
    {
        internal const int NoDataAvailable = 0;
        internal const int GreedyPropertiesMayHaveData = 1;
        internal const int ValueProviderDataAvailable = 2;
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
        public HasValidatorsValidationMetadataProvider(System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IModelValidatorProvider> modelValidatorProviders) { }
        public void CreateValidationMetadata(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ValidationMetadataProviderContext context) { }
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
        public const int DefaultChunkSize = 4050;
        public ChunkingCookieManager() { }
        public int? ChunkSize { get { throw null; } set { } }
        public bool ThrowForPartialCookies { get { throw null; } set { } }
        public void AppendResponseCookie(Microsoft.AspNetCore.Http.HttpContext context, string key, string value, Microsoft.AspNetCore.Http.CookieOptions options) { }
        public void DeleteCookie(Microsoft.AspNetCore.Http.HttpContext context, string key, Microsoft.AspNetCore.Http.CookieOptions options) { }
        public string GetRequestCookie(Microsoft.AspNetCore.Http.HttpContext context, string key) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc
{
    internal partial class ApiDescriptionActionData
    {
        public ApiDescriptionActionData() { }
        public string GroupName { get { throw null; } set { } }
    }
}
namespace Microsoft.AspNetCore.Mvc.Routing
{
    internal static partial class ViewEnginePath
    {
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
    internal partial class DynamicControllerEndpointSelector
    {
        public DynamicControllerEndpointSelector(Microsoft.AspNetCore.Mvc.Routing.ControllerActionEndpointDataSource dataSource) { }
        protected DynamicControllerEndpointSelector(Microsoft.AspNetCore.Routing.EndpointDataSource dataSource) { }
        public void Dispose() { }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> SelectEndpoints(Microsoft.AspNetCore.Routing.RouteValueDictionary values) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Routing
{
    internal sealed partial class DataSourceDependentCache<T> where T : class
    {
        public DataSourceDependentCache(Microsoft.AspNetCore.Routing.EndpointDataSource dataSource, System.Func<System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint>, T> initialize) { }
        public T Value { get { throw null; } }
        public void Dispose() { }
        public T EnsureInitialized() { throw null; }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    internal partial class ApiBehaviorOptionsSetup
    {
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
        public MvcCoreBuilder(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager manager) { }
        public Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager PartManager { get { throw null; } }
        public Microsoft.Extensions.DependencyInjection.IServiceCollection Services { get { throw null; } }
    }
    internal partial class MvcBuilder : Microsoft.Extensions.DependencyInjection.IMvcBuilder
    {
        public MvcBuilder(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager manager) { }
        public Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager PartManager { get { throw null; } }
        public Microsoft.Extensions.DependencyInjection.IServiceCollection Services { get { throw null; } }
    }
}
namespace Microsoft.Extensions.Internal
{
    internal partial class CopyOnWriteDictionary<TKey, TValue> : System.Collections.Generic.IDictionary<TKey, TValue>
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
        public PropertyActivator(System.Reflection.PropertyInfo propertyInfo, System.Func<TContext, object> valueAccessor) { }
        public System.Reflection.PropertyInfo PropertyInfo { get { throw null; } }
        public object Activate(object instance, TContext context) { throw null; }
        public static Microsoft.Extensions.Internal.PropertyActivator<TContext>[] GetPropertiesToActivate(System.Type type, System.Type activateAttributeType, System.Func<System.Reflection.PropertyInfo, Microsoft.Extensions.Internal.PropertyActivator<TContext>> createActivateInfo) { throw null; }
        public static Microsoft.Extensions.Internal.PropertyActivator<TContext>[] GetPropertiesToActivate(System.Type type, System.Type activateAttributeType, System.Func<System.Reflection.PropertyInfo, Microsoft.Extensions.Internal.PropertyActivator<TContext>> createActivateInfo, bool includeNonPublic) { throw null; }
    }
    internal partial class PropertyHelper
    {
        public PropertyHelper(System.Reflection.PropertyInfo property) { }
        public virtual string Name { get { throw null; } protected set { } }
        public System.Reflection.PropertyInfo Property { get { throw null; } }
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
}
namespace System.Text.Json
{
    internal static partial class JsonSerializerOptionsCopyConstructor
    {
        public static System.Text.Json.JsonSerializerOptions Copy(this System.Text.Json.JsonSerializerOptions serializerOptions, System.Text.Encodings.Web.JavaScriptEncoder encoder) { throw null; }
    }
}