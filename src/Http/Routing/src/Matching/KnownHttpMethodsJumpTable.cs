// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching;

internal class KnownHttpMethodsJumpTable
{
    public required int ConnectDestination { get; init; }
    public required int DeleteDestination { get; init; }
    public required int GetDestination { get; init; }
    public required int HeadDestination { get; init; }
    public required int OptionsDestination { get; init; }
    public required int PatchDestination { get; init; }
    public required int PostDestination { get; init; }
    public required int PutDestination { get; init; }
    public required int TraceDestination { get; init; }

    public bool TryGetKnownValue(string method, out int destination)
    {
        // Implementation skeleton taken from https://github.com/dotnet/runtime/blob/13b43155c31beb844b1b04766fea65235ccd8363/src/libraries/System.Net.Http/src/System/Net/Http/HttpMethod.cs#L179
        if (method.Length >= 3) // 3 == smallest known method
        {
            (string? matchedMethod, destination) = (method[0] | 0x20) switch
            {
                'c' => (HttpMethods.Connect, ConnectDestination),
                'd' => (HttpMethods.Delete, DeleteDestination),
                'g' => (HttpMethods.Get, GetDestination),
                'h' => (HttpMethods.Head, HeadDestination),
                'o' => (HttpMethods.Options, OptionsDestination),
                'p' => method.Length switch
                {
                    3 => (HttpMethods.Put, PutDestination),
                    4 => (HttpMethods.Post, PostDestination),
                    _ => (HttpMethods.Patch, PatchDestination),
                },
                't' => (HttpMethods.Trace, TraceDestination),
                _ => (null, 0),
            };

            return matchedMethod is not null && method.Equals(matchedMethod, StringComparison.OrdinalIgnoreCase);
        }
        destination = default;
        return false;
    }
}
