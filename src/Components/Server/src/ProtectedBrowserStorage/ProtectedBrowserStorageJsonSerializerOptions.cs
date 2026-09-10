// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

internal sealed class ProtectedBrowserStorageJsonSerializerOptions
{
    public ProtectedBrowserStorageJsonSerializerOptions(IOptions<CircuitOptions> circuitOptions)
    {
        Options = new JsonSerializerOptions(JsonSerializerOptionsProvider.Options);

#pragma warning disable ASPNETCORE9004 // The framework implements this experimental extension point.
        for (var i = circuitOptions.Value.JsonTypeInfoResolvers.Count - 1; i >= 0; i--)
        {
            Options.TypeInfoResolverChain.Insert(0, circuitOptions.Value.JsonTypeInfoResolvers[i]);
        }
#pragma warning restore ASPNETCORE9004
    }

    public JsonSerializerOptions Options { get; }
}
