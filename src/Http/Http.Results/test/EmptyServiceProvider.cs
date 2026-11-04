// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.HttpResults;

internal sealed class EmptyServiceProvider : IServiceProvider
{
    public static IServiceProvider Instance { get; } = new EmptyServiceProvider();

    public object GetService(Type serviceType) => null;
}
