// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.IIS.Core;

internal partial class IISHttpContext : IHttpRequestLifetimeFeature
{
    private CancellationTokenSource? _abortedCts;
    private CancellationToken? _manuallySetRequestAbortToken;
    private readonly object _abortLock = new object();
    protected volatile bool _requestAborted;

    CancellationToken IHttpRequestLifetimeFeature.RequestAborted
    {
        get
        {
            // If a request abort token was previously explicitly set, return it.
            if (_manuallySetRequestAbortToken.HasValue)
            {
                return _manuallySetRequestAbortToken.Value;
            }

            lock (_abortLock)
            {
                if (_requestAborted)
                {
                    return new CancellationToken(true);
                }

                if (_abortedCts == null)
                {
                    _abortedCts = new CancellationTokenSource();
                }

                return _abortedCts.Token;
            }
        }
        set
        {
            // Set an abort token, overriding one we create internally.  This setter and associated
            // field exist purely to support IHttpRequestLifetimeFeature.set_RequestAborted.
            _manuallySetRequestAbortToken = value;
        }
    }

    void IHttpRequestLifetimeFeature.Abort()
    {
        Abort(new ConnectionAbortedException(CoreStrings.ConnectionAbortedByApplication));
    }
}
