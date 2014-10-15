// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Formatting.Mocks;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json.Linq;
using Xunit;
using Microsoft.TestCommon;

namespace System.Net.Http.Formatting
{
    public class DefaultContentNegotiatorTests
    {
        private readonly DefaultContentNegotiator _negotiator = new DefaultContentNegotiator();
        private readonly HttpRequestMessage _request = new HttpRequestMessage();

        public static TheoryData<string, string[], string> MatchRequestMediaTypeData
        {
            get
            {
                // string requestMediaType, string[] supportedMediaTypes, string expectedMediaType
                return new TheoryData<string, string[], string>
                {
                    { "text/plain", new string[0], null },
                    { "text/plain", new string[] { "text/xml", "application/xml" }, null },
                    { "application/xml", new string[] { "application/xml", "text/xml" }, "application/xml" },
                    { "APPLICATION/XML", new string[] { "text/xml", "application/xml" }, "application/xml" },
                    { "application/xml; charset=utf-8", new string[] { "text/xml", "application/xml" }, "application/xml" },
                    { "application/xml; charset=utf-8; parameter=value", new string[] { "text/xml", "application/xml" }, "application/xml" },
                };
            }
        }

        public static TheoryData<string[], string[], string, double, int> MatchAcceptHeaderData
        {
            get
            {
                // string[] acceptHeader, string[] supportedMediaTypes, string expectedMediaType, double matchQuality, int range
                return new TheoryData<string[], string[], string, double, int>
                {
                    { new string[] { "text/plain" }, new string[0], null, 0.0, (int)MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderLiteral },

                    { new string[] { "text/plain" }, new string[] { "text/xml", "application/xml" }, null, 0.0, (int)MediaTypeFormatterMatchRanking.None },
                    { new string[] { "text/plain; q=0.5" }, new string[] { "text/xml", "application/xml" }, null, 0.0, (int)MediaTypeFormatterMatchRanking.None },

                    { new string[] { "application/xml" }, new string[] { "application/xml", "text/xml" }, "application/xml", 1.0, (int)MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderLiteral },
                    { new string[] { "APPLICATION/XML; q=0.5" }, new string[] { "text/xml", "application/xml" }, "application/xml", 0.5, (int)MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderLiteral },
                    { new string[] { "text/xml; q=0.5", "APPLICATION/XML; q=0.7" }, new string[] { "text/xml", "application/xml" }, "application/xml", 0.7, (int)MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderLiteral },
                    { new string[] { "application/xml; q=0.0" }, new string[] { "application/xml", "text/xml" }, null, 0.0, (int)MediaTypeFormatterMatchRanking.None },
                    { new string[] { "APPLICATION/XML; q=0.0" }, new string[] { "text/xml", "application/xml" }, null, 0.0, (int)MediaTypeFormatterMatchRanking.None },
                    { new string[] { "text/xml; q=0.0", "APPLICATION/XML; q=0.0" }, new string[] { "text/xml", "application/xml" }, null, 0.0, (int)MediaTypeFormatterMatchRanking.None },

                    { new string[] { "text/*" }, new string[] { "text/xml", "application/xml" }, "text/xml", 1.0, (int)MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderSubtypeMediaRange },
                    { new string[] { "text/*", "application/xml" }, new string[] { "text/xml", "application/xml" }, "application/xml", 1.0, (int)MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderLiteral },
                    { new string[] { "text/*", "application/xml; q=0.5" }, new string[] { "text/xml", "application/xml" }, "text/xml", 1.0, (int)MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderSubtypeMediaRange },
                    { new string[] { "text/*; q=0.5" }, new string[] { "text/xml", "application/xml" }, "text/xml", 0.5, (int)MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderSubtypeMediaRange },
                    { new string[] { "text/*; q=0.5", "application/xml" }, new string[] { "text/xml", "application/xml" }, "application/xml", 1.0, (int)MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderLiteral },
                    { new string[] { "text/*; q=0.0", "application/xml; q=0.0" }, new string[] { "text/xml", "application/xml" }, null, 0.0, (int)MediaTypeFormatterMatchRanking.None },
                    { new string[] { "text/*; q=0.0", "application/xml" }, new string[] { "text/xml", "application/xml" }, "application/xml", 1.0, (int)MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderLiteral },

                    { new string[] { "*/*; q=0.5" }, new string[] { "text/xml", "application/xml" }, "text/xml", 0.5, (int)MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderAllMediaRange },
                    { new string[] { "*/*; q=0.0" }, new string[] { "text/xml", "application/xml" }, null, 0.0, (int)MediaTypeFormatterMatchRanking.None },
                    { new string[] { "*/*; q=0.5", "application/xml" }, new string[] { "text/xml", "application/xml" }, "application/xml", 1.0, (int)MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderLiteral },
                    { new string[] { "*/*; q=1.0", "application/xml; q=0.5" }, new string[] { "text/xml", "application/xml" }, "text/xml", 1.0, (int)MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderAllMediaRange },
                    { new string[] { "*/*", "application/xml" }, new string[] { "text/xml", "application/xml" }, "application/xml", 1.0, (int)MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderLiteral },

                    { new string[] { "text/*; q=0.5", "*/*; q=0.2", "application/xml; q=1.0" }, new string[] { "text/xml", "application/xml" }, "application/xml", 1.0,  (int)MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderLiteral },

                    { new string[] { "application/xml; q=0.5" }, new string[] { "text/xml", "application/xml" }, "application/xml", 0.5, (int)MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderLiteral },
                };
            }
        }

