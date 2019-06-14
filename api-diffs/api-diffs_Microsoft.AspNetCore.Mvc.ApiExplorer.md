# Microsoft.AspNetCore.Mvc.ApiExplorer

``` diff
 namespace Microsoft.AspNetCore.Mvc.ApiExplorer {
     public class DefaultApiDescriptionProvider : IApiDescriptionProvider {
-        public DefaultApiDescriptionProvider(IOptions<MvcOptions> optionsAccessor, IInlineConstraintResolver constraintResolver, IModelMetadataProvider modelMetadataProvider);

-        public DefaultApiDescriptionProvider(IOptions<MvcOptions> optionsAccessor, IInlineConstraintResolver constraintResolver, IModelMetadataProvider modelMetadataProvider, IActionResultTypeMapper mapper);

     }
 }
```

