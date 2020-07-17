// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public sealed partial class PageActionEndpointConventionBuilder : Microsoft.AspNetCore.Builder.IEndpointConventionBuilder
    {
        internal PageActionEndpointConventionBuilder() { }
        public void Add(System.Action<Microsoft.AspNetCore.Builder.EndpointBuilder> convention) { }
    }
    public static partial class RazorPagesEndpointRouteBuilderExtensions
    {
        public static void MapDynamicPageRoute<TTransformer>(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string pattern) where TTransformer : Microsoft.AspNetCore.Mvc.Routing.DynamicRouteValueTransformer { }
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapFallbackToAreaPage(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string page, string area) { throw null; }
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapFallbackToAreaPage(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string pattern, string page, string area) { throw null; }
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapFallbackToPage(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string page) { throw null; }
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapFallbackToPage(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string pattern, string page) { throw null; }
        public static Microsoft.AspNetCore.Builder.PageActionEndpointConventionBuilder MapRazorPages(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    public partial interface IPageApplicationModelConvention : Microsoft.AspNetCore.Mvc.ApplicationModels.IPageConvention
    {
        void Apply(Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModel model);
    }
    public partial interface IPageApplicationModelPartsProvider
    {
        Microsoft.AspNetCore.Mvc.ApplicationModels.PageHandlerModel CreateHandlerModel(System.Reflection.MethodInfo method);
        Microsoft.AspNetCore.Mvc.ApplicationModels.PageParameterModel CreateParameterModel(System.Reflection.ParameterInfo parameter);
        Microsoft.AspNetCore.Mvc.ApplicationModels.PagePropertyModel CreatePropertyModel(System.Reflection.PropertyInfo property);
        bool IsHandler(System.Reflection.MethodInfo methodInfo);
    }
    public partial interface IPageApplicationModelProvider
    {
        int Order { get; }
        void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModelProviderContext context);
        void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModelProviderContext context);
    }
    public partial interface IPageConvention
    {
    }
    public partial interface IPageHandlerModelConvention : Microsoft.AspNetCore.Mvc.ApplicationModels.IPageConvention
    {
        void Apply(Microsoft.AspNetCore.Mvc.ApplicationModels.PageHandlerModel model);
    }
    public partial interface IPageRouteModelConvention : Microsoft.AspNetCore.Mvc.ApplicationModels.IPageConvention
    {
        void Apply(Microsoft.AspNetCore.Mvc.ApplicationModels.PageRouteModel model);
    }
    public partial interface IPageRouteModelProvider
    {
        int Order { get; }
        void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.ApplicationModels.PageRouteModelProviderContext context);
        void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.ApplicationModels.PageRouteModelProviderContext context);
    }
    public partial class PageApplicationModel
    {
        public PageApplicationModel(Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModel other) { }
        public PageApplicationModel(Microsoft.AspNetCore.Mvc.RazorPages.PageActionDescriptor actionDescriptor, System.Reflection.TypeInfo handlerType, System.Collections.Generic.IReadOnlyList<object> handlerAttributes) { }
        public PageApplicationModel(Microsoft.AspNetCore.Mvc.RazorPages.PageActionDescriptor actionDescriptor, System.Reflection.TypeInfo declaredModelType, System.Reflection.TypeInfo handlerType, System.Collections.Generic.IReadOnlyList<object> handlerAttributes) { }
        public Microsoft.AspNetCore.Mvc.RazorPages.PageActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string AreaName { get { throw null; } }
        public System.Reflection.TypeInfo DeclaredModelType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IList<object> EndpointMetadata { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata> Filters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ApplicationModels.PageHandlerModel> HandlerMethods { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ApplicationModels.PagePropertyModel> HandlerProperties { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Reflection.TypeInfo HandlerType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<object> HandlerTypeAttributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Reflection.TypeInfo ModelType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Reflection.TypeInfo PageType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IDictionary<object, object> Properties { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string RelativePath { get { throw null; } }
        public string RouteTemplate { get { throw null; } }
        public string ViewEnginePath { get { throw null; } }
    }
    public partial class PageApplicationModelProviderContext
    {
        public PageApplicationModelProviderContext(Microsoft.AspNetCore.Mvc.RazorPages.PageActionDescriptor descriptor, System.Reflection.TypeInfo pageTypeInfo) { }
        public Microsoft.AspNetCore.Mvc.RazorPages.PageActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModel PageApplicationModel { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Reflection.TypeInfo PageType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class PageConventionCollection : System.Collections.ObjectModel.Collection<Microsoft.AspNetCore.Mvc.ApplicationModels.IPageConvention>
    {
        public PageConventionCollection() { }
        public PageConventionCollection(System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ApplicationModels.IPageConvention> conventions) { }
        public Microsoft.AspNetCore.Mvc.ApplicationModels.IPageApplicationModelConvention AddAreaFolderApplicationModelConvention(string areaName, string folderPath, System.Action<Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModel> action) { throw null; }
        public Microsoft.AspNetCore.Mvc.ApplicationModels.IPageRouteModelConvention AddAreaFolderRouteModelConvention(string areaName, string folderPath, System.Action<Microsoft.AspNetCore.Mvc.ApplicationModels.PageRouteModel> action) { throw null; }
        public Microsoft.AspNetCore.Mvc.ApplicationModels.IPageApplicationModelConvention AddAreaPageApplicationModelConvention(string areaName, string pageName, System.Action<Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModel> action) { throw null; }
        public Microsoft.AspNetCore.Mvc.ApplicationModels.IPageRouteModelConvention AddAreaPageRouteModelConvention(string areaName, string pageName, System.Action<Microsoft.AspNetCore.Mvc.ApplicationModels.PageRouteModel> action) { throw null; }
        public Microsoft.AspNetCore.Mvc.ApplicationModels.IPageApplicationModelConvention AddFolderApplicationModelConvention(string folderPath, System.Action<Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModel> action) { throw null; }
        public Microsoft.AspNetCore.Mvc.ApplicationModels.IPageRouteModelConvention AddFolderRouteModelConvention(string folderPath, System.Action<Microsoft.AspNetCore.Mvc.ApplicationModels.PageRouteModel> action) { throw null; }
        public Microsoft.AspNetCore.Mvc.ApplicationModels.IPageApplicationModelConvention AddPageApplicationModelConvention(string pageName, System.Action<Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModel> action) { throw null; }
        public Microsoft.AspNetCore.Mvc.ApplicationModels.IPageRouteModelConvention AddPageRouteModelConvention(string pageName, System.Action<Microsoft.AspNetCore.Mvc.ApplicationModels.PageRouteModel> action) { throw null; }
        public void RemoveType(System.Type pageConventionType) { }
        public void RemoveType<TPageConvention>() where TPageConvention : Microsoft.AspNetCore.Mvc.ApplicationModels.IPageConvention { }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("PageHandlerModel: Name={Name}")]
    public partial class PageHandlerModel : Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel, Microsoft.AspNetCore.Mvc.ApplicationModels.IPropertyModel
    {
        public PageHandlerModel(Microsoft.AspNetCore.Mvc.ApplicationModels.PageHandlerModel other) { }
        public PageHandlerModel(System.Reflection.MethodInfo handlerMethod, System.Collections.Generic.IReadOnlyList<object> attributes) { }
        public System.Collections.Generic.IReadOnlyList<object> Attributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string HandlerName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string HttpMethod { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Reflection.MethodInfo MethodInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        System.Reflection.MemberInfo Microsoft.AspNetCore.Mvc.ApplicationModels.ICommonModel.MemberInfo { get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModel Page { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ApplicationModels.PageParameterModel> Parameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IDictionary<object, object> Properties { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public sealed partial class PageRouteMetadata
    {
        public PageRouteMetadata(string pageRoute, string routeTemplate) { }
        public string PageRoute { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string RouteTemplate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class PageRouteModel
    {
        public PageRouteModel(Microsoft.AspNetCore.Mvc.ApplicationModels.PageRouteModel other) { }
        public PageRouteModel(string relativePath, string viewEnginePath) { }
        public PageRouteModel(string relativePath, string viewEnginePath, string areaName) { }
        public string AreaName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IDictionary<object, object> Properties { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string RelativePath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Routing.IOutboundParameterTransformer RouteParameterTransformer { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IDictionary<string, string> RouteValues { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ApplicationModels.SelectorModel> Selectors { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string ViewEnginePath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class PageRouteModelProviderContext
    {
        public PageRouteModelProviderContext() { }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ApplicationModels.PageRouteModel> RouteModels { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class PageRouteTransformerConvention : Microsoft.AspNetCore.Mvc.ApplicationModels.IPageConvention, Microsoft.AspNetCore.Mvc.ApplicationModels.IPageRouteModelConvention
    {
        public PageRouteTransformerConvention(Microsoft.AspNetCore.Routing.IOutboundParameterTransformer parameterTransformer) { }
        public void Apply(Microsoft.AspNetCore.Mvc.ApplicationModels.PageRouteModel model) { }
        protected virtual bool ShouldApply(Microsoft.AspNetCore.Mvc.ApplicationModels.PageRouteModel action) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.Diagnostics
{
    public sealed partial class AfterHandlerMethodEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.AfterHandlerMethod";
        public AfterHandlerMethodEventData(Microsoft.AspNetCore.Mvc.ActionContext actionContext, System.Collections.Generic.IReadOnlyDictionary<string, object> arguments, Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.HandlerMethodDescriptor handlerMethodDescriptor, object instance, Microsoft.AspNetCore.Mvc.IActionResult result) { }
        public Microsoft.AspNetCore.Mvc.ActionContext ActionContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IReadOnlyDictionary<string, object> Arguments { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.HandlerMethodDescriptor HandlerMethodDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public object Instance { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.IActionResult Result { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public sealed partial class AfterPageFilterOnPageHandlerExecutedEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.AfterOnPageHandlerExecuted";
        public AfterPageFilterOnPageHandlerExecutedEventData(Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutedContext handlerExecutedContext, Microsoft.AspNetCore.Mvc.Filters.IPageFilter filter) { }
        public Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IPageFilter Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutedContext HandlerExecutedContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
    }
    public sealed partial class AfterPageFilterOnPageHandlerExecutingEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.AfterOnPageHandlerExecuting";
        public AfterPageFilterOnPageHandlerExecutingEventData(Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutingContext handlerExecutingContext, Microsoft.AspNetCore.Mvc.Filters.IPageFilter filter) { }
        public Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IPageFilter Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutingContext HandlerExecutingContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
    }
    public sealed partial class AfterPageFilterOnPageHandlerExecutionEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.AfterOnPageHandlerExecution";
        public AfterPageFilterOnPageHandlerExecutionEventData(Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutedContext handlerExecutedContext, Microsoft.AspNetCore.Mvc.Filters.IAsyncPageFilter filter) { }
        public Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IAsyncPageFilter Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutedContext HandlerExecutedContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
    }
    public sealed partial class AfterPageFilterOnPageHandlerSelectedEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.AfterOnPageHandlerSelected";
        public AfterPageFilterOnPageHandlerSelectedEventData(Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.Filters.PageHandlerSelectedContext handlerSelectedContext, Microsoft.AspNetCore.Mvc.Filters.IPageFilter filter) { }
        public Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IPageFilter Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.PageHandlerSelectedContext HandlerSelectedContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
    }
    public sealed partial class AfterPageFilterOnPageHandlerSelectionEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.AfterOnPageHandlerSelection";
        public AfterPageFilterOnPageHandlerSelectionEventData(Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.Filters.PageHandlerSelectedContext handlerSelectedContext, Microsoft.AspNetCore.Mvc.Filters.IAsyncPageFilter filter) { }
        public Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IAsyncPageFilter Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.PageHandlerSelectedContext HandlerSelectedContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
    }
    public sealed partial class BeforeHandlerMethodEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.BeforeHandlerMethod";
        public BeforeHandlerMethodEventData(Microsoft.AspNetCore.Mvc.ActionContext actionContext, System.Collections.Generic.IReadOnlyDictionary<string, object> arguments, Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.HandlerMethodDescriptor handlerMethodDescriptor, object instance) { }
        public Microsoft.AspNetCore.Mvc.ActionContext ActionContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IReadOnlyDictionary<string, object> Arguments { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.HandlerMethodDescriptor HandlerMethodDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public object Instance { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
    }
    public sealed partial class BeforePageFilterOnPageHandlerExecutedEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.BeforeOnPageHandlerExecuted";
        public BeforePageFilterOnPageHandlerExecutedEventData(Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutedContext handlerExecutedContext, Microsoft.AspNetCore.Mvc.Filters.IPageFilter filter) { }
        public Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IPageFilter Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutedContext HandlerExecutedContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
    }
    public sealed partial class BeforePageFilterOnPageHandlerExecutingEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.BeforeOnPageHandlerExecuting";
        public BeforePageFilterOnPageHandlerExecutingEventData(Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutingContext handlerExecutingContext, Microsoft.AspNetCore.Mvc.Filters.IPageFilter filter) { }
        public Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IPageFilter Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutingContext HandlerExecutingContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
    }
    public sealed partial class BeforePageFilterOnPageHandlerExecutionEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.BeforeOnPageHandlerExecution";
        public BeforePageFilterOnPageHandlerExecutionEventData(Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutingContext handlerExecutionContext, Microsoft.AspNetCore.Mvc.Filters.IAsyncPageFilter filter) { }
        public Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IAsyncPageFilter Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutingContext HandlerExecutionContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
    }
    public sealed partial class BeforePageFilterOnPageHandlerSelectedEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.BeforeOnPageHandlerSelected";
        public BeforePageFilterOnPageHandlerSelectedEventData(Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.Filters.PageHandlerSelectedContext handlerSelectedContext, Microsoft.AspNetCore.Mvc.Filters.IPageFilter filter) { }
        public Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IPageFilter Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.PageHandlerSelectedContext HandlerSelectedContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
    }
    public sealed partial class BeforePageFilterOnPageHandlerSelectionEventData : Microsoft.AspNetCore.Mvc.Diagnostics.EventData
    {
        public const string EventName = "Microsoft.AspNetCore.Mvc.BeforeOnPageHandlerSelection";
        public BeforePageFilterOnPageHandlerSelectionEventData(Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.Filters.PageHandlerSelectedContext handlerSelectedContext, Microsoft.AspNetCore.Mvc.Filters.IAsyncPageFilter filter) { }
        public Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor ActionDescriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override int Count { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.IAsyncPageFilter Filter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Filters.PageHandlerSelectedContext HandlerSelectedContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw null; } }
    }
}
namespace Microsoft.AspNetCore.Mvc.Filters
{
    public partial interface IAsyncPageFilter : Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata
    {
        System.Threading.Tasks.Task OnPageHandlerExecutionAsync(Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutingContext context, Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutionDelegate next);
        System.Threading.Tasks.Task OnPageHandlerSelectionAsync(Microsoft.AspNetCore.Mvc.Filters.PageHandlerSelectedContext context);
    }
    public partial interface IPageFilter : Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata
    {
        void OnPageHandlerExecuted(Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutedContext context);
        void OnPageHandlerExecuting(Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutingContext context);
        void OnPageHandlerSelected(Microsoft.AspNetCore.Mvc.Filters.PageHandlerSelectedContext context);
    }
    public partial class PageHandlerExecutedContext : Microsoft.AspNetCore.Mvc.Filters.FilterContext
    {
        public PageHandlerExecutedContext(Microsoft.AspNetCore.Mvc.RazorPages.PageContext pageContext, System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata> filters, Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.HandlerMethodDescriptor handlerMethod, object handlerInstance) : base (default(Microsoft.AspNetCore.Mvc.ActionContext), default(System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata>)) { }
        public virtual new Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor ActionDescriptor { get { throw null; } }
        public virtual bool Canceled { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual System.Exception Exception { get { throw null; } set { } }
        public virtual System.Runtime.ExceptionServices.ExceptionDispatchInfo ExceptionDispatchInfo { get { throw null; } set { } }
        public virtual bool ExceptionHandled { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual object HandlerInstance { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public virtual Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.HandlerMethodDescriptor HandlerMethod { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public virtual Microsoft.AspNetCore.Mvc.IActionResult Result { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class PageHandlerExecutingContext : Microsoft.AspNetCore.Mvc.Filters.FilterContext
    {
        public PageHandlerExecutingContext(Microsoft.AspNetCore.Mvc.RazorPages.PageContext pageContext, System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata> filters, Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.HandlerMethodDescriptor handlerMethod, System.Collections.Generic.IDictionary<string, object> handlerArguments, object handlerInstance) : base (default(Microsoft.AspNetCore.Mvc.ActionContext), default(System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata>)) { }
        public virtual new Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor ActionDescriptor { get { throw null; } }
        public virtual System.Collections.Generic.IDictionary<string, object> HandlerArguments { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public virtual object HandlerInstance { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public virtual Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.HandlerMethodDescriptor HandlerMethod { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public virtual Microsoft.AspNetCore.Mvc.IActionResult Result { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public delegate System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutedContext> PageHandlerExecutionDelegate();
    public partial class PageHandlerSelectedContext : Microsoft.AspNetCore.Mvc.Filters.FilterContext
    {
        public PageHandlerSelectedContext(Microsoft.AspNetCore.Mvc.RazorPages.PageContext pageContext, System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata> filters, object handlerInstance) : base (default(Microsoft.AspNetCore.Mvc.ActionContext), default(System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata>)) { }
        public virtual new Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor ActionDescriptor { get { throw null; } }
        public virtual object HandlerInstance { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public virtual Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.HandlerMethodDescriptor HandlerMethod { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
}
namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    public partial class CompiledPageActionDescriptor : Microsoft.AspNetCore.Mvc.RazorPages.PageActionDescriptor
    {
        public CompiledPageActionDescriptor() { }
        public CompiledPageActionDescriptor(Microsoft.AspNetCore.Mvc.RazorPages.PageActionDescriptor actionDescriptor) { }
        public System.Reflection.TypeInfo DeclaredModelTypeInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Http.Endpoint Endpoint { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.HandlerMethodDescriptor> HandlerMethods { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Reflection.TypeInfo HandlerTypeInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Reflection.TypeInfo ModelTypeInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Reflection.TypeInfo PageTypeInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial interface IPageActivatorProvider
    {
        System.Func<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, Microsoft.AspNetCore.Mvc.Rendering.ViewContext, object> CreateActivator(Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor descriptor);
        System.Action<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, Microsoft.AspNetCore.Mvc.Rendering.ViewContext, object> CreateReleaser(Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor descriptor);
    }
    public partial interface IPageFactoryProvider
    {
        System.Action<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, Microsoft.AspNetCore.Mvc.Rendering.ViewContext, object> CreatePageDisposer(Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor descriptor);
        System.Func<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, Microsoft.AspNetCore.Mvc.Rendering.ViewContext, object> CreatePageFactory(Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor descriptor);
    }
    public partial interface IPageModelActivatorProvider
    {
        System.Func<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, object> CreateActivator(Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor descriptor);
        System.Action<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, object> CreateReleaser(Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor descriptor);
    }
    public partial interface IPageModelFactoryProvider
    {
        System.Action<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, object> CreateModelDisposer(Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor descriptor);
        System.Func<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, object> CreateModelFactory(Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor descriptor);
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, AllowMultiple=false, Inherited=true)]
    public partial class NonHandlerAttribute : System.Attribute
    {
        public NonHandlerAttribute() { }
    }
    public abstract partial class Page : Microsoft.AspNetCore.Mvc.RazorPages.PageBase
    {
        protected Page() { }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("{DebuggerDisplayString,nq}")]
    public partial class PageActionDescriptor : Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor
    {
        public PageActionDescriptor() { }
        public PageActionDescriptor(Microsoft.AspNetCore.Mvc.RazorPages.PageActionDescriptor other) { }
        public string AreaName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override string DisplayName { get { throw null; } set { } }
        public string RelativePath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ViewEnginePath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public abstract partial class PageBase : Microsoft.AspNetCore.Mvc.Razor.RazorPageBase
    {
        protected PageBase() { }
        public Microsoft.AspNetCore.Http.HttpContext HttpContext { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider MetadataProvider { get { throw null; } set { } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary ModelState { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.RazorPages.PageContext PageContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Http.HttpRequest Request { get { throw null; } }
        public Microsoft.AspNetCore.Http.HttpResponse Response { get { throw null; } }
        public Microsoft.AspNetCore.Routing.RouteData RouteData { get { throw null; } }
        public override Microsoft.AspNetCore.Mvc.Rendering.ViewContext ViewContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual Microsoft.AspNetCore.Mvc.BadRequestResult BadRequest() { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.BadRequestObjectResult BadRequest(Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.BadRequestObjectResult BadRequest(object error) { throw null; }
        public override void BeginContext(int position, int length, bool isLiteral) { }
        public virtual Microsoft.AspNetCore.Mvc.ChallengeResult Challenge() { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ChallengeResult Challenge(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ChallengeResult Challenge(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, params string[] authenticationSchemes) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ChallengeResult Challenge(params string[] authenticationSchemes) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ContentResult Content(string content) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ContentResult Content(string content, Microsoft.Net.Http.Headers.MediaTypeHeaderValue contentType) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ContentResult Content(string content, string contentType) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ContentResult Content(string content, string contentType, System.Text.Encoding contentEncoding) { throw null; }
        public override void EndContext() { }
        public override void EnsureRenderedBodyOrSections() { }
        public virtual Microsoft.AspNetCore.Mvc.FileContentResult File(byte[] fileContents, string contentType) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.FileContentResult File(byte[] fileContents, string contentType, string fileDownloadName) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.FileStreamResult File(System.IO.Stream fileStream, string contentType) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.FileStreamResult File(System.IO.Stream fileStream, string contentType, string fileDownloadName) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.VirtualFileResult File(string virtualPath, string contentType) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.VirtualFileResult File(string virtualPath, string contentType, string fileDownloadName) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ForbidResult Forbid() { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ForbidResult Forbid(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ForbidResult Forbid(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, params string[] authenticationSchemes) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ForbidResult Forbid(params string[] authenticationSchemes) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.LocalRedirectResult LocalRedirect(string localUrl) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.LocalRedirectResult LocalRedirectPermanent(string localUrl) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.LocalRedirectResult LocalRedirectPermanentPreserveMethod(string localUrl) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.LocalRedirectResult LocalRedirectPreserveMethod(string localUrl) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.NotFoundResult NotFound() { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.NotFoundObjectResult NotFound(object value) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RazorPages.PageResult Page() { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.PartialViewResult Partial(string viewName) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.PartialViewResult Partial(string viewName, object model) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.PhysicalFileResult PhysicalFile(string physicalPath, string contentType) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.PhysicalFileResult PhysicalFile(string physicalPath, string contentType, string fileDownloadName) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectResult Redirect(string url) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectResult RedirectPermanent(string url) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectResult RedirectPermanentPreserveMethod(string url) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectResult RedirectPreserveMethod(string url) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToAction(string actionName) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToAction(string actionName, object routeValues) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToAction(string actionName, string controllerName) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToAction(string actionName, string controllerName, object routeValues) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToAction(string actionName, string controllerName, object routeValues, string fragment) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToAction(string actionName, string controllerName, string fragment) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToActionPermanent(string actionName) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToActionPermanent(string actionName, object routeValues) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToActionPermanent(string actionName, string controllerName) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToActionPermanent(string actionName, string controllerName, object routeValues) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToActionPermanent(string actionName, string controllerName, object routeValues, string fragment) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToActionPermanent(string actionName, string controllerName, string fragment) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToActionPermanentPreserveMethod(string actionName = null, string controllerName = null, object routeValues = null, string fragment = null) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToActionPreserveMethod(string actionName = null, string controllerName = null, object routeValues = null, string fragment = null) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPage() { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPage(object routeValues) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPage(string pageName) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPage(string pageName, object routeValues) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPage(string pageName, string pageHandler) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPage(string pageName, string pageHandler, object routeValues, string fragment) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPage(string pageName, string pageHandler, string fragment) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPagePermanent(string pageName) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPagePermanent(string pageName, object routeValues) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPagePermanent(string pageName, string pageHandler) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPagePermanent(string pageName, string pageHandler, object routeValues) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPagePermanent(string pageName, string pageHandler, object routeValues, string fragment) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPagePermanent(string pageName, string pageHandler, string fragment) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPagePermanentPreserveMethod(string pageName = null, string pageHandler = null, object routeValues = null, string fragment = null) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPagePreserveMethod(string pageName = null, string pageHandler = null, object routeValues = null, string fragment = null) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoute(object routeValues) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoute(string routeName) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoute(string routeName, object routeValues) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoute(string routeName, object routeValues, string fragment) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoute(string routeName, string fragment) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoutePermanent(object routeValues) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoutePermanent(string routeName) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoutePermanent(string routeName, object routeValues) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoutePermanent(string routeName, object routeValues, string fragment) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoutePermanent(string routeName, string fragment) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoutePermanentPreserveMethod(string routeName = null, object routeValues = null, string fragment = null) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoutePreserveMethod(string routeName = null, object routeValues = null, string fragment = null) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.SignInResult SignIn(System.Security.Claims.ClaimsPrincipal principal, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, string authenticationScheme) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.SignInResult SignIn(System.Security.Claims.ClaimsPrincipal principal, string authenticationScheme) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.SignOutResult SignOut(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, params string[] authenticationSchemes) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.SignOutResult SignOut(params string[] authenticationSchemes) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.StatusCodeResult StatusCode(int statusCode) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ObjectResult StatusCode(int statusCode, object value) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<bool> TryUpdateModelAsync(object model, System.Type modelType, string prefix) { throw null; }
        public System.Threading.Tasks.Task<bool> TryUpdateModelAsync(object model, System.Type modelType, string prefix, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider valueProvider, System.Func<Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata, bool> propertyFilter) { throw null; }
        public virtual System.Threading.Tasks.Task<bool> TryUpdateModelAsync<TModel>(TModel model) where TModel : class { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<bool> TryUpdateModelAsync<TModel>(TModel model, string prefix) where TModel : class { throw null; }
        public virtual System.Threading.Tasks.Task<bool> TryUpdateModelAsync<TModel>(TModel model, string prefix, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider valueProvider) where TModel : class { throw null; }
        public System.Threading.Tasks.Task<bool> TryUpdateModelAsync<TModel>(TModel model, string prefix, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider valueProvider, System.Func<Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata, bool> propertyFilter) where TModel : class { throw null; }
        public System.Threading.Tasks.Task<bool> TryUpdateModelAsync<TModel>(TModel model, string prefix, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider valueProvider, params System.Linq.Expressions.Expression<System.Func<TModel, object>>[] includeExpressions) where TModel : class { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task<bool> TryUpdateModelAsync<TModel>(TModel model, string prefix, System.Func<Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata, bool> propertyFilter) where TModel : class { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task<bool> TryUpdateModelAsync<TModel>(TModel model, string prefix, params System.Linq.Expressions.Expression<System.Func<TModel, object>>[] includeExpressions) where TModel : class { throw null; }
        public virtual bool TryValidateModel(object model) { throw null; }
        public virtual bool TryValidateModel(object model, string prefix) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.UnauthorizedResult Unauthorized() { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ViewComponentResult ViewComponent(string componentName) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ViewComponentResult ViewComponent(string componentName, object arguments) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ViewComponentResult ViewComponent(System.Type componentType) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ViewComponentResult ViewComponent(System.Type componentType, object arguments) { throw null; }
    }
    public partial class PageContext : Microsoft.AspNetCore.Mvc.ActionContext
    {
        public PageContext() { }
        public PageContext(Microsoft.AspNetCore.Mvc.ActionContext actionContext) { }
        public virtual new Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor ActionDescriptor { get { throw null; } set { } }
        public virtual System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ModelBinding.IValueProviderFactory> ValueProviderFactories { get { throw null; } set { } }
        public virtual Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary ViewData { get { throw null; } set { } }
        public virtual System.Collections.Generic.IList<System.Func<Microsoft.AspNetCore.Mvc.Razor.IRazorPage>> ViewStartFactories { get { throw null; } set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public partial class PageContextAttribute : System.Attribute
    {
        public PageContextAttribute() { }
    }
    [Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageModelAttribute]
    public abstract partial class PageModel : Microsoft.AspNetCore.Mvc.Filters.IAsyncPageFilter, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata, Microsoft.AspNetCore.Mvc.Filters.IPageFilter
    {
        protected PageModel() { }
        public Microsoft.AspNetCore.Http.HttpContext HttpContext { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider MetadataProvider { get { throw null; } set { } }
        public Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary ModelState { get { throw null; } }
        [Microsoft.AspNetCore.Mvc.RazorPages.PageContextAttribute]
        public Microsoft.AspNetCore.Mvc.RazorPages.PageContext PageContext { get { throw null; } set { } }
        public Microsoft.AspNetCore.Http.HttpRequest Request { get { throw null; } }
        public Microsoft.AspNetCore.Http.HttpResponse Response { get { throw null; } }
        public Microsoft.AspNetCore.Routing.RouteData RouteData { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary TempData { get { throw null; } set { } }
        public Microsoft.AspNetCore.Mvc.IUrlHelper Url { get { throw null; } set { } }
        public System.Security.Claims.ClaimsPrincipal User { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary ViewData { get { throw null; } }
        public virtual Microsoft.AspNetCore.Mvc.BadRequestResult BadRequest() { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.BadRequestObjectResult BadRequest(Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.BadRequestObjectResult BadRequest(object error) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ChallengeResult Challenge() { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ChallengeResult Challenge(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ChallengeResult Challenge(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, params string[] authenticationSchemes) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ChallengeResult Challenge(params string[] authenticationSchemes) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ContentResult Content(string content) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ContentResult Content(string content, Microsoft.Net.Http.Headers.MediaTypeHeaderValue contentType) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ContentResult Content(string content, string contentType) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ContentResult Content(string content, string contentType, System.Text.Encoding contentEncoding) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.FileContentResult File(byte[] fileContents, string contentType) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.FileContentResult File(byte[] fileContents, string contentType, string fileDownloadName) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.FileStreamResult File(System.IO.Stream fileStream, string contentType) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.FileStreamResult File(System.IO.Stream fileStream, string contentType, string fileDownloadName) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.VirtualFileResult File(string virtualPath, string contentType) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.VirtualFileResult File(string virtualPath, string contentType, string fileDownloadName) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ForbidResult Forbid() { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ForbidResult Forbid(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ForbidResult Forbid(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, params string[] authenticationSchemes) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ForbidResult Forbid(params string[] authenticationSchemes) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.LocalRedirectResult LocalRedirect(string localUrl) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.LocalRedirectResult LocalRedirectPermanent(string localUrl) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.LocalRedirectResult LocalRedirectPermanentPreserveMethod(string localUrl) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.LocalRedirectResult LocalRedirectPreserveMethod(string localUrl) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.NotFoundResult NotFound() { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.NotFoundObjectResult NotFound(object value) { throw null; }
        public virtual void OnPageHandlerExecuted(Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutedContext context) { }
        public virtual void OnPageHandlerExecuting(Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutingContext context) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task OnPageHandlerExecutionAsync(Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutingContext context, Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutionDelegate next) { throw null; }
        public virtual void OnPageHandlerSelected(Microsoft.AspNetCore.Mvc.Filters.PageHandlerSelectedContext context) { }
        public virtual System.Threading.Tasks.Task OnPageHandlerSelectionAsync(Microsoft.AspNetCore.Mvc.Filters.PageHandlerSelectedContext context) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RazorPages.PageResult Page() { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.PartialViewResult Partial(string viewName) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.PartialViewResult Partial(string viewName, object model) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.PhysicalFileResult PhysicalFile(string physicalPath, string contentType) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.PhysicalFileResult PhysicalFile(string physicalPath, string contentType, string fileDownloadName) { throw null; }
        protected internal Microsoft.AspNetCore.Mvc.RedirectResult Redirect(string url) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectResult RedirectPermanent(string url) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectResult RedirectPermanentPreserveMethod(string url) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectResult RedirectPreserveMethod(string url) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToAction(string actionName) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToAction(string actionName, object routeValues) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToAction(string actionName, string controllerName) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToAction(string actionName, string controllerName, object routeValues) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToAction(string actionName, string controllerName, object routeValues, string fragment) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToAction(string actionName, string controllerName, string fragment) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToActionPermanent(string actionName) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToActionPermanent(string actionName, object routeValues) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToActionPermanent(string actionName, string controllerName) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToActionPermanent(string actionName, string controllerName, object routeValues) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToActionPermanent(string actionName, string controllerName, object routeValues, string fragment) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToActionPermanent(string actionName, string controllerName, string fragment) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToActionPermanentPreserveMethod(string actionName = null, string controllerName = null, object routeValues = null, string fragment = null) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToActionResult RedirectToActionPreserveMethod(string actionName = null, string controllerName = null, object routeValues = null, string fragment = null) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPage() { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPage(object routeValues) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPage(string pageName) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPage(string pageName, object routeValues) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPage(string pageName, string pageHandler) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPage(string pageName, string pageHandler, object routeValues) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPage(string pageName, string pageHandler, object routeValues, string fragment) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPage(string pageName, string pageHandler, string fragment) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPagePermanent(string pageName) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPagePermanent(string pageName, object routeValues) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPagePermanent(string pageName, object routeValues, string fragment) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPagePermanent(string pageName, string pageHandler) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPagePermanent(string pageName, string pageHandler, object routeValues) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPagePermanent(string pageName, string pageHandler, object routeValues, string fragment) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPagePermanent(string pageName, string pageHandler, string fragment) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPagePermanentPreserveMethod(string pageName = null, string pageHandler = null, object routeValues = null, string fragment = null) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToPageResult RedirectToPagePreserveMethod(string pageName = null, string pageHandler = null, object routeValues = null, string fragment = null) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoute(object routeValues) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoute(string routeName) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoute(string routeName, object routeValues) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoute(string routeName, object routeValues, string fragment) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoute(string routeName, string fragment) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoutePermanent(object routeValues) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoutePermanent(string routeName) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoutePermanent(string routeName, object routeValues) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoutePermanent(string routeName, object routeValues, string fragment) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoutePermanent(string routeName, string fragment) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoutePermanentPreserveMethod(string routeName = null, object routeValues = null, string fragment = null) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.RedirectToRouteResult RedirectToRoutePreserveMethod(string routeName = null, object routeValues = null, string fragment = null) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.SignInResult SignIn(System.Security.Claims.ClaimsPrincipal principal, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, string authenticationScheme) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.SignInResult SignIn(System.Security.Claims.ClaimsPrincipal principal, string authenticationScheme) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.SignOutResult SignOut(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, params string[] authenticationSchemes) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.SignOutResult SignOut(params string[] authenticationSchemes) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.StatusCodeResult StatusCode(int statusCode) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ObjectResult StatusCode(int statusCode, object value) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected internal System.Threading.Tasks.Task<bool> TryUpdateModelAsync(object model, System.Type modelType, string name) { throw null; }
        protected internal System.Threading.Tasks.Task<bool> TryUpdateModelAsync(object model, System.Type modelType, string name, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider valueProvider, System.Func<Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata, bool> propertyFilter) { throw null; }
        protected internal System.Threading.Tasks.Task<bool> TryUpdateModelAsync<TModel>(TModel model) where TModel : class { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected internal System.Threading.Tasks.Task<bool> TryUpdateModelAsync<TModel>(TModel model, string name) where TModel : class { throw null; }
        protected internal System.Threading.Tasks.Task<bool> TryUpdateModelAsync<TModel>(TModel model, string name, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider valueProvider) where TModel : class { throw null; }
        protected internal System.Threading.Tasks.Task<bool> TryUpdateModelAsync<TModel>(TModel model, string name, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider valueProvider, System.Func<Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata, bool> propertyFilter) where TModel : class { throw null; }
        protected internal System.Threading.Tasks.Task<bool> TryUpdateModelAsync<TModel>(TModel model, string name, Microsoft.AspNetCore.Mvc.ModelBinding.IValueProvider valueProvider, params System.Linq.Expressions.Expression<System.Func<TModel, object>>[] includeExpressions) where TModel : class { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected internal System.Threading.Tasks.Task<bool> TryUpdateModelAsync<TModel>(TModel model, string name, System.Func<Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata, bool> propertyFilter) where TModel : class { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected internal System.Threading.Tasks.Task<bool> TryUpdateModelAsync<TModel>(TModel model, string name, params System.Linq.Expressions.Expression<System.Func<TModel, object>>[] includeExpressions) where TModel : class { throw null; }
        public virtual bool TryValidateModel(object model) { throw null; }
        public virtual bool TryValidateModel(object model, string name) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.UnauthorizedResult Unauthorized() { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ViewComponentResult ViewComponent(string componentName) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ViewComponentResult ViewComponent(string componentName, object arguments) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ViewComponentResult ViewComponent(System.Type componentType) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.ViewComponentResult ViewComponent(System.Type componentType, object arguments) { throw null; }
    }
    public partial class PageResult : Microsoft.AspNetCore.Mvc.ActionResult
    {
        public PageResult() { }
        public string ContentType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public object Model { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.RazorPages.PageBase Page { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int? StatusCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary ViewData { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override System.Threading.Tasks.Task ExecuteResultAsync(Microsoft.AspNetCore.Mvc.ActionContext context) { throw null; }
    }
    public partial class RazorPagesOptions : System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch>, System.Collections.IEnumerable
    {
        public RazorPagesOptions() { }
        public Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection Conventions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string RootDirectory { get { throw null; } set { } }
        System.Collections.Generic.IEnumerator<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch> System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public partial class HandlerMethodDescriptor
    {
        public HandlerMethodDescriptor() { }
        public string HttpMethod { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Reflection.MethodInfo MethodInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.HandlerParameterDescriptor> Parameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class HandlerParameterDescriptor : Microsoft.AspNetCore.Mvc.Abstractions.ParameterDescriptor, Microsoft.AspNetCore.Mvc.Infrastructure.IParameterInfoParameterDescriptor
    {
        public HandlerParameterDescriptor() { }
        public System.Reflection.ParameterInfo ParameterInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial interface IPageHandlerMethodSelector
    {
        Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.HandlerMethodDescriptor Select(Microsoft.AspNetCore.Mvc.RazorPages.PageContext context);
    }
    [System.ObsoleteAttribute("This type is obsolete. Use PageLoader instead.")]
    public partial interface IPageLoader
    {
        Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor Load(Microsoft.AspNetCore.Mvc.RazorPages.PageActionDescriptor actionDescriptor);
    }
    public partial class PageActionDescriptorProvider : Microsoft.AspNetCore.Mvc.Abstractions.IActionDescriptorProvider
    {
        public PageActionDescriptorProvider(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ApplicationModels.IPageRouteModelProvider> pageRouteModelProviders, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> mvcOptionsAccessor, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.RazorPages.RazorPagesOptions> pagesOptionsAccessor) { }
        public int Order { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        protected System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.ApplicationModels.PageRouteModel> BuildModel() { throw null; }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptorProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptorProviderContext context) { }
    }
    public partial class PageBoundPropertyDescriptor : Microsoft.AspNetCore.Mvc.Abstractions.ParameterDescriptor, Microsoft.AspNetCore.Mvc.Infrastructure.IPropertyInfoParameterDescriptor
    {
        public PageBoundPropertyDescriptor() { }
        System.Reflection.PropertyInfo Microsoft.AspNetCore.Mvc.Infrastructure.IPropertyInfoParameterDescriptor.PropertyInfo { get { throw null; } }
        public System.Reflection.PropertyInfo Property { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public abstract partial class PageLoader : Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.IPageLoader
    {
        protected PageLoader() { }
        public abstract System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor> LoadAsync(Microsoft.AspNetCore.Mvc.RazorPages.PageActionDescriptor actionDescriptor);
        Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.IPageLoader.Load(Microsoft.AspNetCore.Mvc.RazorPages.PageActionDescriptor actionDescriptor) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=false, Inherited=true)]
    public partial class PageModelAttribute : System.Attribute
    {
        public PageModelAttribute() { }
    }
    public partial class PageResultExecutor : Microsoft.AspNetCore.Mvc.ViewFeatures.ViewExecutor
    {
        public PageResultExecutor(Microsoft.AspNetCore.Mvc.Infrastructure.IHttpResponseStreamWriterFactory writerFactory, Microsoft.AspNetCore.Mvc.ViewEngines.ICompositeViewEngine compositeViewEngine, Microsoft.AspNetCore.Mvc.Razor.IRazorViewEngine razorViewEngine, Microsoft.AspNetCore.Mvc.Razor.IRazorPageActivator razorPageActivator, System.Diagnostics.DiagnosticListener diagnosticListener, System.Text.Encodings.Web.HtmlEncoder htmlEncoder) : base (default(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcViewOptions>), default(Microsoft.AspNetCore.Mvc.Infrastructure.IHttpResponseStreamWriterFactory), default(Microsoft.AspNetCore.Mvc.ViewEngines.ICompositeViewEngine), default(Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory), default(System.Diagnostics.DiagnosticListener), default(Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider)) { }
        public virtual System.Threading.Tasks.Task ExecuteAsync(Microsoft.AspNetCore.Mvc.RazorPages.PageContext pageContext, Microsoft.AspNetCore.Mvc.RazorPages.PageResult result) { throw null; }
    }
    public partial class PageViewLocationExpander : Microsoft.AspNetCore.Mvc.Razor.IViewLocationExpander
    {
        public PageViewLocationExpander() { }
        public System.Collections.Generic.IEnumerable<string> ExpandViewLocations(Microsoft.AspNetCore.Mvc.Razor.ViewLocationExpanderContext context, System.Collections.Generic.IEnumerable<string> viewLocations) { throw null; }
        public void PopulateValues(Microsoft.AspNetCore.Mvc.Razor.ViewLocationExpanderContext context) { }
    }
    public partial class RazorPageAdapter : Microsoft.AspNetCore.Mvc.Razor.IRazorPage
    {
        public RazorPageAdapter(Microsoft.AspNetCore.Mvc.Razor.RazorPageBase page, System.Type modelType) { }
        public Microsoft.AspNetCore.Html.IHtmlContent BodyContent { get { throw null; } set { } }
        public bool IsLayoutBeingRendered { get { throw null; } set { } }
        public string Layout { get { throw null; } set { } }
        public string Path { get { throw null; } set { } }
        public System.Collections.Generic.IDictionary<string, Microsoft.AspNetCore.Mvc.Razor.RenderAsyncDelegate> PreviousSectionWriters { get { throw null; } set { } }
        public System.Collections.Generic.IDictionary<string, Microsoft.AspNetCore.Mvc.Razor.RenderAsyncDelegate> SectionWriters { get { throw null; } }
        public Microsoft.AspNetCore.Mvc.Rendering.ViewContext ViewContext { get { throw null; } set { } }
        public void EnsureRenderedBodyOrSections() { }
        public System.Threading.Tasks.Task ExecuteAsync() { throw null; }
    }
    [System.ObsoleteAttribute("This attribute has been superseded by RazorCompiledItem and will not be used by the runtime.")]
    public partial class RazorPageAttribute : Microsoft.AspNetCore.Mvc.Razor.Compilation.RazorViewAttribute
    {
        public RazorPageAttribute(string path, System.Type viewType, string routeTemplate) : base (default(string), default(System.Type)) { }
        public string RouteTemplate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class ServiceBasedPageModelActivatorProvider : Microsoft.AspNetCore.Mvc.RazorPages.IPageModelActivatorProvider
    {
        public ServiceBasedPageModelActivatorProvider() { }
        public System.Func<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, object> CreateActivator(Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor descriptor) { throw null; }
        public System.Action<Microsoft.AspNetCore.Mvc.RazorPages.PageContext, object> CreateReleaser(Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor descriptor) { throw null; }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class MvcRazorPagesMvcBuilderExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddRazorPagesOptions(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder, System.Action<Microsoft.AspNetCore.Mvc.RazorPages.RazorPagesOptions> setupAction) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder WithRazorPagesAtContentRoot(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder WithRazorPagesRoot(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder, string rootDirectory) { throw null; }
    }
    public static partial class MvcRazorPagesMvcCoreBuilderExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddRazorPages(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddRazorPages(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder, System.Action<Microsoft.AspNetCore.Mvc.RazorPages.RazorPagesOptions> setupAction) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder WithRazorPagesRoot(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder, string rootDirectory) { throw null; }
    }
    public static partial class PageConventionCollectionExtensions
    {
        public static Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection Add(this Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection conventions, Microsoft.AspNetCore.Mvc.ApplicationModels.IParameterModelBaseConvention convention) { throw null; }
        public static Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection AddAreaPageRoute(this Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection conventions, string areaName, string pageName, string route) { throw null; }
        public static Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection AddPageRoute(this Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection conventions, string pageName, string route) { throw null; }
        public static Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection AllowAnonymousToAreaFolder(this Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection conventions, string areaName, string folderPath) { throw null; }
        public static Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection AllowAnonymousToAreaPage(this Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection conventions, string areaName, string pageName) { throw null; }
        public static Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection AllowAnonymousToFolder(this Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection conventions, string folderPath) { throw null; }
        public static Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection AllowAnonymousToPage(this Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection conventions, string pageName) { throw null; }
        public static Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection AuthorizeAreaFolder(this Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection conventions, string areaName, string folderPath) { throw null; }
        public static Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection AuthorizeAreaFolder(this Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection conventions, string areaName, string folderPath, string policy) { throw null; }
        public static Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection AuthorizeAreaPage(this Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection conventions, string areaName, string pageName) { throw null; }
        public static Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection AuthorizeAreaPage(this Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection conventions, string areaName, string pageName, string policy) { throw null; }
        public static Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection AuthorizeFolder(this Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection conventions, string folderPath) { throw null; }
        public static Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection AuthorizeFolder(this Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection conventions, string folderPath, string policy) { throw null; }
        public static Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection AuthorizePage(this Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection conventions, string pageName) { throw null; }
        public static Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection AuthorizePage(this Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection conventions, string pageName, string policy) { throw null; }
        public static Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection ConfigureFilter(this Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection conventions, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata filter) { throw null; }
        public static Microsoft.AspNetCore.Mvc.ApplicationModels.IPageApplicationModelConvention ConfigureFilter(this Microsoft.AspNetCore.Mvc.ApplicationModels.PageConventionCollection conventions, System.Func<Microsoft.AspNetCore.Mvc.ApplicationModels.PageApplicationModel, Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata> factory) { throw null; }
    }
}
