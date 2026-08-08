// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;

#pragma warning disable ASPNETCORE9004 // Built-in Native AOT metadata consumes the experimental descriptor model.

namespace Microsoft.AspNetCore.Components.Infrastructure;

internal static class BuiltInComponentDescriptors
{
    internal static ComponentDescriptor[] GetDescriptors()
        =>
        [
            CreateAuthorizeViewDescriptor(),
            CreateAuthorizeRouteViewDescriptor(),
            CreateCascadingAuthenticationStateDescriptor(),
            CreateAuthorizeRouteViewCoreDescriptor(),
        ];

    private static ComponentDescriptor CreateAuthorizeViewDescriptor()
        => new()
        {
            Type = typeof(AuthorizeView),
            Parameters =
            [
                CreateAuthenticationStateParameter(),
            ],
            Injectables = CreateAuthorizeViewCoreInjectables(),
        };

    private static ComponentDescriptor CreateAuthorizeRouteViewDescriptor()
        => new()
        {
            Type = typeof(AuthorizeRouteView),
            Parameters =
            [
                new ComponentParameterDescriptor
                {
                    Name = "ExistingCascadedAuthenticationState",
                    ParameterType = typeof(Task<AuthenticationState>),
                    Attribute = new CascadingParameterAttribute(),
                    GetValue = static target =>
                        GetExistingCascadedAuthenticationState((AuthorizeRouteView)target),
                    SetValue = static (target, value) =>
                        SetExistingCascadedAuthenticationState(
                            (AuthorizeRouteView)target,
                            (Task<AuthenticationState>?)value),
                },
            ],
        };

    private static ComponentDescriptor CreateCascadingAuthenticationStateDescriptor()
        => new()
        {
            Type = typeof(CascadingAuthenticationState),
            Injectables =
            [
                new ComponentInjectableDescriptor
                {
                    Name = "AuthenticationStateProvider",
                    ServiceType = typeof(AuthenticationStateProvider),
                    Attribute = new InjectAttribute(),
                    SetValue = static (target, value) =>
                        SetAuthenticationStateProvider(
                            (CascadingAuthenticationState)target,
                            (AuthenticationStateProvider)value!),
                },
            ],
        };

    private static ComponentDescriptor CreateAuthorizeRouteViewCoreDescriptor()
        => new()
        {
            Type = AuthorizeRouteView.GetAuthorizeRouteViewCoreType(),
            CreateInstance = static _ => AuthorizeRouteView.CreateAuthorizeRouteViewCore(),
            Parameters =
            [
                CreateAuthorizeViewCoreParameter<RenderFragment<AuthenticationState>?>(
                    nameof(AuthorizeViewCore.ChildContent),
                    static target => target.ChildContent,
                    static (target, value) => target.ChildContent = value),
                CreateAuthorizeViewCoreParameter<RenderFragment<AuthenticationState>?>(
                    nameof(AuthorizeViewCore.NotAuthorized),
                    static target => target.NotAuthorized,
                    static (target, value) => target.NotAuthorized = value),
                CreateAuthorizeViewCoreParameter<RenderFragment<AuthenticationState>?>(
                    nameof(AuthorizeViewCore.Authorized),
                    static target => target.Authorized,
                    static (target, value) => target.Authorized = value),
                CreateAuthorizeViewCoreParameter<RenderFragment?>(
                    nameof(AuthorizeViewCore.Authorizing),
                    static target => target.Authorizing,
                    static (target, value) => target.Authorizing = value),
                CreateAuthorizeViewCoreParameter<object?>(
                    nameof(AuthorizeViewCore.Resource),
                    static target => target.Resource,
                    static (target, value) => target.Resource = value),
                new ComponentParameterDescriptor
                {
                    Name = "RouteData",
                    ParameterType = typeof(RouteData),
                    Attribute = new ParameterAttribute(),
                    GetValue = static target =>
                        AuthorizeRouteView.GetAuthorizeRouteViewCoreRouteData((AuthorizeViewCore)target),
                    SetValue = static (target, value) =>
                        AuthorizeRouteView.SetAuthorizeRouteViewCoreRouteData(
                            (AuthorizeViewCore)target,
                            (RouteData)value!),
                },
                CreateAuthenticationStateParameter(),
            ],
            Injectables = CreateAuthorizeViewCoreInjectables(),
        };

    private static ComponentParameterDescriptor CreateAuthorizeViewCoreParameter<TValue>(
        string name,
        Func<AuthorizeViewCore, TValue> getValue,
        Action<AuthorizeViewCore, TValue> setValue)
        => new()
        {
            Name = name,
            ParameterType = typeof(TValue),
            Attribute = new ParameterAttribute(),
            GetValue = target => getValue((AuthorizeViewCore)target),
            SetValue = (target, value) =>
                setValue((AuthorizeViewCore)target, value is null ? default! : (TValue)value),
        };

    private static ComponentParameterDescriptor CreateAuthenticationStateParameter()
        => new()
        {
            Name = "AuthenticationState",
            ParameterType = typeof(Task<AuthenticationState>),
            Attribute = new CascadingParameterAttribute(),
            GetValue = static target => GetAuthenticationState((AuthorizeViewCore)target),
            SetValue = static (target, value) =>
                SetAuthenticationState((AuthorizeViewCore)target, (Task<AuthenticationState>?)value),
        };

    private static ComponentInjectableDescriptor[] CreateAuthorizeViewCoreInjectables()
        =>
        [
            new ComponentInjectableDescriptor
            {
                Name = "AuthorizationPolicyProvider",
                ServiceType = typeof(IAuthorizationPolicyProvider),
                Attribute = new InjectAttribute(),
                SetValue = static (target, value) =>
                    SetAuthorizationPolicyProvider(
                        (AuthorizeViewCore)target,
                        (IAuthorizationPolicyProvider)value!),
            },
            new ComponentInjectableDescriptor
            {
                Name = "AuthorizationService",
                ServiceType = typeof(IAuthorizationService),
                Attribute = new InjectAttribute(),
                SetValue = static (target, value) =>
                    SetAuthorizationService((AuthorizeViewCore)target, (IAuthorizationService)value!),
            },
        ];

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_AuthenticationState")]
    private static extern Task<AuthenticationState>? GetAuthenticationState(AuthorizeViewCore target);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_AuthenticationState")]
    private static extern void SetAuthenticationState(
        AuthorizeViewCore target,
        Task<AuthenticationState>? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_AuthorizationPolicyProvider")]
    private static extern void SetAuthorizationPolicyProvider(
        AuthorizeViewCore target,
        IAuthorizationPolicyProvider value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_AuthorizationService")]
    private static extern void SetAuthorizationService(
        AuthorizeViewCore target,
        IAuthorizationService value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_ExistingCascadedAuthenticationState")]
    private static extern Task<AuthenticationState>? GetExistingCascadedAuthenticationState(
        AuthorizeRouteView target);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_ExistingCascadedAuthenticationState")]
    private static extern void SetExistingCascadedAuthenticationState(
        AuthorizeRouteView target,
        Task<AuthenticationState>? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_AuthenticationStateProvider")]
    private static extern void SetAuthenticationStateProvider(
        CascadingAuthenticationState target,
        AuthenticationStateProvider value);
}

#pragma warning restore ASPNETCORE9004
