// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Authentication;

internal class WebApplicationAuthenticationBuilder : AuthenticationBuilder
{
    public bool IsAuthenticationConfigured { get; private set; }

    public WebApplicationAuthenticationBuilder(IServiceCollection services) : base(services) { }

    public override AuthenticationBuilder AddPolicyScheme(string authenticationScheme, string? displayName, Action<PolicySchemeOptions> configureOptions)
    {
        RegisterServices(authenticationScheme);
        return base.AddPolicyScheme(authenticationScheme, displayName, configureOptions);
    }

    public override AuthenticationBuilder AddRemoteScheme<TOptions, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>(string authenticationScheme, string? displayName, Action<TOptions>? configureOptions)
    {
        RegisterServices(authenticationScheme);
        return base.AddRemoteScheme<TOptions, THandler>(authenticationScheme, displayName, configureOptions);
    }

    public override AuthenticationBuilder AddScheme<TOptions, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>(string authenticationScheme, string? displayName, Action<TOptions>? configureOptions)
    {
        RegisterServices(authenticationScheme);
        return base.AddScheme<TOptions, THandler>(authenticationScheme, displayName, configureOptions);
    }

    public override AuthenticationBuilder AddScheme<TOptions, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>(string authenticationScheme, Action<TOptions>? configureOptions)
    {
        RegisterServices(authenticationScheme);
        return base.AddScheme<TOptions, THandler>(authenticationScheme, configureOptions);
    }

    private void RegisterServices(string authenticationScheme)
    {
        IsAuthenticationConfigured = true;
        Services.AddAuthentication(authenticationScheme);
        Services.AddAuthorization();
    }
}
