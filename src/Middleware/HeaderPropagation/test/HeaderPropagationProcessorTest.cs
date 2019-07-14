// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.HeaderPropagation.Tests
{
    public class HeaderPropagationProcessorTest
    {
        public HeaderPropagationProcessorTest()
        {
            Configuration = new HeaderPropagationOptions();
            State = new HeaderPropagationValues();
            Processor = new HeaderPropagationProcessor(new OptionsWrapper<HeaderPropagationOptions>(Configuration), State);
            RequestHeaders = new Dictionary<string, StringValues>();
        }

        public HeaderPropagationOptions Configuration { get; set; }
        public HeaderPropagationValues State { get; set; }
        public HeaderPropagationProcessor Processor { get; set; }
        public IDictionary<string, StringValues> RequestHeaders { get; set; }

        [Fact]
        public void HeaderInRequest_AddCorrectValue()
        {
            // Arrange
            Configuration.Headers.Add("in");
            RequestHeaders.Add("in", "test");

            // Act
            Processor.ProcessRequest(RequestHeaders);

            // Assert
            Assert.Contains("in", State.Headers.Keys);
            Assert.Equal(new[] { "test" }, State.Headers["in"]);
        }

        [Fact]
        public void NoHeaderInRequest_DoesNotAddIt()
        {
            // Arrange
            Configuration.Headers.Add("in");

            // Act
            Processor.ProcessRequest(RequestHeaders);

            // Assert
            Assert.Empty(State.Headers);
        }

        [Fact]
        public void HeaderInRequest_NotInOptions_DoesNotAddIt()
        {
            // Arrange
            RequestHeaders.Add("in", "test");

            // Act
            Processor.ProcessRequest(RequestHeaders);

            // Assert
            Assert.Empty(State.Headers);
        }

        [Fact]
        public void MultipleHeadersInRequest_AddAllHeaders()
        {
            // Arrange
            Configuration.Headers.Add("in");
            Configuration.Headers.Add("another");
            RequestHeaders.Add("in", "test");
            RequestHeaders.Add("another", "test2");

            // Act
            Processor.ProcessRequest(RequestHeaders);

            // Assert
            Assert.Contains("in", State.Headers.Keys);
            Assert.Equal(new[] { "test" }, State.Headers["in"]);
            Assert.Contains("another", State.Headers.Keys);
            Assert.Equal(new[] { "test2" }, State.Headers["another"]);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void HeaderEmptyInRequest_DoesNotAddIt(string headerValue)
        {
            // Arrange
            Configuration.Headers.Add("in");
            RequestHeaders.Add("in", headerValue);

            // Act
            Processor.ProcessRequest(RequestHeaders);

            // Assert
            Assert.DoesNotContain("in", State.Headers.Keys);
        }

        [Theory]
        [InlineData(new[] { "default" }, new[] { "default" })]
        [InlineData(new[] { "default", "other" }, new[] { "default", "other" })]
        public void UsesValueFilter(string[] filterValues, string[] expectedValues)
        {
            // Arrange
            string receivedName = null;
            StringValues receivedValue = default;
            IDictionary<string, StringValues> receivedRequestHeaders = null;
            Configuration.Headers.Add("in", context =>
            {
                receivedValue = context.HeaderValue;
                receivedName = context.HeaderName;
                receivedRequestHeaders = context.RequestHeaders;
                return filterValues;
            });

            RequestHeaders.Add("in", "value");

            // Act
            Processor.ProcessRequest(RequestHeaders);

            // Assert
            Assert.Contains("in", State.Headers.Keys);
            Assert.Equal(expectedValues, State.Headers["in"]);
            Assert.Equal("in", receivedName);
            Assert.Equal(new StringValues("value"), receivedValue);
            Assert.Same(RequestHeaders, receivedRequestHeaders);
        }

        [Fact]
        public void PreferValueFilter_OverRequestHeader()
        {
            // Arrange
            Configuration.Headers.Add("in", context => "test");
            RequestHeaders.Add("in", "no");

            // Act
            Processor.ProcessRequest(RequestHeaders);

            // Assert
            Assert.Contains("in", State.Headers.Keys);
            Assert.Equal("test", State.Headers["in"]);
        }

        [Fact]
        public void EmptyValuesFromValueFilter_DoesNotAddIt()
        {
            // Arrange
            Configuration.Headers.Add("in", (context) => StringValues.Empty);

            // Act
            Processor.ProcessRequest(RequestHeaders);

            // Assert
            Assert.DoesNotContain("in", State.Headers.Keys);
        }

        [Fact]
        public void MultipleEntries_AddsFirstToProduceValue()
        {
            // Arrange
            Configuration.Headers.Add("in");
            Configuration.Headers.Add("in", (context) => StringValues.Empty);
            Configuration.Headers.Add("in", (context) => "Test");

            // Act
            Processor.ProcessRequest(RequestHeaders);

            // Assert
            Assert.Contains("in", State.Headers.Keys);
            Assert.Equal("Test", State.Headers["in"]);
        }

        [Fact]
        public void MultipleCalls_ThrowsException()
        {
            // Arrange
            Configuration.Headers.Add("in");
            Configuration.Headers.Add("in", (context) => StringValues.Empty);
            Configuration.Headers.Add("in", (context) => "Test");

            // Act
            Processor.ProcessRequest(RequestHeaders);
            var exception = Assert.Throws<InvalidOperationException>(() => Processor.ProcessRequest(RequestHeaders));

            // Assert
            Assert.Equal(
                "The HeaderPropagationValues.Headers was already initialized. " +
                "Each invocation of HeaderPropagationProcessor.ProcessRequest() must be in a separate async context.",
                exception.Message);
        }
    }
}
