// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using Microsoft.AspNetCore.Connections.Features;

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// Default implementation of <see cref="IConnectionLifetimeNotificationFeature"/>.
    /// </summary>
    internal sealed class DefaultConnectionLifetimeNotificationFeature : IConnectionLifetimeNotificationFeature
    {
        private readonly IHttpResponseFeature? _httpResponseFeature;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpResponseFeature"></param>
        public DefaultConnectionLifetimeNotificationFeature(IHttpResponseFeature? httpResponseFeature)
        {
            _httpResponseFeature = httpResponseFeature;
        }

        ///<inheritdoc/>
        public CancellationToken ConnectionClosedRequested { get; set; }

        ///<inheritdoc/>
        public void RequestClose()
        {
            if (_httpResponseFeature != null)
            {
                if (!_httpResponseFeature.HasStarted)
                {
                    _httpResponseFeature.Headers.Connection = "close";
                }
            }
        }
    }
}
