// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Net.Http.Headers;
using Xunit;


namespace Microsoft.AspNet.Mvc
{
    public class FormatterMappingsTest
    {
        [Theory]
        [InlineData(".xml", "application/xml", "xml")]
        [InlineData("json", "application/json", "JSON")]
        [InlineData(".foo", "text/foo", "Foo")]
        [InlineData(".Json", "application/json", "json")]
        [InlineData("FOo", "text/foo", "FOO")]
        public void FormatterMappings_SetFormatMapping_DiffSetGetFormat(string setFormat, string contentType, string getFormat)
        {
            // Arrange
            var mediaType = MediaTypeHeaderValue.Parse(contentType);
            var options = new FormatterMappings();
            options.SetMediaTypeMappingForFormat(setFormat, mediaType);

            // Act 
            var returnMediaType = options.GetMediaTypeMappingForFormat(getFormat);

            // Assert
            Assert.Equal(mediaType, returnMediaType);
        }
        
        [Theory]
        [InlineData("application/*")]
        [InlineData("*/json")]
        [InlineData("*/*")]
        public void FormatterMappings_Wildcardformat(string format)
        {
            // Arrange
            var options = new FormatterMappings();
            var expected = string.Format(@"The media type {0} is not valid. The media type containing ""<mediatype>/*"" are not supported.", format);

            // Act and assert
            var exception = Assert.Throws<ArgumentException>(() => options.SetMediaTypeMappingForFormat("star", MediaTypeHeaderValue.Parse(format)));
            Assert.Equal(expected, exception.Message);
        }
    }
}