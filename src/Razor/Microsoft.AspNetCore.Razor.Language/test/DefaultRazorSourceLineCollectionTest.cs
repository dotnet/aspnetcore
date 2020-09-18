// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.RegularExpressions;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Test
{
    public class DefaultRazorSourceLineCollectionTest
    {
        [Fact]
        public void GetLocation_Negative()
        {
            var content = @"@addTagHelper, * Stuff
@* A comment *@";
            var document = TestRazorSourceDocument.Create(content);
            var collection = new DefaultRazorSourceLineCollection(document);

            Assert.Throws<IndexOutOfRangeException>(() => collection.GetLocation(-1));
        }

        [Fact]
        public void GetLocation_TooBig()
        {
            var content = @"addTagHelper, * Stuff
@* A comment *@";
            var document = TestRazorSourceDocument.Create(content);
            var collection = new DefaultRazorSourceLineCollection(document);

            Assert.Throws<IndexOutOfRangeException>(() => collection.GetLocation(40));
        }

        [Fact]
        public void GetLocation_AtStart()
        {
            var content = @"@addTaghelper, * Stuff
@* A comment *@";
            var document = TestRazorSourceDocument.Create(content);
            var collection = new DefaultRazorSourceLineCollection(document);

            var location = collection.GetLocation(0);

            var expected = new SourceLocation("test.cshtml", 0, 0, 0);
            Assert.Equal(expected, location);
        }

        [Fact]
        public void GetLocation_AtEnd()
        {
            var content = @"@addTagHelper, * Stuff
@* A comment *@";
            var document = TestRazorSourceDocument.Create(content);
            var collection = new DefaultRazorSourceLineCollection(document);
            var length = content.Length;

            var location = collection.GetLocation(length);

            var expected = new SourceLocation("test.cshtml", length, 1, 15);
            Assert.Equal(expected, location);
        }

        [Fact]
        public void GetLineLength_Negative()
        {
            var content = @"@addTagHelper, * Stuff
@* A comment *@";
            var document = TestRazorSourceDocument.Create(content);
            var collection = new DefaultRazorSourceLineCollection(document);

            Assert.Throws<IndexOutOfRangeException>(() => collection.GetLineLength(-1));
        }

        [Fact]
        public void GetLineLength_Bigger()
        {
            var content = @"@addTagHelper, * Stuff
@* A comment *@";
            var document = TestRazorSourceDocument.Create(content);
            var collection = new DefaultRazorSourceLineCollection(document);

            Assert.Throws<IndexOutOfRangeException>(() => collection.GetLineLength(40));
        }

        [Fact]
        public void GetLineLength_AtStart()
        {
            var content = @"@addTagHelper, * Stuff
@* A comment *@";
            var document = TestRazorSourceDocument.Create(content);
            var collection = new DefaultRazorSourceLineCollection(document);

            var lineLength = collection.GetLineLength(0);

            var expectedLineLength = 22 + Environment.NewLine.Length;
            Assert.Equal(expectedLineLength, lineLength);
        }

        [Fact]
        public void GetLineLength_AtEnd()
        {
            var content = @"@addTagHelper, * Stuff
@* A comment *@";
            var document = TestRazorSourceDocument.Create(content);
            var collection = new DefaultRazorSourceLineCollection(document);

            var lineLength = collection.GetLineLength(1);

            var expectedLineLength = 15;
            Assert.Equal(expectedLineLength, lineLength);
        }
    }
}
