using System;

namespace Microsoft.AspNet.Abstractions
{
    /// <summary>
    /// Provides correct handling for QueryString value when needed to reconstruct a request or redirect URI string
    /// </summary>
    public struct QueryString : IEquatable<QueryString>
    {
        /// <summary>
        /// Represents the empty query string. This field is read-only.
        /// </summary>
        public static readonly QueryString Empty = new QueryString(String.Empty);

        private readonly string _value;

        /// <summary>
        /// Initalize the query string with a given value. This value must be in escaped and delimited format without 
        /// a leading '?' character. 
        /// </summary>
        /// <param name="value">The query string to be assigned to the Value property.</param>
        public QueryString(string value)
        {
            _value = value;
        }

        /// <summary>
        /// Initialize a query string with a single given parameter name and value. The value is 
        /// </summary>
        /// <param name="name">The unencoded parameter name</param>
        /// <param name="value">The unencoded parameter value</param>
        public QueryString(string name, string value)
        {
            _value = Uri.EscapeDataString(name) + '=' + Uri.EscapeDataString(value);
        }

        /// <summary>
        /// The unescaped query string without the leading '?' character
        /// </summary>
        public string Value
        {
            get { return _value; }
        }

        /// <summary>
        /// True if the query string is not empty
        /// </summary>
        public bool HasValue
        {
            get { return !String.IsNullOrWhiteSpace(_value); }
        }

        /// <summary>
        /// Provides the query string escaped in a way which is correct for combining into the URI representation. 
        /// A leading '?' character will be prepended unless the Value is null or empty. Characters which are potentally
        /// dangerous are escaped.
        /// </summary>
        /// <returns>The query string value</returns>
        public override string ToString()
        {
            return ToUriComponent();
        }

        /// <summary>
        /// Provides the query string escaped in a way which is correct for combining into the URI representation. 
        /// A leading '?' character will be prepended unless the Value is null or empty. Characters which are potentially
        /// dangerous are escaped.
        /// </summary>
        /// <returns>The query string value</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1055:UriReturnValuesShouldNotBeStrings", Justification = "Purpose of the method is to return a string")]
        public string ToUriComponent()
        {
            // Escape things properly so System.Uri doesn't mis-interpret the data.
            return HasValue ? "?" + _value.Replace("#", "%23") : String.Empty;
        }

        /// <summary>
        /// Returns an QueryString given the query as it is escaped in the URI format. The string MUST NOT contain any
        /// value that is not a query.
        /// </summary>
        /// <param name="uriComponent">The escaped query as it appears in the URI format.</param>
        /// <returns>The resulting QueryString</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1057:StringUriOverloadsCallSystemUriOverloads", Justification = "Delimiter characters ? and # must be escaped by this method instead of truncating the value")]
        public static QueryString FromUriComponent(string uriComponent)
        {
            if (String.IsNullOrEmpty(uriComponent))
            {
                return new QueryString(string.Empty);
            }
            if (uriComponent[0] != '?')
            {
                throw new ArgumentException(""/*Resources.Exception_QueryStringMustStartWithDelimiter*/, "uriComponent");
            }
            return new QueryString(uriComponent.Substring(1));
        }

        /// <summary>
        /// Returns an QueryString given the query as from a Uri object. Relative Uri objects are not supported.
        /// </summary>
        /// <param name="uri">The Uri object</param>
        /// <returns>The resulting QueryString</returns>
        public static QueryString FromUriComponent(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }
            return new QueryString(uri.GetComponents(UriComponents.Query, UriFormat.UriEscaped));
        }

        public bool Equals(QueryString other)
        {
            return string.Equals(_value, other._value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            return obj is QueryString && Equals((QueryString)obj);
        }

        public override int GetHashCode()
        {
            return (_value != null ? _value.GetHashCode() : 0);
        }

        public static bool operator ==(QueryString left, QueryString right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(QueryString left, QueryString right)
        {
            return !left.Equals(right);
        }
    }
}
