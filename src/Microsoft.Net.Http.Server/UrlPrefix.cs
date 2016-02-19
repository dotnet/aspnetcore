// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

//------------------------------------------------------------------------------
// <copyright file="UrlPrefix.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Globalization;

namespace Microsoft.Net.Http.Server
{
    public class UrlPrefix
    {
        private UrlPrefix(bool isHttps, string scheme, string host, string port, int portValue, string path)
        {
            IsHttps = isHttps;
            Scheme = scheme;
            Host = host;
            Port = port;
            PortValue = portValue;
            Path = path;
            Whole = string.Format(CultureInfo.InvariantCulture, "{0}://{1}:{2}{3}", Scheme, Host, Port, Path);
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
            string whole = prefix ?? string.Empty;

            int delimiterStart1 = whole.IndexOf("://", StringComparison.Ordinal);
            if (delimiterStart1 < 0)
            {
                int aPort;
                if (int.TryParse(whole, NumberStyles.None, CultureInfo.InvariantCulture, out aPort))
                {
                    return UrlPrefix.Create("http", "localhost", aPort, "/");
                }

                throw new FormatException("Invalid prefix, missing scheme separator: " + prefix);
            }
            int delimiterEnd1 = delimiterStart1 + "://".Length;

            int delimiterStart3 = whole.IndexOf("/", delimiterEnd1, StringComparison.Ordinal);
            if (delimiterStart3 < 0)
            {
                delimiterStart3 = whole.Length;
            }
            int delimiterStart2 = whole.LastIndexOf(":", delimiterStart3 - 1, delimiterStart3 - delimiterEnd1, StringComparison.Ordinal);
            int delimiterEnd2 = delimiterStart2 + ":".Length;
            if (delimiterStart2 < 0)
            {
                delimiterStart2 = delimiterStart3;
                delimiterEnd2 = delimiterStart3;
            }

            scheme = whole.Substring(0, delimiterStart1);
            string portString = whole.Substring(delimiterEnd2, delimiterStart3 - delimiterEnd2);
            int portValue;
            if (int.TryParse(portString, NumberStyles.Integer, CultureInfo.InvariantCulture, out portValue))
            {
                host = whole.Substring(delimiterEnd1, delimiterStart2 - delimiterEnd1);
                port = portValue;
            }
            else
            {
                host = whole.Substring(delimiterEnd1, delimiterStart3 - delimiterEnd1);
            }
            path = whole.Substring(delimiterStart3);

            return UrlPrefix.Create(scheme, host, port, path);
        }

        public bool IsHttps { get; private set; }
        public string Scheme { get; private set; }
        public string Host { get; private set; }
        public string Port { get; private set; }
        public int PortValue { get; private set; }
        public string Path { get; private set; }
        public string Whole { get; private set; }

        public override bool Equals(object obj)
        {
            return string.Equals(Whole, Convert.ToString(obj), StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(Whole);
        }

        public override string ToString()
        {
            return Whole;
        }
    }
}
