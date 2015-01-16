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
            options.SetFormatMapping(setFormat, mediaType);

            // Act 
            var returnmediaType = options.GetContentTypeForFormat(getFormat);

            // Assert
            Assert.Equal(mediaType, returnmediaType);
        }

        [Theory]
        [InlineData("xml", null)]
        [InlineData(".json", null)]
        [InlineData(null, "application/json")]
        [InlineData("", "text/foo")]        
        public void FormatterMappings_SetFormatMapping_Invalid(string format, string contentType)
        {
            // Arrange
            MediaTypeHeaderValue mediaType = null;
            if (!string.IsNullOrEmpty(contentType))
            {
                mediaType = MediaTypeHeaderValue.Parse(contentType);
            }

            var options = new FormatterMappings();            

            // Act and Assert
            Assert.Throws<ArgumentException>(() => options.SetFormatMapping(format, mediaType));
        }
    }
}