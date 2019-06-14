# Microsoft.AspNetCore.Mvc.DataAnnotations

``` diff
 namespace Microsoft.AspNetCore.Mvc.DataAnnotations {
     public class MvcDataAnnotationsLocalizationOptions : IEnumerable, IEnumerable<ICompatibilitySwitch> {
-        public bool AllowDataAnnotationsLocalizationForEnumDisplayAttributes { get; set; }

-        public IEnumerator<ICompatibilitySwitch> GetEnumerator();

+        IEnumerator<ICompatibilitySwitch> System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch>.GetEnumerator();
     }
+    public sealed class RequiredAttributeAdapter : AttributeAdapterBase<RequiredAttribute> {
+        public RequiredAttributeAdapter(RequiredAttribute attribute, IStringLocalizer stringLocalizer);
+        public override void AddValidation(ClientModelValidationContext context);
+        public override string GetErrorMessage(ModelValidationContextBase validationContext);
+    }
 }
```

