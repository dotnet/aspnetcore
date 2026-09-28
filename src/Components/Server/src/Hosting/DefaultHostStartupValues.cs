// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Hosting;

internal sealed class DefaultHostStartupValues(
    IServiceProvider services,
    InteractiveServerContext context) : IHostStartupValues
{
    private IHostStartupValues? _values;

    public string? GetValue(string key)
        => Values.GetValue(key);

    public string GetRequired(string key)
        => Values.GetRequired(key);

    private IHostStartupValues Values => _values ??= context.IsInteractive
        ? services.GetRequiredKeyedService<IHostStartupValues>(HostInitializerKey.Server)
        : services.GetKeyedService<IHostStartupValues>(HostInitializerKey.Static)
            ?? services.GetRequiredKeyedService<IHostStartupValues>(HostInitializerKey.Server);
}
