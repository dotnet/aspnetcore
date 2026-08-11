// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;

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
                Type = typeof(Endpoints.SSRRenderModeBoundary),
            },
            new ComponentDescriptor
            {
                Type = typeof(ConfigureBrowser),
                CreateInstance = static _ => new ConfigureBrowser(),
                Parameters =
                [
                    CreateParameter<ConfigureBrowser, HttpContext?>(
                        nameof(ConfigureBrowser.HttpContext),
                        static target => target.HttpContext,
                        static (target, value) => target.HttpContext = value,
                        new CascadingParameterAttribute()),
                ],
            },
            new ComponentDescriptor
            {
                Type = typeof(Endpoints.BasePath),
                CreateInstance = static _ => new Endpoints.BasePath(),
                Injectables =
                [
                    CreateInjectable<Endpoints.BasePath, NavigationManager>(
                        "NavigationManager",
                        SetNavigationManager),
                ],
            },
            new ComponentDescriptor
            {
                Type = typeof(ResourcePreloader),
                CreateInstance = static _ => new ResourcePreloader(),
                Injectables =
                [
                    CreateInjectable<ResourcePreloader, Endpoints.ResourcePreloadService>(
                        nameof(ResourcePreloader.Service),
                        static (target, value) => target.Service = value),
                ],
            },
            new ComponentDescriptor
            {
                Type = typeof(CacheView),
                CreateInstance = static _ => new CacheView(),
                Parameters =
                [
                    CreateParameter<CacheView, RenderFragment?>(
                        nameof(CacheView.ChildContent),
                        static target => target.ChildContent,
                        static (target, value) => target.ChildContent = value),
                    CreateParameter<CacheView, string?>(
                        nameof(CacheView.CacheKey),
                        static target => target.CacheKey,
                        static (target, value) => target.CacheKey = value),
                    CreateParameter<CacheView, bool>(
                        nameof(CacheView.Enabled),
                        static target => target.Enabled,
                        static (target, value) => target.Enabled = value),
                    CreateParameter<CacheView, TimeSpan?>(
                        nameof(CacheView.ExpiresAfter),
                        static target => target.ExpiresAfter,
                        static (target, value) => target.ExpiresAfter = value),
                    CreateParameter<CacheView, DateTimeOffset?>(
                        nameof(CacheView.ExpiresOn),
                        static target => target.ExpiresOn,
                        static (target, value) => target.ExpiresOn = value),
                    CreateParameter<CacheView, TimeSpan?>(
                        nameof(CacheView.ExpiresSliding),
                        static target => target.ExpiresSliding,
                        static (target, value) => target.ExpiresSliding = value),
                    CreateParameter<CacheView, string?>(
                        nameof(CacheView.VaryByQuery),
                        static target => target.VaryByQuery,
                        static (target, value) => target.VaryByQuery = value),
                    CreateParameter<CacheView, string?>(
                        nameof(CacheView.VaryByRoute),
                        static target => target.VaryByRoute,
                        static (target, value) => target.VaryByRoute = value),
                    CreateParameter<CacheView, string?>(
                        nameof(CacheView.VaryByHeader),
                        static target => target.VaryByHeader,
                        static (target, value) => target.VaryByHeader = value),
                    CreateParameter<CacheView, string?>(
                        nameof(CacheView.VaryByCookie),
                        static target => target.VaryByCookie,
                        static (target, value) => target.VaryByCookie = value),
                    CreateParameter<CacheView, bool>(
                        nameof(CacheView.VaryByUser),
                        static target => target.VaryByUser,
                        static (target, value) => target.VaryByUser = value),
                    CreateParameter<CacheView, bool>(
                        nameof(CacheView.VaryByCulture),
                        static target => target.VaryByCulture,
                        static (target, value) => target.VaryByCulture = value),
                    CreateParameter<CacheView, string?>(
                        nameof(CacheView.VaryBy),
                        static target => target.VaryBy,
                        static (target, value) => target.VaryBy = value),
                    CreateParameter<CacheView, HttpContext?>(
                        nameof(CacheView.HttpContext),
                        static target => target.HttpContext,
                        static (target, value) => target.HttpContext = value,
                        new CascadingParameterAttribute()),
                ],
                Injectables =
                [
                    CreateInjectable<CacheView, Endpoints.CacheViewService>(
                        nameof(CacheView.CacheService),
                        static (target, value) => target.CacheService = value),
                ],
            },
            new ComponentDescriptor
            {
                Type = typeof(Endpoints.RazorComponentEndpointHost),
                CreateInstance = static _ => new Endpoints.RazorComponentEndpointHost(),
                Parameters =
                [
                    CreateParameter<Endpoints.RazorComponentEndpointHost, Type>(
                        nameof(Endpoints.RazorComponentEndpointHost.ComponentType),
                        static target => target.ComponentType,
                        SetComponentType),
                    CreateParameter<Endpoints.RazorComponentEndpointHost, IReadOnlyDictionary<string, object?>?>(
                        nameof(Endpoints.RazorComponentEndpointHost.ComponentParameters),
                        static target => target.ComponentParameters,
                        static (target, value) => target.ComponentParameters = value),
                ],
            },
        ];

    private static ComponentParameterDescriptor CreateParameter<TComponent, TValue>(
        string name,
        Func<TComponent, TValue> getValue,
        Action<TComponent, TValue> setValue,
        Attribute? attribute = null)
        where TComponent : IComponent
        => new()
        {
            Name = name,
            ParameterType = typeof(TValue),
            Attribute = attribute ?? new ParameterAttribute(),
            GetValue = target => getValue((TComponent)target),
            SetValue = (target, value) => setValue(
                (TComponent)target,
                value is null ? default! : (TValue)value),
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

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2067",
        Justification = "RazorComponentEndpointHost parameter binding is backed by its built-in component descriptor.")]
    private static void SetComponentType(Endpoints.RazorComponentEndpointHost target, Type value)
        => target.ComponentType = value;

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_NavigationManager")]
    private static extern void SetNavigationManager(Endpoints.BasePath target, NavigationManager value);
}
