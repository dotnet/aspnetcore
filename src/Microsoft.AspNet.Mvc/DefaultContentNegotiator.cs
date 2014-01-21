//// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Http;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Class that selects a <see cref="MediaTypeFormatter"/> for an <see cref="HttpRequestMessage"/>
    /// or <see cref="HttpResponseMessage"/>.
    /// </summary>
    public class DefaultContentNegotiator : IOwinContentNegotiator
    {
//        public DefaultContentNegotiator()
//            : this(false)
//        {
//        }

//        /// <summary>
//        /// Initializes a new instance of the <see cref="DefaultContentNegotiator"/> with
//        /// the given setting for <paramref name="excludeMatchOnTypeOnly"/>.
//        /// </summary>
//        /// <param name="excludeMatchOnTypeOnly">
//        /// If ExcludeMatchOnTypeOnly is true then we don't match on type only which means
//        /// that we return null if we can't match on anything in the request. This is useful
//        /// for generating 406 (Not Acceptable) status codes.
//        /// </param>
//        public DefaultContentNegotiator(bool excludeMatchOnTypeOnly)
//        {
//            ExcludeMatchOnTypeOnly = excludeMatchOnTypeOnly;
//        }

//        /// <summary>
//        /// If ExcludeMatchOnTypeOnly is true then we don't match on type only which means
//        /// that we return null if we can't match on anything in the request. This is useful
//        /// for generating 406 (Not Acceptable) status codes.
//        /// </summary>
//        public bool ExcludeMatchOnTypeOnly { get; private set; }

//        /// <summary>
//        /// Performs content negotiating by selecting the most appropriate <see cref="MediaTypeFormatter"/> out of the passed in
//        /// <paramref name="formatters"/> for the given <paramref name="request"/> that can serialize an object of the given
//        /// <paramref name="type"/>.
//        /// </summary>
//        /// <param name="type">The type to be serialized.</param>
//        /// <param name="request">The request.</param>
//        /// <param name="formatters">The set of <see cref="MediaTypeFormatter"/> objects from which to choose.</param>
//        /// <returns>The result of the negotiation containing the most appropriate <see cref="MediaTypeFormatter"/> instance,
//        /// or <c>null</c> if there is no appropriate formatter.</returns>
//        public virtual ContentNegotiationResult Negotiate(Type type, IOwinContext context, IEnumerable<MediaTypeFormatter> formatters)
//        {
//            // Performance-sensitive
//            if (type == null)
//            {
//                throw new ArgumentNullException("type");
//            }
//            if (context == null)
//            {
//                throw new ArgumentNullException("context");
//            }
//            if (formatters == null)
//            {
//                throw new ArgumentNullException("formatters");
//            }

//            var request = context.Request;

//            // Go through each formatter to compute how well it matches.
//            Collection<MediaTypeFormatterMatch> matches = ComputeFormatterMatches(type, request, formatters);

//            // Select best formatter match among the matches
//            MediaTypeFormatterMatch bestFormatterMatch = SelectResponseMediaTypeFormatter(matches);

//            // We found a best formatter
//            if (bestFormatterMatch != null)
//            {
//                // Find the best character encoding for the selected formatter
//                Encoding bestEncodingMatch = SelectResponseCharacterEncoding(request, bestFormatterMatch.Formatter);
//                if (bestEncodingMatch != null)
//                {
//                    bestFormatterMatch.MediaType.CharSet = bestEncodingMatch.WebName;
//                }

//                MediaTypeHeaderValue bestMediaType = bestFormatterMatch.MediaType;
//                MediaTypeFormatter bestFormatter = bestFormatterMatch.Formatter; // this is OData only scenario at the moment: .GetPerRequestFormatterInstance(type, request, bestMediaType);
//                return new ContentNegotiationResult(bestFormatter, bestMediaType);
//            }

//            return null;
//        }

//        /// <summary>
//        /// Determine how well each formatter matches by associating a <see cref="MediaTypeFormatterMatchRanking"/> value
//        /// with the formatter. Then associate the quality of the match based on q-factors and other parameters. The result of this 
//        /// method is a collection of the matches found categorized and assigned a quality value.
//        /// </summary>
//        /// <param name="type">The type to be serialized.</param>
//        /// <param name="request">The request.</param>
//        /// <param name="formatters">The set of <see cref="MediaTypeFormatter"/> objects from which to choose.</param>
//        /// <returns>A collection containing all the matches.</returns>
//        protected virtual Collection<MediaTypeFormatterMatch> ComputeFormatterMatches(Type type, IOwinRequest request, IEnumerable<MediaTypeFormatter> formatters)
//        {
//            // Performance-sensitive
//            if (type == null)
//            {
//                throw new ArgumentNullException("type");
//            }
//            if (request == null)
//            {
//                throw new ArgumentNullException("request");
//            }
//            if (formatters == null)
//            {
//                throw new ArgumentNullException("formatters");
//            }

//            IEnumerable<MediaTypeWithQualityHeaderValue> sortedAcceptValues = null;

//            // Go through each formatter to find how well it matches.
//            List<MediaTypeFormatterMatch> matches = new List<MediaTypeFormatterMatch>();
//            MediaTypeFormatter[] writingFormatters = GetWritingFormatters(formatters);
//            for (int i = 0; i < writingFormatters.Length; i++)
//            {
//                MediaTypeFormatter formatter = writingFormatters[i];
//                MediaTypeFormatterMatch match = null;

//                // Check first that formatter can write the actual type
//                if (!formatter.CanWriteType(type))
//                {
//                    // Formatter can't even write the type so no match at all
//                    continue;
//                }

//                // Match against media type mapping.
//                if ((match = MatchMediaTypeMapping(request, formatter)) != null)
//                {
//                    matches.Add(match);
//                    continue;
//                }

//                // Match against the accept header values.
//                if (sortedAcceptValues == null)
//                {
//                    // Sort the Accept header values in descending order based on q-factor
//                    sortedAcceptValues = SortMediaTypeWithQualityHeaderValuesByQFactor(request.Headers.Accept);
//                }
//                if ((match = MatchAcceptHeader(sortedAcceptValues, formatter)) != null)
//                {
//                    matches.Add(match);
//                    continue;
//                }

//                // Match against request's media type if any
//                if ((match = MatchRequestMediaType(request, formatter)) != null)
//                {
//                    matches.Add(match);
//                    continue;
//                }

//                // Check whether we should match on type or stop the matching process. 
//                // The latter is used to generate 406 (Not Acceptable) status codes.
//                bool shouldMatchOnType = ShouldMatchOnType(sortedAcceptValues);

//                // Match against the type of object we are writing out
//                if (shouldMatchOnType && (match = MatchType(type, formatter)) != null)
//                {
//                    matches.Add(match);
//                    continue;
//                }
//            }

//            return matches;
//        }

//        /// <summary>
//        /// Select the best match among the candidate matches found.
//        /// </summary>
//        /// <param name="matches">The collection of matches.</param>
//        /// <returns>The <see cref="MediaTypeFormatterMatch"/> determined to be the best match.</returns>
//        protected virtual MediaTypeFormatterMatch SelectResponseMediaTypeFormatter(ICollection<MediaTypeFormatterMatch> matches)
//        {
//            // Performance-sensitive
//            if (matches == null)
//            {
//                throw new ArgumentNullException("matches");
//            }

//            List<MediaTypeFormatterMatch> matchList = matches.AsList();

//            MediaTypeFormatterMatch bestMatchOnType = null;
//            MediaTypeFormatterMatch bestMatchOnAcceptHeaderLiteral = null;
//            MediaTypeFormatterMatch bestMatchOnAcceptHeaderSubtypeMediaRange = null;
//            MediaTypeFormatterMatch bestMatchOnAcceptHeaderAllMediaRange = null;
//            MediaTypeFormatterMatch bestMatchOnMediaTypeMapping = null;
//            MediaTypeFormatterMatch bestMatchOnRequestMediaType = null;

//            // Go through each formatter to find the best match in each category.
//            for (int i = 0; i < matchList.Count; i++)
//            {
//                MediaTypeFormatterMatch match = matchList[i];
//                switch (match.Ranking)
//                {
//                    case MediaTypeFormatterMatchRanking.MatchOnCanWriteType:
//                        // First match by type trumps all other type matches
//                        if (bestMatchOnType == null)
//                        {
//                            bestMatchOnType = match;
//                        }
//                        break;

//                    case MediaTypeFormatterMatchRanking.MatchOnRequestWithMediaTypeMapping:
//                        // Matches on accept headers using mappings must choose the highest quality match
//                        bestMatchOnMediaTypeMapping = UpdateBestMatch(bestMatchOnMediaTypeMapping, match);
//                        break;

//                    case MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderLiteral:
//                        // Matches on accept headers must choose the highest quality match.
//                        // A match of 0.0 means we won't use it at all.
//                        bestMatchOnAcceptHeaderLiteral = UpdateBestMatch(bestMatchOnAcceptHeaderLiteral, match);
//                        break;

//                    case MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderSubtypeMediaRange:
//                        // Matches on accept headers must choose the highest quality match.
//                        // A match of 0.0 means we won't use it at all.
//                        bestMatchOnAcceptHeaderSubtypeMediaRange = UpdateBestMatch(bestMatchOnAcceptHeaderSubtypeMediaRange, match);
//                        break;

//                    case MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderAllMediaRange:
//                        // Matches on accept headers must choose the highest quality match.
//                        // A match of 0.0 means we won't use it at all.
//                        bestMatchOnAcceptHeaderAllMediaRange = UpdateBestMatch(bestMatchOnAcceptHeaderAllMediaRange, match);
//                        break;

//                    case MediaTypeFormatterMatchRanking.MatchOnRequestMediaType:
//                        // First match on request content type trumps other request content matches
//                        if (bestMatchOnRequestMediaType == null)
//                        {
//                            bestMatchOnRequestMediaType = match;
//                        }
//                        break;
//                }
//            }

//            // If we received matches based on both supported media types and from media type mappings,
//            // we want to give precedence to the media type mappings, but only if their quality is >= that of the supported media type.
//            // We do this because media type mappings are the user's extensibility point and must take precedence over normal
//            // supported media types in the case of a tie. The 99% case is where both have quality 1.0.
//            if (bestMatchOnMediaTypeMapping != null)
//            {
//                MediaTypeFormatterMatch mappingOverride = bestMatchOnMediaTypeMapping;
//                mappingOverride = UpdateBestMatch(mappingOverride, bestMatchOnAcceptHeaderLiteral);
//                mappingOverride = UpdateBestMatch(mappingOverride, bestMatchOnAcceptHeaderSubtypeMediaRange);
//                mappingOverride = UpdateBestMatch(mappingOverride, bestMatchOnAcceptHeaderAllMediaRange);
//                if (mappingOverride != bestMatchOnMediaTypeMapping)
//                {
//                    bestMatchOnMediaTypeMapping = null;
//                }
//            }

//            // now select the formatter and media type
//            // A MediaTypeMapping is highest precedence -- it is an extensibility point
//            // allowing the user to override normal accept header matching
//            MediaTypeFormatterMatch bestMatch = null;
//            if (bestMatchOnMediaTypeMapping != null)
//            {
//                bestMatch = bestMatchOnMediaTypeMapping;
//            }
//            else if (bestMatchOnAcceptHeaderLiteral != null ||
//                bestMatchOnAcceptHeaderSubtypeMediaRange != null ||
//                bestMatchOnAcceptHeaderAllMediaRange != null)
//            {
//                bestMatch = UpdateBestMatch(bestMatch, bestMatchOnAcceptHeaderLiteral);
//                bestMatch = UpdateBestMatch(bestMatch, bestMatchOnAcceptHeaderSubtypeMediaRange);
//                bestMatch = UpdateBestMatch(bestMatch, bestMatchOnAcceptHeaderAllMediaRange);
//            }
//            else if (bestMatchOnRequestMediaType != null)
//            {
//                bestMatch = bestMatchOnRequestMediaType;
//            }
//            else if (bestMatchOnType != null)
//            {
//                bestMatch = bestMatchOnType;
//            }

//            return bestMatch;
//        }

//        /// <summary>
//        /// Determine the best character encoding for writing the response. First we look
//        /// for accept-charset headers and if not found then we try to match
//        /// any charset encoding in the request (in case of PUT, POST, etc.)
//        /// If no encoding is found then we use the default for the formatter.
//        /// </summary>
//        /// <returns>The <see cref="Encoding"/> determined to be the best match.</returns>
//        protected virtual Encoding SelectResponseCharacterEncoding(IOwinRequest request, MediaTypeFormatter formatter)
//        {
//            if (request == null)
//            {
//                throw new ArgumentNullException("request");
//            }
//            if (formatter == null)
//            {
//                throw new ArgumentNullException("formatter");
//            }

//            // If there are any SupportedEncodings then we pick an encoding
//            List<Encoding> supportedEncodings = formatter.SupportedEncodingsInternal;
//            if (supportedEncodings.Count > 0)
//            {
//                // Sort Accept-Charset header values
//                IEnumerable<StringWithQualityHeaderValue> sortedAcceptCharsetValues = SortStringWithQualityHeaderValuesByQFactor(request.Headers.AcceptCharset);

//                // Check for match based on accept-charset headers
//                foreach (StringWithQualityHeaderValue acceptCharset in sortedAcceptCharsetValues)
//                {
//                    for (int i = 0; i < supportedEncodings.Count; i++)
//                    {
//                        Encoding encoding = supportedEncodings[i];
//                        if (encoding != null && acceptCharset.Quality != FormattingUtilities.NoMatch &&
//                            (acceptCharset.Value.Equals(encoding.WebName, StringComparison.OrdinalIgnoreCase) ||
//                            acceptCharset.Value.Equals("*", StringComparison.OrdinalIgnoreCase)))
//                        {
//                            return encoding;
//                        }
//                    }
//                }

//                // Check for match based on any request entity body

//                // TODO: Transform to use request content headers directly
//                // was - request.Content != null ? request.Content.Headers : null
//                return formatter.SelectCharacterEncoding(null);
//            }

//            return null;
//        }

//        /// <summary>
//        /// Match a request against the <see cref="MediaTypeMapping"/>s registered with the formatter.
//        /// </summary>
//        /// <param name="request">The request to match.</param>
//        /// <param name="formatter">The formatter to match against.</param>
//        /// <returns>A <see cref="MediaTypeFormatterMatch"/> indicating the quality of the match or null is no match.</returns>
//        protected virtual MediaTypeFormatterMatch MatchMediaTypeMapping(IOwinRequest request, MediaTypeFormatter formatter)
//        {
//            if (request == null)
//            {
//                throw new ArgumentNullException("request");
//            }
//            if (formatter == null)
//            {
//                throw new ArgumentNullException("formatter");
//            }

//            List<MediaTypeMapping> mediaTypeMappings = formatter.MediaTypeMappingsInternal;
//            for (int i = 0; i < mediaTypeMappings.Count; i++)
//            {
//                MediaTypeMapping mapping = mediaTypeMappings[i];
//                double quality;
//                if (mapping != null && ((quality = mapping.TryMatchMediaType(request)) > FormattingUtilities.NoMatch))
//                {
//                    return new MediaTypeFormatterMatch(formatter, mapping.MediaType, quality, MediaTypeFormatterMatchRanking.MatchOnRequestWithMediaTypeMapping);
//                }
//            }

//            return null;
//        }

//        /// <summary>
//        /// Match the request accept header field values against the formatter's registered supported media types.
//        /// </summary>
//        /// <param name="sortedAcceptValues">The sorted accept header values to match.</param>
//        /// <param name="formatter">The formatter to match against.</param>
//        /// <returns>A <see cref="MediaTypeFormatterMatch"/> indicating the quality of the match or null is no match.</returns>
//        protected virtual MediaTypeFormatterMatch MatchAcceptHeader(IEnumerable<MediaTypeWithQualityHeaderValue> sortedAcceptValues, MediaTypeFormatter formatter)
//        {
//            if (sortedAcceptValues == null)
//            {
//                throw Error.ArgumentNull("sortedAcceptValues");
//            }
//            if (formatter == null)
//            {
//                throw Error.ArgumentNull("formatter");
//            }

//            foreach (MediaTypeWithQualityHeaderValue acceptMediaTypeValue in sortedAcceptValues)
//            {
//                List<MediaTypeHeaderValue> supportedMediaTypes = formatter.SupportedMediaTypesInternal;
//                for (int i = 0; i < supportedMediaTypes.Count; i++)
//                {
//                    MediaTypeHeaderValue supportedMediaType = supportedMediaTypes[i];
//                    MediaTypeHeaderValueRange range;
//                    if (supportedMediaType != null && acceptMediaTypeValue.Quality != FormattingUtilities.NoMatch &&
//                        supportedMediaType.IsSubsetOf(acceptMediaTypeValue, out range))
//                    {
//                        MediaTypeFormatterMatchRanking ranking;
//                        switch (range)
//                        {
//                            case MediaTypeHeaderValueRange.AllMediaRange:
//                                ranking = MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderAllMediaRange;
//                                break;

//                            case MediaTypeHeaderValueRange.SubtypeMediaRange:
//                                ranking = MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderSubtypeMediaRange;
//                                break;

//                            default:
//                                ranking = MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderLiteral;
//                                break;
//                        }

//                        return new MediaTypeFormatterMatch(formatter, supportedMediaType, acceptMediaTypeValue.Quality, ranking);
//                    }
//                }
//            }

//            return null;
//        }

//        /// <summary>
//        /// Match any request media type (in case there is a request entity body) against the formatter's registered
//        /// media types.
//        /// </summary>
//        /// <param name="request">The request to match.</param>
//        /// <param name="formatter">The formatter to match against.</param>
//        /// <returns>A <see cref="MediaTypeFormatterMatch"/> indicating the quality of the match or null is no match.</returns>
//        protected virtual MediaTypeFormatterMatch MatchRequestMediaType(IOwinRequest request, MediaTypeFormatter formatter)
//        {
//            if (request == null)
//            {
//                throw new ArgumentNullException("request");
//            }
//            if (formatter == null)
//            {
//                throw new ArgumentNullException("formatter");
//            }

//            if (request.Content != null)
//            {
//                MediaTypeHeaderValue requestMediaType = request.Content.Headers.ContentType;
//                if (requestMediaType != null)
//                {
//                    List<MediaTypeHeaderValue> supportedMediaTypes = formatter.SupportedMediaTypesInternal;
//                    for (int i = 0; i < supportedMediaTypes.Count; i++)
//                    {
//                        MediaTypeHeaderValue supportedMediaType = supportedMediaTypes[i];
//                        if (supportedMediaType != null && supportedMediaType.IsSubsetOf(requestMediaType))
//                        {
//                            return new MediaTypeFormatterMatch(formatter, supportedMediaType, FormattingUtilities.Match, MediaTypeFormatterMatchRanking.MatchOnRequestMediaType);
//                        }
//                    }
//                }
//            }

//            return null;
//        }

//        /// <summary>
//        /// Determine whether to match on type or not. This is used to determine whether to
//        /// generate a 406 response or use the default media type formatter in case there
//        /// is no match against anything in the request. If ExcludeMatchOnTypeOnly is true 
//        /// then we don't match on type unless there are no accept headers.
//        /// </summary>
//        /// <param name="sortedAcceptValues">The sorted accept header values to match.</param>
//        /// <returns>True if not ExcludeMatchOnTypeOnly and accept headers with a q-factor bigger than 0.0 are present.</returns>
//        protected virtual bool ShouldMatchOnType(IEnumerable<MediaTypeWithQualityHeaderValue> sortedAcceptValues)
//        {
//            if (sortedAcceptValues == null)
//            {
//                throw new ArgumentNullException("sortedAcceptValues");
//            }

//            return !(ExcludeMatchOnTypeOnly && sortedAcceptValues.Any());
//        }

//        /// <summary>
//        /// Pick the first supported media type and indicate we've matched only on type
//        /// </summary>
//        /// <param name="type">The type to be serialized.</param>
//        /// <param name="formatter">The formatter we are matching against.</param>
//        /// <returns>A <see cref="MediaTypeFormatterMatch"/> indicating the quality of the match or null is no match.</returns>
//        protected virtual MediaTypeFormatterMatch MatchType(Type type, MediaTypeFormatter formatter)
//        {
//            // Performance-sensitive
//            if (type == null)
//            {
//                throw new ArgumentNullException("type");
//            }
//            if (formatter == null)
//            {
//                throw new ArgumentNullException("formatter");
//            }

//            // We already know that we do match on type -- otherwise we wouldn't even be called --
//            // so this is just a matter of determining how we match.
//            MediaTypeHeaderValue mediaType = null;
//            List<MediaTypeHeaderValue> supportedMediaTypes = formatter.SupportedMediaTypesInternal;
//            if (supportedMediaTypes.Count > 0)
//            {
//                mediaType = supportedMediaTypes[0];
//            }
//            return new MediaTypeFormatterMatch(formatter, mediaType, FormattingUtilities.Match, MediaTypeFormatterMatchRanking.MatchOnCanWriteType);
//        }

//        /// <summary>
//        /// Sort Accept header values and related header field values with similar syntax rules 
//        /// (if more than 1) in descending order based on q-factor.
//        /// </summary>
//        /// <param name="headerValues">The header values to sort.</param>
//        /// <returns>The sorted header values.</returns>
//        protected virtual IEnumerable<MediaTypeWithQualityHeaderValue> SortMediaTypeWithQualityHeaderValuesByQFactor(ICollection<MediaTypeWithQualityHeaderValue> headerValues)
//        {
//            if (headerValues == null)
//            {
//                throw new ArgumentNullException("headerValues");
//            }

//            if (headerValues.Count > 1)
//            {
//                // Use OrderBy() instead of Array.Sort() as it performs fewer comparisons. In this case the comparisons
//                // are quite expensive so OrderBy() performs better.
//                return headerValues.OrderByDescending(m => m, MediaTypeWithQualityHeaderValueComparer.QualityComparer).ToArray();
//            }
//            else
//            {
//                return headerValues;
//            }
//        }

//        /// <summary>
//        /// Sort Accept-Charset, Accept-Encoding, Accept-Language and related header field values with similar syntax rules 
//        /// (if more than 1) in descending order based on q-factor.
//        /// </summary>
//        /// <param name="headerValues">The header values to sort.</param>
//        /// <returns>The sorted header values.</returns>
//        protected virtual IEnumerable<StringWithQualityHeaderValue> SortStringWithQualityHeaderValuesByQFactor(ICollection<StringWithQualityHeaderValue> headerValues)
//        {
//            if (headerValues == null)
//            {
//                throw new ArgumentNullException("headerValues");
//            }

//            if (headerValues.Count > 1)
//            {
//                // Use OrderBy() instead of Array.Sort() as it performs fewer comparisons. In this case the comparisons
//                // are quite expensive so OrderBy() performs better.
//                return headerValues.OrderByDescending(m => m, StringWithQualityHeaderValueComparer.QualityComparer).ToArray();
//            }
//            else
//            {
//                return headerValues;
//            }
//        }

//        /// <summary>
//        /// Evaluates whether a match is better than the current match and if so returns the replacement; otherwise returns the 
//        /// current match.
//        /// </summary>
//        protected virtual MediaTypeFormatterMatch UpdateBestMatch(MediaTypeFormatterMatch current, MediaTypeFormatterMatch potentialReplacement)
//        {
//            if (potentialReplacement == null)
//            {
//                return current;
//            }

//            if (current != null)
//            {
//                return (potentialReplacement.Quality > current.Quality) ? potentialReplacement : current;
//            }

//            return potentialReplacement;
//        }

//        private static MediaTypeFormatter[] GetWritingFormatters(IEnumerable<MediaTypeFormatter> formatters)
//        {
//            Contract.Assert(formatters != null);
//            MediaTypeFormatterCollection formatterCollection = formatters as MediaTypeFormatterCollection;
//            if (formatterCollection != null)
//            {
//                return formatterCollection.WritingFormatters;
//            }
//            return formatters.AsArray();
//        }

        public ContentNegotiationResult Negotiate(Type type, IOwinContext context, IEnumerable<MediaTypeFormatter> formatters)
        {
            return new ContentNegotiationResult(formatters.First(), formatters.First().SupportedMediaTypes.First());
        }
    }
}