        public static TheoryData<bool, string[], bool> ShouldMatchOnTypeData
        {
            get
            {
                // bool excludeMatchOnType, string[] acceptHeaders, bool expectedResult
                return new TheoryData<bool, string[], bool>
                {
                    { false, new string[0], true },
                    { true, new string[0], true },

                    { false, new string[] { "application/xml" }, true },
                    { true, new string[] { "application/xml" }, false },

                    { false, new string[] { "application/xml; q=1.0" }, true },
                    { true, new string[] { "application/xml; q=1.0" }, false },

                    { false, new string[] { "application/xml; q=0.0" }, true },
                    { true, new string[] { "application/xml; q=0.0" }, false },

                    { false, new string[] { "application/xml; q=0.0", "application/json" }, true },
                    { true, new string[] { "application/xml; q=0.0", "application/json" }, false },

                    { false, new string[] { "text/nomatch" }, true },
                    { true, new string[] { "text/nomatch" }, false },
                };
            }
        }

        public static TheoryData<string[], string> MatchTypeData
        {
            get
            {
                // string[] supportedMediaTypes, string expectedMediaType
                return new TheoryData<string[], string>
                {
                    { new string[0], "application/octet-stream" },

                    { new string[] { "text/xml", "application/xml" }, "text/xml" },
                    { new string[] { "application/xml", "text/xml" }, "application/xml" },
                };
            }
        }

        public static TheoryData<string[], string, string[], string> SelectResponseCharacterEncodingData
        {
            get
            {
                // string[] acceptEncodings, string requestEncoding, string[] supportedEncodings, string expectedEncoding
                return new TheoryData<string[], string, string[], string>
                {
                    { new string[] { "utf-8" }, null, new string[0], null },
                    { new string[0], "utf-8", new string[0], null },

                    { new string[0], null, new string[] { "utf-8", "utf-16"}, "utf-8" },
                    { new string[0], "utf-16", new string[] { "utf-8", "utf-16"}, "utf-16" },

                    { new string[] { "utf-8" }, null, new string[] { "utf-8", "utf-16"}, "utf-8" },
                    { new string[] { "utf-16" }, "utf-8", new string[] { "utf-8", "utf-16"}, "utf-16" },
                    { new string[] { "utf-16; q=0.5" }, "utf-8", new string[] { "utf-8", "utf-16"}, "utf-16" },

                    { new string[] { "utf-8; q=0.0" }, null, new string[] { "utf-8", "utf-16"}, "utf-8" },
                    { new string[] { "utf-8; q=0.0" }, "utf-16", new string[] { "utf-8", "utf-16"}, "utf-16" },
                    { new string[] { "utf-8; q=0.0", "utf-16; q=0.0" }, "utf-16", new string[] { "utf-8", "utf-16"}, "utf-16" },
                    { new string[] { "utf-8; q=0.0", "utf-16; q=0.0" }, null, new string[] { "utf-8", "utf-16"}, "utf-8" },
                    { new string[] { "*; q=0.0" }, null, new string[] { "utf-8", "utf-16"}, "utf-8" },
                    { new string[] { "*; q=0.0" }, "utf-16", new string[] { "utf-8", "utf-16"}, "utf-16" },
                };
            }
        }

