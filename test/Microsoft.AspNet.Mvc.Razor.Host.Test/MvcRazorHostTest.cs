// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.Razor;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Text;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class MvcRazorHostTest
    {
        [Fact]
        public void MvcRazorHost_GeneratesTagHelperModelExpressionCode_DesignTime()
        {
            // Arrange
            var host = new MvcRazorHost(new TestFileSystem())
            {
                DesignTimeMode = true
            };
            var expectedLineMappings = new List<LineMapping>
            {
                BuildLineMapping(documentAbsoluteIndex: 7, 
                                 documentLineIndex: 0,
                                 documentCharacterIndex: 7, 
                                 generatedAbsoluteIndex: 444,
                                 generatedLineIndex: 12,
                                 generatedCharacterIndex: 7, 
                                 contentLength: 8),
                BuildLineMapping(documentAbsoluteIndex: 33,
                                 documentLineIndex: 2,
                                 documentCharacterIndex: 14,
                                 generatedAbsoluteIndex: 823,
                                 generatedLineIndex: 25,
                                 generatedCharacterIndex: 14,
                                 contentLength: 85)
            };

            // Act and Assert
            RunDesignTimeTest(host, 
                              testName: "ModelExpressionTagHelper", 
                              expectedLineMappings: expectedLineMappings);
        }

        [Theory]
        [InlineData("Basic")]
        [InlineData("Inject")]
        [InlineData("InjectWithModel")]
        [InlineData("Model")]
        [InlineData("ModelExpressionTagHelper")]
        public void MvcRazorHost_ParsesAndGeneratesCodeForBasicScenarios(string scenarioName)
        {
            // Arrange
            var host = new MvcRazorHost(new TestFileSystem());

            // Act and Assert
            RunRuntimeTest(host, scenarioName);
        }

        [Fact]
        public void InjectVisitor_GeneratesCorrectLineMappings()
        {
            // Arrange
            var host = new MvcRazorHost(new TestFileSystem())
            {
                DesignTimeMode = true
            };
            host.NamespaceImports.Clear();
            var expectedLineMappings = new List<LineMapping>
            {
                BuildLineMapping(1, 0, 1, 59, 3, 0, 17),
                BuildLineMapping(28, 1, 8, 688, 26, 8, 20)
            };

            // Act and Assert
            RunDesignTimeTest(host, "Inject", expectedLineMappings);
        }

        [Fact]
        public void InjectVisitorWithModel_GeneratesCorrectLineMappings()
        {
            // Arrange
            var host = new MvcRazorHost(new TestFileSystem())
            {
                DesignTimeMode = true
            };
            host.NamespaceImports.Clear();
            var expectedLineMappings = new[]
            {
                BuildLineMapping(7, 0, 7, 214, 6, 7, 7),
                BuildLineMapping(24, 1, 8, 713, 26, 8, 20),
                BuildLineMapping(54, 2, 8, 921, 34, 8, 23)
            };

            // Act and Assert
            RunDesignTimeTest(host, "InjectWithModel", expectedLineMappings);
        }

        [Fact]
        public void ModelVisitor_GeneratesCorrectLineMappings()
        {
            // Arrange
            var host = new MvcRazorHost(new TestFileSystem())
            {
                DesignTimeMode = true
            };
            host.NamespaceImports.Clear();
            var expectedLineMappings = new[]
            {
                BuildLineMapping(7, 0, 7, 194, 6, 7, 30),
            };

            // Act and Assert
            RunDesignTimeTest(host, "Model", expectedLineMappings);
        }

        private static void RunRuntimeTest(MvcRazorHost host,
                                           string testName)
        {
            var inputFile = "TestFiles/Input/" + testName + ".cshtml";
            var expectedCode = ReadResource("TestFiles/Output/Runtime/" + testName + ".cs");

            // Act
            GeneratorResults results;
            using (var stream = GetResourceStream(inputFile))
            {
                results = host.GenerateCode(inputFile, stream);
            }

            // Assert
            Assert.True(results.Success);
            Assert.Equal(expectedCode, results.GeneratedCode);
            Assert.Empty(results.ParserErrors);
        }

        private static void RunDesignTimeTest(MvcRazorHost host,
                                              string testName,
                                              IEnumerable<LineMapping> expectedLineMappings)
        {
            var inputFile = "TestFiles/Input/" + testName + ".cshtml";
            var expectedCode = ReadResource("TestFiles/Output/DesignTime/" + testName + ".cs");

            // Act
            GeneratorResults results;
            using (var stream = GetResourceStream(inputFile))
            {
                results = host.GenerateCode(inputFile, stream);
            }

            // Assert
            Assert.True(results.Success);
            Assert.Equal(expectedCode, results.GeneratedCode);
            Assert.Empty(results.ParserErrors);
            Assert.Equal(expectedLineMappings, results.DesignTimeLineMappings);
        }

        private static string ReadResource(string resourceName)
        {
            using (var stream = GetResourceStream(resourceName))
            {
                using (var streamReader = new StreamReader(stream))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }

        private static Stream GetResourceStream(string resourceName)
        {
            var assembly = typeof(MvcRazorHostTest).Assembly;
            return assembly.GetManifestResourceStream(resourceName);
        }

        private static LineMapping BuildLineMapping(int documentAbsoluteIndex,
                                                    int documentLineIndex,
                                                    int documentCharacterIndex,
                                                    int generatedAbsoluteIndex,
                                                    int generatedLineIndex,
                                                    int generatedCharacterIndex,
                                                    int contentLength)
        {
            var documentLocation = new SourceLocation(documentAbsoluteIndex,
                                                      documentLineIndex,
                                                      documentCharacterIndex);
            var generatedLocation = new SourceLocation(generatedAbsoluteIndex,
                                                       generatedLineIndex,
                                                       generatedCharacterIndex);

            return new LineMapping(
                documentLocation: new MappingLocation(documentLocation, contentLength),
                generatedLocation: new MappingLocation(generatedLocation, contentLength));
        }
    }
}