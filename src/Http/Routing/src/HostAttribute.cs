// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Attribute for providing host metdata that is used during routing.
    /// </summary>
    [DebuggerDisplay("{DebuggerToString(),nq}")]
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
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }
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
            if (hosts == null)
            {
                throw new ArgumentNullException(nameof(hosts));
            }

            Hosts = hosts.ToArray();
        }

        /// <summary>
        /// Returns a read-only collection of hosts used during routing.
        /// Hosts will be Unicode rather than punycode, and may have a port.
        /// An empty collection means any host will be accepted.
        /// </summary>
        public IReadOnlyList<string> Hosts { get; }

        private string DebuggerToString()
        {
            var hostsDisplay = (Hosts.Count == 0)
                ? "*:*"
                : string.Join(",", Hosts.Select(h => h.Contains(':') ? h : h + ":*"));

            return $"Hosts: {hostsDisplay}";
        }
    }
}