        public static TheoryData<ICollection<MediaTypeFormatterMatch>, MediaTypeFormatterMatch> SelectResponseMediaTypeData
        {
            get
            {
#if !ASPNETCORE50
                // Only mapping and accept makes sense with q != 1.0
                MediaTypeFormatterMatch matchMapping10 = CreateMatch(1.0, MediaTypeFormatterMatchRanking.MatchOnRequestWithMediaTypeMapping);
                MediaTypeFormatterMatch matchMapping05 = CreateMatch(0.5, MediaTypeFormatterMatchRanking.MatchOnRequestWithMediaTypeMapping);
#endif

                MediaTypeFormatterMatch matchAccept10 = CreateMatch(1.0, MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderLiteral);
                MediaTypeFormatterMatch matchAccept05 = CreateMatch(0.5, MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderLiteral);

                MediaTypeFormatterMatch matchAcceptSubTypeRange10 = CreateMatch(1.0, MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderSubtypeMediaRange);
                MediaTypeFormatterMatch matchAcceptSubTypeRange05 = CreateMatch(0.5, MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderSubtypeMediaRange);

                MediaTypeFormatterMatch matchAcceptAllRange10 = CreateMatch(1.0, MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderAllMediaRange);
                MediaTypeFormatterMatch matchAcceptAllRange05 = CreateMatch(0.5, MediaTypeFormatterMatchRanking.MatchOnRequestAcceptHeaderAllMediaRange);

                MediaTypeFormatterMatch matchRequest10 = CreateMatch(1.0, MediaTypeFormatterMatchRanking.MatchOnRequestMediaType);
                MediaTypeFormatterMatch matchType10 = CreateMatch(1.0, MediaTypeFormatterMatchRanking.MatchOnCanWriteType);

                // ICollection<MediaTypeFormatterMatch> candidateMatches, MediaTypeFormatterMatch winner
                return new TheoryData<ICollection<MediaTypeFormatterMatch>, MediaTypeFormatterMatch>
                {
                    { new List<MediaTypeFormatterMatch>(), null },
                    { new List<MediaTypeFormatterMatch>() { matchType10 }, matchType10 },
                    { new List<MediaTypeFormatterMatch>() { matchType10, matchRequest10 }, matchRequest10 },
                    { new List<MediaTypeFormatterMatch>() { matchType10, matchRequest10, matchAcceptAllRange10 }, matchAcceptAllRange10 },
                    { new List<MediaTypeFormatterMatch>() { matchType10, matchRequest10, matchAcceptAllRange10, matchAcceptSubTypeRange10 }, matchAcceptSubTypeRange10 },
                    { new List<MediaTypeFormatterMatch>() { matchType10, matchRequest10, matchAcceptAllRange10, matchAcceptSubTypeRange10, matchAccept10 }, matchAccept10 },
#if !ASPNETCORE50
                    { new List<MediaTypeFormatterMatch>() { matchType10, matchRequest10, matchAcceptAllRange10, matchAcceptSubTypeRange10, matchAccept10, matchMapping10 }, matchMapping10 },
#endif
                    { new List<MediaTypeFormatterMatch>() { matchAccept05, matchAccept10 }, matchAccept10 },
                    { new List<MediaTypeFormatterMatch>() { matchAccept10, matchAccept05 }, matchAccept10 },

                    { new List<MediaTypeFormatterMatch>() { matchAcceptSubTypeRange05, matchAcceptSubTypeRange10 }, matchAcceptSubTypeRange10 },
                    { new List<MediaTypeFormatterMatch>() { matchAcceptSubTypeRange10, matchAcceptSubTypeRange05 }, matchAcceptSubTypeRange10 },

                    { new List<MediaTypeFormatterMatch>() { matchAcceptAllRange05, matchAcceptAllRange10 }, matchAcceptAllRange10 },
                    { new List<MediaTypeFormatterMatch>() { matchAcceptAllRange10, matchAcceptAllRange05 }, matchAcceptAllRange10 },
#if !ASPNETCORE50
                    { new List<MediaTypeFormatterMatch>() { matchMapping05, matchMapping10 }, matchMapping10 },
                    { new List<MediaTypeFormatterMatch>() { matchMapping10, matchMapping05 }, matchMapping10 },

                    { new List<MediaTypeFormatterMatch>() { matchMapping05, matchAccept05 }, matchMapping05 },
                    { new List<MediaTypeFormatterMatch>() { matchMapping10, matchAccept10 }, matchMapping10 },

                    { new List<MediaTypeFormatterMatch>() { matchMapping05, matchAcceptSubTypeRange05 }, matchMapping05 },
                    { new List<MediaTypeFormatterMatch>() { matchMapping10, matchAcceptSubTypeRange10 }, matchMapping10 },

                    { new List<MediaTypeFormatterMatch>() { matchMapping05, matchAcceptAllRange05 }, matchMapping05 },
                    { new List<MediaTypeFormatterMatch>() { matchMapping10, matchAcceptAllRange10 }, matchMapping10 },

                    { new List<MediaTypeFormatterMatch>() { matchMapping05, matchAccept10 }, matchAccept10 },
                    { new List<MediaTypeFormatterMatch>() { matchMapping05, matchAcceptSubTypeRange10 }, matchAcceptSubTypeRange10 },
                    { new List<MediaTypeFormatterMatch>() { matchMapping05, matchAcceptAllRange10 }, matchAcceptAllRange10 },
#endif
                };
            }
        }

