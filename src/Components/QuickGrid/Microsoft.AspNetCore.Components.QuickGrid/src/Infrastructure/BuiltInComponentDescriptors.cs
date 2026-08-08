// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.QuickGrid;
using Microsoft.AspNetCore.Components.QuickGrid.Infrastructure;
using Microsoft.JSInterop;

#pragma warning disable ASPNETCORE9004 // Built-in Native AOT metadata consumes the experimental descriptor model.

namespace Microsoft.AspNetCore.Components.Infrastructure;

internal static class BuiltInComponentDescriptors
{
    internal static ComponentDescriptor[] GetDescriptors()
        =>
        [
            new ComponentDescriptor
            {
                Type = typeof(Paginator),
                CreateInstance = static _ => new Paginator(),
                Injectables =
                [
                    CreateInjectable<Paginator, NavigationManager>(
                        "NavigationManager",
                        PaginatorAccessors.SetNavigationManager),
                ],
            },
        ];

    internal static ComponentDescriptor[] CreateQuickGridDescriptors<TGridItem>()
        =>
        [
            new ComponentDescriptor
            {
                Type = typeof(QuickGrid<TGridItem>),
                CreateInstance = static _ => new QuickGrid<TGridItem>(),
                Injectables =
                [
                    CreateInjectable<QuickGrid<TGridItem>, IServiceProvider>(
                        "Services",
                        QuickGridAccessors<TGridItem>.SetServices),
                    CreateInjectable<QuickGrid<TGridItem>, IJSRuntime>(
                        "JS",
                        QuickGridAccessors<TGridItem>.SetJS),
                    CreateInjectable<QuickGrid<TGridItem>, NavigationManager>(
                        "NavigationManager",
                        QuickGridAccessors<TGridItem>.SetNavigationManager),
                ],
            },
            new ComponentDescriptor
            {
                Type = typeof(CascadingValue<InternalGridContext<TGridItem>>),
                CreateInstance = static _ => new CascadingValue<InternalGridContext<TGridItem>>(),
                Parameters =
                [
                    CreateParameter<CascadingValue<InternalGridContext<TGridItem>>, RenderFragment?>(
                        nameof(CascadingValue<InternalGridContext<TGridItem>>.ChildContent),
                        static target => target.ChildContent,
                        static (target, value) => target.ChildContent = value),
                    CreateParameter<CascadingValue<InternalGridContext<TGridItem>>, InternalGridContext<TGridItem>?>(
                        nameof(CascadingValue<InternalGridContext<TGridItem>>.Value),
                        static target => target.Value,
                        static (target, value) => target.Value = value),
                    CreateParameter<CascadingValue<InternalGridContext<TGridItem>>, string?>(
                        nameof(CascadingValue<InternalGridContext<TGridItem>>.Name),
                        static target => target.Name,
                        static (target, value) => target.Name = value),
                    CreateParameter<CascadingValue<InternalGridContext<TGridItem>>, bool>(
                        nameof(CascadingValue<InternalGridContext<TGridItem>>.IsFixed),
                        static target => target.IsFixed,
                        static (target, value) => target.IsFixed = value),
                ],
            },
            .. VirtualizeDescriptorFactory.CreateDescriptors<(int RowIndex, TGridItem Data)>(null),
        ];

    internal static ComponentDescriptor[] CreatePropertyColumnDescriptors<TGridItem, TProp>()
        =>
        [
            CreateColumnDescriptor<PropertyColumn<TGridItem, TProp>, TGridItem>(
                static _ => new PropertyColumn<TGridItem, TProp>()),
        ];

    internal static ComponentDescriptor[] CreateTemplateColumnDescriptors<TGridItem>()
        =>
        [
            CreateColumnDescriptor<TemplateColumn<TGridItem>, TGridItem>(
                static _ => new TemplateColumn<TGridItem>()),
        ];

    internal static ComponentDescriptor[] CreateColumnsCollectedNotifierDescriptors<TGridItem>()
        =>
        [
            new ComponentDescriptor
            {
                Type = typeof(ColumnsCollectedNotifier<TGridItem>),
                CreateInstance = static _ => new ColumnsCollectedNotifier<TGridItem>(),
                Parameters =
                [
                    CreateCascadingParameter<ColumnsCollectedNotifier<TGridItem>, InternalGridContext<TGridItem>>(
                        "InternalGridContext",
                        ColumnsCollectedNotifierAccessors<TGridItem>.GetInternalGridContext,
                        ColumnsCollectedNotifierAccessors<TGridItem>.SetInternalGridContext),
                ],
            },
        ];

