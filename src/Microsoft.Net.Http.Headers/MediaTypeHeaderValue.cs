// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Headers
{
    public class MediaTypeHeaderValue
    {
        private const string CharsetString = "charset";
        private const string BoundaryString = "boundary";

        private static readonly HttpHeaderParser<MediaTypeHeaderValue> SingleValueParser
            = new GenericHeaderParser<MediaTypeHeaderValue>(false, GetMediaTypeLength);
        private static readonly HttpHeaderParser<MediaTypeHeaderValue> MultipleValueParser
            = new GenericHeaderParser<MediaTypeHeaderValue>(true, GetMediaTypeLength);

        // Use a collection instead of a dictionary since we may have multiple parameters with the same name.
        private ObjectCollection<NameValueHeaderValue> _parameters;
        private StringSegment _mediaType;
        private bool _isReadOnly;

        private MediaTypeHeaderValue()
        {
            // Used by the parser to create a new instance of this type.
        }

        public MediaTypeHeaderValue(StringSegment mediaType)
        {
            CheckMediaTypeFormat(mediaType, "mediaType");
            _mediaType = mediaType;
        }

        public MediaTypeHeaderValue(StringSegment mediaType, double quality)
            : this(mediaType)
        {
            Quality = quality;
        }

        public StringSegment Charset
        {
            get
            {
                return NameValueHeaderValue.Find(_parameters, CharsetString)?.Value.Value;
            }
            set
            {
                HeaderUtilities.ThrowIfReadOnly(IsReadOnly);
                // We don't prevent a user from setting whitespace-only charsets. Like we can't prevent a user from
                // setting a non-existing charset.
                var charsetParameter = NameValueHeaderValue.Find(_parameters, CharsetString);
                if (StringSegment.IsNullOrEmpty(value))
                {
                    // Remove charset parameter
                    if (charsetParameter != null)
                    {
                        Parameters.Remove(charsetParameter);
                    }
                }
                else
                {
                    if (charsetParameter != null)
                    {
                        charsetParameter.Value = value;
                    }
                    else
                    {
                        Parameters.Add(new NameValueHeaderValue(CharsetString, value));
                    }
                }
            }
        }

        public Encoding Encoding
        {
            get
            {
                var charset = Charset;
                if (!StringSegment.IsNullOrEmpty(charset))
                {
                    try
                    {
                        return Encoding.GetEncoding(charset.Value);
                    }
                    catch (ArgumentException)
                    {
                        // Invalid or not supported
                    }
                }
                return null;
            }
            set
            {
                HeaderUtilities.ThrowIfReadOnly(IsReadOnly);
                if (value == null)
                {
                    Charset = null;
                }
                else
                {
                    Charset = value.WebName;
                }
            }
        }

        public StringSegment Boundary
        {
            get
            {
                return NameValueHeaderValue.Find(_parameters, BoundaryString)?.Value ?? default(StringSegment);
            }
            set
            {
                HeaderUtilities.ThrowIfReadOnly(IsReadOnly);
                var boundaryParameter = NameValueHeaderValue.Find(_parameters, BoundaryString);
                if (StringSegment.IsNullOrEmpty(value))
                {
                    // Remove charset parameter
                    if (boundaryParameter != null)
                    {
                        Parameters.Remove(boundaryParameter);
                    }
                }
                else
                {
                    if (boundaryParameter != null)
                    {
                        boundaryParameter.Value = value;
                    }
                    else
                    {
                        Parameters.Add(new NameValueHeaderValue(BoundaryString, value));
                    }
                }
            }
        }

        public IList<NameValueHeaderValue> Parameters
        {
            get
            {
                if (_parameters == null)
                {
                    if (IsReadOnly)
                    {
                        _parameters = ObjectCollection<NameValueHeaderValue>.EmptyReadOnlyCollection;
                    }
                    else
                    {
                        _parameters = new ObjectCollection<NameValueHeaderValue>();
                    }
                }
                return _parameters;
            }
        }

        public double? Quality
        {
            get { return HeaderUtilities.GetQuality(_parameters); }
            set
            {
                HeaderUtilities.ThrowIfReadOnly(IsReadOnly);
                HeaderUtilities.SetQuality(Parameters, value);
            }
        }

        public StringSegment MediaType
        {
            get { return _mediaType; }
            set
            {
                HeaderUtilities.ThrowIfReadOnly(IsReadOnly);
                CheckMediaTypeFormat(value, "value");
                _mediaType = value;
            }
        }

        public StringSegment Type
        {
            get
            {
                return _mediaType.Subsegment(0, _mediaType.IndexOf('/'));
            }
        }

        public StringSegment SubType
        {
            get
            {
                return _mediaType.Subsegment(_mediaType.IndexOf('/') + 1);
            }
        }

        /// <summary>
        /// MediaType = "*/*"
        /// </summary>
        public bool MatchesAllTypes
        {
            get
            {
                return MediaType.Equals("*/*", StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// SubType = "*"
        /// </summary>
        public bool MatchesAllSubTypes
        {
            get
            {
                return SubType.Equals("*", StringComparison.Ordinal);
            }
        }

        public bool IsReadOnly
        {
            get { return _isReadOnly; }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="MediaTypeHeaderValue"/> is a subset of
        /// <paramref name="otherMediaType"/>. A "subset" is defined as the same or a more specific media type
        /// according to the precedence described in https://www.ietf.org/rfc/rfc2068.txt section 14.1, Accept.
        /// </summary>
        /// <param name="otherMediaType">The <see cref="MediaTypeHeaderValue"/> to compare.</param>
        /// <returns>
        /// A value indicating whether this <see cref="MediaTypeHeaderValue"/> is a subset of
        /// <paramref name="otherMediaType"/>.
        /// </returns>
        /// <remarks>
        /// For example "multipart/mixed; boundary=1234" is a subset of "multipart/mixed; boundary=1234",
        /// "multipart/mixed", "multipart/*", and "*/*" but not "multipart/mixed; boundary=2345" or
        /// "multipart/message; boundary=1234".
        /// </remarks>
        public bool IsSubsetOf(MediaTypeHeaderValue otherMediaType)
        {
            if (otherMediaType == null)
            {
                return false;
            }

            // "text/plain" is a subset of "text/plain", "text/*" and "*/*". "*/*" is a subset only of "*/*".
            if (!Type.Equals(otherMediaType.Type, comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                if (!otherMediaType.MatchesAllTypes)
                {
                    return false;
                }
            }
            else if (!SubType.Equals(otherMediaType.SubType, comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                if (!otherMediaType.MatchesAllSubTypes)
                {
                    return false;
                }
            }

            // "text/plain; charset=utf-8; level=1" is a subset of "text/plain; charset=utf-8". In turn
            // "text/plain; charset=utf-8" is a subset of "text/plain".
            if (otherMediaType._parameters != null && otherMediaType._parameters.Count != 0)
            {
                // Make sure all parameters in the potential superset are included locally. Fine to have additional
                // parameters locally; they make this one more specific.
                foreach (var parameter in otherMediaType._parameters)
                {
                    if (parameter.Name.Equals("q", StringComparison.OrdinalIgnoreCase))
                    {
                        // "q" and later parameters are not involved in media type matching. Quoting the RFC: The first
                        // "q" parameter (if any) separates the media-range parameter(s) from the accept-params.
                        break;
                    }

                    var localParameter = NameValueHeaderValue.Find(_parameters, parameter.Name);
                    if (localParameter == null)
                    {
                        // Not found.
                        return false;
                    }

                    if (!StringSegment.Equals(parameter.Value, localParameter.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Performs a deep copy of this object and all of it's NameValueHeaderValue sub components,
        /// while avoiding the cost of revalidating the components.
        /// </summary>
        /// <returns>A deep copy.</returns>
        public MediaTypeHeaderValue Copy()
        {
            var other = new MediaTypeHeaderValue();
            other._mediaType = _mediaType;

            if (_parameters != null)
            {
                other._parameters = new ObjectCollection<NameValueHeaderValue>(
                    _parameters.Select(item => item.Copy()));
            }
            return other;
        }

        /// <summary>
        /// Performs a deep copy of this object and all of it's NameValueHeaderValue sub components,
        /// while avoiding the cost of revalidating the components. This copy is read-only.
        /// </summary>
        /// <returns>A deep, read-only, copy.</returns>
        public MediaTypeHeaderValue CopyAsReadOnly()
        {
            if (IsReadOnly)
            {
                return this;
            }

            var other = new MediaTypeHeaderValue();
            other._mediaType = _mediaType;
            if (_parameters != null)
            {
                other._parameters = new ObjectCollection<NameValueHeaderValue>(
                    _parameters.Select(item => item.CopyAsReadOnly()), isReadOnly: true);
            }
            other._isReadOnly = true;
            return other;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append(_mediaType);
            NameValueHeaderValue.ToString(_parameters, separator: ';', leadingSeparator: true, destination: builder);
            return builder.ToString();
        }

        public override bool Equals(object obj)
        {
            var other = obj as MediaTypeHeaderValue;

            if (other == null)
            {
                return false;
            }

            return _mediaType.Equals(other._mediaType, StringComparison.OrdinalIgnoreCase) &&
                HeaderUtilities.AreEqualCollections(_parameters, other._parameters);
        }

        public override int GetHashCode()
        {
            // The media-type string is case-insensitive.
            return StringSegmentComparer.OrdinalIgnoreCase.GetHashCode(_mediaType) ^ NameValueHeaderValue.GetHashCode(_parameters);
        }

        public static MediaTypeHeaderValue Parse(StringSegment input)
        {
            var index = 0;
            return SingleValueParser.ParseValue(input, ref index);
        }

        public static bool TryParse(StringSegment input, out MediaTypeHeaderValue parsedValue)
        {
            var index = 0;
            return SingleValueParser.TryParseValue(input, ref index, out parsedValue);
        }

        public static IList<MediaTypeHeaderValue> ParseList(IList<string> inputs)
        {
            return MultipleValueParser.ParseValues(inputs);
        }

        public static IList<MediaTypeHeaderValue> ParseStrictList(IList<string> inputs)
        {
            return MultipleValueParser.ParseStrictValues(inputs);
        }

        public static bool TryParseList(IList<string> inputs, out IList<MediaTypeHeaderValue> parsedValues)
        {
            return MultipleValueParser.TryParseValues(inputs, out parsedValues);
        }

        public static bool TryParseStrictList(IList<string> inputs, out IList<MediaTypeHeaderValue> parsedValues)
        {
            return MultipleValueParser.TryParseStrictValues(inputs, out parsedValues);
        }

        private static int GetMediaTypeLength(StringSegment input, int startIndex, out MediaTypeHeaderValue parsedValue)
        {
            Contract.Requires(startIndex >= 0);

            parsedValue = null;

            if (StringSegment.IsNullOrEmpty(input) || (startIndex >= input.Length))
            {
                return 0;
            }

            // Caller must remove leading whitespace. If not, we'll return 0.
            var mediaTypeLength = MediaTypeHeaderValue.GetMediaTypeExpressionLength(input, startIndex, out var mediaType);

            if (mediaTypeLength == 0)
            {
                return 0;
            }

            var current = startIndex + mediaTypeLength;
            current = current + HttpRuleParser.GetWhitespaceLength(input, current);
            MediaTypeHeaderValue mediaTypeHeader = null;

            // If we're not done and we have a parameter delimiter, then we have a list of parameters.
            if ((current < input.Length) && (input[current] == ';'))
            {
                mediaTypeHeader = new MediaTypeHeaderValue();
                mediaTypeHeader._mediaType = mediaType;

                current++; // skip delimiter.
                var parameterLength = NameValueHeaderValue.GetNameValueListLength(input, current, ';',
                    mediaTypeHeader.Parameters);

                parsedValue = mediaTypeHeader;
                return current + parameterLength - startIndex;
            }

            // We have a media type without parameters.
            mediaTypeHeader = new MediaTypeHeaderValue();
            mediaTypeHeader._mediaType = mediaType;
            parsedValue = mediaTypeHeader;
            return current - startIndex;
        }

        private static int GetMediaTypeExpressionLength(StringSegment input, int startIndex, out StringSegment mediaType)
        {
            Contract.Requires((input != null) && (input.Length > 0) && (startIndex < input.Length));

            // This method just parses the "type/subtype" string, it does not parse parameters.
            mediaType = null;

            // Parse the type, i.e. <type> in media type string "<type>/<subtype>; param1=value1; param2=value2"
            var typeLength = HttpRuleParser.GetTokenLength(input, startIndex);

            if (typeLength == 0)
            {
                return 0;
            }

            var current = startIndex + typeLength;
            current = current + HttpRuleParser.GetWhitespaceLength(input, current);

            // Parse the separator between type and subtype
            if ((current >= input.Length) || (input[current] != '/'))
            {
                return 0;
            }
            current++; // skip delimiter.
            current = current + HttpRuleParser.GetWhitespaceLength(input, current);

            // Parse the subtype, i.e. <subtype> in media type string "<type>/<subtype>; param1=value1; param2=value2"
            var subtypeLength = HttpRuleParser.GetTokenLength(input, current);

            if (subtypeLength == 0)
            {
                return 0;
            }

            // If there is no whitespace between <type> and <subtype> in <type>/<subtype> get the media type using
            // one Substring call. Otherwise get substrings for <type> and <subtype> and combine them.
            var mediaTypeLength = current + subtypeLength - startIndex;
            if (typeLength + subtypeLength + 1 == mediaTypeLength)
            {
                mediaType = input.Subsegment(startIndex, mediaTypeLength);
            }
            else
            {
                mediaType = input.Substring(startIndex, typeLength) + "/" + input.Substring(current, subtypeLength);
            }

            return mediaTypeLength;
        }

        private static void CheckMediaTypeFormat(StringSegment mediaType, string parameterName)
        {
            if (StringSegment.IsNullOrEmpty(mediaType))
            {
                throw new ArgumentException("An empty string is not allowed.", parameterName);
            }

            // When adding values using strongly typed objects, no leading/trailing LWS (whitespace) is allowed.
            // Also no LWS between type and subtype is allowed.
            var mediaTypeLength = GetMediaTypeExpressionLength(mediaType, 0, out var tempMediaType);
            if ((mediaTypeLength == 0) || (tempMediaType.Length != mediaType.Length))
            {
                throw new FormatException(string.Format(CultureInfo.InvariantCulture, "Invalid media type '{0}'.", mediaType));
            }
        }
    }
}