        public static TheoryData<MediaTypeFormatterMatch, MediaTypeFormatterMatch, bool> UpdateBestMatchData
        {
            get
            {
                MediaTypeFormatterMatch matchMapping10 = CreateMatch(1.0, MediaTypeFormatterMatchRanking.None);
                MediaTypeFormatterMatch matchMapping05 = CreateMatch(0.5, MediaTypeFormatterMatchRanking.None);

                // MediaTypeFormatterMatch current, MediaTypeFormatterMatch potentialReplacement, currentWins
                return new TheoryData<MediaTypeFormatterMatch, MediaTypeFormatterMatch, bool>
                {
                    { null, matchMapping10, false },
                    { null, matchMapping05, false },

                    { matchMapping10, matchMapping10, true },
                    { matchMapping10, matchMapping05, true },

                    { matchMapping05, matchMapping10, false },
                    { matchMapping05, matchMapping05, true },
                };
            }
        }

        private static MediaTypeFormatterMatch CreateMatch(double? quality, MediaTypeFormatterMatchRanking ranking)
        {
            MockMediaTypeFormatter formatter = new MockMediaTypeFormatter();
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("text/test");
            return new MediaTypeFormatterMatch(formatter, mediaType, quality, ranking);
        }

        [Fact]
        public void TypeIsCorrect()
        {
            new TypeAssert().HasProperties(typeof(DefaultContentNegotiator), TypeAssert.TypeProperties.IsPublicVisibleClass);
        }

        [Fact]
        public void Negotiate_ForEmptyFormatterCollection_ReturnsNull()
        {
            var result = _negotiator.Negotiate(typeof(string), _request, Enumerable.Empty<MediaTypeFormatter>());

            Assert.Null(result);
        }

#if !ASPNETCORE50

        [Fact]
        public void Negotiate_MediaTypeMappingTakesPrecedenceOverAcceptHeader()
        {
            // Prepare the request message
            _request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
            _request.Headers.Add("Browser", "IE");
            _request.Headers.Add("Cookie", "ABC");

            // Prepare the formatters
            List<MediaTypeFormatter> formatters = new List<MediaTypeFormatter>();
            formatters.Add(new JsonMediaTypeFormatter());
            formatters.Add(new XmlMediaTypeFormatter());
            PlainTextFormatter frmtr = new PlainTextFormatter();
            frmtr.SupportedMediaTypes.Clear();
            frmtr.MediaTypeMappings.Clear();
            frmtr.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/xml"));
            frmtr.MediaTypeMappings.Add(new MyMediaTypeMapping(new MediaTypeHeaderValue(("application/xml"))));
            formatters.Add(frmtr);

            // Act
            var result = _negotiator.Negotiate(typeof(string), _request, formatters);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("application/xml", result.MediaType.MediaType);
            Assert.IsType<PlainTextFormatter>(result.Formatter);
        }

#endif

        [Fact]
        public void Negotiate_ForRequestReturnsFirstMatchingFormatter()
        {
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("application/myMediaType");

            MediaTypeFormatter formatter1 = new MockMediaTypeFormatter()
            {
                CanWriteTypeCallback = (Type t) => false
            };

            MediaTypeFormatter formatter2 = new MockMediaTypeFormatter()
            {
                CanWriteTypeCallback = (Type t) => true
            };

            formatter2.SupportedMediaTypes.Add(mediaType);

            MediaTypeFormatterCollection collection = new MediaTypeFormatterCollection(
                new MediaTypeFormatter[]
                {
                    formatter1,
                    formatter2
                });

            _request.Content = new StringContent("test", Encoding.UTF8, mediaType.MediaType);

            var result = _negotiator.Negotiate(typeof(string), _request, collection);
            Assert.Same(formatter2, result.Formatter);
            new MediaTypeAssert().AreEqual(mediaType, result.MediaType, "Expected the formatter's media type to be returned.");
        }

