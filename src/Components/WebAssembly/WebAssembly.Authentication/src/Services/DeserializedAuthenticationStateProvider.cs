// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

internal sealed class DeserializedAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly Task<AuthenticationState> _defaultUnauthenticatedTask =
        Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    private readonly Task<AuthenticationState> _authenticationStateTask;

    [SupplyParameterFromPersistentComponentState]
    private AuthenticationStateData? AuthStateData { get; set; }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = $"{nameof(DeserializedAuthenticationStateProvider)} uses the {nameof(DynamicDependencyAttribute)} to preserve the necessary members.")]
    [DynamicDependency(JsonSerialized, typeof(AuthenticationStateData))]
    [DynamicDependency(JsonSerialized, typeof(IList<ClaimData>))]
    [DynamicDependency(JsonSerialized, typeof(ClaimData))]
    public DeserializedAuthenticationStateProvider(IOptions<AuthenticationStateDeserializationOptions> options)
    {
        _authenticationStateTask = AuthStateData is not null
            ? options.Value.DeserializationCallback(AuthStateData)
            : _defaultUnauthenticatedTask;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => _authenticationStateTask;
}
