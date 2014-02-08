//------------------------------------------------------------------------------
// <copyright file="HttpListener.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Globalization;

namespace Microsoft.AspNet.Server.WebListener
{
    internal class Prefix
    {
        private Prefix(bool isHttps, string scheme, string host, string port, int portValue, string path)
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
        public static Prefix Create(string scheme, string host, string port, string path)
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

            int portValue;
            if (string.IsNullOrWhiteSpace(port))
            {
                port = isHttps ? "443" : "80";
                portValue = isHttps ? 443 : 80;
            }
            else
            {
                portValue = int.Parse(port, NumberStyles.None, CultureInfo.InvariantCulture);
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

            return new Prefix(isHttps, scheme, host, port, portValue, path);
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
            return string.Equals(Whole, obj as string, StringComparison.OrdinalIgnoreCase);
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