        [Fact]
        public void Negotiate_SelectsJsonAsDefaultFormatter()
        {
            // Arrange
            _request.Content = new StringContent("test");

            // Act
            var result = _negotiator.Negotiate(typeof(string), _request, new MediaTypeFormatterCollection());

            // Assert
            Assert.IsType<JsonMediaTypeFormatter>(result.Formatter);
            Assert.Equal(MediaTypeConstants.ApplicationJsonMediaType.MediaType, result.MediaType.MediaType);
        }

        [Fact]
        public void Negotiate_SelectsXmlFormatter_ForXhrRequestThatAcceptsXml()
        {
            // Arrange
            _request.Content = new StringContent("test");
            _request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
            _request.Headers.Add("x-requested-with", "XMLHttpRequest");

            // Act
            var result = _negotiator.Negotiate(typeof(string), _request, new MediaTypeFormatterCollection());

            // Assert
            Assert.Equal("application/xml", result.MediaType.MediaType);
            Assert.IsType<XmlMediaTypeFormatter>(result.Formatter);
        }

        [Fact]
        public void Negotiate_SelectsJsonFormatter_ForXhrRequestThatDoesNotSpecifyAcceptHeaders()
        {
            // Arrange
            _request.Content = new StringContent("test");
            _request.Headers.Add("x-requested-with", "XMLHttpRequest");

            // Act
            var result = _negotiator.Negotiate(typeof(string), _request, new MediaTypeFormatterCollection());

            // Assert
            Assert.Equal("application/json", result.MediaType.MediaType);
            Assert.IsType<JsonMediaTypeFormatter>(result.Formatter);
        }

#if !ASPNETCORE50

        [Fact]
        public void Negotiate_RespectsFormatterOrdering_ForXhrRequestThatDoesNotSpecifyAcceptHeaders()
        {
            // Arrange
            _request.Content = new StringContent("test");
            _request.Headers.Add("x-requested-with", "XMLHttpRequest");

            MediaTypeFormatterCollection formatters = new MediaTypeFormatterCollection(new MediaTypeFormatter[]
            {
                new XmlMediaTypeFormatter(),
                new JsonMediaTypeFormatter(),
                new FormUrlEncodedMediaTypeFormatter()
            });

            // Act
            var result = _negotiator.Negotiate(typeof(string), _request, formatters);

            // Assert
            Assert.Equal("application/json", result.MediaType.MediaType);
            Assert.IsType<JsonMediaTypeFormatter>(result.Formatter);
        }

#endif

        [Fact]
        public void Negotiate_SelectsJsonFormatter_ForXHRAndJsonValueResponse()
        {
            // Arrange
            _request.Content = new StringContent("test");
            _request.Headers.Add("x-requested-with", "XMLHttpRequest");

            // Act
            var result = _negotiator.Negotiate(typeof(JToken), _request, new MediaTypeFormatterCollection());

            Assert.Equal("application/json", result.MediaType.MediaType);
            Assert.IsType<JsonMediaTypeFormatter>(result.Formatter);
        }

        [Fact]
        public void Negotiate_SelectsJsonFormatter_ForXHRAndMatchAllAcceptHeader()
        {
            // Accept
            _request.Content = new StringContent("test");
            _request.Headers.Add("x-requested-with", "XMLHttpRequest");
            _request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

            // Act
            var result = _negotiator.Negotiate(typeof(string), _request, new MediaTypeFormatterCollection());

            // Assert
            Assert.Equal("application/json", result.MediaType.MediaType);
            Assert.IsType<JsonMediaTypeFormatter>(result.Formatter);
        }

