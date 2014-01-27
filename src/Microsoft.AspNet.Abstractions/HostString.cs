using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Microsoft.AspNet.Abstractions
{
    /// <summary>
    /// Represents the host portion of a Uri can be used to construct Uri's properly formatted and encoded for use in
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
        [SuppressMessage("Microsoft.Design", "CA1055:UriReturnValuesShouldNotBeStrings", Justification = "Only the host segment of a uri is returned.")]
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
                return "[" + _value + "]";
            }
            else if (index >= 0)
            {
                // Has a port
                string port = _value.Substring(index);
                IdnMapping mapping = new IdnMapping();
                return mapping.GetAscii(_value, 0, index) + port;
            }
            else
            {
                IdnMapping mapping = new IdnMapping();
                return mapping.GetAscii(_value);
            }
        }

        /// <summary>
        /// Creates a new HostString from the given uri component.
        /// Any punycode will be converted to Unicode.
        /// </summary>
        /// <param name="uriComponent"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1057:StringUriOverloadsCallSystemUriOverloads", Justification = "Only the host segment of a uri is provided.")]
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
                        IdnMapping mapping = new IdnMapping();
                        uriComponent = mapping.GetUnicode(uriComponent, 0, index) + port;
                    }
                    else
                    {
                        IdnMapping mapping = new IdnMapping();
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
                throw new ArgumentNullException("uri");
            }
            return new HostString(uri.GetComponents(
#if !NET40
                UriComponents.NormalizedHost | // Always convert punycode to Unicode.
#endif
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

#if K10
        internal class IdnMapping
        {
            //
            // Summary:
            //     Encodes a string of domain name labels that consist of Unicode characters
            //     to a string of displayable Unicode characters in the US-ASCII character range.
            //     The string is formatted according to the IDNA standard.
            //
            // Parameters:
            //   unicode:
            //     The string to convert, which consists of one or more domain name labels delimited
            //     with label separators.
            //
            // Returns:
            //     The equivalent of the string specified by the unicode parameter, consisting
            //     of displayable Unicode characters in the US-ASCII character range (U+0020
            //     to U+007E) and formatted according to the IDNA standard.
            //
            // Exceptions:
            //   System.ArgumentNullException:
            //     unicode is null.
            //
            //   System.ArgumentException:
            //     unicode is invalid based on the System.Globalization.IdnMapping.AllowUnassigned
            //     and System.Globalization.IdnMapping.UseStd3AsciiRules properties, and the
            //     IDNA standard.
            public string GetAscii(string unicode) { throw new NotImplementedException(); }

            //
            // Summary:
            //     Encodes a substring of domain name labels that include Unicode characters
            //     outside the US-ASCII character range. The substring is converted to a string
            //     of displayable Unicode characters in the US-ASCII character range and is
            //     formatted according to the IDNA standard.
            //
            // Parameters:
            //   unicode:
            //     The string to convert, which consists of one or more domain name labels delimited
            //     with label separators.
            //
            //   index:
            //     A zero-based offset into unicode that specifies the start of the substring
            //     to convert. The conversion operation continues to the end of the unicode
            //     string.
            //
            // Returns:
            //     The equivalent of the substring specified by the unicode and index parameters,
            //     consisting of displayable Unicode characters in the US-ASCII character range
            //     (U+0020 to U+007E) and formatted according to the IDNA standard.
            //
            // Exceptions:
            //   System.ArgumentNullException:
            //     unicode is null.
            //
            //   System.ArgumentOutOfRangeException:
            //     index is less than zero.-or-index is greater than the length of unicode.
            //
            //   System.ArgumentException:
            //     unicode is invalid based on the System.Globalization.IdnMapping.AllowUnassigned
            //     and System.Globalization.IdnMapping.UseStd3AsciiRules properties, and the
            //     IDNA standard.
            public string GetAscii(string unicode, int index) { throw new NotImplementedException(); }

            //
            // Summary:
            //     Encodes the specified number of characters in a substring of domain name
            //     labels that include Unicode characters outside the US-ASCII character range.
            //     The substring is converted to a string of displayable Unicode characters
            //     in the US-ASCII character range and is formatted according to the IDNA standard.
            //
            // Parameters:
            //   unicode:
            //     The string to convert, which consists of one or more domain name labels delimited
            //     with label separators.
            //
            //   index:
            //     A zero-based offset into unicode that specifies the start of the substring.
            //
            //   count:
            //     The number of characters to convert in the substring that starts at the position
            //     specified by index in the unicode string.
            //
            // Returns:
            //     The equivalent of the substring specified by the unicode, index, and count
            //     parameters, consisting of displayable Unicode characters in the US-ASCII
            //     character range (U+0020 to U+007E) and formatted according to the IDNA standard.
            //
            // Exceptions:
            //   System.ArgumentNullException:
            //     unicode is null.
            //
            //   System.ArgumentOutOfRangeException:
            //     index or count is less than zero.-or-index is greater than the length of
            //     unicode.-or-index is greater than the length of unicode minus count.
            //
            //   System.ArgumentException:
            //     unicode is invalid based on the System.Globalization.IdnMapping.AllowUnassigned
            //     and System.Globalization.IdnMapping.UseStd3AsciiRules properties, and the
            //     IDNA standard.
            public string GetAscii(string unicode, int index, int count) { throw new NotImplementedException(); }

            //
            // Summary:
            //     Decodes a string of one or more domain name labels, encoded according to
            //     the IDNA standard, to a string of Unicode characters.
            //
            // Parameters:
            //   ascii:
            //     The string to decode, which consists of one or more labels in the US-ASCII
            //     character range (U+0020 to U+007E) encoded according to the IDNA standard.
            //
            // Returns:
            //     The Unicode equivalent of the IDNA substring specified by the ascii parameter.
            //
            // Exceptions:
            //   System.ArgumentNullException:
            //     ascii is null.
            //
            //   System.ArgumentException:
            //     ascii is invalid based on the System.Globalization.IdnMapping.AllowUnassigned
            //     and System.Globalization.IdnMapping.UseStd3AsciiRules properties, and the
            //     IDNA standard.
            public string GetUnicode(string ascii) { throw new NotImplementedException(); }

            //
            // Summary:
            //     Decodes a substring of one or more domain name labels, encoded according
            //     to the IDNA standard, to a string of Unicode characters.
            //
            // Parameters:
            //   ascii:
            //     The string to decode, which consists of one or more labels in the US-ASCII
            //     character range (U+0020 to U+007E) encoded according to the IDNA standard.
            //
            //   index:
            //     A zero-based offset into ascii that specifies the start of the substring
            //     to decode. The decoding operation continues to the end of the ascii string.
            //
            // Returns:
            //     The Unicode equivalent of the IDNA substring specified by the ascii and index
            //     parameters.
            //
            // Exceptions:
            //   System.ArgumentNullException:
            //     ascii is null.
            //
            //   System.ArgumentOutOfRangeException:
            //     index is less than zero.-or-index is greater than the length of ascii.
            //
            //   System.ArgumentException:
            //     ascii is invalid based on the System.Globalization.IdnMapping.AllowUnassigned
            //     and System.Globalization.IdnMapping.UseStd3AsciiRules properties, and the
            //     IDNA standard.
            public string GetUnicode(string ascii, int index) { throw new NotImplementedException(); }

            //
            // Summary:
            //     Decodes a substring of a specified length that contains one or more domain
            //     name labels, encoded according to the IDNA standard, to a string of Unicode
            //     characters.
            //
            // Parameters:
            //   ascii:
            //     The string to decode, which consists of one or more labels in the US-ASCII
            //     character range (U+0020 to U+007E) encoded according to the IDNA standard.
            //
            //   index:
            //     A zero-based offset into ascii that specifies the start of the substring.
            //
            //   count:
            //     The number of characters to convert in the substring that starts at the position
            //     specified by index in the ascii string.
            //
            // Returns:
            //     The Unicode equivalent of the IDNA substring specified by the ascii, index,
            //     and count parameters.
            //
            // Exceptions:
            //   System.ArgumentNullException:
            //     ascii is null.
            //
            //   System.ArgumentOutOfRangeException:
            //     index or count is less than zero.-or-index is greater than the length of
            //     ascii.-or-index is greater than the length of ascii minus count.
            //
            //   System.ArgumentException:
            //     ascii is invalid based on the System.Globalization.IdnMapping.AllowUnassigned
            //     and System.Globalization.IdnMapping.UseStd3AsciiRules properties, and the
            //     IDNA standard.
            public string GetUnicode(string ascii, int index, int count) { throw new NotImplementedException(); }
        }
#endif
    }
}
