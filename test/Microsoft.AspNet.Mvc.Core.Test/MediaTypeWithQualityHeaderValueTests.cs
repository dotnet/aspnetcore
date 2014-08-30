// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.HeaderValueAbstractions;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test
{
    public class MediaTypeWithQualityHeaderValueTests
    {
        public static IEnumerable<object[]> SortValues
        {
            get
            {
                yield return new object[] {
                    new string[]
                        {
                            "application/*",
                            "text/plain",
                            "text/plain;q=1.0",
                            "text/plain",
                            "text/plain;q=0",
                            "*/*;q=0.8",
                            "*/*;q=1",
                            "text/*;q=1",
                            "text/plain;q=0.8",
                            "text/*;q=0.8",
                            "text/*;q=0.6",
                            "text/*;q=1.0",
                            "*/*;q=0.4",
                            "text/plain;q=0.6",
                            "text/xml",
                        },
                    new string[]
                        {
                            "text/plain",
                            "text/plain;q=1.0",
                            "text/plain",
                            "text/xml",
                            "application/*",
                            "text/*;q=1",
                            "text/*;q=1.0",
                            "*/*;q=1",
                            "text/plain;q=0.8",
                            "text/*;q=0.8",
                            "*/*;q=0.8",
                            "text/plain;q=0.6",
                            "text/*;q=0.6",
                            "*/*;q=0.4",
                            "text/plain;q=0",
                        }
                };
            }
        }

        [Theory]
        [MemberData(nameof(SortValues))]
        public void SortMediaTypeWithQualityHeaderValuesByQFactor_SortsCorrectly(IEnumerable<string> unsorted, IEnumerable<string> expectedSorted)
        {
            // Arrange
            var unsortedValues =
                new List<MediaTypeWithQualityHeaderValue>(unsorted.Select(u => MediaTypeWithQualityHeaderValue.Parse(u)));

            var expectedSortedValues =
                new List<MediaTypeWithQualityHeaderValue>(expectedSorted.Select(u => MediaTypeWithQualityHeaderValue.Parse(u)));

            // Act
            var actualSorted = unsortedValues.OrderByDescending(m => m, MediaTypeWithQualityHeaderValueComparer.QualityComparer).ToArray();

            // Assert
            for (int i = 0; i < expectedSortedValues.Count; i++)
            {
                Assert.True(MediaTypeWithQualityHeaderValueComparer.QualityComparer.Compare(expectedSortedValues[i], actualSorted[i]) == 0);
            }
        }
    }
}
