# Microsoft.AspNetCore.Mvc

``` diff
 namespace Microsoft.AspNetCore.Mvc {
     public class ApiBehaviorOptions : IEnumerable, IEnumerable<ICompatibilitySwitch> {
-        public bool AllowInferringBindingSourceForCollectionTypesAsFromQuery { get; set; }

-        public bool SuppressUseValidationProblemDetailsForInvalidModelStateResponses { get; set; }

     }
     public class ApiControllerAttribute : ControllerAttribute, IApiBehaviorMetadata, IFilterMetadata
     public enum CompatibilityVersion {
+        Version_3_0 = 3,
     }
     public class ConsumesAttribute : Attribute, IActionConstraint, IActionConstraintMetadata, IApiRequestMetadataProvider, IConsumesActionConstraint, IFilterMetadata, IResourceFilter
     public abstract class Controller : ControllerBase, IActionFilter, IAsyncActionFilter, IDisposable, IFilterMetadata {
-        public virtual JsonResult Json(object data, JsonSerializerSettings serializerSettings);

+        public virtual JsonResult Json(object data, object serializerSettings);
     }
     public class CookieTempDataProviderOptions {
-        public string CookieName { get; set; }

-        public string Domain { get; set; }

-        public string Path { get; set; }

     }
+    public class JsonOptions {
+        public JsonOptions();
+        public JsonSerializerOptions JsonSerializerOptions { get; }
+    }
-    public static class JsonPatchExtensions {
 {
-        public static void ApplyTo<T>(this JsonPatchDocument<T> patchDoc, T objectToApplyTo, ModelStateDictionary modelState) where T : class;

-        public static void ApplyTo<T>(this JsonPatchDocument<T> patchDoc, T objectToApplyTo, ModelStateDictionary modelState, string prefix) where T : class;

-    }
     public class JsonResult : ActionResult, IActionResult, IStatusCodeActionResult {
-        public JsonResult(object value, JsonSerializerSettings serializerSettings);

+        public JsonResult(object value, object serializerSettings);
-        public JsonSerializerSettings SerializerSettings { get; set; }
+        public object SerializerSettings { get; set; }
     }
     public class LocalRedirectResult : ActionResult {
-        public override void ExecuteResult(ActionContext context);

     }
-    public class MvcJsonOptions : IEnumerable, IEnumerable<ICompatibilitySwitch> {
 {
-        public MvcJsonOptions();

-        public bool AllowInputFormatterExceptionMessages { get; set; }

-        public JsonSerializerSettings SerializerSettings { get; }

-        IEnumerator<ICompatibilitySwitch> System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch>.GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-    }
     public class MvcOptions : IEnumerable, IEnumerable<ICompatibilitySwitch> {
-        public bool AllowBindingHeaderValuesToNonStringModelTypes { get; set; }

-        public bool AllowCombiningAuthorizeFilters { get; set; }

-        public bool AllowShortCircuitingValidationWhenNoValidatorsArePresent { get; set; }

-        public bool AllowValidatingTopLevelNodes { get; set; }

-        public InputFormatterExceptionPolicy InputFormatterExceptionPolicy { get; set; }

+        public int MaxModelBindingCollectionSize { get; set; }
+        public int MaxModelBindingRecursionDepth { get; set; }
+        public bool SuppressAsyncSuffixInActionNames { get; set; }
-        public bool SuppressBindingUndefinedValueToEnumType { get; set; }

+        public bool SuppressImplicitRequiredAttributeForNonNullableReferenceTypes { get; set; }
+        public bool SuppressOutputFormatterBuffering { get; set; }
+        public bool ValidateComplexTypesIfChildValidationFails { get; set; }
     }
     public class MvcViewOptions : IEnumerable, IEnumerable<ICompatibilitySwitch> {
-        public bool AllowRenderingMaxLengthAttribute { get; set; }

-        public bool SuppressTempDataAttributePrefix { get; set; }

     }
+    public class PageRemoteAttribute : RemoteAttributeBase {
+        public PageRemoteAttribute();
+        public string PageHandler { get; set; }
+        public string PageName { get; set; }
+        protected override string GetUrl(ClientModelValidationContext context);
+    }
     public class RedirectResult : ActionResult, IActionResult, IKeepTempDataResult {
-        public override void ExecuteResult(ActionContext context);

     }
     public class RedirectToActionResult : ActionResult, IActionResult, IKeepTempDataResult {
-        public override void ExecuteResult(ActionContext context);

     }
     public class RedirectToPageResult : ActionResult, IActionResult, IKeepTempDataResult {
-        public override void ExecuteResult(ActionContext context);

     }
     public class RedirectToRouteResult : ActionResult, IActionResult, IKeepTempDataResult {
-        public override void ExecuteResult(ActionContext context);

     }
-    public class RemoteAttribute : ValidationAttribute, IClientModelValidator {
+    public class RemoteAttribute : RemoteAttributeBase {
-        public string AdditionalFields { get; set; }

-        public string HttpMethod { get; set; }

-        protected RouteValueDictionary RouteData { get; }

-        public virtual void AddValidation(ClientModelValidationContext context);

-        public string FormatAdditionalFieldsForClientValidation(string property);

-        public override string FormatErrorMessage(string name);

-        public static string FormatPropertyForClientValidation(string property);

-        protected virtual string GetUrl(ClientModelValidationContext context);
+        protected override string GetUrl(ClientModelValidationContext context);
-        public override bool IsValid(object value);

     }
+    public abstract class RemoteAttributeBase : ValidationAttribute, IClientModelValidator {
+        protected RemoteAttributeBase();
+        public string AdditionalFields { get; set; }
+        public string HttpMethod { get; set; }
+        protected RouteValueDictionary RouteData { get; }
+        public virtual void AddValidation(ClientModelValidationContext context);
+        public string FormatAdditionalFieldsForClientValidation(string property);
+        public override string FormatErrorMessage(string name);
+        public static string FormatPropertyForClientValidation(string property);
+        protected abstract string GetUrl(ClientModelValidationContext context);
+        public override bool IsValid(object value);
+    }
     public static class UrlHelperExtensions {
+        public static string ActionLink(this IUrlHelper helper, string action = null, string controller = null, object values = null, string protocol = null, string host = null, string fragment = null);
+        public static string PageLink(this IUrlHelper urlHelper, string pageName = null, string pageHandler = null, object values = null, string protocol = null, string host = null, string fragment = null);
     }
 }
```

