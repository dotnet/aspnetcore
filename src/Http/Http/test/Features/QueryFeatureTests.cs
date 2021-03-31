// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Http.Features
{
    public class QueryFeatureTests
    {
        [Fact]
        public void QueryReturnsParsedQueryCollection()
        {
            // Arrange
            var features = new FeatureCollection();
            var request = new HttpRequestFeature();
            request.QueryString = "foo=bar";
            features[typeof(IHttpRequestFeature)] = request;

            var provider = new QueryFeature(features);

            // Act
            var queryCollection = provider.Query;

            // Assert
            Assert.Equal("bar", queryCollection["foo"]);
        }

        [Theory]
        [InlineData("?q", "q")]
        [InlineData("?q&", "q")]
        [InlineData("?q1=abc&q2", "q2")]
        [InlineData("?q=", "q")]
        [InlineData("?q=&", "q")]
        public void KeyWithoutValuesAddedToQueryCollection(string queryString, string emptyParam)
        {
            var features = new FeatureCollection();
            var request = new HttpRequestFeature();
            request.QueryString = queryString;
            features[typeof(IHttpRequestFeature)] = request;

            var provider = new QueryFeature(features);

            var queryCollection = provider.Query;

            Assert.True(queryCollection.Keys.Contains(emptyParam));
            Assert.Equal(string.Empty, queryCollection[emptyParam]);
        }

        [Theory]
        [InlineData("?&&")]
        [InlineData("?&")]
        [InlineData("&&")]
        public void EmptyKeysNotAddedToQueryCollection(string queryString)
        {
            var features = new FeatureCollection();
            var request = new HttpRequestFeature();
            request.QueryString = queryString;
            features[typeof(IHttpRequestFeature)] = request;

            var provider = new QueryFeature(features);

            var queryCollection = provider.Query;

            Assert.Equal(0, queryCollection.Count);
        }
    }
}
