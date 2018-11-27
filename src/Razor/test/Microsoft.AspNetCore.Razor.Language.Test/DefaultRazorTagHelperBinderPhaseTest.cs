// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class DefaultRazorTagHelperBinderPhaseTest
    {
        [Fact]
        public void Execute_CanHandleSingleLengthAddTagHelperDirective()
        {
            // Arrange
            var projectEngine = RazorProjectEngine.Create(builder =>
            {
                builder.AddTagHelpers(new TagHelperDescriptor[0]);
            });

            var phase = new DefaultRazorTagHelperBinderPhase()
            {
                Engine = projectEngine.Engine,
            };
            var expectedDiagnostics = new[]
            {
                RazorDiagnosticFactory.CreateParsing_UnterminatedStringLiteral(
                    new SourceSpan(new SourceLocation(14 + Environment.NewLine.Length, 1, 14), contentLength: 1)),
                RazorDiagnosticFactory.CreateParsing_InvalidTagHelperLookupText(
                    new SourceSpan(new SourceLocation(14 + Environment.NewLine.Length, 1, 14), contentLength: 1), "\"")
            };

            var content =
            @"
@addTagHelper """;
            var sourceDocument = TestRazorSourceDocument.Create(content, filePath: null);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var originalTree = RazorSyntaxTree.Parse(sourceDocument);
            codeDocument.SetSyntaxTree(originalTree);

            // Act
            phase.Execute(codeDocument);

            // Assert
            var rewrittenTree = codeDocument.GetSyntaxTree();
            var erroredNode = rewrittenTree.Root.DescendantNodes().First(n => n.GetSpanContext()?.ChunkGenerator is AddTagHelperChunkGenerator);
            var chunkGenerator = Assert.IsType<AddTagHelperChunkGenerator>(erroredNode.GetSpanContext().ChunkGenerator);
            Assert.Equal(expectedDiagnostics, chunkGenerator.Diagnostics);
        }

        [Fact]
        public void Execute_CanHandleSingleLengthRemoveTagHelperDirective()
        {
            // Arrange
            var projectEngine = RazorProjectEngine.Create(builder =>
            {
                builder.AddTagHelpers(new TagHelperDescriptor[0]);
            });

            var phase = new DefaultRazorTagHelperBinderPhase()
            {
                Engine = projectEngine.Engine,
            };
            var expectedDiagnostics = new[]
            {
                RazorDiagnosticFactory.CreateParsing_UnterminatedStringLiteral(
                    new SourceSpan(new SourceLocation(17 + Environment.NewLine.Length, 1, 17), contentLength: 1)),
                RazorDiagnosticFactory.CreateParsing_InvalidTagHelperLookupText(
                    new SourceSpan(new SourceLocation(17 + Environment.NewLine.Length, 1, 17), contentLength: 1), "\"")
            };

            var content =
            @"
@removeTagHelper """;
            var sourceDocument = TestRazorSourceDocument.Create(content, filePath: null);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var originalTree = RazorSyntaxTree.Parse(sourceDocument);
            codeDocument.SetSyntaxTree(originalTree);

            // Act
            phase.Execute(codeDocument);

            // Assert
            var rewrittenTree = codeDocument.GetSyntaxTree();
            var erroredNode = rewrittenTree.Root.DescendantNodes().First(n => n.GetSpanContext()?.ChunkGenerator is RemoveTagHelperChunkGenerator);
            var chunkGenerator = Assert.IsType<RemoveTagHelperChunkGenerator>(erroredNode.GetSpanContext().ChunkGenerator);
            Assert.Equal(expectedDiagnostics, chunkGenerator.Diagnostics);
        }

        [Fact]
        public void Execute_CanHandleSingleLengthTagHelperPrefix()
        {
            // Arrange
            var projectEngine = RazorProjectEngine.Create(builder =>
            {
                builder.AddTagHelpers(new TagHelperDescriptor[0]);
            });

            var phase = new DefaultRazorTagHelperBinderPhase()
            {
                Engine = projectEngine.Engine,
            };
            var expectedDiagnostics = new[]
            {
                RazorDiagnosticFactory.CreateParsing_UnterminatedStringLiteral(
                    new SourceSpan(new SourceLocation(17 + Environment.NewLine.Length, 1, 17), contentLength: 1)),
                RazorDiagnosticFactory.CreateParsing_InvalidTagHelperPrefixValue(
                    new SourceSpan(new SourceLocation(17 + Environment.NewLine.Length, 1, 17), contentLength: 1), "tagHelperPrefix", '\"', "\""),
            };

            var content =
            @"
@tagHelperPrefix """;
            var sourceDocument = TestRazorSourceDocument.Create(content, filePath: null);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var originalTree = RazorSyntaxTree.Parse(sourceDocument);
            codeDocument.SetSyntaxTree(originalTree);

            // Act
            phase.Execute(codeDocument);

            // Assert
            var rewrittenTree = codeDocument.GetSyntaxTree();
            var erroredNode = rewrittenTree.Root.DescendantNodes().First(n => n.GetSpanContext()?.ChunkGenerator is TagHelperPrefixDirectiveChunkGenerator);
            var chunkGenerator = Assert.IsType<TagHelperPrefixDirectiveChunkGenerator>(erroredNode.GetSpanContext().ChunkGenerator);
            Assert.Equal(expectedDiagnostics, chunkGenerator.Diagnostics);
        }

        [Fact]
        public void Execute_RewritesTagHelpers()
        {
            // Arrange
            var projectEngine = RazorProjectEngine.Create(builder =>
            {
                builder.AddTagHelpers(new[]
                {
                    CreateTagHelperDescriptor(
                        tagName: "form",
                        typeName: "TestFormTagHelper",
                        assemblyName: "TestAssembly"),
                    CreateTagHelperDescriptor(
                        tagName: "input",
                        typeName: "TestInputTagHelper",
                        assemblyName: "TestAssembly"),
                });
            });

            var phase = new DefaultRazorTagHelperBinderPhase()
            {
                Engine = projectEngine.Engine,
            };

            var sourceDocument = CreateTestSourceDocument();
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var originalTree = RazorSyntaxTree.Parse(sourceDocument);
            codeDocument.SetSyntaxTree(originalTree);

            // Act
            phase.Execute(codeDocument);

            // Assert
            var rewrittenTree = codeDocument.GetSyntaxTree();
            var descendantNodes = rewrittenTree.Root.DescendantNodes();
            Assert.Empty(rewrittenTree.Diagnostics);
            var tagHelperNodes = descendantNodes.Where(n => n is MarkupTagHelperElementSyntax tagHelper).Cast<MarkupTagHelperElementSyntax>().ToArray();
            Assert.Equal("form", tagHelperNodes[0].TagHelperInfo.TagName);
            Assert.Equal("input", tagHelperNodes[1].TagHelperInfo.TagName);
        }

        [Fact]
        public void Execute_WithTagHelperDescriptorsFromCodeDocument_RewritesTagHelpers()
        {
            // Arrange
            var projectEngine = RazorProjectEngine.Create();
            var tagHelpers = new[]
            {
                CreateTagHelperDescriptor(
                    tagName: "form",
                    typeName: "TestFormTagHelper",
                    assemblyName: "TestAssembly"),
                CreateTagHelperDescriptor(
                    tagName: "input",
                    typeName: "TestInputTagHelper",
                    assemblyName: "TestAssembly"),
            };

            var phase = new DefaultRazorTagHelperBinderPhase()
            {
                Engine = projectEngine.Engine,
            };

            var sourceDocument = CreateTestSourceDocument();
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var originalTree = RazorSyntaxTree.Parse(sourceDocument);
            codeDocument.SetSyntaxTree(originalTree);
            codeDocument.SetTagHelpers(tagHelpers);

            // Act
            phase.Execute(codeDocument);

            // Assert
            var rewrittenTree = codeDocument.GetSyntaxTree();
            var descendantNodes = rewrittenTree.Root.DescendantNodes();
            Assert.Empty(rewrittenTree.Diagnostics);
            var tagHelperNodes = descendantNodes.Where(n => n is MarkupTagHelperElementSyntax tagHelper).Cast<MarkupTagHelperElementSyntax>().ToArray();
            Assert.Equal("form", tagHelperNodes[0].TagHelperInfo.TagName);
            Assert.Equal("input", tagHelperNodes[1].TagHelperInfo.TagName);
        }

        [Fact]
        public void Execute_NullTagHelperDescriptorsFromCodeDocument_FallsBackToTagHelperFeature()
        {
            // Arrange
            var tagHelpers = new[]
            {
                CreateTagHelperDescriptor(
                    tagName: "form",
                    typeName: "TestFormTagHelper",
                    assemblyName: "TestAssembly"),
                CreateTagHelperDescriptor(
                    tagName: "input",
                    typeName: "TestInputTagHelper",
                    assemblyName: "TestAssembly"),
            };
            var projectEngine = RazorProjectEngine.Create(builder => builder.AddTagHelpers(tagHelpers));

            var phase = new DefaultRazorTagHelperBinderPhase()
            {
                Engine = projectEngine.Engine,
            };

            var sourceDocument = CreateTestSourceDocument();
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var originalTree = RazorSyntaxTree.Parse(sourceDocument);
            codeDocument.SetSyntaxTree(originalTree);
            codeDocument.SetTagHelpers(tagHelpers: null);

            // Act
            phase.Execute(codeDocument);

            // Assert
            var rewrittenTree = codeDocument.GetSyntaxTree();
            var descendantNodes = rewrittenTree.Root.DescendantNodes();
            Assert.Empty(rewrittenTree.Diagnostics);
            var tagHelperNodes = descendantNodes.Where(n => n is MarkupTagHelperElementSyntax tagHelper).Cast<MarkupTagHelperElementSyntax>().ToArray();
            Assert.Equal("form", tagHelperNodes[0].TagHelperInfo.TagName);
            Assert.Equal("input", tagHelperNodes[1].TagHelperInfo.TagName);
        }

        [Fact]
        public void Execute_EmptyTagHelperDescriptorsFromCodeDocument_DoesNotFallbackToTagHelperFeature()
        {
            // Arrange
            var tagHelpers = new[]
            {
                CreateTagHelperDescriptor(
                    tagName: "form",
                    typeName: "TestFormTagHelper",
                    assemblyName: "TestAssembly"),
                CreateTagHelperDescriptor(
                    tagName: "input",
                    typeName: "TestInputTagHelper",
                    assemblyName: "TestAssembly"),
            };
            var projectEngine = RazorProjectEngine.Create(builder => builder.AddTagHelpers(tagHelpers));

            var phase = new DefaultRazorTagHelperBinderPhase()
            {
                Engine = projectEngine.Engine,
            };

            var sourceDocument = CreateTestSourceDocument();
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var originalTree = RazorSyntaxTree.Parse(sourceDocument);
            codeDocument.SetSyntaxTree(originalTree);
            codeDocument.SetTagHelpers(tagHelpers: Array.Empty<TagHelperDescriptor>());

            // Act
            phase.Execute(codeDocument);

            // Assert
            var rewrittenTree = codeDocument.GetSyntaxTree();
            var descendantNodes = rewrittenTree.Root.DescendantNodes();
            Assert.Empty(rewrittenTree.Diagnostics);
            var tagHelperNodes = descendantNodes.Where(n => n is MarkupTagHelperElementSyntax tagHelper).Cast<MarkupTagHelperElementSyntax>().ToArray();
            Assert.Empty(tagHelperNodes);
        }

        [Fact]
        public void Execute_DirectiveWithoutQuotes_RewritesTagHelpers_TagHelperMatchesElementTwice()
        {
            // Arrange
            var descriptor = CreateTagHelperDescriptor(
                tagName: "form",
                typeName: "TestFormTagHelper",
                assemblyName: "TestAssembly",
                ruleBuilders: new Action<TagMatchingRuleDescriptorBuilder>[]
                {
                    ruleBuilder => ruleBuilder
                        .RequireAttributeDescriptor(attribute => attribute
                            .Name("a")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch)),
                    ruleBuilder => ruleBuilder
                        .RequireAttributeDescriptor(attribute => attribute
                            .Name("b")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch)),
                });

            var projectEngine = RazorProjectEngine.Create(builder =>
            {
                builder.AddTagHelpers(new[] { descriptor });
            });

            var phase = new DefaultRazorTagHelperBinderPhase()
            {
                Engine = projectEngine.Engine,
            };

            var content = @"
@addTagHelper *, TestAssembly
<form a=""hi"" b=""there"">
</form>";

            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var originalTree = RazorSyntaxTree.Parse(sourceDocument);
            codeDocument.SetSyntaxTree(originalTree);

            // Act
            phase.Execute(codeDocument);

            // Assert
            var rewrittenTree = codeDocument.GetSyntaxTree();
            var descendantNodes = rewrittenTree.Root.DescendantNodes();
            Assert.Empty(rewrittenTree.Diagnostics);
            var tagHelperNodes = descendantNodes.Where(n => n is MarkupTagHelperElementSyntax tagHelper).Cast<MarkupTagHelperElementSyntax>().ToArray();

            var formTagHelper = Assert.Single(tagHelperNodes);
            Assert.Equal("form", formTagHelper.TagHelperInfo.TagName);
            Assert.Equal(2, formTagHelper.TagHelperInfo.BindingResult.GetBoundRules(descriptor).Count());
        }

        [Fact]
        public void Execute_DirectiveWithQuotes_RewritesTagHelpers_TagHelperMatchesElementTwice()
        {
            // Arrange
            var descriptor = CreateTagHelperDescriptor(
                tagName: "form",
                typeName: "TestFormTagHelper",
                assemblyName: "TestAssembly",
                ruleBuilders: new Action<TagMatchingRuleDescriptorBuilder>[]
                {
                    ruleBuilder => ruleBuilder
                        .RequireAttributeDescriptor(attribute => attribute
                            .Name("a")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch)),
                    ruleBuilder => ruleBuilder
                        .RequireAttributeDescriptor(attribute => attribute
                            .Name("b")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch)),
                });

            var projectEngine = RazorProjectEngine.Create(builder =>
            {
                builder.AddTagHelpers(new[] { descriptor });
            });

            var phase = new DefaultRazorTagHelperBinderPhase()
            {
                Engine = projectEngine.Engine,
            };

            var content = @"
@addTagHelper ""*, TestAssembly""
<form a=""hi"" b=""there"">
</form>";

            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var originalTree = RazorSyntaxTree.Parse(sourceDocument);
            codeDocument.SetSyntaxTree(originalTree);

            // Act
            phase.Execute(codeDocument);

            // Assert
            var rewrittenTree = codeDocument.GetSyntaxTree();
            var descendantNodes = rewrittenTree.Root.DescendantNodes();
            Assert.Empty(rewrittenTree.Diagnostics);
            var tagHelperNodes = descendantNodes.Where(n => n is MarkupTagHelperElementSyntax tagHelper).Cast<MarkupTagHelperElementSyntax>().ToArray();

            var formTagHelper = Assert.Single(tagHelperNodes);
            Assert.Equal("form", formTagHelper.TagHelperInfo.TagName);
            Assert.Equal(2, formTagHelper.TagHelperInfo.BindingResult.GetBoundRules(descriptor).Count());
        }

        [Fact]
        public void Execute_TagHelpersFromCodeDocumentAndFeature_PrefersCodeDocument()
        {
            // Arrange
            var featureTagHelpers = new[]
            {
                CreateTagHelperDescriptor(
                    tagName: "input",
                    typeName: "TestInputTagHelper",
                    assemblyName: "TestAssembly"),
            };
            var projectEngine = RazorProjectEngine.Create(builder => builder.AddTagHelpers(featureTagHelpers));

            var phase = new DefaultRazorTagHelperBinderPhase()
            {
                Engine = projectEngine.Engine,
            };

            var sourceDocument = CreateTestSourceDocument();
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var originalTree = RazorSyntaxTree.Parse(sourceDocument);
            codeDocument.SetSyntaxTree(originalTree);

            var codeDocumentTagHelpers = new[]
            {
                CreateTagHelperDescriptor(
                    tagName: "form",
                    typeName: "TestFormTagHelper",
                    assemblyName: "TestAssembly"),
            };
            codeDocument.SetTagHelpers(codeDocumentTagHelpers);

            // Act
            phase.Execute(codeDocument);

            // Assert
            var rewrittenTree = codeDocument.GetSyntaxTree();
            var descendantNodes = rewrittenTree.Root.DescendantNodes();
            Assert.Empty(rewrittenTree.Diagnostics);
            var tagHelperNodes = descendantNodes.Where(n => n is MarkupTagHelperElementSyntax tagHelper).Cast<MarkupTagHelperElementSyntax>().ToArray();

            var formTagHelper = Assert.Single(tagHelperNodes);
            Assert.Equal("form", formTagHelper.TagHelperInfo.TagName);
        }

        [Fact]
        public void Execute_NoopsWhenNoTagHelpersFromCodeDocumentOrFeature()
        {
            // Arrange
            var projectEngine = RazorProjectEngine.Create();
            var phase = new DefaultRazorTagHelperBinderPhase()
            {
                Engine = projectEngine.Engine,
            };
            var sourceDocument = CreateTestSourceDocument();
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var originalTree = RazorSyntaxTree.Parse(sourceDocument);
            codeDocument.SetSyntaxTree(originalTree);

            // Act
            phase.Execute(codeDocument);

            // Assert
            var outputTree = codeDocument.GetSyntaxTree();
            Assert.Empty(outputTree.Diagnostics);
            Assert.Same(originalTree, outputTree);
        }

        [Fact]
        public void Execute_NoopsWhenNoTagHelperDescriptorsAreResolved()
        {
            // Arrange
            var projectEngine = RazorProjectEngine.Create(builder =>
            {
                builder.Features.Add(new TestTagHelperFeature());
            });

            var phase = new DefaultRazorTagHelperBinderPhase()
            {
                Engine = projectEngine.Engine,
            };

            // No taghelper directives here so nothing is resolved.
            var sourceDocument = TestRazorSourceDocument.Create("Hello, world");
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var originalTree = RazorSyntaxTree.Parse(sourceDocument);
            codeDocument.SetSyntaxTree(originalTree);

            // Act
            phase.Execute(codeDocument);

            // Assert
            var outputTree = codeDocument.GetSyntaxTree();
            Assert.Empty(outputTree.Diagnostics);
            Assert.Same(originalTree, outputTree);
        }

        [Fact]
        public void Execute_SetsTagHelperDocumentContext()
        {
            // Arrange
            var projectEngine = RazorProjectEngine.Create(builder =>
            {
                builder.Features.Add(new TestTagHelperFeature());
            });

            var phase = new DefaultRazorTagHelperBinderPhase()
            {
                Engine = projectEngine.Engine,
            };

            // No taghelper directives here so nothing is resolved.
            var sourceDocument = TestRazorSourceDocument.Create("Hello, world");
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var originalTree = RazorSyntaxTree.Parse(sourceDocument);
            codeDocument.SetSyntaxTree(originalTree);

            // Act
            phase.Execute(codeDocument);

            // Assert
            var context = codeDocument.GetTagHelperContext();
            Assert.Null(context.Prefix);
            Assert.Empty(context.TagHelpers);
        }

        [Fact]
        public void Execute_CombinesErrorsOnRewritingErrors()
        {
            // Arrange
            var projectEngine = RazorProjectEngine.Create(builder =>
            {
                builder.AddTagHelpers(new[]
                {
                    CreateTagHelperDescriptor(
                        tagName: "form",
                        typeName: "TestFormTagHelper",
                        assemblyName: "TestAssembly"),
                    CreateTagHelperDescriptor(
                        tagName: "input",
                        typeName: "TestInputTagHelper",
                        assemblyName: "TestAssembly"),
                });
            });

            var phase = new DefaultRazorTagHelperBinderPhase()
            {
                Engine = projectEngine.Engine,
            };

            var content =
            @"
@addTagHelper *, TestAssembly
<form>
    <input value='Hello' type='text' />";
            var sourceDocument = TestRazorSourceDocument.Create(content, filePath: null);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);

            var originalTree = RazorSyntaxTree.Parse(sourceDocument);

            var initialError = RazorDiagnostic.Create(
                new RazorDiagnosticDescriptor("RZ9999", () => "Initial test error", RazorDiagnosticSeverity.Error),
                new SourceSpan(SourceLocation.Zero, contentLength: 1));
            var expectedRewritingError = RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                new SourceSpan(new SourceLocation(Environment.NewLine.Length * 2 + 30, 2, 1), contentLength: 4), "form");

            var erroredOriginalTree = RazorSyntaxTree.Create(originalTree.Root, originalTree.Source, new[] { initialError }, originalTree.Options);
            codeDocument.SetSyntaxTree(erroredOriginalTree);

            // Act
            phase.Execute(codeDocument);

            // Assert
            var outputTree = codeDocument.GetSyntaxTree();
            Assert.Empty(originalTree.Diagnostics);
            Assert.NotSame(erroredOriginalTree, outputTree);
            Assert.Equal(new[] { initialError, expectedRewritingError }, outputTree.Diagnostics);
        }

        private static string AssemblyA => "TestAssembly";

        private static string AssemblyB => "AnotherAssembly";

        private static TagHelperDescriptor Valid_PlainTagHelperDescriptor
        {
            get
            {
                return CreateTagHelperDescriptor(
                    tagName: "valid_plain",
                    typeName: "Microsoft.AspNetCore.Razor.TagHelpers.ValidPlainTagHelper",
                    assemblyName: AssemblyA);
            }
        }

        private static TagHelperDescriptor Valid_InheritedTagHelperDescriptor
        {
            get
            {
                return CreateTagHelperDescriptor(
                    tagName: "valid_inherited",
                    typeName: "Microsoft.AspNetCore.Razor.TagHelpers.ValidInheritedTagHelper",
                    assemblyName: AssemblyA);
            }
        }

        private static TagHelperDescriptor String_TagHelperDescriptor
        {
            get
            {
                // We're treating 'string' as a TagHelper so we can test TagHelpers in multiple assemblies without
                // building a separate assembly with a single TagHelper.
                return CreateTagHelperDescriptor(
                    tagName: "string",
                    typeName: "System.String",
                    assemblyName: AssemblyB);
            }
        }

        public static TheoryData ProcessTagHelperPrefixData
        {
            get
            {
                // source, expected prefix
                return new TheoryData<string, string>
                {
                    {
                        $@"
@tagHelperPrefix """"
@addTagHelper Microsoft.AspNetCore.Razor.TagHelpers.ValidPlain*, TestAssembly",
                        null
                    },
                    {
                        $@"
@tagHelperPrefix th:
@addTagHelper Microsoft.AspNetCore.Razor.TagHelpers.ValidPlain*, {AssemblyA}",
                        "th:"
                    },
                    {
                        $@"
@addTagHelper *, {AssemblyA}
@tagHelperPrefix th:",
                        "th:"
                    },
                    {
                        $@"
@tagHelperPrefix th-
@addTagHelper Microsoft.AspNetCore.Razor.TagHelpers.ValidPlain*, {AssemblyA}
@addTagHelper Microsoft.AspNetCore.Razor.TagHelpers.ValidInherited*, {AssemblyA}",
                        "th-"
                    },
                    {
                        $@"
@tagHelperPrefix 
@addTagHelper Microsoft.AspNetCore.Razor.TagHelpers.ValidPlain*, {AssemblyA}
@addTagHelper Microsoft.AspNetCore.Razor.TagHelpers.ValidInherited*, {AssemblyA}",
                        null
                    },
                    {
                        $@"
@tagHelperPrefix ""th""
@addTagHelper *, {AssemblyA}
@addTagHelper *, {AssemblyB}",
                        "th"
                    },
                    {
                        $@"
@addTagHelper *, {AssemblyA}
@tagHelperPrefix th:-
@addTagHelper *, {AssemblyB}",
                        "th:-"
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(ProcessTagHelperPrefixData))]
        public void DirectiveVisitor_ExtractsPrefixFromSyntaxTree(
            string source,
            string expectedPrefix)
        {
            // Arrange
            var sourceDocument = TestRazorSourceDocument.Create(source, filePath: "TestFile");
            var parser = new RazorParser();
            var syntaxTree = parser.Parse(sourceDocument);
            var visitor = new DefaultRazorTagHelperBinderPhase.DirectiveVisitor(tagHelpers: new List<TagHelperDescriptor>());

            // Act
            visitor.Visit(syntaxTree.Root);

            // Assert
            Assert.Equal(expectedPrefix, visitor.TagHelperPrefix);
        }

        public static TheoryData ProcessTagHelperMatchesData
        {
            get
            {
                // source, taghelpers, expected descriptors
                return new TheoryData<string, TagHelperDescriptor[], TagHelperDescriptor[]>
                {
                    {
                        $@"
@addTagHelper *, {AssemblyA}",
                        new [] { Valid_PlainTagHelperDescriptor, },
                        new [] { Valid_PlainTagHelperDescriptor }
                    },
                    {
                        $@"
@addTagHelper *, {AssemblyA}
@addTagHelper *, {AssemblyB}",
                        new [] { Valid_PlainTagHelperDescriptor, String_TagHelperDescriptor },
                        new [] { Valid_PlainTagHelperDescriptor, String_TagHelperDescriptor }
                    },
                    {
                        $@"
@addTagHelper *, {AssemblyA}
@removeTagHelper *, {AssemblyB}",
                        new [] { Valid_PlainTagHelperDescriptor, String_TagHelperDescriptor },
                        new [] { Valid_PlainTagHelperDescriptor }
                    },
                    {
                        $@"
@addTagHelper *, {AssemblyA}
@addTagHelper *, {AssemblyB}
@removeTagHelper *, {AssemblyA}",
                        new [] { Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor, String_TagHelperDescriptor },
                        new [] { String_TagHelperDescriptor }
                    },
                    {
                        $@"
@addTagHelper {Valid_PlainTagHelperDescriptor.Name}, {AssemblyA}
@addTagHelper *, {AssemblyA}",
                        new [] { Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor, },
                        new [] { Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor }
                    },
                    {
                        $@"
@addTagHelper *, {AssemblyA}
@removeTagHelper {Valid_PlainTagHelperDescriptor.Name}, {AssemblyA}",
                        new [] { Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor, },
                        new [] { Valid_InheritedTagHelperDescriptor }
                    },
                    {
                        $@"
@addTagHelper *, {AssemblyA}
@removeTagHelper *, {AssemblyA}
@addTagHelper *, {AssemblyA}",
                        new [] { Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor, },
                        new [] { Valid_InheritedTagHelperDescriptor, Valid_PlainTagHelperDescriptor }
                    },
                    {
                        $@"
@addTagHelper *, {AssemblyA}
@addTagHelper *, {AssemblyA}",
                        new [] { Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor, },
                        new [] { Valid_InheritedTagHelperDescriptor, Valid_PlainTagHelperDescriptor }
                    },
                    {
                        $@"
@addTagHelper Microsoft.AspNetCore.Razor.TagHelpers.ValidPlain*, {AssemblyA}",
                        new [] { Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor, },
                        new [] { Valid_PlainTagHelperDescriptor }
                    },
                    {
                        $@"
@addTagHelper Microsoft.AspNetCore.Razor.TagHelpers.*, {AssemblyA}",
                        new [] { Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor, },
                        new [] { Valid_PlainTagHelperDescriptor, Valid_PlainTagHelperDescriptor }
                    },
                    {
                        $@"
@addTagHelper *, {AssemblyA}
@removeTagHelper Microsoft.AspNetCore.Razor.TagHelpers.ValidP*, {AssemblyA}
@addTagHelper *, {AssemblyA}",
                        new [] { Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor, },
                        new [] { Valid_InheritedTagHelperDescriptor, Valid_PlainTagHelperDescriptor }
                    },
                    {
                        $@"
@addTagHelper *, {AssemblyA}
@removeTagHelper Str*, {AssemblyB}",
                        new [] { Valid_PlainTagHelperDescriptor, String_TagHelperDescriptor, },
                        new [] { Valid_PlainTagHelperDescriptor }
                    },
                    {
                        $@"
@addTagHelper *, {AssemblyA}
@removeTagHelper *, {AssemblyB}",
                        new [] { Valid_PlainTagHelperDescriptor, String_TagHelperDescriptor, },
                        new [] { Valid_PlainTagHelperDescriptor }
                    },
                    {
                        $@"
@addTagHelper *, {AssemblyA}
@addTagHelper System.{String_TagHelperDescriptor.Name}, {AssemblyB}",
                        new [] { Valid_PlainTagHelperDescriptor, String_TagHelperDescriptor, },
                        new [] { Valid_PlainTagHelperDescriptor }
                    },
                    {
                        $@"
@addTagHelper *, {AssemblyA}
@addTagHelper *, {AssemblyB}
@removeTagHelper Microsoft.*, {AssemblyA}",
                        new [] { Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor, String_TagHelperDescriptor },
                        new [] { String_TagHelperDescriptor }
                    },
                    {
                        $@"
@addTagHelper *, {AssemblyA}
@addTagHelper *, {AssemblyB}
@removeTagHelper ?Microsoft*, {AssemblyA}
@removeTagHelper System.{String_TagHelperDescriptor.Name}, {AssemblyB}",
                        new [] { Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor, String_TagHelperDescriptor },
                        new []
                        {
                            Valid_InheritedTagHelperDescriptor,
                            Valid_PlainTagHelperDescriptor,
                            String_TagHelperDescriptor
                        }
                    },
                    {
                        $@"
@addTagHelper *, {AssemblyA}
@addTagHelper *, {AssemblyB}
@removeTagHelper TagHelper*, {AssemblyA}
@removeTagHelper System.{String_TagHelperDescriptor.Name}, {AssemblyB}",
                        new [] { Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor, String_TagHelperDescriptor },
                        new []
                        {
                            Valid_InheritedTagHelperDescriptor,
                            Valid_PlainTagHelperDescriptor,
                            String_TagHelperDescriptor
                        }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(ProcessTagHelperMatchesData))]
        public void DirectiveVisitor_FiltersTagHelpersByDirectives(
            string source,
            object tagHelpers,
            object expectedDescriptors)
        {
            // Arrange
            var expected = (TagHelperDescriptor[])expectedDescriptors;
            var sourceDocument = TestRazorSourceDocument.Create(source, filePath: "TestFile");
            var parser = new RazorParser();
            var syntaxTree = parser.Parse(sourceDocument);
            var visitor = new DefaultRazorTagHelperBinderPhase.DirectiveVisitor((TagHelperDescriptor[])tagHelpers);

            // Act
            visitor.Visit(syntaxTree.Root);

            // Assert
            Assert.Equal(expected.Count(), visitor.Matches.Count());

            foreach (var expectedDescriptor in expected)
            {
                Assert.Contains(expectedDescriptor, visitor.Matches, TagHelperDescriptorComparer.Default);
            }
        }

        public static TheoryData ProcessTagHelperMatches_EmptyResultData
        {
            get
            {
                // source, taghelpers
                return new TheoryData<string, IEnumerable<TagHelperDescriptor>>
                {
                    {
                        $@"
@addTagHelper *, {AssemblyA}
@removeTagHelper *, {AssemblyA}",
                        new TagHelperDescriptor[]
                        {
                            Valid_PlainTagHelperDescriptor,
                        }
                    },
                    {
                        $@"
@addTagHelper *, {AssemblyA}
@removeTagHelper {Valid_PlainTagHelperDescriptor.Name}, {AssemblyA}
@removeTagHelper {Valid_InheritedTagHelperDescriptor.Name}, {AssemblyA}",
                        new TagHelperDescriptor[]
                        {
                            Valid_PlainTagHelperDescriptor,
                            Valid_InheritedTagHelperDescriptor,
                        }
                    },
                    {
                        $@"
@addTagHelper *, {AssemblyA}
@addTagHelper *, {AssemblyB}
@removeTagHelper *, {AssemblyA}
@removeTagHelper *, {AssemblyB}",
                        new TagHelperDescriptor[]
                        {
                            Valid_PlainTagHelperDescriptor,
                            Valid_InheritedTagHelperDescriptor,
                            String_TagHelperDescriptor,
                        }
                    },
                    {
                        $@"
@addTagHelper *, {AssemblyA}
@addTagHelper *, {AssemblyB}
@removeTagHelper {Valid_PlainTagHelperDescriptor.Name}, {AssemblyA}
@removeTagHelper {Valid_InheritedTagHelperDescriptor.Name}, {AssemblyA}
@removeTagHelper {String_TagHelperDescriptor.Name}, {AssemblyB}",
                        new TagHelperDescriptor[]
                        {
                            Valid_PlainTagHelperDescriptor,
                            Valid_InheritedTagHelperDescriptor,
                            String_TagHelperDescriptor,
                        }
                    },
                    {
                        $@"
@removeTagHelper *, {AssemblyA}
@removeTagHelper {Valid_PlainTagHelperDescriptor.Name}, {AssemblyA}",
                        new TagHelperDescriptor[0]
                    },
                    {
                        $@"
@addTagHelper *, {AssemblyA}
@removeTagHelper Mic*, {AssemblyA}",
                        new TagHelperDescriptor[]
                        {
                            Valid_PlainTagHelperDescriptor,
                        }
                    },
                    {
                        $@"
@addTagHelper Mic*, {AssemblyA}
@removeTagHelper {Valid_PlainTagHelperDescriptor.Name}, {AssemblyA}
@removeTagHelper {Valid_InheritedTagHelperDescriptor.Name}, {AssemblyA}",
                        new TagHelperDescriptor[]
                        {
                            Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor
                        }
                    },
                    {
                        $@"
@addTagHelper Microsoft.*, {AssemblyA}
@addTagHelper System.*, {AssemblyB}
@removeTagHelper Microsoft.AspNetCore.Razor.TagHelpers*, {AssemblyA}
@removeTagHelper System.*, {AssemblyB}",
                        new TagHelperDescriptor[]
                        {
                            Valid_PlainTagHelperDescriptor,
                            Valid_InheritedTagHelperDescriptor,
                            String_TagHelperDescriptor,
                        }
                    },
                    {
                        $@"
@addTagHelper ?icrosoft.*, {AssemblyA}
@addTagHelper ?ystem.*, {AssemblyB}
@removeTagHelper *?????r, {AssemblyA}
@removeTagHelper Sy??em.*, {AssemblyB}",
                        new TagHelperDescriptor[]
                        {
                            Valid_PlainTagHelperDescriptor,
                            Valid_InheritedTagHelperDescriptor,
                            String_TagHelperDescriptor,
                        }
                    },
                    {
                        $@"
@addTagHelper ?i?crosoft.*, {AssemblyA}
@addTagHelper ??ystem.*, {AssemblyB}",
                        new TagHelperDescriptor[]
                        {
                            Valid_PlainTagHelperDescriptor,
                            Valid_InheritedTagHelperDescriptor,
                            String_TagHelperDescriptor,
                        }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(ProcessTagHelperMatches_EmptyResultData))]
        public void ProcessDirectives_CanReturnEmptyDescriptorsBasedOnDirectiveDescriptors(
            string source,
            object tagHelpers)
        {
            // Arrange
            var sourceDocument = TestRazorSourceDocument.Create(source, filePath: "TestFile");
            var parser = new RazorParser();
            var syntaxTree = parser.Parse(sourceDocument);
            var visitor = new DefaultRazorTagHelperBinderPhase.DirectiveVisitor((TagHelperDescriptor[])tagHelpers);

            // Act
            visitor.Visit(syntaxTree.Root);

            // Assert
            Assert.Empty(visitor.Matches);
        }

        private static TagHelperDescriptor CreatePrefixedValidPlainDescriptor(string prefix)
        {
            return Valid_PlainTagHelperDescriptor;
        }

        private static TagHelperDescriptor CreatePrefixedValidInheritedDescriptor(string prefix)
        {
            return Valid_InheritedTagHelperDescriptor;
        }

        private static TagHelperDescriptor CreatePrefixedStringDescriptor(string prefix)
        {
            return String_TagHelperDescriptor;
        }

        private static RazorSourceDocument CreateTestSourceDocument()
        {
            var content =
            @"
@addTagHelper *, TestAssembly
<form>
    <input value='Hello' type='text' />
</form>";
            var sourceDocument = TestRazorSourceDocument.Create(content, filePath: null);
            return sourceDocument;
        }

        private static TagHelperDescriptor CreateTagHelperDescriptor(
            string tagName,
            string typeName,
            string assemblyName,
            IEnumerable<Action<BoundAttributeDescriptorBuilder>> attributes = null,
            IEnumerable<Action<TagMatchingRuleDescriptorBuilder>> ruleBuilders = null)
        {
            var builder = TagHelperDescriptorBuilder.Create(typeName, assemblyName);
            builder.TypeName(typeName);

            if (attributes != null)
            {
                foreach (var attributeBuilder in attributes)
                {
                    builder.BoundAttributeDescriptor(attributeBuilder);
                }
            }

            if (ruleBuilders != null)
            {
                foreach (var ruleBuilder in ruleBuilders)
                {
                    builder.TagMatchingRuleDescriptor(innerRuleBuilder =>
                    {
                        innerRuleBuilder.RequireTagName(tagName);
                        ruleBuilder(innerRuleBuilder);
                    });
                }
            }
            else
            {
                builder.TagMatchingRuleDescriptor(ruleBuilder => ruleBuilder.RequireTagName(tagName));
            }

            var descriptor = builder.Build();

            return descriptor;
        }
    }
}