        [Fact]
        public void Negotiate_UsesRequestedFormatterForXHRAndMatchAllPlusOtherAcceptHeader()
        {
            // Arrange
            _request.Content = new StringContent("test");
            _request.Headers.Add("x-requested-with", "XMLHttpRequest");
            _request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8"); // XHR header sent by Firefox 3b5

            // Act
            var result = _negotiator.Negotiate(typeof(string), _request, new MediaTypeFormatterCollection());

            // Assert
            Assert.Equal("application/xml", result.MediaType.MediaType);
            Assert.IsType<XmlMediaTypeFormatter>(result.Formatter);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Negotiate_ObservesExcludeMatchOnTypeOnly(bool excludeMatchOnTypeOnly)
        {
            // Arrange
            MockContentNegotiator negotiator = new MockContentNegotiator(excludeMatchOnTypeOnly);
            _request.Content = new StringContent("test");
            _request.Headers.Accept.ParseAdd("text/html");

            // Act
            var result = negotiator.Negotiate(typeof(string), _request, new MediaTypeFormatterCollection());

            // Assert
            if (excludeMatchOnTypeOnly)
            {
                Assert.Null(result);
            }
            else
            {
                Assert.NotNull(result);
                Assert.Equal("application/json", result.MediaType.MediaType);
            }
        }

#if !ASPNETCORE50

        [Fact]
        public void MatchMediaTypeMapping_ReturnsMatch()
        {
            // Arrange
            MockContentNegotiator negotiator = new MockContentNegotiator();

            HttpRequestMessage request = new HttpRequestMessage();
            MediaTypeHeaderValue mappingMediatype = MediaTypeHeaderValue.Parse("application/other");
            MockMediaTypeMapping mockMediaTypeMapping = new MockMediaTypeMapping(mappingMediatype, 0.75);

            MockMediaTypeFormatter formatter = new MockMediaTypeFormatter();
            formatter.MediaTypeMappings.Add(mockMediaTypeMapping);

            // Act
            MediaTypeFormatterMatch match = negotiator.MatchMediaTypeMapping(request, formatter);

            // Assert
            Assert.True(mockMediaTypeMapping.WasInvoked);
            Assert.Same(request, mockMediaTypeMapping.Request);

            Assert.Same(formatter, match.Formatter);
            Assert.Equal(mockMediaTypeMapping.MediaType, match.MediaType);
            Assert.Equal(mockMediaTypeMapping.MatchQuality, match.Quality);
            Assert.Equal(MediaTypeFormatterMatchRanking.MatchOnRequestWithMediaTypeMapping, match.Ranking);
        }

#endif

        [Theory]
        [MemberData("MatchAcceptHeaderData")]
        public void MatchAcceptHeader_ReturnsMatch(string[] acceptHeaders, string[] supportedMediaTypes, string expectedMediaType, double expectedQuality, int ranking)
        {
            // Arrange
            MockContentNegotiator negotiator = new MockContentNegotiator();

            List<MediaTypeWithQualityHeaderValue> unsortedAcceptHeaders = acceptHeaders.Select(a => MediaTypeWithQualityHeaderValue.Parse(a)).ToList();
            IEnumerable<MediaTypeWithQualityHeaderValue> sortedAcceptHeaders = negotiator.SortMediaTypeWithQualityHeaderValuesByQFactor(unsortedAcceptHeaders);

            MockMediaTypeFormatter formatter = new MockMediaTypeFormatter();
            foreach (string supportedMediaType in supportedMediaTypes)
            {
                formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(supportedMediaType));
            }

            // Act
            MediaTypeFormatterMatch match = negotiator.MatchAcceptHeader(sortedAcceptHeaders, formatter);

            // Assert
            if (expectedMediaType == null)
            {
                Assert.Null(match);
            }
            else
            {
                Assert.Same(formatter, match.Formatter);
                Assert.Equal(MediaTypeHeaderValue.Parse(expectedMediaType), match.MediaType);
                Assert.Equal(expectedQuality, match.Quality);
                Assert.Equal(ranking, (int)match.Ranking);
            }
        }

        [Theory]
        [MemberData("MatchRequestMediaTypeData")]
        public void MatchRequestMediaType_ReturnsMatch(string requestMediaType, string[] supportedMediaTypes, string expectedMediaType)
        {
            // Arrange
            MockContentNegotiator negotiator = new MockContentNegotiator();

            HttpRequestMessage request = new HttpRequestMessage();
            request.Content = new StringContent(String.Empty);
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(requestMediaType);

            MockMediaTypeFormatter formatter = new MockMediaTypeFormatter();
            foreach (string supportedMediaType in supportedMediaTypes)
            {
                formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(supportedMediaType));
            }

            // Act
            MediaTypeFormatterMatch match = negotiator.MatchRequestMediaType(request, formatter);

            // Assert
            if (expectedMediaType == null)
            {
                Assert.Null(match);
            }
            else
            {
                Assert.Same(formatter, match.Formatter);
                Assert.Equal(MediaTypeHeaderValue.Parse(expectedMediaType), match.MediaType);
                Assert.Equal(1.0, match.Quality);
                Assert.Equal(MediaTypeFormatterMatchRanking.MatchOnRequestMediaType, match.Ranking);
            }
        }

