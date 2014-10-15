// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Text;

namespace System.Net.Http.Formatting.Mocks
{
    public class MockContentNegotiator : DefaultContentNegotiator
    {
        public MockContentNegotiator()
        {
        }

        public MockContentNegotiator(bool excludeMatchOnTypeOnly)
            : base(excludeMatchOnTypeOnly)
        {
        }

        public new Collection<MediaTypeFormatterMatch> ComputeFormatterMatches(Type type, HttpRequestMessage request, IEnumerable<MediaTypeFormatter> formatters)
        {
            return base.ComputeFormatterMatches(type, request, formatters);
        }

        public new MediaTypeFormatterMatch SelectResponseMediaTypeFormatter(ICollection<MediaTypeFormatterMatch> matches)
        {
            return base.SelectResponseMediaTypeFormatter(matches);
        }

        public new Encoding SelectResponseCharacterEncoding(HttpRequestMessage request, MediaTypeFormatter formatter)
        {
            return base.SelectResponseCharacterEncoding(request, formatter);
        }

#if !ASPNETCORE50

        public new MediaTypeFormatterMatch MatchMediaTypeMapping(HttpRequestMessage request, MediaTypeFormatter formatter)
        {
            return base.MatchMediaTypeMapping(request, formatter);
        }

#endif

        public new MediaTypeFormatterMatch MatchAcceptHeader(IEnumerable<MediaTypeWithQualityHeaderValue> sortedAcceptValues, MediaTypeFormatter formatter)
        {
            return base.MatchAcceptHeader(sortedAcceptValues, formatter);
        }

        public new MediaTypeFormatterMatch MatchRequestMediaType(HttpRequestMessage request, MediaTypeFormatter formatter)
        {
            return base.MatchRequestMediaType(request, formatter);
        }

        public new bool ShouldMatchOnType(IEnumerable<MediaTypeWithQualityHeaderValue> sortedAcceptValues)
        {
            return base.ShouldMatchOnType(sortedAcceptValues);
        }

        public new MediaTypeFormatterMatch MatchType(Type type, MediaTypeFormatter formatter)
        {
            return base.MatchType(type, formatter);
        }

        public new IEnumerable<MediaTypeWithQualityHeaderValue> SortMediaTypeWithQualityHeaderValuesByQFactor(ICollection<MediaTypeWithQualityHeaderValue> headerValues)
        {
            return base.SortMediaTypeWithQualityHeaderValuesByQFactor(headerValues);
        }

        public new IEnumerable<StringWithQualityHeaderValue> SortStringWithQualityHeaderValuesByQFactor(ICollection<StringWithQualityHeaderValue> headerValues)
        {
            return base.SortStringWithQualityHeaderValuesByQFactor(headerValues);
        }

        public new MediaTypeFormatterMatch UpdateBestMatch(MediaTypeFormatterMatch current, MediaTypeFormatterMatch potentialReplacement)
        {
            return base.UpdateBestMatch(current, potentialReplacement);
        }
    }
}
