// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

internal sealed class AddressBindContext
{
    public AddressBindContext(
        ServerAddressesFeature serverAddressesFeature,
        KestrelServerOptions serverOptions,
        ILogger logger,
        Func<ListenOptions, CancellationToken, Task> createBinding)
    {
        ServerAddressesFeature = serverAddressesFeature;
        ServerOptions = serverOptions;
        Logger = logger;
        CreateBinding = createBinding;
    }

    public ServerAddressesFeature ServerAddressesFeature { get; }
    public ICollection<string> Addresses => ServerAddressesFeature.InternalCollection;

    public KestrelServerOptions ServerOptions { get; }
    public ILogger Logger { get; }

    public Func<ListenOptions, CancellationToken, Task> CreateBinding { get; }
}