        [Theory]
        [MemberData("ShouldMatchOnTypeData")]
        public void ShouldMatchOnType_ReturnsExpectedResult(bool excludeMatchOnType, string[] acceptHeaders, bool expectedResult)
        {
            // Arrange
            MockContentNegotiator negotiator = new MockContentNegotiator(excludeMatchOnType);
            List<MediaTypeWithQualityHeaderValue> unsortedAcceptHeaders = acceptHeaders.Select(a => MediaTypeWithQualityHeaderValue.Parse(a)).ToList();
            IEnumerable<MediaTypeWithQualityHeaderValue> sortedAcceptHeaders = negotiator.SortMediaTypeWithQualityHeaderValuesByQFactor(unsortedAcceptHeaders);

            // Act
            bool result = negotiator.ShouldMatchOnType(sortedAcceptHeaders);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [MemberData("MatchTypeData")]
        public void MatchType_ReturnsMatch(string[] supportedMediaTypes, string expectedMediaType)
        {
            // Arrange
            MockContentNegotiator negotiator = new MockContentNegotiator();

            MockMediaTypeFormatter formatter = new MockMediaTypeFormatter();
            foreach (string supportedMediaType in supportedMediaTypes)
            {
                formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(supportedMediaType));
            }

            // Act
            MediaTypeFormatterMatch match = negotiator.MatchType(typeof(object), formatter);

            // Assert
            Assert.Same(formatter, match.Formatter);
            Assert.Equal(MediaTypeHeaderValue.Parse(expectedMediaType), match.MediaType);
            Assert.Equal(1.0, match.Quality);
            Assert.Equal(MediaTypeFormatterMatchRanking.MatchOnCanWriteType, match.Ranking);
        }

        [Theory]
        [MemberData("SelectResponseMediaTypeData")]
        public void SelectResponseMediaTypeFormatter_SelectsMediaType(ICollection<MediaTypeFormatterMatch> matches, MediaTypeFormatterMatch expectedWinner)
        {
            // Arrange
            MockContentNegotiator negotiator = new MockContentNegotiator();

            // Act
            MediaTypeFormatterMatch actualWinner = negotiator.SelectResponseMediaTypeFormatter(matches);

            // Assert
            Assert.Same(expectedWinner, actualWinner);
        }

        [Theory]
        [MemberData("SelectResponseCharacterEncodingData")]
        public void SelectResponseCharacterEncoding_SelectsEncoding(string[] acceptCharsetHeaders, string requestEncoding, string[] supportedEncodings, string expectedEncoding)
        {
            // Arrange
            MockContentNegotiator negotiator = new MockContentNegotiator();

            HttpRequestMessage request = new HttpRequestMessage();
            foreach (string acceptCharsetHeader in acceptCharsetHeaders)
            {
                request.Headers.AcceptCharset.Add(StringWithQualityHeaderValue.Parse(acceptCharsetHeader));
            }

            if (requestEncoding != null)
            {
                Encoding reqEncoding = Encoding.GetEncoding(requestEncoding);
                StringContent content = new StringContent("", reqEncoding, "text/plain");
                request.Content = content;
            }

            MockMediaTypeFormatter formatter = new MockMediaTypeFormatter() { CallBase = true };
            foreach (string supportedEncoding in supportedEncodings)
            {
                formatter.SupportedEncodings.Add(Encoding.GetEncoding(supportedEncoding));
            }

            // Act
            Encoding actualEncoding = negotiator.SelectResponseCharacterEncoding(request, formatter);

            // Assert
            if (expectedEncoding == null)
            {
                Assert.Null(actualEncoding);
            }
            else
            {
                Assert.Equal(Encoding.GetEncoding(expectedEncoding), actualEncoding);
            }
        }

        [Theory]
        [TestDataSet(typeof(DefaultContentNegotiatorTests), nameof(MediaTypeWithQualityHeaderValueComparerTestsBeforeAfterSortedValues))]
        public void SortMediaTypeWithQualityHeaderValuesByQFactor_SortsCorrectly(IEnumerable<string> unsorted, IEnumerable<string> expectedSorted)
        {
            // Arrange
            MockContentNegotiator negotiator = new MockContentNegotiator();

            List<MediaTypeWithQualityHeaderValue> unsortedValues =
                new List<MediaTypeWithQualityHeaderValue>(unsorted.Select(u => MediaTypeWithQualityHeaderValue.Parse(u)));

            List<MediaTypeWithQualityHeaderValue> expectedSortedValues =
                new List<MediaTypeWithQualityHeaderValue>(expectedSorted.Select(u => MediaTypeWithQualityHeaderValue.Parse(u)));

            // Act
            IEnumerable<MediaTypeWithQualityHeaderValue> actualSorted = negotiator.SortMediaTypeWithQualityHeaderValuesByQFactor(unsortedValues);

            // Assert
            Assert.True(expectedSortedValues.SequenceEqual(actualSorted));
        }

