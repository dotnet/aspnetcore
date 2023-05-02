// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core;

internal sealed class AnyIPListenOptions : ListenOptions
{
    internal AnyIPListenOptions(int port)
        : base(new IPEndPoint(IPAddress.IPv6Any, port))
    {
    }

    internal override async Task BindAsync(AddressBindContext context, CancellationToken cancellationToken)
    {
        Debug.Assert(IPEndPoint != null);

        // when address is 'http://hostname:port', 'http://*:port', or 'http://+:port'
        try
        {
            await base.BindAsync(context, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not IOException
            // HttpsConnectionMiddleware.CreateHttp3Options, Http3 doesn't support OnAuthenticate.
            && ex is not NotSupportedException)
        {
            if (context.Logger.IsEnabled(LogLevel.Trace))
            {
                context.Logger.LogTrace(ex, CoreStrings.FailedToBindToIPv6Any, IPEndPoint.Port);
            }
            if (context.Logger.IsEnabled(LogLevel.Debug))
            {
                context.Logger.LogDebug(CoreStrings.FallbackToIPv4Any, IPEndPoint.Port, IPEndPoint.Port);
            }

            // for machines that do not support IPv6
            EndPoint = new IPEndPoint(IPAddress.Any, IPEndPoint.Port);
            await base.BindAsync(context, cancellationToken).ConfigureAwait(false);
        }
    }
}
