using System;
using System.Text;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc.Formatters
{
    /// <summary>
    /// A set of operations to manipulate the encoding of a media type value.
    /// </summary>
    public class MediaTypeEncoding
    {
        /// <summary>
        /// Gets the <see cref="Encoding"/> for the given <see cref="mediaType"/> if it exists.
        /// </summary>
        /// <param name="mediaType">The media type from which to get the charset parameter.</param>
        /// <returns>The <see cref="Encoding"/> of the media type if it exists; otherwise <code>null</code>.</returns>
        public static Encoding GetEncoding(StringSegment mediaType)
        {
            var charset = GetCharsetParameter(mediaType);
            return GetEncodingFromCharset(charset);
        }

        /// <summary>
        /// Gets the <see cref="Encoding"/> for the given <see cref="mediaType"/> if it exists.
        /// </summary>
        /// <param name="mediaType">The media type from which to get the charset parameter.</param>
        /// <returns>The <see cref="Encoding"/> of the media type if it exists or a <see cref="StringSegment"/> without value if not.</returns>
        public static Encoding GetEncoding(string mediaType)
        {
            var charset = GetCharsetParameter(new StringSegment(mediaType));
            return GetEncodingFromCharset(charset);
        }

        /// <summary>
        /// Gets the charset parameter of the given <paramref name="mediaType"/> if it exists.
        /// </summary>
        /// <param name="mediaType">The media type from which to get the charset parameter.</param>
        /// <returns>The charset of the media type if it exists or a <see cref="StringSegment"/> without value if not.</returns>
        public static StringSegment GetCharsetParameter(StringSegment mediaType)
        {
            MediaTypeHeaderValue parsedMediaType;
            if (MediaTypeHeaderValue.TryParse(mediaType.Value, out parsedMediaType))
            {
                return new StringSegment(parsedMediaType.Charset);
            }
            return new StringSegment();
        }

        /// <summary>
        /// Replaces the encoding of the given <paramref name="mediaType"/> with the provided
        /// <paramref name="encoding"/>.
        /// </summary>
        /// <param name="mediaType">The media type whose encoding will be replaced.</param>
        /// <param name="encoding">The encoding that will replace the encoding in the <paramref name="mediaType"/></param>
        /// <returns>A media type with the replaced encoding.</returns>
        public static string ReplaceEncoding(string mediaType, Encoding encoding)
        {
            return ReplaceEncoding(new StringSegment(mediaType), encoding);
        }

        /// <summary>
        /// Replaces the encoding of the given <paramref name="mediaType"/> with the provided
        /// <paramref name="encoding"/>.
        /// </summary>
        /// <param name="mediaType">The media type whose encoding will be replaced.</param>
        /// <param name="encoding">The encoding that will replace the encoding in the <paramref name="mediaType"/></param>
        /// <returns>A media type with the replaced encoding.</returns>
        public static string ReplaceEncoding(StringSegment mediaType, Encoding encoding)
        {
            var parsedMediaType = MediaTypeHeaderValue.Parse(mediaType.Value);
            parsedMediaType.Encoding = encoding;

            return parsedMediaType.ToString();
        }

        private static Encoding GetEncodingFromCharset(StringSegment charset)
        {
            if (charset.Equals("utf-8", StringComparison.OrdinalIgnoreCase))
            {
                // This is an optimization for utf-8 that prevents the Substring caused by
                // charset.Value
                return Encoding.UTF8;
            }

            try
            {
                // charset.Value might be an invalid encoding name as in charset=invalid.
                // For that reason, we catch the exception thrown by Encoding.GetEncoding
                // and return null instead.
                return charset.HasValue ? Encoding.GetEncoding(charset.Value) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
