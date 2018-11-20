// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
