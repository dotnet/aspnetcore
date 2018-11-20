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
            Assert.True(responseInformation.Headers.IsReadOnly);
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

        [Fact]
        public void StatusCode_ThrowsWhenHasStarted()
        {
            var responseInformation = new ResponseFeature();
            responseInformation.HasStarted = true;

            Assert.Throws<InvalidOperationException>(() => responseInformation.StatusCode = 400);
            Assert.Throws<InvalidOperationException>(() => responseInformation.ReasonPhrase = "Hello World");
        }

        [Fact]
        public void StatusCode_MustBeGreaterThan99()
        {
            var responseInformation = new ResponseFeature();

            Assert.Throws<ArgumentOutOfRangeException>(() => responseInformation.StatusCode = 99);
            Assert.Throws<ArgumentOutOfRangeException>(() => responseInformation.StatusCode = 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => responseInformation.StatusCode = -200);
            responseInformation.StatusCode = 100;
            responseInformation.StatusCode = 200;
            responseInformation.StatusCode = 1000;
        }
    }
}