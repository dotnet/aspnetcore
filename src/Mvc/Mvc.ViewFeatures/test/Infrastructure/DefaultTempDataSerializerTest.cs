// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure
{
    public class DefaultTempDataSerializerTest : TempDataSerializerTestBase
    {
        protected override TempDataSerializer GetTempDataSerializer() => new DefaultTempDataSerializer();

        [Fact]
        public void RoundTripTest_StringThatLooksLikeCompliantDateTime()
        {
            // This is an unintentional side-effect of trying to support a compat with JSON.NET.
            // Any string that looks like a compliant DateTime object will be parsed as a DateTime.
            // This test documents this behavior.
            // Arrange
            var key = "test-key";
            var testProvider = GetTempDataSerializer();
            var value = new DateTime(2009, 1, 1, 12, 37, 43);
            var input = new Dictionary<string, object>
            {
                { key, value.ToString("r") }
            };

            // Act
            var bytes = testProvider.Serialize(input);
            var values = testProvider.Deserialize(bytes);

            // Assert
            var roundTripValue = Assert.IsType<DateTime>(values[key]);
            Assert.Equal(value, roundTripValue);
        }

        [Fact]
        public void RoundTripTest_StringThatIsNotCompliantDateTime()
        {
            // This is an unintentional side-effect of trying to support a compat with JSON.NET.
            // Any string that looks like a compliant DateTime object will be parsed as a DateTime.
            // This test documents this behavior.
            // Arrange
            var key = "test-key";
            var testProvider = GetTempDataSerializer();
            var value = new DateTime(2009, 1, 1, 12, 37, 43);
            var input = new Dictionary<string, object>
            {
                { key, value.ToString() }
            };

            // Act
            var bytes = testProvider.Serialize(input);
            var values = testProvider.Deserialize(bytes);

            // Assert
            var roundTripValue = Assert.IsType<string>(values[key]);
            Assert.Equal(value.ToString(), roundTripValue);
        }

        [Fact]
        public void RoundTripTest_StringThatIsNotCompliantGuid()
        {
            // This is an unintentional side-effect of trying to support a compat with JSON.NET.
            // Any string that looks like a compliant DateTime object will be parsed as a DateTime.
            // This test documents this behavior.
            // Arrange
            var key = "test-key";
            var testProvider = GetTempDataSerializer();
            var value = Guid.NewGuid();
            var input = new Dictionary<string, object>
            {
                { key, value.ToString() }
            };

            // Act
            var bytes = testProvider.Serialize(input);
            var values = testProvider.Deserialize(bytes);

            // Assert
            var roundTripValue = Assert.IsType<string>(values[key]);
            Assert.Equal(value.ToString(), roundTripValue);
        }
    }
}
