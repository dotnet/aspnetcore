// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ApiExplorer
{
    public static partial class ApiDescriptionExtensions
    {
        public static T GetProperty<T>(this Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescription apiDescription) { throw null; }
        public static void SetProperty<T>(this Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescription apiDescription, T value) { }
    }
    public partial class ApiDescriptionGroup
    {
        public ApiDescriptionGroup(string groupName, System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescription> items) { }
        public string GroupName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescription> Items { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class ApiDescriptionGroupCollection
    {
        public ApiDescriptionGroupCollection(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescriptionGroup> items, int version) { }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescriptionGroup> Items { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public int Version { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class ApiDescriptionGroupCollectionProvider : Microsoft.AspNetCore.Mvc.ApiExplorer.IApiDescriptionGroupCollectionProvider
    {
        public ApiDescriptionGroupCollectionProvider(Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider actionDescriptorCollectionProvider, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ApiExplorer.IApiDescriptionProvider> apiDescriptionProviders) { }
        public Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescriptionGroupCollection ApiDescriptionGroups { get { throw null; } }
    }
    public partial class DefaultApiDescriptionProvider : Microsoft.AspNetCore.Mvc.ApiExplorer.IApiDescriptionProvider
    {
        public DefaultApiDescriptionProvider(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> optionsAccessor, Microsoft.AspNetCore.Routing.IInlineConstraintResolver constraintResolver, Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider modelMetadataProvider, Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultTypeMapper mapper, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Routing.RouteOptions> routeOptions) { }
        public int Order { get { throw null; } }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescriptionProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescriptionProviderContext context) { }
    }
    public partial interface IApiDescriptionGroupCollectionProvider
    {
        Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescriptionGroupCollection ApiDescriptionGroups { get; }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class MvcApiExplorerMvcCoreBuilderExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddApiExplorer(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder) { throw null; }
    }
}
