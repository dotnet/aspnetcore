using System;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.Net.Http.Headers;
using Xunit;


namespace Microsoft.AspNet.Mvc.Core.Test
{
    public class OutputFormatterOptionsTest
    {
        [Theory]
        [InlineData("xml", "application/xml")]
        [InlineData("json", "application/json")]
        [InlineData("foo", "text/foo")]
        [InlineData(".json", "application/json")]
        [InlineData(".foo", "text/foo")]
        public void OutputFormatterOptions_AddFormatMapping_Valid(string format, string contentType) 
        {
            // Arrange
            var mediaType = MediaTypeHeaderValue.Parse(contentType);
            OutputFormatterOptions options = new OutputFormatterOptions();
            options.AddFormatMapping(format, mediaType);

            // Act 
            var returnmediaType = options.GetContentTypeForFormat(format);

            // Assert
            Assert.Equal(mediaType, returnmediaType);
        }

        [Theory]
        [InlineData(".xml", "application/xml", "xml")]
        [InlineData("json", "application/json", "JSON")]
        [InlineData(".foo", "text/foo", "Foo")]
        [InlineData(".Json", "application/json", "json")]
        [InlineData("FOo", "text/foo", "FOO")]
        public void OutputFormatterOptions_AddFormatMapping_DiffSetGetFormat(string setFormat, string contentType, string getFormat)
        {
            // Arrange
            var mediaType = MediaTypeHeaderValue.Parse(contentType);
            OutputFormatterOptions options = new OutputFormatterOptions();
            options.AddFormatMapping(setFormat, mediaType);

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
        public void OutputFormatterOptions_AddFormatMapping_Invalid(string format, string contentType)
        {
            // Arrange
            MediaTypeHeaderValue mediaType = null;
            if (!string.IsNullOrEmpty(contentType))
            {
                mediaType = MediaTypeHeaderValue.Parse(contentType);
            }

            OutputFormatterOptions options = new OutputFormatterOptions();
            options.AddFormatMapping(format, mediaType);

            // Act and Assert
            Assert.Throws<ArgumentException>(() => options.GetContentTypeForFormat(format));
        }
    }
}