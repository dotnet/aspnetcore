// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR;

/// <summary>
/// A proxy abstraction for invoking hub methods on the client and getting a result.
/// </summary>
public interface ISingleClientProxy : IClientProxy
{
    // client proxy method is called InvokeCoreAsync instead of InvokeAsync so that arrays of references
    // like string[], e.g. InvokeAsync(string, string[]), do not choose InvokeAsync(string, object[])
    // over InvokeAsync(string, object) overload

    /// <summary>
    /// Invokes a method on the connection represented by the <see cref="ISingleClientProxy"/> instance and waits for a result.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="method">Name of the method to invoke.</param>
    /// <param name="args">A collection of arguments to pass to the client.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. It is recommended to set a max wait for expecting a result.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous invoke and wait for a client result.</returns>
    Task<T> InvokeCoreAsync<T>(string method, object?[] args, CancellationToken cancellationToken);
}
