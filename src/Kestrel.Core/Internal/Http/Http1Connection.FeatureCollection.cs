// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    public partial class Http1Connection : IHttpUpgradeFeature
    {
        bool IHttpUpgradeFeature.IsUpgradableRequest => IsUpgradableRequest;

        async Task<Stream> IHttpUpgradeFeature.UpgradeAsync()
        {
            if (!((IHttpUpgradeFeature)this).IsUpgradableRequest)
            {
                throw new InvalidOperationException(CoreStrings.CannotUpgradeNonUpgradableRequest);
            }

            if (IsUpgraded)
            {
                throw new InvalidOperationException(CoreStrings.UpgradeCannotBeCalledMultipleTimes);
            }

            if (!ServiceContext.ConnectionManager.UpgradedConnectionCount.TryLockOne())
            {
                throw new InvalidOperationException(CoreStrings.UpgradedConnectionLimitReached);
            }

            IsUpgraded = true;

            ConnectionFeatures.Get<IDecrementConcurrentConnectionCountFeature>()?.ReleaseConnection();

            StatusCode = StatusCodes.Status101SwitchingProtocols;
            ReasonPhrase = "Switching Protocols";
            ResponseHeaders["Connection"] = "Upgrade";

            await FlushAsync(default(CancellationToken));

            return _streams.Upgrade();
        }
    }
}
