// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Testing.xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class DnsHostNameIsResolvableAttribute : Attribute, ITestCondition
    {
        private string _hostname;

        public bool IsMet
        {
            get
            {
                try
                {
                    _hostname = Dns.GetHostName();
                    var addresses = Dns.GetHostAddresses(_hostname);
                    if (addresses.Any(i => !IPAddress.IsLoopback(i)))
                    {
                        return true;
                    }
                }
                catch
                { }

                return false;
            }
        }

        public string SkipReason => _hostname != null
            ? $"Could not resolve any non-loopback IP address(es) for hostname '{_hostname}'"
            : "Could not determine hostname for current test machine";
    }
}
