// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ApiExplorerWebSite;

public class WellKnownChangeToken
{
    public CancellationTokenSource TokenSource { get; } = new CancellationTokenSource();
}
