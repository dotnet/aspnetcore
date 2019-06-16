# Microsoft.AspNetCore.Mvc.ApiExplorer

``` diff
 namespace Microsoft.AspNetCore.Mvc.ApiExplorer {
     public sealed class ApiConventionNameMatchAttribute : Attribute {
         public ApiConventionNameMatchAttribute(ApiConventionNameMatchBehavior matchBehavior);
         public ApiConventionNameMatchBehavior MatchBehavior { get; }
     }
     public enum ApiConventionNameMatchBehavior {
         Any = 0,
         Exact = 1,
         Prefix = 2,
         Suffix = 3,
     }
     public sealed class ApiConventionResult {
         public ApiConventionResult(IReadOnlyList<IApiResponseMetadataProvider> responseMetadataProviders);
         public IReadOnlyList<IApiResponseMetadataProvider> ResponseMetadataProviders { get; }
     }
     public sealed class ApiConventionTypeMatchAttribute : Attribute {
         public ApiConventionTypeMatchAttribute(ApiConventionTypeMatchBehavior matchBehavior);
         public ApiConventionTypeMatchBehavior MatchBehavior { get; }
     }
     public enum ApiConventionTypeMatchBehavior {
         Any = 0,
         AssignableFrom = 1,
     }
     public class ApiDescription {
         public ApiDescription();
         public ActionDescriptor ActionDescriptor { get; set; }
         public string GroupName { get; set; }
         public string HttpMethod { get; set; }
         public IList<ApiParameterDescription> ParameterDescriptions { get; }
         public IDictionary<object, object> Properties { get; }
         public string RelativePath { get; set; }
         public IList<ApiRequestFormat> SupportedRequestFormats { get; }
         public IList<ApiResponseType> SupportedResponseTypes { get; }
     }
     public static class ApiDescriptionExtensions {
         public static T GetProperty<T>(this ApiDescription apiDescription);
         public static void SetProperty<T>(this ApiDescription apiDescription, T value);
     }
     public class ApiDescriptionGroup {
         public ApiDescriptionGroup(string groupName, IReadOnlyList<ApiDescription> items);
         public string GroupName { get; }
         public IReadOnlyList<ApiDescription> Items { get; }
     }
     public class ApiDescriptionGroupCollection {
         public ApiDescriptionGroupCollection(IReadOnlyList<ApiDescriptionGroup> items, int version);
         public IReadOnlyList<ApiDescriptionGroup> Items { get; }
         public int Version { get; }
     }
     public class ApiDescriptionGroupCollectionProvider : IApiDescriptionGroupCollectionProvider {
         public ApiDescriptionGroupCollectionProvider(IActionDescriptorCollectionProvider actionDescriptorCollectionProvider, IEnumerable<IApiDescriptionProvider> apiDescriptionProviders);
         public ApiDescriptionGroupCollection ApiDescriptionGroups { get; }
     }
     public class ApiDescriptionProviderContext {
         public ApiDescriptionProviderContext(IReadOnlyList<ActionDescriptor> actions);
         public IReadOnlyList<ActionDescriptor> Actions { get; }
         public IList<ApiDescription> Results { get; }
     }
     public class ApiParameterDescription {
         public ApiParameterDescription();
         public object DefaultValue { get; set; }
         public bool IsRequired { get; set; }
         public ModelMetadata ModelMetadata { get; set; }
         public string Name { get; set; }
         public ParameterDescriptor ParameterDescriptor { get; set; }
         public ApiParameterRouteInfo RouteInfo { get; set; }
         public BindingSource Source { get; set; }
         public Type Type { get; set; }
     }
     public class ApiParameterRouteInfo {
         public ApiParameterRouteInfo();
         public IEnumerable<IRouteConstraint> Constraints { get; set; }
         public object DefaultValue { get; set; }
         public bool IsOptional { get; set; }
     }
     public class ApiRequestFormat {
         public ApiRequestFormat();
         public IInputFormatter Formatter { get; set; }
         public string MediaType { get; set; }
     }
     public class ApiResponseFormat {
         public ApiResponseFormat();
         public IOutputFormatter Formatter { get; set; }
         public string MediaType { get; set; }
     }
     public class ApiResponseType {
         public ApiResponseType();
         public IList<ApiResponseFormat> ApiResponseFormats { get; set; }
         public bool IsDefaultResponse { get; set; }
         public ModelMetadata ModelMetadata { get; set; }
         public int StatusCode { get; set; }
         public Type Type { get; set; }
     }
     public class DefaultApiDescriptionProvider : IApiDescriptionProvider {
-        public DefaultApiDescriptionProvider(IOptions<MvcOptions> optionsAccessor, IInlineConstraintResolver constraintResolver, IModelMetadataProvider modelMetadataProvider);

-        public DefaultApiDescriptionProvider(IOptions<MvcOptions> optionsAccessor, IInlineConstraintResolver constraintResolver, IModelMetadataProvider modelMetadataProvider, IActionResultTypeMapper mapper);

         public DefaultApiDescriptionProvider(IOptions<MvcOptions> optionsAccessor, IInlineConstraintResolver constraintResolver, IModelMetadataProvider modelMetadataProvider, IActionResultTypeMapper mapper, IOptions<RouteOptions> routeOptions);
         public int Order { get; }
         public void OnProvidersExecuted(ApiDescriptionProviderContext context);
         public void OnProvidersExecuting(ApiDescriptionProviderContext context);
     }
     public interface IApiDefaultResponseMetadataProvider : IApiResponseMetadataProvider, IFilterMetadata
     public interface IApiDescriptionGroupCollectionProvider {
         ApiDescriptionGroupCollection ApiDescriptionGroups { get; }
     }
     public interface IApiDescriptionGroupNameProvider {
         string GroupName { get; }
     }
     public interface IApiDescriptionProvider {
         int Order { get; }
         void OnProvidersExecuted(ApiDescriptionProviderContext context);
         void OnProvidersExecuting(ApiDescriptionProviderContext context);
     }
     public interface IApiDescriptionVisibilityProvider {
         bool IgnoreApi { get; }
     }
     public interface IApiRequestFormatMetadataProvider {
         IReadOnlyList<string> GetSupportedContentTypes(string contentType, Type objectType);
     }
     public interface IApiRequestMetadataProvider : IFilterMetadata {
         void SetContentTypes(MediaTypeCollection contentTypes);
     }
     public interface IApiResponseMetadataProvider : IFilterMetadata {
         int StatusCode { get; }
         Type Type { get; }
         void SetContentTypes(MediaTypeCollection contentTypes);
     }
     public interface IApiResponseTypeMetadataProvider {
         IReadOnlyList<string> GetSupportedContentTypes(string contentType, Type objectType);
     }
 }
```

