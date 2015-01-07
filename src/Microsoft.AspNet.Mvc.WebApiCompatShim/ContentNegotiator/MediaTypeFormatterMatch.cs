// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNETCORE50

using System.Net.Http.Headers;

namespace System.Net.Http.Formatting
{
    /// <summary>
    /// This class describes how well a particular <see cref="MediaTypeFormatter"/> matches a request.
    /// </summary>
    public class MediaTypeFormatterMatch
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaTypeFormatterMatch"/> class.
        /// </summary>
        /// <param name="formatter">The matching formatter.</param>
        /// <param name="mediaType">
        /// The media type. Can be <c>null</c> in which case the media type <c>application/octet-stream</c> is used.
        /// </param>
        /// <param name="quality">
        /// The quality of the match. Can be <c>null</c> in which case it is considered a full match with a value of
        /// 1.0.
        /// </param>
        /// <param name="ranking">The kind of match.</param>
        public MediaTypeFormatterMatch(
            MediaTypeFormatter formatter,
            MediaTypeHeaderValue mediaType,
            double? quality,
            MediaTypeFormatterMatchRanking ranking)
        {
            Formatter = formatter;
            MediaType = mediaType != null ? mediaType : MediaTypeConstants.ApplicationOctetStreamMediaType;
            Quality = quality ?? FormattingUtilities.Match;
            Ranking = ranking;
        }

        /// <summary>
        /// Gets the media type formatter.
        /// </summary>
        public MediaTypeFormatter Formatter { get; private set; }

        /// <summary>
        /// Gets the matched media type.
        /// </summary>
        public MediaTypeHeaderValue MediaType { get; private set; }

        /// <summary>
        /// Gets the quality of the match
        /// </summary>
        public double Quality { get; private set; }

        /// <summary>
        /// Gets the kind of match that occurred.
        /// </summary>
        public MediaTypeFormatterMatchRanking Ranking { get; private set; }
    }
}
#endif