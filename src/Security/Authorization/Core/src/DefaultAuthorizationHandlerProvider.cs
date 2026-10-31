// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// The default implementation of a handler provider,
/// which provides the <see cref="IAuthorizationHandler"/>s for an authorization request.
/// </summary>
public class DefaultAuthorizationHandlerProvider : IAuthorizationHandlerProvider
{
    private readonly Task<IEnumerable<IAuthorizationHandler>> _handlersTask;

    /// <summary>
    /// Creates a new instance of <see cref="DefaultAuthorizationHandlerProvider"/>.
    /// </summary>
    /// <param name="handlers">The <see cref="IAuthorizationHandler"/>s.</param>
    public DefaultAuthorizationHandlerProvider(IEnumerable<IAuthorizationHandler> handlers)
    {
        ArgumentNullThrowHelper.ThrowIfNull(handlers);

        _handlersTask = Task.FromResult(handlers);
    }

    /// <inheritdoc />
    public Task<IEnumerable<IAuthorizationHandler>> GetHandlersAsync(AuthorizationHandlerContext context)
        => _handlersTask;
}
