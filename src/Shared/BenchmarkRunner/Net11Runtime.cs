// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes;

internal sealed class Net11Runtime : Runtime
{
    public static readonly Net11Runtime Instance = new();

    private Net11Runtime()
        : base(RuntimeMoniker.Net10_0, "net11.0", ".NET 11.0")
    {
    }
}
