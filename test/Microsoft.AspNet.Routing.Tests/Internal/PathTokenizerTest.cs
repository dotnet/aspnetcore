// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Xunit;

namespace Microsoft.AspNet.Routing.Internal
{
    public class PathTokenizerTest
    {
        public static TheoryData<string, PathSegment[]> TokenizationData
        {
            get
            {
                return new TheoryData<string, PathSegment[]>
                {
                    { string.Empty, new PathSegment[] { } },
                    { "/", new PathSegment[] { } },
                    { "//", new PathSegment[] { new PathSegment("//", 1, 0) } },
                    {
                        "///",
                        new PathSegment[]
                        {
                            new PathSegment("///", 1, 0),
                            new PathSegment("///", 2, 0),
                        }
                    },
                    {
                        "////",
                        new PathSegment[]
                        {
                            new PathSegment("////", 1, 0),
                            new PathSegment("////", 2, 0),
                            new PathSegment("////", 3, 0),
                        }
                    },
                    { "/zero", new PathSegment[] { new PathSegment("/zero", 1, 4) } },
                    { "/zero/", new PathSegment[] { new PathSegment("/zero/", 1, 4) } },
                    {
                        "/zero/one",
                        new PathSegment[]
                        {
                            new PathSegment("/zero/one", 1, 4),
                            new PathSegment("/zero/one", 6, 3),
                        }
                    },
                    {
                        "/zero/one/",
                        new PathSegment[]
                        {
                            new PathSegment("/zero/one/", 1, 4),
                            new PathSegment("/zero/one/", 6, 3),
                        }
                    },
                    {
                        "/zero/one/two",
                        new PathSegment[]
                        {
                            new PathSegment("/zero/one/two", 1, 4),
                            new PathSegment("/zero/one/two", 6, 3),
                            new PathSegment("/zero/one/two", 10, 3),
                        }
                    },
                    {
                        "/zero/one/two/",
                        new PathSegment[]
                        {
                            new PathSegment("/zero/one/two/", 1, 4),
                            new PathSegment("/zero/one/two/", 6, 3),
                            new PathSegment("/zero/one/two/", 10, 3),
                        }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(TokenizationData))]
        public void PathTokenizer_Count(string path, PathSegment[] expectedSegments)
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
        public void PathTokenizer_Indexer(string path, PathSegment[] expectedSegments)
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
        public void PathTokenizer_Enumerator(string path, PathSegment[] expectedSegments)
        {
            // Arrange
            var tokenizer = new PathTokenizer(new PathString(path));

            // Act & Assert
            Assert.Equal<PathSegment>(expectedSegments, tokenizer);
        }
    }
}
