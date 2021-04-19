// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.HttpLogging.Tests
{
    public class HttpLoggingOptionsTests
    {
        [Fact]
        public void DefaultsMediaTypes()
        {
            var options = new HttpLoggingOptions();
            var defaultMediaTypes = options.MediaTypeOptions.MediaTypeStates;
            Assert.Equal(5, defaultMediaTypes.Count);

            Assert.Contains(defaultMediaTypes, w => w.MediaTypeHeaderValue.MediaType.Equals("application/json"));
            Assert.Contains(defaultMediaTypes, w => w.MediaTypeHeaderValue.MediaType.Equals("application/*+json"));
            Assert.Contains(defaultMediaTypes, w => w.MediaTypeHeaderValue.MediaType.Equals("application/xml"));
            Assert.Contains(defaultMediaTypes, w => w.MediaTypeHeaderValue.MediaType.Equals("application/*+xml"));
            Assert.Contains(defaultMediaTypes, w => w.MediaTypeHeaderValue.MediaType.Equals("text/*"));
        }

        [Fact]
        public void CanAddMediaTypesString()
        {
            var options = new HttpLoggingOptions();
            options.MediaTypeOptions.AddText("test/*");

            var defaultMediaTypes = options.MediaTypeOptions.MediaTypeStates;
            Assert.Equal(6, defaultMediaTypes.Count);

            Assert.Contains(defaultMediaTypes, w => w.MediaTypeHeaderValue.MediaType.Equals("test/*"));
        }

        [Fact]
        public void CanAddMediaTypesWithCharset()
        {
            var options = new HttpLoggingOptions();
            options.MediaTypeOptions.AddText("test/*; charset=ascii");

            var defaultMediaTypes = options.MediaTypeOptions.MediaTypeStates;
            Assert.Equal(6, defaultMediaTypes.Count);

            Assert.Contains(defaultMediaTypes, w => w.MediaTypeHeaderValue.Encoding.WebName.Equals("us-ascii"));
        }

        [Fact]
        public void CanClearMediaTypes()
        {
            var options = new HttpLoggingOptions();
            options.MediaTypeOptions.Clear();
            Assert.Empty(options.MediaTypeOptions.MediaTypeStates);
        }

        [Fact]
        public void HeadersAreCaseInsensitive()
        {
            var options = new HttpLoggingOptions();
            options.RequestHeaders.Clear();
            options.RequestHeaders.Add("Test");
            options.RequestHeaders.Add("test");

            Assert.Single(options.RequestHeaders);
        }
    }
}