        public static TheoryData<string[], string[]> MediaTypeWithQualityHeaderValueComparerTestsBeforeAfterSortedValues
        {
            get
            {
                return new TheoryData<string[], string[]>
                {
                    { 
                        new string[]
                        {
                            "application/*",
                            "text/plain",
                            "text/plain;q=1.0",
                            "text/plain",
                            "text/plain;q=0",
                            "*/*;q=0.8",
                            "*/*;q=1",
                            "text/*;q=1",
                            "text/plain;q=0.8",
                            "text/*;q=0.8",
                            "text/*;q=0.6",
                            "text/*;q=1.0",
                            "*/*;q=0.4",
                            "text/plain;q=0.6",
                            "text/xml",
                        }, 
                        new string[]
                        {
                            "text/plain",
                            "text/plain;q=1.0",
                            "text/plain",
                            "text/xml",
                            "application/*",
                            "text/*;q=1",
                            "text/*;q=1.0",
                            "*/*;q=1",
                            "text/plain;q=0.8",
                            "text/*;q=0.8",
                            "*/*;q=0.8",
                            "text/plain;q=0.6",
                            "text/*;q=0.6",
                            "*/*;q=0.4",
                            "text/plain;q=0",
                        }
                    }
                };
            }
        }

        [Theory]
        [TestDataSet(typeof(DefaultContentNegotiatorTests), nameof(StringWithQualityHeaderValueComparerTestsBeforeAfterSortedValues))]
        public void SortStringWithQualityHeaderValuesByQFactor_SortsCorrectly(IEnumerable<string> unsorted, IEnumerable<string> expectedSorted)
        {
            // Arrange
            MockContentNegotiator negotiator = new MockContentNegotiator();

            List<StringWithQualityHeaderValue> unsortedValues =
                new List<StringWithQualityHeaderValue>(unsorted.Select(u => StringWithQualityHeaderValue.Parse(u)));

            List<StringWithQualityHeaderValue> expectedSortedValues =
                new List<StringWithQualityHeaderValue>(expectedSorted.Select(u => StringWithQualityHeaderValue.Parse(u)));

            // Act
            IEnumerable<StringWithQualityHeaderValue> actualSorted = negotiator.SortStringWithQualityHeaderValuesByQFactor(unsortedValues);

            // Assert
            Assert.True(expectedSortedValues.SequenceEqual(actualSorted));
        }

        public static TheoryData<string[], string[]> StringWithQualityHeaderValueComparerTestsBeforeAfterSortedValues
        {
            get
            {
                return new TheoryData<string[], string[]>
                {
                    {
                        new string[]
                        {
                            "text",
                            "text;q=1.0",
                            "text",
                            "text;q=0",
                            "*;q=0.8",
                            "*;q=1",
                            "text;q=0.8",
                            "*;q=0.6",
                            "text;q=1.0",
                            "*;q=0.4",
                            "text;q=0.6",
                        },
                        new string[]
                        {
                            "text",
                            "text;q=1.0",
                            "text",
                            "text;q=1.0",
                            "*;q=1",
                            "text;q=0.8",
                            "*;q=0.8",
                            "text;q=0.6",
                            "*;q=0.6",
                            "*;q=0.4",
                            "text;q=0",
                        }
                    }
                };
            }
        }

        [Theory]
        [MemberData("UpdateBestMatchData")]
        public void UpdateBestMatch_SelectsCorrectly(MediaTypeFormatterMatch current, MediaTypeFormatterMatch replacement, bool currentWins)
        {
            // Arrange
            MockContentNegotiator negotiator = new MockContentNegotiator();

            // Act
            MediaTypeFormatterMatch actualResult = negotiator.UpdateBestMatch(current, replacement);

            // Assert
            if (currentWins)
            {
                Assert.Same(current, actualResult);
            }
            else
            {
                Assert.Same(replacement, actualResult);
            }
        }

        private class PlainTextFormatter : MediaTypeFormatter
        {
            public override bool CanReadType(Type type)
            {
                return true;
            }

            public override bool CanWriteType(Type type)
            {
                return true;
            }
        }

#if !ASPNETCORE50

        private class MyMediaTypeMapping : MediaTypeMapping
        {
            public MyMediaTypeMapping(MediaTypeHeaderValue mediaType)
                : base(mediaType)
            {
            }

            public override double TryMatchMediaType(HttpRequestMessage request)
            {
                if (request.Headers.Contains("Cookie"))
                {
                    return 1.0;
                }
                else
                {
                    return 0;
                }
            }
        }

#endif

    }
}
