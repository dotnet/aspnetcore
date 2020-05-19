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

        public string Host { get; private set; }
        public string PathBase { get; private set; }
        public int Port { get; internal set; }
        public string Scheme { get; private set; }

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

                var unixPipeHostPrefixLength = UnixPipeHostPrefix.Length;
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // "/" character in unix refers to root. Windows has drive letters and volume separator (c:)
                    unixPipeHostPrefixLength--;
                }
                return Host.Substring(unixPipeHostPrefixLength);
            }
        }


        public override string ToString()
        {
            if (IsUnixPipe)
            {
                return Scheme.ToLowerInvariant() + "://" + Host.ToLowerInvariant();
            }
            else
            {
                return Scheme.ToLowerInvariant() + "://" + Host.ToLowerInvariant() + ":" + Port.ToString(CultureInfo.InvariantCulture) + PathBase;
            }
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override bool Equals(object obj)
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
            address = address ?? string.Empty;

            int schemeDelimiterStart = address.IndexOf("://", StringComparison.Ordinal);
            if (schemeDelimiterStart < 0)
            {
                throw new FormatException($"Invalid url: '{address}'");
            }
            int schemeDelimiterEnd = schemeDelimiterStart + "://".Length;

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

            var serverAddress = new BindingAddress();
            serverAddress.Scheme = address.Substring(0, schemeDelimiterStart);

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
                        serverAddress.Host = address.Substring(schemeDelimiterEnd, portDelimiterStart - schemeDelimiterEnd);
                        serverAddress.Port = portNumber;
                    }
                }

                if (!hasSpecifiedPort)
                {
                    if (string.Equals(serverAddress.Scheme, "http", StringComparison.OrdinalIgnoreCase))
                    {
                        serverAddress.Port = 80;
                    }
                    else if (string.Equals(serverAddress.Scheme, "https", StringComparison.OrdinalIgnoreCase))
                    {
                        serverAddress.Port = 443;
                    }
                }
            }

            if (!hasSpecifiedPort)
            {
                serverAddress.Host = address.Substring(schemeDelimiterEnd, pathDelimiterStart - schemeDelimiterEnd);
            }

            if (string.IsNullOrEmpty(serverAddress.Host))
            {
                throw new FormatException($"Invalid url: '{address}'");
            }

            if (isUnixPipe && !Path.IsPathRooted(serverAddress.UnixPipePath))
            {
                throw new FormatException($"Invalid url, unix socket path must be absolute: '{address}'");
            }

            if (address[address.Length - 1] == '/')
            {
                serverAddress.PathBase = address.Substring(pathDelimiterEnd, address.Length - pathDelimiterEnd - 1);
            }
            else
            {
                serverAddress.PathBase = address.Substring(pathDelimiterEnd);
            }

            return serverAddress;
        }
    }
}
