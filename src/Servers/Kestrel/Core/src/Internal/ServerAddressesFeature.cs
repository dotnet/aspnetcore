// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

[DebuggerDisplay("{DebuggerToString(),nq}")]
[DebuggerTypeProxy(typeof(ServerAddressesFeatureDebugView))]
internal sealed class ServerAddressesFeature : IServerAddressesFeature
{
    public ServerAddressesCollection InternalCollection { get; } = new ServerAddressesCollection();

    ICollection<string> IServerAddressesFeature.Addresses => InternalCollection.PublicCollection;
    public bool PreferHostingUrls { get; set; }

    private string DebuggerToString() => $"Addresses = {InternalCollection.Count}";

    private sealed class ServerAddressesFeatureDebugView(ServerAddressesFeature feature)
    {
        private readonly ServerAddressesFeature _feature = feature;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public string[] Items => _feature.InternalCollection.ToArray();
    }
}
