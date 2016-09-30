// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Razor.Host.Internal;
using Microsoft.AspNetCore.Razor.Chunks;
using Microsoft.AspNetCore.Razor.CodeGenerators;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Host.Test.Internal
{
    public class ViewComponentTagHelperChunkVisitorTest
    {
        public static TheoryData CodeGenerationData
        {
            get
            {
                var oneInstanceChunks = ChunkVisitorTestFactory.GetTestChunks(visitedTagHelperChunks: true);
                var twoInstanceChunks = ChunkVisitorTestFactory.GetTestChunks(visitedTagHelperChunks: true);
                twoInstanceChunks.Add(twoInstanceChunks[twoInstanceChunks.Count - 1]);

                return new TheoryData<IList<Chunk>>
                {
                    oneInstanceChunks,
                    twoInstanceChunks
                };
            }
        }

        [Theory]
        [MemberData(nameof(CodeGenerationData))]
        public void Accept_CorrectlyGeneratesCode(IList<Chunk> chunks)
        {
            // Arrange
            var writer = new CSharpCodeWriter();
            var context = ChunkVisitorTestFactory.CreateCodeGeneratorContext();
            var chunkVisitor = new ViewComponentTagHelperChunkVisitor(writer, context);

            var assembly = typeof(ViewComponentTagHelperChunkVisitorTest).GetTypeInfo().Assembly;
            var path = "TestFiles/Output/Runtime/GeneratedViewComponentTagHelperClasses.cs";
            var expectedOutput = ResourceFile.ReadResource(assembly, path, sourceFile: true);

            // Act
            chunkVisitor.Accept(chunks);
            var resultOutput = writer.GenerateCode();

#if GENERATE_BASELINES
            if (!string.Equals(expectedOutput, resultOutput, System.StringComparison.Ordinal))
            {
                ResourceFile.UpdateFile(assembly, path, expectedOutput, resultOutput);
            }
#else
            // Assert
            Assert.Equal(expectedOutput, resultOutput, ignoreLineEndingDifferences: true);
#endif

        }
    }
}
