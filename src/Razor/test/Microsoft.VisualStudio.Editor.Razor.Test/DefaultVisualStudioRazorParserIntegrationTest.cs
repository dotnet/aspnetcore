// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Test;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public class DefaultVisualStudioRazorParserIntegrationTest : ForegroundDispatcherTestBase
    {
        private const string TestLinePragmaFileName = "C:\\This\\Path\\Is\\Just\\For\\Line\\Pragmas.cshtml";
        private const string TestProjectPath = "C:\\This\\Path\\Is\\Just\\For\\Project.csproj";

        public DefaultVisualStudioRazorParserIntegrationTest()
        {
            Workspace = CodeAnalysis.TestWorkspace.Create();
            ProjectSnapshot = new EphemeralProjectSnapshot(Workspace.Services, TestProjectPath);
        }

        private ProjectSnapshot ProjectSnapshot { get; }

        private CodeAnalysis.Workspace Workspace { get; }

        [ForegroundFact]
        public async Task BufferChangeStartsFullReparseIfChangeOverlapsMultipleSpans()
        {
            // Arrange
            var original = new StringTextSnapshot("Foo @bar Baz");
            var testBuffer = new TestTextBuffer(original);
            var documentTracker = CreateDocumentTracker(testBuffer);
            using (var manager = CreateParserManager(original))
            {
                var changed = new StringTextSnapshot("Foo @bap Daz");
                var edit = new TestEdit(7, 3, original, 3, changed, "p D");

                await manager.InitializeWithDocumentAsync(edit.OldSnapshot);

                // Act - 1
                await manager.ApplyEditAndWaitForParseAsync(edit);

                // Assert - 1
                Assert.Equal(2, manager.ParseCount);

                // Act - 2
                await manager.ApplyEditAndWaitForParseAsync(edit);

                // Assert - 2
                Assert.Equal(3, manager.ParseCount);
            }
        }

        [ForegroundFact]
        public async Task AwaitPeriodInsertionAcceptedProvisionally()
        {
            // Arrange
            var original = new StringTextSnapshot("foo @await Html baz");
            using (var manager = CreateParserManager(original))
            {
                var changed = new StringTextSnapshot("foo @await Html. baz");
                var edit = new TestEdit(15, 0, original, 1, changed, ".");
                await manager.InitializeWithDocumentAsync(edit.OldSnapshot);

                // Act
                await manager.ApplyEditAndWaitForReparseAsync(edit);

                // Assert
                Assert.Equal(2, manager.ParseCount);
                VerifyCurrentSyntaxTree(manager);
            }
        }

        [ForegroundFact]
        public async Task ImpExprAcceptsDCIInStmtBlkAfterIdentifiers()
        {
            // ImplicitExpressionAcceptsDotlessCommitInsertionsInStatementBlockAfterIdentifiers
            var changed = new StringTextSnapshot("@{" + Environment.NewLine
                                                + "    @DateTime." + Environment.NewLine
                                                + "}");
            var original = new StringTextSnapshot("@{" + Environment.NewLine
                                            + "    @DateTime" + Environment.NewLine
                                            + "}");

            var edit = new TestEdit(15 + Environment.NewLine.Length, 0, original, 1, changed, ".");
            using (var manager = CreateParserManager(original))
            {
                void ApplyAndVerifyPartialChange(TestEdit testEdit, string expectedCode)
                {
                    manager.ApplyEdit(testEdit);
                    Assert.Equal(1, manager.ParseCount);

                    VerifyPartialParseTree(manager, changed.GetText(), expectedCode);
                };

                await manager.InitializeWithDocumentAsync(edit.OldSnapshot);

                // This is the process of a dotless commit when doing "." insertions to commit intellisense changes.
                ApplyAndVerifyPartialChange(edit, "DateTime.");

                original = changed;
                changed = new StringTextSnapshot("@{" + Environment.NewLine
                                                + "    @DateTime.." + Environment.NewLine
                                                + "}");
                edit = new TestEdit(16 + Environment.NewLine.Length, 0, original, 1, changed, ".");

                ApplyAndVerifyPartialChange(edit, "DateTime..");

                original = changed;
                changed = new StringTextSnapshot("@{" + Environment.NewLine
                                                + "    @DateTime.Now." + Environment.NewLine
                                                + "}");
                edit = new TestEdit(16 + Environment.NewLine.Length, 0, original, 3, changed, "Now");

                ApplyAndVerifyPartialChange(edit, "DateTime.Now.");
            }
        }

        [ForegroundFact]
        public async Task ImpExprAcceptsDCIInStatementBlock()
        {
            // ImpExprAcceptsDotlessCommitInsertionsInStatementBlock
            var changed = new StringTextSnapshot("@{" + Environment.NewLine
                                                    + "    @DateT." + Environment.NewLine
                                                    + "}");
            var original = new StringTextSnapshot("@{" + Environment.NewLine
                                            + "    @DateT" + Environment.NewLine
                                            + "}");

            var edit = new TestEdit(12 + Environment.NewLine.Length, 0, original, 1, changed, ".");
            using (var manager = CreateParserManager(original))
            {
                void ApplyAndVerifyPartialChange(TestEdit testEdit, string expectedCode)
                {
                    manager.ApplyEdit(testEdit);
                    Assert.Equal(1, manager.ParseCount);
                    VerifyPartialParseTree(manager, changed.GetText(), expectedCode);
                };

                await manager.InitializeWithDocumentAsync(edit.OldSnapshot);

                // This is the process of a dotless commit when doing "." insertions to commit intellisense changes.
                ApplyAndVerifyPartialChange(edit, "DateT.");

                original = changed;
                changed = new StringTextSnapshot("@{" + Environment.NewLine
                                                + "    @DateTime." + Environment.NewLine
                                                + "}");
                edit = new TestEdit(12 + Environment.NewLine.Length, 0, original, 3, changed, "ime");

                ApplyAndVerifyPartialChange(edit, "DateTime.");
            }
        }

        [ForegroundFact]
        public async Task ImpExprProvisionallyAcceptsDCI()
        {
            // ImpExprProvisionallyAcceptsDotlessCommitInsertions
            var changed = new StringTextSnapshot("foo @DateT. baz");
            var original = new StringTextSnapshot("foo @DateT baz");
            var edit = new TestEdit(10, 0, original, 1, changed, ".");
            using (var manager = CreateParserManager(original))
            {
                void ApplyAndVerifyPartialChange(TestEdit testEdit, string expectedCode)
                {
                    manager.ApplyEdit(testEdit);
                    Assert.Equal(1, manager.ParseCount);

                    VerifyPartialParseTree(manager, testEdit.NewSnapshot.GetText(), expectedCode);
                };

                await manager.InitializeWithDocumentAsync(edit.OldSnapshot);

                // This is the process of a dotless commit when doing "." insertions to commit intellisense changes.
                ApplyAndVerifyPartialChange(edit, "DateT.");

                original = changed;
                changed = new StringTextSnapshot("foo @DateTime. baz");
                edit = new TestEdit(10, 0, original, 3, changed, "ime");

                ApplyAndVerifyPartialChange(edit, "DateTime.");

                // Verify the reparse finally comes
                await manager.WaitForReparseAsync();

                Assert.Equal(2, manager.ParseCount);
                VerifyCurrentSyntaxTree(manager);
            }
        }

        [ForegroundFact]
        public async Task ImpExprProvisionallyAcceptsDCIAfterIdentifiers()
        {
            // ImplicitExpressionProvisionallyAcceptsDotlessCommitInsertionsAfterIdentifiers
            var changed = new StringTextSnapshot("foo @DateTime. baz");
            var original = new StringTextSnapshot("foo @DateTime baz");
            var edit = new TestEdit(13, 0, original, 1, changed, ".");
            using (var manager = CreateParserManager(original))
            {
                void ApplyAndVerifyPartialChange(TestEdit testEdit, string expectedCode)
                {
                    manager.ApplyEdit(testEdit);
                    Assert.Equal(1, manager.ParseCount);

                    VerifyPartialParseTree(manager, testEdit.NewSnapshot.GetText(), expectedCode);
                };

                await manager.InitializeWithDocumentAsync(edit.OldSnapshot);

                // This is the process of a dotless commit when doing "." insertions to commit intellisense changes.
                ApplyAndVerifyPartialChange(edit, "DateTime.");

                original = changed;
                changed = new StringTextSnapshot("foo @DateTime.. baz");
                edit = new TestEdit(14, 0, original, 1, changed, ".");

                ApplyAndVerifyPartialChange(edit, "DateTime..");

                original = changed;
                changed = new StringTextSnapshot("foo @DateTime.Now. baz");
                edit = new TestEdit(14, 0, original, 3, changed, "Now");

                ApplyAndVerifyPartialChange(edit, "DateTime.Now.");

                // Verify the reparse eventually happens
                await manager.WaitForReparseAsync();

                Assert.Equal(2, manager.ParseCount);
                VerifyCurrentSyntaxTree(manager);
            }
        }

        [ForegroundFact]
        public async Task ImpExprProvisionallyAccCaseInsensitiveDCI_NewRoslynIntegration()
        {
            // ImplicitExpressionProvisionallyAcceptsCaseInsensitiveDotlessCommitInsertions_NewRoslynIntegration
            var original = new StringTextSnapshot("foo @date baz");
            var changed = new StringTextSnapshot("foo @date. baz");
            var edit = new TestEdit(9, 0, original, 1, changed, ".");
            using (var manager = CreateParserManager(original))
            {
                void ApplyAndVerifyPartialChange(Action applyEdit, string expectedCode)
                {
                    applyEdit();
                    Assert.Equal(1, manager.ParseCount);

                    VerifyPartialParseTree(manager, changed.GetText(), expectedCode);
                };

                await manager.InitializeWithDocumentAsync(edit.OldSnapshot);

                // This is the process of a dotless commit when doing "." insertions to commit intellisense changes.

                // @date => @date.
                ApplyAndVerifyPartialChange(() => manager.ApplyEdit(edit), "date.");

                original = changed;
                changed = new StringTextSnapshot("foo @date baz");
                edit = new TestEdit(9, 1, original, 0, changed, "");

                // @date. => @date
                ApplyAndVerifyPartialChange(() => manager.ApplyEdit(edit), "date");

                original = changed;
                changed = new StringTextSnapshot("foo @DateTime baz");
                edit = new TestEdit(5, 4, original, 8, changed, "DateTime");

                // @date => @DateTime
                ApplyAndVerifyPartialChange(() => manager.ApplyEdit(edit), "DateTime");

                original = changed;
                changed = new StringTextSnapshot("foo @DateTime. baz");
                edit = new TestEdit(13, 0, original, 1, changed, ".");

                // @DateTime => @DateTime.
                ApplyAndVerifyPartialChange(() => manager.ApplyEdit(edit), "DateTime.");

                // Verify the reparse eventually happens
                await manager.WaitForReparseAsync();

                Assert.Equal(2, manager.ParseCount);
                VerifyCurrentSyntaxTree(manager);
            }
        }

        [ForegroundFact]
        public async Task ImpExprRejectsAcceptableChangeIfPrevWasProvisionallyAccepted()
        {
            // ImplicitExpressionRejectsChangeWhichWouldHaveBeenAcceptedIfLastChangeWasProvisionallyAcceptedOnDifferentSpan
            // Arrange
            var dotTyped = new TestEdit(8, 0, new StringTextSnapshot("foo @foo @bar"), 1, new StringTextSnapshot("foo @foo. @bar"), ".");
            var charTyped = new TestEdit(14, 0, new StringTextSnapshot("foo @foo. @bar"), 1, new StringTextSnapshot("foo @foo. @barb"), "b");
            using (var manager = CreateParserManager(dotTyped.OldSnapshot))
            {
                await manager.InitializeWithDocumentAsync(dotTyped.OldSnapshot);

                // Apply the dot change
                await manager.ApplyEditAndWaitForReparseAsync(dotTyped);

                // Act (apply the identifier start char change)
                await manager.ApplyEditAndWaitForParseAsync(charTyped);

                // Assert
                Assert.Equal(2, manager.ParseCount);
                VerifyPartialParseTree(manager, charTyped.NewSnapshot.GetText());
            }
        }

        [ForegroundFact]
        public async Task ImpExprAcceptsIdentifierTypedAfterDotIfLastChangeProvisional()
        {
            // ImplicitExpressionAcceptsIdentifierTypedAfterDotIfLastChangeWasProvisionalAcceptanceOfDot
            // Arrange
            var dotTyped = new TestEdit(8, 0, new StringTextSnapshot("foo @foo bar"), 1, new StringTextSnapshot("foo @foo. bar"), ".");
            var charTyped = new TestEdit(9, 0, new StringTextSnapshot("foo @foo. bar"), 1, new StringTextSnapshot("foo @foo.b bar"), "b");
            using (var manager = CreateParserManager(dotTyped.OldSnapshot))
            {
                await manager.InitializeWithDocumentAsync(dotTyped.OldSnapshot);

                // Apply the dot change
                manager.ApplyEdit(dotTyped);

                // Act (apply the identifier start char change)
                manager.ApplyEdit(charTyped);

                // Assert
                Assert.Equal(1, manager.ParseCount);
                VerifyPartialParseTree(manager, charTyped.NewSnapshot.GetText());
            }
        }

        [ForegroundFact]
        public async Task ImpExpr_AcceptsParenthesisAtEnd_SingleEdit()
        {
            // Arrange
            var edit = new TestEdit(8, 0, new StringTextSnapshot("foo @foo bar"), 2, new StringTextSnapshot("foo @foo() bar"), "()");

            using (var manager = CreateParserManager(edit.OldSnapshot))
            {
                await manager.InitializeWithDocumentAsync(edit.OldSnapshot);

                // Apply the () edit
                manager.ApplyEdit(edit);

                // Assert
                Assert.Equal(1, manager.ParseCount);
                VerifyPartialParseTree(manager, edit.NewSnapshot.GetText());
            }
        }

        [ForegroundFact]
        public async Task ImpExpr_AcceptsParenthesisAtEnd_TwoEdits()
        {
            // Arrange
            var edit1 = new TestEdit(8, 0, new StringTextSnapshot("foo @foo bar"), 1, new StringTextSnapshot("foo @foo( bar"), "(");
            var edit2 = new TestEdit(9, 0, new StringTextSnapshot("foo @foo( bar"), 1, new StringTextSnapshot("foo @foo() bar"), ")");
            using (var manager = CreateParserManager(edit1.OldSnapshot))
            {
                await manager.InitializeWithDocumentAsync(edit1.OldSnapshot);

                // Apply the ( edit
                manager.ApplyEdit(edit1);

                // Apply the ) edit
                manager.ApplyEdit(edit2);

                // Assert
                Assert.Equal(1, manager.ParseCount);
                VerifyPartialParseTree(manager, edit2.NewSnapshot.GetText());
            }
        }

        [ForegroundFact]
        public async Task ImpExprCorrectlyTriggersReparseIfIfKeywordTyped()
        {
            await RunTypeKeywordTestAsync("if");
        }

        [ForegroundFact]
        public async Task ImpExprCorrectlyTriggersReparseIfDoKeywordTyped()
        {
            await RunTypeKeywordTestAsync("do");
        }

        [ForegroundFact]
        public async Task ImpExprCorrectlyTriggersReparseIfTryKeywordTyped()
        {
            await RunTypeKeywordTestAsync("try");
        }

        [ForegroundFact]
        public async Task ImplicitExpressionCorrectlyTriggersReparseIfForKeywordTyped()
        {
            await RunTypeKeywordTestAsync("for");
        }

        [ForegroundFact]
        public async Task ImpExprCorrectlyTriggersReparseIfForEachKeywordTyped()
        {
            await RunTypeKeywordTestAsync("foreach");
        }

        [ForegroundFact]
        public async Task ImpExprCorrectlyTriggersReparseIfWhileKeywordTyped()
        {
            await RunTypeKeywordTestAsync("while");
        }

        [ForegroundFact]
        public async Task ImpExprCorrectlyTriggersReparseIfSwitchKeywordTyped()
        {
            await RunTypeKeywordTestAsync("switch");
        }

        [ForegroundFact]
        public async Task ImpExprCorrectlyTriggersReparseIfLockKeywordTyped()
        {
            await RunTypeKeywordTestAsync("lock");
        }

        [ForegroundFact]
        public async Task ImpExprCorrectlyTriggersReparseIfUsingKeywordTyped()
        {
            await RunTypeKeywordTestAsync("using");
        }

        [ForegroundFact]
        public async Task ImpExprCorrectlyTriggersReparseIfSectionKeywordTyped()
        {
            await RunTypeKeywordTestAsync("section");
        }

        [ForegroundFact]
        public async Task ImpExprCorrectlyTriggersReparseIfInheritsKeywordTyped()
        {
            await RunTypeKeywordTestAsync("inherits");
        }

        [ForegroundFact]
        public async Task ImpExprCorrectlyTriggersReparseIfFunctionsKeywordTyped()
        {
            await RunTypeKeywordTestAsync("functions");
        }

        [ForegroundFact]
        public async Task ImpExprCorrectlyTriggersReparseIfNamespaceKeywordTyped()
        {
            await RunTypeKeywordTestAsync("namespace");
        }

        [ForegroundFact]
        public async Task ImpExprCorrectlyTriggersReparseIfClassKeywordTyped()
        {
            await RunTypeKeywordTestAsync("class");
        }

        private void VerifyPartialParseTree(TestParserManager manager, string content, string expectedCode = null)
        {
            if (expectedCode != null)
            {
                // Verify if the syntax tree represents the expected input.
                var syntaxTreeContent = manager.PartialParsingSyntaxTreeRoot.ToFullString();
                Assert.Contains(expectedCode, syntaxTreeContent);
            }

            var sourceDocument = TestRazorSourceDocument.Create(content);
            var syntaxTree = RazorSyntaxTree.Create(manager.PartialParsingSyntaxTreeRoot, sourceDocument, manager.CurrentSyntaxTree.Diagnostics, manager.CurrentSyntaxTree.Options);
            BaselineTest(syntaxTree);
        }

        private void VerifyCurrentSyntaxTree(TestParserManager manager)
        {
            BaselineTest(manager.CurrentSyntaxTree);
        }

        private TestParserManager CreateParserManager(ITextSnapshot originalSnapshot)
        {
            var textBuffer = new TestTextBuffer(originalSnapshot);
            var documentTracker = CreateDocumentTracker(textBuffer);
            var templateEngineFactory = CreateProjectEngineFactory();
            var parser = new DefaultVisualStudioRazorParser(
                Dispatcher,
                documentTracker,
                templateEngineFactory,
                new DefaultErrorReporter(),
                new TestCompletionBroker())
            {
                // We block idle work with the below reset events. Therefore, make tests fast and have the idle timer fire as soon as possible.
                IdleDelay = TimeSpan.FromMilliseconds(1),
                NotifyForegroundIdleStart = new ManualResetEventSlim(),
                BlockBackgroundIdleWork = new ManualResetEventSlim(),
            };

            parser.StartParser();

            return new TestParserManager(parser);
        }

        private static ProjectSnapshotProjectEngineFactory CreateProjectEngineFactory(
            string path = TestLinePragmaFileName,
            IEnumerable<TagHelperDescriptor> tagHelpers = null)
        {
            var fileSystem = new TestRazorProjectFileSystem();
            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, fileSystem, builder =>
            {
                RazorExtensions.Register(builder);

                builder.AddDefaultImports("@addTagHelper *, Test");

                if (tagHelpers != null)
                {
                    builder.AddTagHelpers(tagHelpers);
                }
            });

            return new TestProjectSnapshotProjectEngineFactory()
            {
                Engine = projectEngine,
            };
        }

        private async Task RunTypeKeywordTestAsync(string keyword)
        {
            // Arrange
            var before = "@" + keyword.Substring(0, keyword.Length - 1);
            var after = "@" + keyword;
            var changed = new StringTextSnapshot(after);
            var old = new StringTextSnapshot(before);
            var change = new SourceChange(keyword.Length, 0, keyword[keyword.Length - 1].ToString());
            var edit = new TestEdit(change, old, changed);
            using (var manager = CreateParserManager(old))
            {
                await manager.InitializeWithDocumentAsync(edit.OldSnapshot);

                // Act
                await manager.ApplyEditAndWaitForParseAsync(edit);

                // Assert
                Assert.Equal(2, manager.ParseCount);
            }
        }

        private static void DoWithTimeoutIfNotDebugging(Func<int, bool> withTimeout)
        {
#if DEBUG
            if (Debugger.IsAttached)
            {
                withTimeout(Timeout.Infinite);
            }
            else
            {
#endif
                Assert.True(withTimeout((int)TimeSpan.FromSeconds(5).TotalMilliseconds), "Timeout expired!");
#if DEBUG
            }
#endif
        }

        private VisualStudioDocumentTracker CreateDocumentTracker(Text.ITextBuffer textBuffer)
        {
            var focusedTextView = Mock.Of<ITextView>(textView => textView.HasAggregateFocus == true);
            var documentTracker = Mock.Of<VisualStudioDocumentTracker>(tracker =>
                tracker.TextBuffer == textBuffer &&
                tracker.TextViews == new[] { focusedTextView } &&
                tracker.FilePath == TestLinePragmaFileName &&
                tracker.ProjectPath == TestProjectPath &&
                tracker.ProjectSnapshot == ProjectSnapshot &&
                tracker.IsSupportedProject == true);
            textBuffer.Properties.AddProperty(typeof(VisualStudioDocumentTracker), documentTracker);

            return documentTracker;
        }

        private class TestParserManager : IDisposable
        {
            public int ParseCount;

            private readonly ManualResetEventSlim _parserComplete;
            private readonly ManualResetEventSlim _reparseComplete;
            private readonly TestTextBuffer _testBuffer;
            private readonly DefaultVisualStudioRazorParser _parser;

            public TestParserManager(DefaultVisualStudioRazorParser parser)
            {
                _parserComplete = new ManualResetEventSlim();
                _reparseComplete = new ManualResetEventSlim();

                _testBuffer = (TestTextBuffer)parser.TextBuffer;
                ParseCount = 0;

                _parser = parser;
                parser.DocumentStructureChanged += (sender, args) =>
                {
                    CurrentSyntaxTree = args.CodeDocument.GetSyntaxTree();

                    Interlocked.Increment(ref ParseCount);

                    if (args.SourceChange == null)
                    {
                        // Reparse occurred
                        _reparseComplete.Set();
                    }

                    _parserComplete.Set();
                };
            }

            public RazorSyntaxTree CurrentSyntaxTree { get; private set; }

            public SyntaxNode PartialParsingSyntaxTreeRoot => _parser._partialParser.ModifiedSyntaxTreeRoot;

            public async Task InitializeWithDocumentAsync(ITextSnapshot snapshot)
            {
                var old = new StringTextSnapshot(string.Empty);
                var initialChange = new SourceChange(0, 0, snapshot.GetText());
                var edit = new TestEdit(initialChange, old, snapshot);
                await ApplyEditAndWaitForParseAsync(edit);
            }

            public void ApplyEdit(TestEdit edit)
            {
                _testBuffer.ApplyEdit(edit);
            }

            public async Task ApplyEditAndWaitForParseAsync(TestEdit edit)
            {
                ApplyEdit(edit);
                await WaitForParseAsync();
            }

            public async Task ApplyEditAndWaitForReparseAsync(TestEdit edit)
            {
                ApplyEdit(edit);
                await WaitForReparseAsync();
            }

            public async Task WaitForParseAsync()
            {
                // Get off of the foreground thread so we can wait for the document structure changed event to fire
                await Task.Run(() =>
                {
                    DoWithTimeoutIfNotDebugging(_parserComplete.Wait);
                });

                _parserComplete.Reset();
            }

            public async Task WaitForReparseAsync()
            {
                Assert.True(_parser._idleTimer != null);

                // Allow background idle work to continue
                _parser.BlockBackgroundIdleWork.Set();

                // Get off of the foreground thread so we can wait for the idle timer to fire
                await Task.Run(() =>
                {
                    DoWithTimeoutIfNotDebugging(_parser.NotifyForegroundIdleStart.Wait);
                });

                Assert.Null(_parser._idleTimer);

                // Get off of the foreground thread so we can wait for the document structure changed event to fire for reparse
                await Task.Run(() =>
                {
                    DoWithTimeoutIfNotDebugging(_reparseComplete.Wait);
                });

                _reparseComplete.Reset();
                _parser.BlockBackgroundIdleWork.Reset();
                _parser.NotifyForegroundIdleStart.Reset();
            }

            public void Dispose()
            {
                _parser.Dispose();
            }
        }

        private class TestCompletionBroker : VisualStudioCompletionBroker
        {
            public override bool IsCompletionActive(ITextView textView) => false;
        }
    }
}