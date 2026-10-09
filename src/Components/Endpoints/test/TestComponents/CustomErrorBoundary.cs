// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Web;

namespace Microsoft.AspNetCore.Components.Endpoints.Tests.TestComponents;

public sealed class CustomErrorBoundary : ErrorBoundary
{
    protected override Task OnErrorAsync(Exception exception)
        => Task.CompletedTask;
}
