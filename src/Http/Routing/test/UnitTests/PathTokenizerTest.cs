// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Routing
{
    public class PathTokenizerTest
    {
        public static TheoryData<string, StringSegment[]> TokenizationData
        {
            get
            {
                return new TheoryData<string, StringSegment[]>
                {
                    { string.Empty, new StringSegment[] { } },
                    { "/", new StringSegment[] { } },
                    { "//", new StringSegment[] { new StringSegment("//", 1, 0) } },
                    {
                        "///",
                        new StringSegment[]
                        {
                            new StringSegment("///", 1, 0),
                            new StringSegment("///", 2, 0),
                        }
                    },
                    {
                        "////",
                        new StringSegment[]
                        {
                            new StringSegment("////", 1, 0),
                            new StringSegment("////", 2, 0),
                            new StringSegment("////", 3, 0),
                        }
                    },
                    { "/zero", new StringSegment[] { new StringSegment("/zero", 1, 4) } },
                    { "/zero/", new StringSegment[] { new StringSegment("/zero/", 1, 4) } },
                    {
                        "/zero/one",
                        new StringSegment[]
                        {
                            new StringSegment("/zero/one", 1, 4),
                            new StringSegment("/zero/one", 6, 3),
                        }
                    },
                    {
                        "/zero/one/",
                        new StringSegment[]
                        {
                            new StringSegment("/zero/one/", 1, 4),
                            new StringSegment("/zero/one/", 6, 3),
                        }
                    },
                    {
                        "/zero/one/two",
                        new StringSegment[]
                        {
                            new StringSegment("/zero/one/two", 1, 4),
                            new StringSegment("/zero/one/two", 6, 3),
                            new StringSegment("/zero/one/two", 10, 3),
                        }
                    },
                    {
                        "/zero/one/two/",
                        new StringSegment[]
                        {
                            new StringSegment("/zero/one/two/", 1, 4),
                            new StringSegment("/zero/one/two/", 6, 3),
                            new StringSegment("/zero/one/two/", 10, 3),
                        }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(TokenizationData))]
        public void PathTokenizer_Count(string path, StringSegment[] expectedSegments)
        {
            // Arrange
            var tokenizer = new PathTokenizer(new PathString(path));

            // Act
            var count = tokenizer.Count;

            // Assert
            Assert.Equal(expectedSegments.Length, count);
        }

        [Theory]
        [MemberData(nameof(TokenizationData))]
        public void PathTokenizer_Indexer(string path, StringSegment[] expectedSegments)
        {
            // Arrange
            var tokenizer = new PathTokenizer(new PathString(path));

            // Act & Assert
            for (var i = 0; i < expectedSegments.Length; i++)
            {
                Assert.Equal(expectedSegments[i], tokenizer[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TokenizationData))]
        public void PathTokenizer_Enumerator(string path, StringSegment[] expectedSegments)
        {
            // Arrange
            var tokenizer = new PathTokenizer(new PathString(path));

            // Act & Assert
            Assert.Equal<StringSegment>(expectedSegments, tokenizer);
        }
    }
}
