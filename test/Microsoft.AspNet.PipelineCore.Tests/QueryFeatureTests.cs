// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.HttpFeature;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Http.Core.Tests
{
    public class QueryFeatureTests
    {
        [Fact]
        public void QueryReturnsParsedQueryCollection()
        {
            // Arrange
            var features = new Mock<IFeatureCollection>();
            var request = new Mock<IHttpRequestFeature>();
            request.SetupGet(r => r.QueryString).Returns("foo=bar");

            object value = request.Object;
            features.Setup(f => f.TryGetValue(typeof(IHttpRequestFeature), out value))
                    .Returns(true);

            var provider = new QueryFeature(features.Object);

            // Act
            var queryCollection = provider.Query;

            // Assert
            Assert.Equal("bar", queryCollection["foo"]);
        }
    }
}
