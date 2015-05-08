// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.FeatureModel;
using Xunit;

namespace Microsoft.AspNet.Http.Features.Internal
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
            features.Add(typeof(IHttpRequestFeature), request);

            var provider = new QueryFeature(features);

            // Act
            var queryCollection = provider.Query;

            // Assert
            Assert.Equal("bar", queryCollection["foo"]);
        }
    }
}
