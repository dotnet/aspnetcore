// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.TestHost
{
    public class ResponseFeatureTests
    {
        [Fact]
        public async Task StatusCode_DefaultsTo200()
        {
            // Arrange & Act
            var responseInformation = new ResponseFeature();

            // Assert
            Assert.Equal(200, responseInformation.StatusCode);
            Assert.False(responseInformation.HasStarted);

            await responseInformation.FireOnSendingHeadersAsync();

            Assert.True(responseInformation.HasStarted);
        }

        [Fact]
        public void OnStarting_ThrowsWhenHasStarted()
        {
            // Arrange
            var responseInformation = new ResponseFeature();
            responseInformation.HasStarted = true;

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                responseInformation.OnStarting((status) =>
                {
                    return Task.FromResult(string.Empty);
                }, state: "string");
            });
        }
    }
}