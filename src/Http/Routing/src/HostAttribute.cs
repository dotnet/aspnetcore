// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Attribute for providing host metdata that is used during routing.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class HostAttribute : Attribute, IHostMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HostAttribute" /> class.
    /// </summary>
    /// <param name="host">
    /// The host used during routing.
    /// Host should be Unicode rather than punycode, and may have a port.
    /// </param>
    public HostAttribute(string host) : this(new[] { host })
    {
        ArgumentNullException.ThrowIfNull(host);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HostAttribute" /> class.
    /// </summary>
    /// <param name="hosts">
    /// The hosts used during routing.
    /// Hosts should be Unicode rather than punycode, and may have a port.
    /// An empty collection means any host will be accepted.
    /// </param>
    public HostAttribute(params string[] hosts)
    {
        ArgumentNullException.ThrowIfNull(hosts);

        Hosts = hosts.ToArray();
    }

    /// <summary>
    /// Returns a read-only collection of hosts used during routing.
    /// Hosts will be Unicode rather than punycode, and may have a port.
    /// An empty collection means any host will be accepted.
    /// </summary>
    public IReadOnlyList<string> Hosts { get; }

    /// <inheritdoc/>
    public override string ToString()
    {
        var hostsDisplay = (Hosts.Count == 0)
            ? "*:*"
            : string.Join(',', Hosts.Select(h => h.Contains(':') ? h : h + ":*"));

        return DebuggerHelpers.GetDebugText(nameof(Hosts), hostsDisplay);
    }
}
