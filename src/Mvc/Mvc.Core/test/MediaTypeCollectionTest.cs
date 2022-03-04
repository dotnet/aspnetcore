// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    public class MediaTypeCollectionTest
    {
        [Fact]
        public void Add_MediaTypeHeaderValue_AddsTheStringSegmentRepresentationOfTheMediaType()
        {
            // Arrange
            var mediaTypeHeaderValue = MediaTypeHeaderValue.Parse("application/json;charset=utf-16");
            var collection = new MediaTypeCollection();

            // Act
            collection.Add(mediaTypeHeaderValue);

            // Assert
            Assert.Contains("application/json; charset=utf-16", collection);
        }

        [Fact]
        public void Insert_MediaTypeHeaderValue_AddsTheStringSegmentRepresentationOfTheMediaTypeOnTheGivenIndex()
        {
            // Arrange
            var mediaTypeHeaderValue = MediaTypeHeaderValue.Parse("application/json;charset=utf-16");
            var collection = new MediaTypeCollection
            {
                MediaTypeHeaderValue.Parse("text/plain"),
                MediaTypeHeaderValue.Parse("text/xml")
            };

            // Act
            collection.Insert(1, mediaTypeHeaderValue);

            // Assert
            Assert.Equal(1, collection.IndexOf("application/json; charset=utf-16"));
        }

        [Fact]
        public void Remove_MediaTypeHeaderValue_RemovesTheStringSegmentRepresentationOfTheMediaType()
        {
            // Arrange
            var collection = new MediaTypeCollection
            {
                MediaTypeHeaderValue.Parse("text/plain"),
                MediaTypeHeaderValue.Parse("text/xml")
            };

            // Act
            collection.Remove(MediaTypeHeaderValue.Parse("text/xml"));

            // Assert
            Assert.DoesNotContain("text/xml", collection);
        }
    }
}
