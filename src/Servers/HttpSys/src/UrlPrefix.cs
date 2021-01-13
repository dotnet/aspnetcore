// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNetCore.HttpSys.Internal;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    public class UrlPrefix
    {
        private UrlPrefix(bool isHttps, string scheme, string host, string port, int portValue, string path)
        {
            IsHttps = isHttps;
            Scheme = scheme;
            Host = host;
            Port = port;
            HostAndPort = string.Format(CultureInfo.InvariantCulture, "{0}:{1}", Host, Port);
            PortValue = portValue;
            Path = path;
            PathWithoutTrailingSlash = Path.Length > 1 ? Path[0..^1] : string.Empty;
            FullPrefix = string.Format(CultureInfo.InvariantCulture, "{0}://{1}:{2}{3}", Scheme, Host, Port, Path);
        }

        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/windows/desktop/aa364698(v=vs.85).aspx
        /// </summary>
        /// <param name="scheme">http or https. Will be normalized to lower case.</param>
        /// <param name="host">+, *, IPv4, [IPv6], or a dns name. Http.Sys does not permit punycode (xn--), use Unicode instead.</param>
        /// <param name="port">If empty, the default port for the given scheme will be used (80 or 443).</param>
        /// <param name="path">Should start and end with a '/', though a missing trailing slash will be added. This value must be un-escaped.</param>
        public static UrlPrefix Create(string scheme, string host, string port, string path)
        {
            int? portValue = null;
            if (!string.IsNullOrWhiteSpace(port))
            {
                portValue = int.Parse(port, NumberStyles.None, CultureInfo.InvariantCulture);
            }

            return UrlPrefix.Create(scheme, host, portValue, path);
        }

        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/windows/desktop/aa364698(v=vs.85).aspx
        /// </summary>
        /// <param name="scheme">http or https. Will be normalized to lower case.</param>
        /// <param name="host">+, *, IPv4, [IPv6], or a dns name. Http.Sys does not permit punycode (xn--), use Unicode instead.</param>
        /// <param name="portValue">If empty, the default port for the given scheme will be used (80 or 443).</param>
        /// <param name="path">Should start and end with a '/', though a missing trailing slash will be added. This value must be un-escaped.</param>
        public static UrlPrefix Create(string scheme, string host, int? portValue, string path)
        {
            bool isHttps;
            if (string.Equals(Constants.HttpScheme, scheme, StringComparison.OrdinalIgnoreCase))
            {
                scheme = Constants.HttpScheme; // Always use a lower case scheme
                isHttps = false;
            }
            else if (string.Equals(Constants.HttpsScheme, scheme, StringComparison.OrdinalIgnoreCase))
            {
                scheme = Constants.HttpsScheme; // Always use a lower case scheme
                isHttps = true;
            }
            else
            {
                throw new ArgumentOutOfRangeException("scheme", scheme, Resources.Exception_UnsupportedScheme);
            }

            if (string.IsNullOrWhiteSpace(host))
            {
                throw new ArgumentNullException("host");
            }

            string port;
            if (!portValue.HasValue)
            {
                port = isHttps ? "443" : "80";
                portValue = isHttps ? 443 : 80;
            }
            else
            {
                port = portValue.Value.ToString(CultureInfo.InvariantCulture);
            }

            // Http.Sys requires the path end with a slash.
            if (string.IsNullOrWhiteSpace(path))
            {
                path = "/";
            }
            else if (!path.EndsWith("/", StringComparison.Ordinal))
            {
                path += "/";
            }

            return new UrlPrefix(isHttps, scheme, host, port, portValue.Value, path);
        }

        public static UrlPrefix Create(string prefix)
        {
            string scheme = null;
            string host = null;
            int? port = null;
            string path = null;
            var whole = prefix ?? string.Empty;

            var schemeDelimiterEnd = whole.IndexOf("://", StringComparison.Ordinal);
            if (schemeDelimiterEnd < 0)
            {
                throw new FormatException("Invalid prefix, missing scheme separator: " + prefix);
            }
            var hostDelimiterStart = schemeDelimiterEnd + "://".Length;

            var pathDelimiterStart = whole.IndexOf("/", hostDelimiterStart, StringComparison.Ordinal);
            if (pathDelimiterStart < 0)
            {
                pathDelimiterStart = whole.Length;
            }
            var hostDelimiterEnd = whole.LastIndexOf(":", pathDelimiterStart - 1, pathDelimiterStart - hostDelimiterStart, StringComparison.Ordinal);
            if (hostDelimiterEnd < 0)
            {
                hostDelimiterEnd = pathDelimiterStart;
            }

            scheme = whole.Substring(0, schemeDelimiterEnd);
            var portString = whole.Substring(hostDelimiterEnd, pathDelimiterStart - hostDelimiterEnd); // The leading ":" is included
            int portValue;
            if (!string.IsNullOrEmpty(portString))
            {
                var portValueString = portString.Substring(1); // Trim the leading ":"
                if (int.TryParse(portValueString, NumberStyles.Integer, CultureInfo.InvariantCulture, out portValue))
                {
                    host = whole.Substring(hostDelimiterStart, hostDelimiterEnd - hostDelimiterStart);
                    port = portValue;
                }
                else
                {
                    // This means a port was specified but was invalid or empty.
                    throw new FormatException("Invalid prefix, invalid port specified: " + prefix);
                }
            }
            else
            {
                host = whole.Substring(hostDelimiterStart, pathDelimiterStart - hostDelimiterStart);
            }
            path = whole.Substring(pathDelimiterStart);

            return Create(scheme, host, port, path);
        }

        public bool IsHttps { get; }
        public string Scheme { get; }
        public string Host { get; }
        public string Port { get; }
        internal string HostAndPort { get; }
        public int PortValue { get; }
        public string Path { get; }
        internal string PathWithoutTrailingSlash { get; }
        public string FullPrefix { get; }

        public override bool Equals(object obj)
        {
            return string.Equals(FullPrefix, Convert.ToString(obj), StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(FullPrefix);
        }

        public override string ToString()
        {
            return FullPrefix;
        }
    }
}
