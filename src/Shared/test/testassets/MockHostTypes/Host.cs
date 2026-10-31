// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace MockHostTypes;

public class Host : IHost
{
    public IServiceProvider Services { get; } = new ServiceProvider();
}
