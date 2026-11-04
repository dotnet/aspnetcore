// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

/// <summary>
/// Options for remote authentication.
/// </summary>
/// <typeparam name="TRemoteAuthenticationProviderOptions">The type of the underlying provider options.</typeparam>
public class RemoteAuthenticationOptions<[DynamicallyAccessedMembers(JsonSerialized)] TRemoteAuthenticationProviderOptions> where TRemoteAuthenticationProviderOptions : new()
{
    /// <summary>
    /// Gets or sets the provider options.
    /// </summary>
    public TRemoteAuthenticationProviderOptions ProviderOptions { get; } = new TRemoteAuthenticationProviderOptions();

    /// <summary>
    /// Gets or sets the <see cref="RemoteAuthenticationApplicationPathsOptions"/>.
    /// </summary>
    public RemoteAuthenticationApplicationPathsOptions AuthenticationPaths { get; } = new RemoteAuthenticationApplicationPathsOptions();

    /// <summary>
    /// Gets or sets the <see cref="RemoteAuthenticationUserOptions"/>.
    /// </summary>
    public RemoteAuthenticationUserOptions UserOptions { get; } = new RemoteAuthenticationUserOptions();
}
