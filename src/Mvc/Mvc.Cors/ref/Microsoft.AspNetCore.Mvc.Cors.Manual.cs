// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Cors
{
    internal partial interface ICorsAuthorizationFilter : Microsoft.AspNetCore.Mvc.Filters.IAsyncAuthorizationFilter, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter
    {
    }
    internal partial class CorsHttpMethodActionConstraint : Microsoft.AspNetCore.Mvc.ActionConstraints.HttpMethodActionConstraint
    {
        public CorsHttpMethodActionConstraint(Microsoft.AspNetCore.Mvc.ActionConstraints.HttpMethodActionConstraint constraint) : base (default(System.Collections.Generic.IEnumerable<string>)) { }
        public override bool Accept(Microsoft.AspNetCore.Mvc.ActionConstraints.ActionConstraintContext context) { throw null; }
    }
    internal partial class DisableCorsAuthorizationFilter : Microsoft.AspNetCore.Mvc.Cors.ICorsAuthorizationFilter, Microsoft.AspNetCore.Mvc.Filters.IAsyncAuthorizationFilter, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter
    {
        public DisableCorsAuthorizationFilter() { }
        public int Order { get { throw null; } }
        public System.Threading.Tasks.Task OnAuthorizationAsync(Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext context) { throw null; }
    }
    internal partial class CorsApplicationModelProvider : Microsoft.AspNetCore.Mvc.ApplicationModels.IApplicationModelProvider
    {
        public CorsApplicationModelProvider(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> mvcOptions) { }
        public int Order { get { throw null; } }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelProviderContext context) { }
    }
    internal partial class CorsAuthorizationFilterFactory : Microsoft.AspNetCore.Mvc.Filters.IFilterFactory, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter
    {
        public CorsAuthorizationFilterFactory(string policyName) { }
        public bool IsReusable { get { throw null; } }
        public int Order { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata CreateInstance(System.IServiceProvider serviceProvider) { throw null; }
    }
}
