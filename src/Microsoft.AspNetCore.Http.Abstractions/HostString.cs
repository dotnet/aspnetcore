// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNetCore.Http.Abstractions;
using Microsoft.AspNetCore.Http.Internal;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Represents the host portion of a URI can be used to construct URI's properly formatted and encoded for use in
    /// HTTP headers.
    /// </summary>
    public struct HostString : IEquatable<HostString>
    {
        private readonly string _value;

        /// <summary>
        /// Creates a new HostString without modification. The value should be Unicode rather than punycode, and may have a port.
        /// IPv4 and IPv6 addresses are also allowed, and also may have ports.
        /// </summary>
        /// <param name="value"></param>
        public HostString(string value)
        {
            _value = value;
        }

        /// <summary>
        /// Creates a new HostString from its host and port parts.
        /// </summary>
        /// <param name="host">The value should be Unicode rather than punycode. IPv6 addresses must use square braces.</param>
        /// <param name="port">A positive, greater than 0 value representing the port in the host string.</param>
        public HostString(string host, int port)
        {
            if(port <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(port), Resources.Exception_PortMustBeGreaterThanZero);
            }

            int index;
            if (host.IndexOf('[') == -1
                && (index = host.IndexOf(':')) >= 0
                && index < host.Length - 1
                && host.IndexOf(':', index + 1) >= 0)
            {
                // IPv6 without brackets ::1 is the only type of host with 2 or more colons
                host =  $"[{host}]";
            }

            _value = host + ":" + port.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Returns the original value from the constructor.
        /// </summary>
        public string Value
        {
            get { return _value; }
        }

        public bool HasValue
        {
            get { return !string.IsNullOrEmpty(_value); }
        }

        /// <summary>
        /// Returns the value of the host part of the value. The port is removed if it was present.
        /// IPv6 addresses will have brackets added if they are missing.
        /// </summary>
        /// <returns></returns>
        public string Host
        {
            get
            {
                string host, port;

                GetParts(out host, out port);

                return host;
            }
        }

        /// <summary>
        /// Returns the value of the port part of the host, or <value>null</value> if none is found.
        /// </summary>
        /// <returns></returns>
        public int? Port
        {
            get
            {
                string host, port;
                int p;

                GetParts(out host, out port);

                if (string.IsNullOrEmpty(port) || !int.TryParse(port, out p))
                {
                    return null;
                }

                return p;
            }
        }

        /// <summary>
        /// Returns the value as normalized by ToUriComponent().
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToUriComponent();
        }

        /// <summary>
        /// Returns the value properly formatted and encoded for use in a URI in a HTTP header.
        /// Any Unicode is converted to punycode. IPv6 addresses will have brackets added if they are missing.
        /// </summary>
        /// <returns></returns>
        public string ToUriComponent()
        {
            if (string.IsNullOrEmpty(_value))
            {
                return string.Empty;
            }

            int i;
            for (i = 0; i < _value.Length; ++i)
            {
                if (!HostStringHelper.IsSafeHostStringChar(_value[i]))
                {
                    break;
                }
            }

            if (i != _value.Length)
            {
                string host, port;
                GetParts(out host, out port);

                var mapping = new IdnMapping();
                host = mapping.GetAscii(host);

                return string.IsNullOrEmpty(port)
                    ? host
                    : string.Concat(host, ":", port);
            }

            return _value;
        }

        /// <summary>
        /// Creates a new HostString from the given URI component.
        /// Any punycode will be converted to Unicode.
        /// </summary>
        /// <param name="uriComponent"></param>
        /// <returns></returns>
        public static HostString FromUriComponent(string uriComponent)
        {
            if (!string.IsNullOrEmpty(uriComponent))
            {
                int index;
                if (uriComponent.IndexOf('[') >= 0)
                {
                    // IPv6 in brackets [::1], maybe with port
                }
                else if ((index = uriComponent.IndexOf(':')) >= 0
                    && index < uriComponent.Length - 1
                    && uriComponent.IndexOf(':', index + 1) >= 0)
                {
                    // IPv6 without brackets ::1 is the only type of host with 2 or more colons
                }
                else if (uriComponent.IndexOf("xn--", StringComparison.Ordinal) >= 0)
                {
                    // Contains punycode
                    if (index >= 0)
                    {
                        // Has a port
                        string port = uriComponent.Substring(index);
                        var mapping = new IdnMapping();
                        uriComponent = mapping.GetUnicode(uriComponent, 0, index) + port;
                    }
                    else
                    {
                        var mapping = new IdnMapping();
                        uriComponent = mapping.GetUnicode(uriComponent);
                    }
                }
            }
            return new HostString(uriComponent);
        }

        /// <summary>
        /// Creates a new HostString from the host and port of the give Uri instance.
        /// Punycode will be converted to Unicode.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static HostString FromUriComponent(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            return new HostString(uri.GetComponents(
                UriComponents.NormalizedHost | // Always convert punycode to Unicode.
                UriComponents.HostAndPort, UriFormat.Unescaped));
        }

        /// <summary>
        /// Compares the equality of the Value property, ignoring case.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(HostString other)
        {
            if (!HasValue && !other.HasValue)
            {
                return true;
            }
            return string.Equals(_value, other._value, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Compares against the given object only if it is a HostString.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return !HasValue;
            }
            return obj is HostString && Equals((HostString)obj);
        }

        /// <summary>
        /// Gets a hash code for the value.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return (HasValue ? StringComparer.OrdinalIgnoreCase.GetHashCode(_value) : 0);
        }

        /// <summary>
        /// Compares the two instances for equality.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==(HostString left, HostString right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares the two instances for inequality.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(HostString left, HostString right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Parses the current value. IPv6 addresses will have brackets added if they are missing.
        /// </summary>
        private void GetParts(out string host, out string port)
        {
            int index;
            port = null;
            host = null;

            if (string.IsNullOrEmpty(_value))
            {
                return;
            }
            else if ((index = _value.IndexOf(']')) >= 0)
            {
                // IPv6 in brackets [::1], maybe with port
                host = _value.Substring(0, index + 1);

                if ((index = _value.IndexOf(':', index + 1)) >= 0)
                {
                    port = _value.Substring(index + 1);
                }
            }
            else if ((index = _value.IndexOf(':')) >= 0
                && index < _value.Length - 1
                && _value.IndexOf(':', index + 1) >= 0)
            {
                // IPv6 without brackets ::1 is the only type of host with 2 or more colons
                host = $"[{_value}]";
                port = null;
            }
            else if (index >= 0)
            {
                // Has a port
                host = _value.Substring(0, index);
                port = _value.Substring(index + 1);
            }
            else
            {
                host = _value;
                port = null;
            }
        }
    }
}
