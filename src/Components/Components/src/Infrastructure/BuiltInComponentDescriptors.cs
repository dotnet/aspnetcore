// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Infrastructure;

[UnconditionalSuppressMessage(
    "Trimming",
    "IL2110",
    Justification = "Built-in component descriptors intentionally store statically analyzable member access delegates.")]
[UnconditionalSuppressMessage(
    "Trimming",
    "IL2111",
    Justification = "Built-in component descriptors intentionally store statically analyzable member access delegates.")]
internal static class BuiltInComponentDescriptors
{
    internal static ComponentDescriptor[] GetDescriptors()
        =>
        [
            new ComponentDescriptor
            {
                Type = typeof(Sections.SectionOutlet.SectionOutletContentRenderer),
                CreateInstance = static _ => new Sections.SectionOutlet.SectionOutletContentRenderer(),
            },
            new ComponentDescriptor
            {
                Type = typeof(Router),
                CreateInstance = static _ => new Router(),
                Parameters =
                [
                    CreateParameter<Assembly>(
                        nameof(Router.AppAssembly),
                        static target => target.AppAssembly,
                        static (target, value) => target.AppAssembly = value),
                    CreateParameter<IEnumerable<Assembly>>(
                        nameof(Router.AdditionalAssemblies),
                        static target => target.AdditionalAssemblies,
                        static (target, value) => target.AdditionalAssemblies = value),
#pragma warning disable CS0618 // Router.NotFound is retained for compatibility.
                    CreateParameter<RenderFragment>(
                        nameof(Router.NotFound),
                        static target => target.NotFound,
                        static (target, value) => target.NotFound = value),
#pragma warning restore CS0618
                    CreateParameter<Type?>(
                        nameof(Router.NotFoundPage),
                        static target => target.NotFoundPage,
                        SetNotFoundPage),
                    CreateParameter<RenderFragment<RouteData>>(
                        nameof(Router.Found),
                        static target => target.Found,
                        static (target, value) => target.Found = value),
                    CreateParameter<RenderFragment?>(
                        nameof(Router.Navigating),
                        static target => target.Navigating,
                        static (target, value) => target.Navigating = value),
                    CreateParameter<EventCallback<NavigationContext>>(
                        nameof(Router.OnNavigateAsync),
                        static target => target.OnNavigateAsync,
                        static (target, value) => target.OnNavigateAsync = value),
                ],
                Injectables =
                [
                    CreateInjectable<NavigationManager>(
                        "NavigationManager",
                        SetNavigationManager),
                    CreateInjectable<INavigationInterception>(
                        "NavigationInterception",
                        SetNavigationInterception),
                    CreateInjectable<IScrollToLocationHash>(
                        "ScrollToLocationHash",
                        SetScrollToLocationHash),
                    CreateInjectable<ILoggerFactory>(
                        "LoggerFactory",
                        SetLoggerFactory),
                    CreateInjectable<IServiceProvider>(
                        "ServiceProvider",
                        SetServiceProvider),
                ],
            },
        ];

    internal static ComponentDescriptor[] CreateOwningComponentBaseDescriptors<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>()
        =>
        [
            new ComponentDescriptor
            {
                Type = typeof(TComponent),
                Injectables =
                [
                    new ComponentInjectableDescriptor
                    {
                        Name = "ScopeFactory",
                        ServiceType = typeof(IServiceScopeFactory),
                        Attribute = new InjectAttribute(),
                        SetValue = static (target, value) =>
                            SetScopeFactory((OwningComponentBase)target, (IServiceScopeFactory)value!),
                    },
                ],
            },
        ];

    private static ComponentParameterDescriptor CreateParameter<TValue>(
        string name,
        Func<Router, TValue> getValue,
        Action<Router, TValue> setValue)
        => new()
        {
            Name = name,
            ParameterType = typeof(TValue),
            Attribute = new ParameterAttribute(),
            GetValue = target => getValue((Router)target),
            SetValue = (target, value) => setValue((Router)target, value is null ? default! : (TValue)value),
        };

    private static ComponentInjectableDescriptor CreateInjectable<TValue>(
        string name,
        Action<Router, TValue> setValue)
        => new()
        {
            Name = name,
            ServiceType = typeof(TValue),
            Attribute = new InjectAttribute(),
            SetValue = (target, value) => setValue((Router)target, (TValue)value!),
        };

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2067",
        Justification = "Router activation and parameter binding are backed by its built-in component descriptor.")]
    private static void SetNotFoundPage(Router target, Type? value)
        => target.NotFoundPage = value;

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_NavigationManager")]
    private static extern void SetNavigationManager(Router target, NavigationManager value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_NavigationInterception")]
    private static extern void SetNavigationInterception(Router target, INavigationInterception value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_ScrollToLocationHash")]
    private static extern void SetScrollToLocationHash(Router target, IScrollToLocationHash value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_LoggerFactory")]
    private static extern void SetLoggerFactory(Router target, ILoggerFactory value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_ServiceProvider")]
    private static extern void SetServiceProvider(Router target, IServiceProvider value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_ScopeFactory")]
    private static extern void SetScopeFactory(OwningComponentBase target, IServiceScopeFactory value);
}
