// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Http
{
    public class BindingAddress
    {
        private const string UnixPipeHostPrefix = "unix:/";

        private BindingAddress(string host, string pathBase, int port, string scheme)
        {
            Host = host;
            PathBase = pathBase;
            Port = port;
            Scheme = scheme;
        }

        [Obsolete("This constructor is obsolete and will be removed in a future version. Use BindingAddress.Parse(address) to create a BindingAddress instance.")]
        public BindingAddress()
        {
            throw new InvalidOperationException("This constructor is obsolete and will be removed in a future version. Use BindingAddress.Parse(address) to create a BindingAddress instance.");
        }

        public string Host { get; }
        public string PathBase { get; }
        public int Port { get; }
        public string Scheme { get; }

        public bool IsUnixPipe
        {
            get
            {
                return Host.StartsWith(UnixPipeHostPrefix, StringComparison.Ordinal);
            }
        }

        public string UnixPipePath
        {
            get
            {
                if (!IsUnixPipe)
                {
                    throw new InvalidOperationException("Binding address is not a unix pipe.");
                }

                return GetUnixPipePath(Host);
            }
        }

        private static string GetUnixPipePath(string host)
        {
            var unixPipeHostPrefixLength = UnixPipeHostPrefix.Length;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // "/" character in unix refers to root. Windows has drive letters and volume separator (c:)
                unixPipeHostPrefixLength--;
            }
            return host.Substring(unixPipeHostPrefixLength);
        }

        public override string ToString()
        {
            if (IsUnixPipe)
            {
                return Scheme.ToLowerInvariant() + Uri.SchemeDelimiter + Host.ToLowerInvariant();
            }
            else
            {
                return Scheme.ToLowerInvariant() + Uri.SchemeDelimiter + Host.ToLowerInvariant() + ":" + Port.ToString(CultureInfo.InvariantCulture) + PathBase;
            }
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            var other = obj as BindingAddress;
            if (other == null)
            {
                return false;
            }
            return string.Equals(Scheme, other.Scheme, StringComparison.OrdinalIgnoreCase)
                && string.Equals(Host, other.Host, StringComparison.OrdinalIgnoreCase)
                && Port == other.Port
                && PathBase == other.PathBase;
        }

        public static BindingAddress Parse(string address)
        {
            // A null/empty address will throw FormatException
            address = address ?? string.Empty;

            int schemeDelimiterStart = address.IndexOf(Uri.SchemeDelimiter, StringComparison.Ordinal);
            if (schemeDelimiterStart < 0)
            {
                throw new FormatException($"Invalid url: '{address}'");
            }
            int schemeDelimiterEnd = schemeDelimiterStart + Uri.SchemeDelimiter.Length;

            var isUnixPipe = address.IndexOf(UnixPipeHostPrefix, schemeDelimiterEnd, StringComparison.Ordinal) == schemeDelimiterEnd;

            int pathDelimiterStart;
            int pathDelimiterEnd;
            if (!isUnixPipe)
            {
                pathDelimiterStart = address.IndexOf("/", schemeDelimiterEnd, StringComparison.Ordinal);
                pathDelimiterEnd = pathDelimiterStart;
            }
            else
            {
                var unixPipeHostPrefixLength = UnixPipeHostPrefix.Length;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Windows has drive letters and volume separator (c:)
                    unixPipeHostPrefixLength += 2;
                    if (schemeDelimiterEnd + unixPipeHostPrefixLength > address.Length)
                    {
                        throw new FormatException($"Invalid url: '{address}'");
                    }
                }

                pathDelimiterStart = address.IndexOf(":", schemeDelimiterEnd + unixPipeHostPrefixLength, StringComparison.Ordinal);
                pathDelimiterEnd = pathDelimiterStart + ":".Length;
            }

            if (pathDelimiterStart < 0)
            {
                pathDelimiterStart = pathDelimiterEnd = address.Length;
            }

            var scheme = address.Substring(0, schemeDelimiterStart);
            string? host = null;
            int port = 0;

            var hasSpecifiedPort = false;
            if (!isUnixPipe)
            {
                int portDelimiterStart = address.LastIndexOf(":", pathDelimiterStart - 1, pathDelimiterStart - schemeDelimiterEnd, StringComparison.Ordinal);
                if (portDelimiterStart >= 0)
                {
                    int portDelimiterEnd = portDelimiterStart + ":".Length;

                    string portString = address.Substring(portDelimiterEnd, pathDelimiterStart - portDelimiterEnd);
                    int portNumber;
                    if (int.TryParse(portString, NumberStyles.Integer, CultureInfo.InvariantCulture, out portNumber))
                    {
                        hasSpecifiedPort = true;
                        host = address.Substring(schemeDelimiterEnd, portDelimiterStart - schemeDelimiterEnd);
                        port = portNumber;
                    }
                }

                if (!hasSpecifiedPort)
                {
                    if (string.Equals(scheme, "http", StringComparison.OrdinalIgnoreCase))
                    {
                        port = 80;
                    }
                    else if (string.Equals(scheme, "https", StringComparison.OrdinalIgnoreCase))
                    {
                        port = 443;
                    }
                }
            }

            if (!hasSpecifiedPort)
            {
                host = address.Substring(schemeDelimiterEnd, pathDelimiterStart - schemeDelimiterEnd);
            }

            if (string.IsNullOrEmpty(host))
            {
                throw new FormatException($"Invalid url: '{address}'");
            }

            if (isUnixPipe && !Path.IsPathRooted(GetUnixPipePath(host)))
            {
                throw new FormatException($"Invalid url, unix socket path must be absolute: '{address}'");
            }

            string pathBase;
            if (address[address.Length - 1] == '/')
            {
                pathBase = address.Substring(pathDelimiterEnd, address.Length - pathDelimiterEnd - 1);
            }
            else
            {
                pathBase = address.Substring(pathDelimiterEnd);
            }

            return new BindingAddress(host: host, pathBase: pathBase, port: port, scheme: scheme);
        }
    }
}
