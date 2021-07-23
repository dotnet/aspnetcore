// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal
{
    internal sealed partial class QuicStreamContext : IPersistentStateFeature
    {
        private IDictionary<object, object?>? _persistentState;

        IDictionary<object, object?> IPersistentStateFeature.State
        {
            get
            {
                // Lazily allocate persistent state
                return _persistentState ?? (_persistentState = new ConnectionItems());
            }
        }

        private void InitializeFeatures()
        {
            _currentIPersistentStateFeature = this;
        }
    }
}
