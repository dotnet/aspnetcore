// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor.Compilation
{
    public class RazorFileHashTest
    {
        public static IEnumerable<object[]> GetHashCodeData
        {
            get
            {
                var longString = string.Join(Environment.NewLine,
                                             Enumerable.Repeat("Turn up for what", 14));

                var stringWith4kChars = new string('a', 4096);

                var stringValues = new[]
                {
                    new[] { "1", "2212294583" },
                    new[] { "Hello world", "2346098258" },
                    new[] { "hello world", "222957957" },
                    new[] { "The quick brown fox jumped over the lazy dog", "2765681502" },
                    new[] { longString, TestPlatformHelper.IsMono ? "4106555590" : "1994223647" },
                    new[] { stringWith4kChars.Substring(1), "2679155331" }, // 4095 chars
                    new[] { stringWith4kChars, "2627329139" },
                    new[] { stringWith4kChars + "a", "556205849" }, // 4097 chars
                    new[] { stringWith4kChars + stringWith4kChars + "aa", "1595203983" }, // 8194
                };

                var bytesToRead = new[]
                {
                    1,
                    2,
                    100,
                    1024,
                    4095,
                    4096
                };

                return from value in stringValues
                       from readBytes in bytesToRead
                       select new object[] { readBytes, value[0], value[1] };
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(2)]
        [InlineData(14)]
        public void GetHash_ThrowsIfHashAlgorithmVersionIsUnknown(int hashAlgorithmVersion)
        {
            // Arrange
            var file = new TestFileInfo();

            // Act and Assert
            ExceptionAssert.ThrowsArgument(() => RazorFileHash.GetHash(file, hashAlgorithmVersion),
                                           "hashAlgorithmVersion",
                                           "Unsupported hash algorithm.");
        }

        [Theory]
        [MemberData(nameof(GetHashCodeData))]
        public void GetHash_CalculatesHashCodeForFile(int bytesToRead, string content, string expected)
        {
            // Arrange
            var bytes = Encoding.UTF8.GetBytes(content);
            var file = new Mock<IFileInfo>();
            file.Setup(f => f.CreateReadStream())
                .Returns(new SlowStream(bytes, bytesToRead));

            // Act
            var result = RazorFileHash.GetHash(file.Object, hashAlgorithmVersion: 1);

            // Assert
            Assert.Equal(expected, result);
        }

        private class SlowStream : MemoryStream
        {
            private readonly int _bytesToRead;

            public SlowStream(byte[] buffer, int bytesToRead)
                : base(buffer)
            {
                _bytesToRead = bytesToRead;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                Debug.Assert(offset == 0 && count == 4096);
                return base.Read(buffer, 0, _bytesToRead);
            }
        }
    }
}