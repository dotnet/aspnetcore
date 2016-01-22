// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Chunks.Generators
{
    // Really tests underlying ParentChunkGenerator
    public class RazorCommentChunkGeneratorTest
    {
        public static TheoryData<RazorCommentChunkGenerator, IParentChunkGenerator> MatchingTestDataSet
        {
            get
            {
                return new TheoryData<RazorCommentChunkGenerator, IParentChunkGenerator>
                {
                    { new RazorCommentChunkGenerator(), new RazorCommentChunkGenerator() },
                };
            }
        }

        public static TheoryData<IParentChunkGenerator, object> NonMatchingTestDataSet
        {
            get
            {
                return new TheoryData<IParentChunkGenerator, object>
                {
                    { new RazorCommentChunkGenerator(), null },
                    { new RazorCommentChunkGenerator(), new object() },
                    { new RazorCommentChunkGenerator(), ParentChunkGenerator.Null },
                    {
                        new RazorCommentChunkGenerator(),
                        new AttributeBlockChunkGenerator(name: null, prefix: null, suffix: null)
                    },
                    {
                        new RazorCommentChunkGenerator(),
                        new DynamicAttributeBlockChunkGenerator(prefix: null, offset: 0, line: 0, col: 0)
                    },
                    { new RazorCommentChunkGenerator(), new ExpressionChunkGenerator() },
                    { new RazorCommentChunkGenerator(), new SectionChunkGenerator(sectionName: null) },
                    {
                        new RazorCommentChunkGenerator(),
                        new TagHelperChunkGenerator(Enumerable.Empty<TagHelperDescriptor>())
                    },
                    { new RazorCommentChunkGenerator(), new TemplateBlockChunkGenerator() },
                    {
                        new RazorCommentChunkGenerator(),
                        new AddImportChunkGenerator(ns: "Fred")
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(MatchingTestDataSet))]
        public void Equals_True_WhenExpected(RazorCommentChunkGenerator leftObject, IParentChunkGenerator rightObject)
        {
            // Arrange & Act
            var result = leftObject.Equals(rightObject);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [MemberData(nameof(NonMatchingTestDataSet))]
        public void Equals_False_WhenExpected(IParentChunkGenerator leftObject, object rightObject)
        {
            // Arrange & Act
            var result = leftObject.Equals(rightObject);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [MemberData(nameof(MatchingTestDataSet))]
        public void GetHashCode_ReturnsSameValue_WhenEqual(
            RazorCommentChunkGenerator leftObject,
            IParentChunkGenerator rightObject)
        {
            // Arrange & Act
            var leftResult = leftObject.GetHashCode();
            var rightResult = rightObject.GetHashCode();

            // Assert
            Assert.Equal(leftResult, rightResult);
        }
    }
}
