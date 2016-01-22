// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace Microsoft.AspNet.Http
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
            int index;
            if (string.IsNullOrEmpty(_value))
            {
                return string.Empty;
            }
            else if (_value.IndexOf('[') >= 0)
            {
                // IPv6 in brackets [::1], maybe with port
                return _value;
            }
            else if ((index = _value.IndexOf(':')) >= 0
                && index < _value.Length - 1
                && _value.IndexOf(':', index + 1) >= 0)
            {
                // IPv6 without brackets ::1 is the only type of host with 2 or more colons
                return $"[{_value}]";
            }
            else if (index >= 0)
            {
                // Has a port
                string port = _value.Substring(index);
                var mapping = new IdnMapping();
                return mapping.GetAscii(_value, 0, index) + port;
            }
            else
            {
                var mapping = new IdnMapping();
                return mapping.GetAscii(_value);
            }
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
                return false;
            }
            return obj is HostString && Equals((HostString)obj);
        }

        /// <summary>
        /// Gets a hash code for the value.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return (_value != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(_value) : 0);
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
    }
}