    internal static ComponentDescriptor[] CreateColumnBaseDescriptors<
        TGridItem,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TColumn>()
        where TColumn : ColumnBase<TGridItem>
        =>
        [
            new ComponentDescriptor
            {
                Type = typeof(TColumn),
                Parameters =
                [
                    CreateCascadingParameter<ColumnBase<TGridItem>, InternalGridContext<TGridItem>>(
                        "InternalGridContext",
                        ColumnBaseAccessors<TGridItem>.GetInternalGridContext,
                        ColumnBaseAccessors<TGridItem>.SetInternalGridContext),
                ],
            },
        ];

    private static ComponentDescriptor CreateColumnDescriptor<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TColumn,
        TGridItem>(
        Func<IServiceProvider, TColumn> createInstance)
        where TColumn : ColumnBase<TGridItem>
        => new()
        {
            Type = typeof(TColumn),
            CreateInstance = createInstance,
            Parameters =
            [
                CreateCascadingParameter<ColumnBase<TGridItem>, InternalGridContext<TGridItem>>(
                    "InternalGridContext",
                    ColumnBaseAccessors<TGridItem>.GetInternalGridContext,
                    ColumnBaseAccessors<TGridItem>.SetInternalGridContext),
            ],
        };

    private static ComponentParameterDescriptor CreateParameter<TComponent, TValue>(
        string name,
        Func<TComponent, TValue> getValue,
        Action<TComponent, TValue> setValue)
        where TComponent : IComponent
        => new()
        {
            Name = name,
            ParameterType = typeof(TValue),
            Attribute = new ParameterAttribute(),
            GetValue = target => getValue((TComponent)target),
            SetValue = (target, value) => setValue(
                (TComponent)target,
                value is null ? default! : (TValue)value),
        };

    private static ComponentParameterDescriptor CreateCascadingParameter<TComponent, TValue>(
        string name,
        Func<TComponent, TValue> getValue,
        Action<TComponent, TValue> setValue)
        where TComponent : IComponent
        => new()
        {
            Name = name,
            ParameterType = typeof(TValue),
            Attribute = new CascadingParameterAttribute(),
            GetValue = target => getValue((TComponent)target),
            SetValue = (target, value) => setValue((TComponent)target, (TValue)value!),
        };

    private static ComponentInjectableDescriptor CreateInjectable<TComponent, TValue>(
        string name,
        Action<TComponent, TValue> setValue)
        where TComponent : IComponent
        => new()
        {
            Name = name,
            ServiceType = typeof(TValue),
            Attribute = new InjectAttribute(),
            SetValue = (target, value) => setValue((TComponent)target, (TValue)value!),
        };

    private static class QuickGridAccessors<TGridItem>
    {
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_Services")]
        internal static extern void SetServices(QuickGrid<TGridItem> target, IServiceProvider value);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_JS")]
        internal static extern void SetJS(QuickGrid<TGridItem> target, IJSRuntime value);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_NavigationManager")]
        internal static extern void SetNavigationManager(QuickGrid<TGridItem> target, NavigationManager value);
    }

    private static class ColumnBaseAccessors<TGridItem>
    {
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_InternalGridContext")]
        internal static extern InternalGridContext<TGridItem> GetInternalGridContext(ColumnBase<TGridItem> target);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_InternalGridContext")]
        internal static extern void SetInternalGridContext(
            ColumnBase<TGridItem> target,
            InternalGridContext<TGridItem> value);
    }

    private static class ColumnsCollectedNotifierAccessors<TGridItem>
    {
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_InternalGridContext")]
        internal static extern InternalGridContext<TGridItem> GetInternalGridContext(
            ColumnsCollectedNotifier<TGridItem> target);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_InternalGridContext")]
        internal static extern void SetInternalGridContext(
            ColumnsCollectedNotifier<TGridItem> target,
            InternalGridContext<TGridItem> value);
    }

    private static class VirtualizeDescriptorFactory
    {
        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CreateVirtualizeDescriptors")]
        internal static extern ComponentDescriptor[] CreateDescriptors<TItem>(
            [UnsafeAccessorType(
                "Microsoft.AspNetCore.Components.Infrastructure.BuiltInComponentDescriptors, Microsoft.AspNetCore.Components.Web")]
            object? target);
    }

    private static class PaginatorAccessors
    {
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_NavigationManager")]
        internal static extern void SetNavigationManager(Paginator target, NavigationManager value);
    }
}

#pragma warning restore ASPNETCORE9004
