// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    internal partial class Http1Connection : IHttpMinRequestBodyDataRateFeature,
                                             IHttpMinResponseDataRateFeature,
                                             IPersistentStateFeature
    {
        // Persistent state collection is not reset with a request by design.
        // If SocketsConections are pooled in the future this state could be moved
        // to the transport layer.
        private IDictionary<object, object?>? _persistentState;

        MinDataRate? IHttpMinRequestBodyDataRateFeature.MinDataRate
        {
            get => MinRequestBodyDataRate;
            set => MinRequestBodyDataRate = value;
        }

        MinDataRate? IHttpMinResponseDataRateFeature.MinDataRate
        {
            get => MinResponseDataRate;
            set => MinResponseDataRate = value;
        }

        IDictionary<object, object?> IPersistentStateFeature.State
        {
            get
            {
                // Lazily allocate persistent state
                return _persistentState ?? (_persistentState = new ConnectionItems());
            }
        }
    }
}
