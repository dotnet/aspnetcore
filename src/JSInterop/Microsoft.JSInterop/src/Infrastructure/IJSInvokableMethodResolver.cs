// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.JSInterop.Infrastructure;

internal interface IJSInvokableMethodResolver
{
    bool TryResolve(
        in JSInvokableMethodInfo methodInfo,
        [NotNullWhen(true)] out JSInvokableMethodDescriptor? descriptor);
}
