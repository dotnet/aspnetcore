// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.InternalTesting;

/// <summary>
/// Defines a lifecycle for attributes or classes that want to know about tests starting
/// or ending. Implement this on a test class, or attribute at the method/class/assembly level.
/// </summary>
/// <remarks>
/// Requires defining <see cref="AspNetTestFramework"/> as the test framework.
/// </remarks>
public interface ITestMethodLifecycle
{
    Task OnTestStartAsync(TestContext context, CancellationToken cancellationToken);

    Task OnTestEndAsync(TestContext context, Exception exception, CancellationToken cancellationToken);
}
