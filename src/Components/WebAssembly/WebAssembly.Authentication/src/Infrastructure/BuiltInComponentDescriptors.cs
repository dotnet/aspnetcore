// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Logging;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

#pragma warning disable ASPNETCORE9004 // Built-in Native AOT metadata consumes the experimental descriptor model.

namespace Microsoft.AspNetCore.Components.Infrastructure;

internal static class BuiltInComponentDescriptors
{
    internal static ComponentDescriptor[] GetDescriptors()
        =>
        [
            new ComponentDescriptor
            {
                Type = typeof(RemoteAuthenticatorView),
                Injectables = CreateRemoteAuthenticatorViewCoreInjectables<RemoteAuthenticationState>(),
            },
        ];

    internal static ComponentDescriptor[] CreateRemoteAuthenticatorViewCoreDescriptors<
        [DynamicallyAccessedMembers(JsonSerialized)] TAuthenticationState>()
        where TAuthenticationState : RemoteAuthenticationState
        =>
        [
            new ComponentDescriptor
            {
                Type = typeof(RemoteAuthenticatorViewCore<TAuthenticationState>),
                Injectables = CreateRemoteAuthenticatorViewCoreInjectables<TAuthenticationState>(),
            },
        ];

    private static ComponentInjectableDescriptor[] CreateRemoteAuthenticatorViewCoreInjectables<
        [DynamicallyAccessedMembers(JsonSerialized)] TAuthenticationState>()
        where TAuthenticationState : RemoteAuthenticationState
        =>
        [
            new ComponentInjectableDescriptor
            {
                Name = nameof(RemoteAuthenticatorViewCore<TAuthenticationState>.Navigation),
                ServiceType = typeof(NavigationManager),
                Attribute = new InjectAttribute(),
                SetValue = static (target, value) =>
                    RemoteAuthenticatorViewCoreAccessors<TAuthenticationState>.SetNavigation(
                        (RemoteAuthenticatorViewCore<TAuthenticationState>)target,
                        (NavigationManager)value!),
            },
            new ComponentInjectableDescriptor
            {
                Name = nameof(RemoteAuthenticatorViewCore<TAuthenticationState>.AuthenticationService),
                ServiceType = typeof(IRemoteAuthenticationService<TAuthenticationState>),
                Attribute = new InjectAttribute(),
                SetValue = static (target, value) =>
                    RemoteAuthenticatorViewCoreAccessors<TAuthenticationState>.SetAuthenticationService(
                        (RemoteAuthenticatorViewCore<TAuthenticationState>)target,
                        (IRemoteAuthenticationService<TAuthenticationState>)value!),
            },
            new ComponentInjectableDescriptor
            {
                Name = nameof(RemoteAuthenticatorViewCore<TAuthenticationState>.RemoteApplicationPathsProvider),
                ServiceType = typeof(IRemoteAuthenticationPathsProvider),
                Attribute = new InjectAttribute(),
                SetValue = static (target, value) =>
                    RemoteAuthenticatorViewCoreAccessors<TAuthenticationState>.SetRemoteApplicationPathsProvider(
                        (RemoteAuthenticatorViewCore<TAuthenticationState>)target,
                        (IRemoteAuthenticationPathsProvider)value!),
            },
            new ComponentInjectableDescriptor
            {
                Name = nameof(RemoteAuthenticatorViewCore<TAuthenticationState>.AuthenticationProvider),
                ServiceType = typeof(AuthenticationStateProvider),
                Attribute = new InjectAttribute(),
                SetValue = static (target, value) =>
                    RemoteAuthenticatorViewCoreAccessors<TAuthenticationState>.SetAuthenticationProvider(
                        (RemoteAuthenticatorViewCore<TAuthenticationState>)target,
                        (AuthenticationStateProvider)value!),
            },
            new ComponentInjectableDescriptor
            {
                Name = nameof(RemoteAuthenticatorViewCore<TAuthenticationState>.Logger),
                ServiceType = typeof(ILogger<RemoteAuthenticatorViewCore<TAuthenticationState>>),
                Attribute = new InjectAttribute(),
                SetValue = static (target, value) =>
                    RemoteAuthenticatorViewCoreAccessors<TAuthenticationState>.SetLogger(
                        (RemoteAuthenticatorViewCore<TAuthenticationState>)target,
                        (ILogger<RemoteAuthenticatorViewCore<TAuthenticationState>>)value!),
            },
        ];

    private static class RemoteAuthenticatorViewCoreAccessors<
        [DynamicallyAccessedMembers(JsonSerialized)] TAuthenticationState>
        where TAuthenticationState : RemoteAuthenticationState
    {
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_Navigation")]
        internal static extern void SetNavigation(
            RemoteAuthenticatorViewCore<TAuthenticationState> target,
            NavigationManager value);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_AuthenticationService")]
        internal static extern void SetAuthenticationService(
            RemoteAuthenticatorViewCore<TAuthenticationState> target,
            IRemoteAuthenticationService<TAuthenticationState> value);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_RemoteApplicationPathsProvider")]
        internal static extern void SetRemoteApplicationPathsProvider(
            RemoteAuthenticatorViewCore<TAuthenticationState> target,
            IRemoteAuthenticationPathsProvider value);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_AuthenticationProvider")]
        internal static extern void SetAuthenticationProvider(
            RemoteAuthenticatorViewCore<TAuthenticationState> target,
            AuthenticationStateProvider value);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_Logger")]
        internal static extern void SetLogger(
            RemoteAuthenticatorViewCore<TAuthenticationState> target,
            ILogger<RemoteAuthenticatorViewCore<TAuthenticationState>> value);
    }
}

#pragma warning restore ASPNETCORE9004
