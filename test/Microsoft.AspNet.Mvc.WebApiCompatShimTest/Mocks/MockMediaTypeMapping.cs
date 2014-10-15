// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if !ASPNETCORE50

using System.Net.Http.Headers;
namespace System.Net.Http.Formatting.Mocks
{
    public class MockMediaTypeMapping : MediaTypeMapping
    {
        public MockMediaTypeMapping(string mediaType, double matchQuality)
            : base(mediaType)
        {
            MatchQuality = matchQuality;
        }

        public MockMediaTypeMapping(MediaTypeHeaderValue mediaType, double matchQuality)
            : base(mediaType)
        {
            MatchQuality = matchQuality;
        }

        public double MatchQuality { get; private set; }

        public HttpRequestMessage Request { get; private set; }

        public bool WasInvoked { get; private set; }

        public override double TryMatchMediaType(HttpRequestMessage request)
        {
            WasInvoked = true;
            Request = request;
            return MatchQuality;
        }
    }
}
#endif