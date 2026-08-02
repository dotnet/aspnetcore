// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.TestObjects;

internal class TestServiceProvider : IServiceProvider
{
    public object GetService(Type serviceType) => null;
}
