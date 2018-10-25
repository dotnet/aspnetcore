// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using static Microsoft.CodeAnalysis.Razor.RazorDocumentExcerptService;

namespace Microsoft.CodeAnalysis.Razor
{
    public class RazorExcerptServiceTest : WorkspaceTestBase
    {
        public RazorExcerptServiceTest()
        {
            HostProject = TestProjectData.SomeProject;
            HostDocument = TestProjectData.SomeProjectFile1;
        }

        private HostProject HostProject { get; }
        private HostDocument HostDocument { get; }

        protected override void ConfigureLanguageServices(List<ILanguageService> services)
        {
            services.Add(new TestTagHelperResolver());
        }

        [Fact]
        public async Task TryGetExcerptInternalAsync_SingleLine_CanClassifyCSharp()
        {
            // Arrange
            var (sourceText, primarySpan) = CreateText(
@"
<html>
@{
    var |foo| = ""Hello, World!"";
}
  <body>@foo</body>
  <div>@(3 + 4)</div><div>@(foo + foo)</div>
</html>
");

            var (primary, secondary) = Initialize(sourceText);
            var service = CreateExcerptService(primary);

            var secondarySpan = await GetSecondarySpanAsync(primary, primarySpan, secondary);
            
            // Act
            var result = await service.TryGetExcerptInternalAsync(secondary, secondarySpan, ExcerptModeInternal.SingleLine, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(secondarySpan, result.Value.Span);
            Assert.Same(secondary, result.Value.Document);

            // Verifies that the right part of the primary document will be highlighted.
            Assert.Equal(
                (await secondary.GetTextAsync()).GetSubText(secondarySpan).ToString(),
                result.Value.Content.GetSubText(result.Value.MappedSpan).ToString(),
                ignoreLineEndingDifferences: true);

            Assert.Equal(@"    var foo = ""Hello, World!"";", result.Value.Content.ToString(), ignoreLineEndingDifferences: true);
            Assert.Collection(
                result.Value.ClassifiedSpans,
                c => 
                {
                    Assert.Equal(ClassificationTypeNames.Keyword, c.ClassificationType);
                    Assert.Equal("var", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.LocalName, c.ClassificationType);
                    Assert.Equal("foo", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Operator, c.ClassificationType);
                    Assert.Equal("=", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.StringLiteral, c.ClassificationType);
                    Assert.Equal("\"Hello, World!\"", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Punctuation, c.ClassificationType);
                    Assert.Equal(";", result.Value.Content.GetSubText(c.TextSpan).ToString());
                });
        }

        [Fact]
        public async Task TryGetExcerptInternalAsync_SingleLine_CanClassifyCSharp_ImplicitExpression()
        {
            // Arrange
            var (sourceText, primarySpan) = CreateText(
@"
<html>
@{
    var foo = ""Hello, World!"";
}
  <body>@|foo|</body>
  <div>@(3 + 4)</div><div>@(foo + foo)</div>
</html>
");

            var (primary, secondary) = Initialize(sourceText);
            var service = CreateExcerptService(primary);

            var secondarySpan = await GetSecondarySpanAsync(primary, primarySpan, secondary);

            // Act
            var result = await service.TryGetExcerptInternalAsync(secondary, secondarySpan, ExcerptModeInternal.SingleLine, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(secondarySpan, result.Value.Span);
            Assert.Same(secondary, result.Value.Document);

            // Verifies that the right part of the primary document will be highlighted.
            Assert.Equal(
                (await secondary.GetTextAsync()).GetSubText(secondarySpan).ToString(),
                result.Value.Content.GetSubText(result.Value.MappedSpan).ToString(),
                ignoreLineEndingDifferences: true);

            Assert.Equal(@"  <body>@foo</body>", result.Value.Content.ToString(), ignoreLineEndingDifferences: true);
            Assert.Collection(
                result.Value.ClassifiedSpans,
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal("  <body>@", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.LocalName, c.ClassificationType);
                    Assert.Equal("foo", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal("</body>", result.Value.Content.GetSubText(c.TextSpan).ToString());
                });
        }

        [Fact]
        public async Task TryGetExcerptInternalAsync_SingleLine_CanClassifyCSharp_ComplexLine()
        {
            // Arrange
            var (sourceText, primarySpan) = CreateText(
@"
<html>
@{
    var foo = ""Hello, World!"";
}
  <body>@foo</body>
  <div>@(3 + 4)</div><div>@(foo + |foo|)</div>
</html>
");

            var (primary, secondary) = Initialize(sourceText);
            var service = CreateExcerptService(primary);

            var secondarySpan = await GetSecondarySpanAsync(primary, primarySpan, secondary);

            // Act
            var result = await service.TryGetExcerptInternalAsync(secondary, secondarySpan, ExcerptModeInternal.SingleLine, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(secondarySpan, result.Value.Span);
            Assert.Same(secondary, result.Value.Document);

            // Verifies that the right part of the primary document will be highlighted.
            Assert.Equal(
                (await secondary.GetTextAsync()).GetSubText(secondarySpan).ToString(),
                result.Value.Content.GetSubText(result.Value.MappedSpan).ToString(),
                ignoreLineEndingDifferences: true);

            Assert.Equal(@"  <div>@(3 + 4)</div><div>@(foo + foo)</div>", result.Value.Content.ToString(), ignoreLineEndingDifferences: true);
            Assert.Collection(
                result.Value.ClassifiedSpans,
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal("  <div>@(", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.NumericLiteral, c.ClassificationType);
                    Assert.Equal("3", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Operator, c.ClassificationType);
                    Assert.Equal("+", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.NumericLiteral, c.ClassificationType);
                    Assert.Equal("4", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal(")</div><div>@(", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.LocalName, c.ClassificationType);
                    Assert.Equal("foo", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Operator, c.ClassificationType);
                    Assert.Equal("+", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.LocalName, c.ClassificationType);
                    Assert.Equal("foo", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal(")</div>", result.Value.Content.GetSubText(c.TextSpan).ToString());
                });
        }

        [Fact]
        public async Task TryGetExcerptInternalAsync_MultiLine_CanClassifyCSharp()
        {
            // Arrange
            var (sourceText, primarySpan) = CreateText(
@"
<html>
@{
    var |foo| = ""Hello, World!"";
}
  <body></body>
  <div></div>
</html>
");

            var (primary, secondary) = Initialize(sourceText);
            var service = CreateExcerptService(primary);

            var secondarySpan = await GetSecondarySpanAsync(primary, primarySpan, secondary);

            // Act
            var result = await service.TryGetExcerptInternalAsync(secondary, secondarySpan, ExcerptModeInternal.Tooltip, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(secondarySpan, result.Value.Span);
            Assert.Same(secondary, result.Value.Document);

            // Verifies that the right part of the primary document will be highlighted.
            Assert.Equal(
                (await secondary.GetTextAsync()).GetSubText(secondarySpan).ToString(),
                result.Value.Content.GetSubText(result.Value.MappedSpan).ToString(),
                ignoreLineEndingDifferences: true);

            Assert.Equal(
@"
<html>
@{
    var foo = ""Hello, World!"";
}
  <body></body>
  <div></div>", 
                result.Value.Content.ToString(), ignoreLineEndingDifferences: true);

            Assert.Collection(
                result.Value.ClassifiedSpans,
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal(
@"
<html>
@{", 
                            result.Value.Content.GetSubText(c.TextSpan).ToString(),
                            ignoreLineEndingDifferences: true);
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Keyword, c.ClassificationType);
                    Assert.Equal("var", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.LocalName, c.ClassificationType);
                    Assert.Equal("foo", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Operator, c.ClassificationType);
                    Assert.Equal("=", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.StringLiteral, c.ClassificationType);
                    Assert.Equal("\"Hello, World!\"", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Punctuation, c.ClassificationType);
                    Assert.Equal(";", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal(
@"}
  <body></body>
  <div></div>", 
                        result.Value.Content.GetSubText(c.TextSpan).ToString(), 
                        ignoreLineEndingDifferences: true);
                });
        }

        [Fact]
        public async Task TryGetExcerptInternalAsync_MultiLine_Boundaries_CanClassifyCSharp()
        {
            // Arrange
            var (sourceText, primarySpan) = CreateText(@"@{ var |foo| = ""Hello, World!""; }");

            var (primary, secondary) = Initialize(sourceText);
            var service = CreateExcerptService(primary);

            var secondarySpan = await GetSecondarySpanAsync(primary, primarySpan, secondary);

            // Act
            var result = await service.TryGetExcerptInternalAsync(secondary, secondarySpan, ExcerptModeInternal.Tooltip, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(secondarySpan, result.Value.Span);
            Assert.Same(secondary, result.Value.Document);

            // Verifies that the right part of the primary document will be highlighted.
            Assert.Equal(
                (await secondary.GetTextAsync()).GetSubText(secondarySpan).ToString(),
                result.Value.Content.GetSubText(result.Value.MappedSpan).ToString(),
                ignoreLineEndingDifferences: true);

            Assert.Equal(
@"@{ var foo = ""Hello, World!""; }",
                result.Value.Content.ToString(), ignoreLineEndingDifferences: true);

            Assert.Collection(
                result.Value.ClassifiedSpans,
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal("@{", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Keyword, c.ClassificationType);
                    Assert.Equal("var", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.LocalName, c.ClassificationType);
                    Assert.Equal("foo", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Operator, c.ClassificationType);
                    Assert.Equal("=", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.StringLiteral, c.ClassificationType);
                    Assert.Equal("\"Hello, World!\"", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Punctuation, c.ClassificationType);
                    Assert.Equal(";", result.Value.Content.GetSubText(c.TextSpan).ToString());
                },
                c =>
                {
                    Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                    Assert.Equal("}", result.Value.Content.GetSubText(c.TextSpan).ToString());
                });
        }

        public (SourceText sourceText, TextSpan span) CreateText(string text)
        {
            // Since we're using positions, normalize to Windows style
            text = text.Replace("\r", "").Replace("\n", "\r\n");

            var start = text.IndexOf('|');
            var length = text.IndexOf('|', start + 1) - start - 1;
            text = text.Replace("|", "");

            if (start < 0 || length < 0)
            {
                throw new InvalidOperationException("Could not find delimited text.");
            }

            return (SourceText.From(text), new TextSpan(start, length));
        }

        // Adds the text to a ProjectSnapshot, generates code, and updates the workspace.
        private (DocumentSnapshot primary, Document secondary) Initialize(SourceText sourceText)
        {
            var project = new DefaultProjectSnapshot(
                ProjectState.Create(Workspace.Services, HostProject)
                .WithAddedHostDocument(HostDocument, () =>
                {
                    return Task.FromResult(TextAndVersion.Create(sourceText, VersionStamp.Create()));
                }));

            var primary = project.GetDocument(HostDocument.FilePath);

            var solution = Workspace.CurrentSolution.AddProject(ProjectInfo.Create(
                ProjectId.CreateNewId(Path.GetFileNameWithoutExtension(HostDocument.FilePath)),
                VersionStamp.Create(),
                Path.GetFileNameWithoutExtension(HostDocument.FilePath),
                Path.GetFileNameWithoutExtension(HostDocument.FilePath),
                LanguageNames.CSharp,
                HostDocument.FilePath));

            solution = solution.AddDocument(
                DocumentId.CreateNewId(solution.ProjectIds.Single(), HostDocument.FilePath),
                HostDocument.FilePath,
                new GeneratedOutputTextLoader(primary, HostDocument.FilePath));

            var secondary = solution.Projects.Single().Documents.Single();
            return (primary, secondary);
        }

        // Maps a span in the primary buffer to the secondary buffer. This is only valid for C# code
        // that appears in the primary buffer.
        private async Task<TextSpan> GetSecondarySpanAsync(DocumentSnapshot primary, TextSpan primarySpan, Document secondary)
        {
            var output = await primary.GetGeneratedOutputAsync();

            var mappings = output.GetCSharpDocument().SourceMappings;
            for (var i = 0; i < mappings.Count; i++)
            {
                var mapping = mappings[i];
                if (mapping.OriginalSpan.AsTextSpan().Contains(primarySpan))
                {
                    var offset = mapping.GeneratedSpan.AbsoluteIndex - mapping.OriginalSpan.AbsoluteIndex;
                    var secondarySpan = new TextSpan(primarySpan.Start + offset, primarySpan.Length);
                    Assert.Equal(
                        (await primary.GetTextAsync()).GetSubText(primarySpan).ToString(),
                        (await secondary.GetTextAsync()).GetSubText(secondarySpan).ToString());
                    return secondarySpan;
                }
            }

            throw new InvalidOperationException("Could not map the primary span to the generated code.");
        }

        private RazorDocumentExcerptService CreateExcerptService(DocumentSnapshot document)
        {
            return new RazorDocumentExcerptService(document, new RazorSpanMappingService(document));
        }
    }
}
