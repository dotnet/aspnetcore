// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ApiExplorer
{
    internal partial class ApiResponseTypeProvider
    {
        public ApiResponseTypeProvider(Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider modelMetadataProvider, Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper mapper, Microsoft.AspNetCore.Mvc.MvcOptions mvcOptions) { }
        public System.Collections.Generic.ICollection<Microsoft.AspNetCore.Mvc.ApiExplorer.ApiResponseType> GetApiResponseTypes(Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor action) { throw null; }
    }

    internal partial class ApiParameterContext
    {
        public ApiParameterContext(Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider metadataProvider, Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor actionDescriptor, System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Routing.Template.TemplatePart> routeParameters) { }
        public Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider MetadataProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ApiExplorer.ApiParameterDescription> Results { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Routing.Template.TemplatePart> RouteParameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }

    public partial class DefaultApiDescriptionProvider : Microsoft.AspNetCore.Mvc.ApiExplorer.IApiDescriptionProvider
    {
        internal static void ProcessIsRequired(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiParameterContext context) { }
        internal static void ProcessParameterDefaultValue(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiParameterContext context) { }
    }
}