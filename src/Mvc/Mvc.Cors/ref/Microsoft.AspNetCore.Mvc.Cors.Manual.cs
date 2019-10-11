// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Cors
{
    internal partial class CorsApplicationModelProvider : Microsoft.AspNetCore.Mvc.ApplicationModels.IApplicationModelProvider
    {
        private readonly Microsoft.AspNetCore.Mvc.MvcOptions _mvcOptions;
        public CorsApplicationModelProvider(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> mvcOptions) { }
        public int Order { get { throw null; } }
        private static void ConfigureCorsActionConstraint(Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel actionModel) { }
        private static void ConfigureCorsEndpointMetadata(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModel applicationModel) { }
        private static void ConfigureCorsFilters(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelProviderContext context) { }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelProviderContext context) { }
    }
}