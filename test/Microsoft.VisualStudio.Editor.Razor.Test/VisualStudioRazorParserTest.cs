// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public class VisualStudioRazorParserTest : ForegroundDispatcherTestBase
    {
        private const string TestLinePragmaFileName = "C:\\This\\Path\\Is\\Just\\For\\Line\\Pragmas.cshtml";

        [Fact]
        public void ConstructorRequiresNonNullPhysicalPath()
        {
            Assert.Throws<ArgumentException>("filePath", () => new VisualStudioRazorParser(Dispatcher, new TestTextBuffer(null), CreateTemplateEngine(), null, new TestCompletionBroker()));
        }

        [Fact]
        public void ConstructorRequiresNonEmptyPhysicalPath()
        {
            Assert.Throws<ArgumentException>("filePath", () => new VisualStudioRazorParser(Dispatcher, new TestTextBuffer(null), CreateTemplateEngine(), string.Empty, new TestCompletionBroker()));
        }

        // [Fact] Silent skip to avoid warnings. Skipping until we can control the parser more directly.
        private void BufferChangeStartsFullReparseIfChangeOverlapsMultipleSpans()
        {
            // Arrange
            var original = new StringTextSnapshot("Foo @bar Baz");
            var testBuffer = new TestTextBuffer(original);
            using (var parser = new VisualStudioRazorParser(Dispatcher, testBuffer, CreateTemplateEngine(), TestLinePragmaFileName, new TestCompletionBroker()))
            {
                parser._idleTimer.Interval = 100;
                var changed = new StringTextSnapshot("Foo @bap Daz");
                var edit = new TestEdit(7, 3, original, 3, changed, "p D");
                var parseComplete = new ManualResetEventSlim();
                var parseCount = 0;
                parser.DocumentStructureChanged += (s, a) =>
                {
                    Interlocked.Increment(ref parseCount);
                    parseComplete.Set();
                };

                // Act - 1
                testBuffer.ApplyEdit(edit);
                DoWithTimeoutIfNotDebugging(parseComplete.Wait); // Wait for the parse to finish

                // Assert - 1
                Assert.Equal(1, parseCount);
                parseComplete.Reset();

                // Act - 2
                testBuffer.ApplyEdit(edit);

                // Assert - 2
                DoWithTimeoutIfNotDebugging(parseComplete.Wait);
                Assert.Equal(2, parseCount);
            }
        }

        // [Fact] Silent skip to avoid warnings. Skipping until we can control the parser more directly.
        private void AwaitPeriodInsertionAcceptedProvisionally()
        {
            // Arrange
            var original = new StringTextSnapshot("foo @await Html baz");
            using (var manager = CreateParserManager(original))
            {
                var factory = new SpanFactory();
                var changed = new StringTextSnapshot("foo @await Html. baz");
                var edit = new TestEdit(15, 0, original, 1, changed, ".");
                manager.InitializeWithDocument(edit.OldSnapshot);

                // Act
                manager.ApplyEditAndWaitForReparse(edit);

                // Assert
                Assert.Equal(2, manager.ParseCount);
                ParserTestBase.EvaluateParseTree(manager.CurrentSyntaxTree.Root, new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("await Html").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.WhiteSpace | AcceptedCharactersInternal.NonWhiteSpace)),
                    factory.Markup(". baz")));
            }
        }

        // [Fact] Silent skip to avoid warnings. Skipping until we can control the parser more directly.
        private void ImplicitExpressionAcceptsDotlessCommitInsertionsInStatementBlockAfterIdentifiers()
        {
            var factory = new SpanFactory();
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
                    ParserTestBase.EvaluateParseTree(manager.CurrentSyntaxTree.Root, new MarkupBlock(
                        factory.EmptyHtml(),
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                            factory.Code(Environment.NewLine + "    ")
                                .AsStatement()
                                .AutoCompleteWith(autoCompleteString: null),
                            new ExpressionBlock(
                                factory.CodeTransition(),
                                factory.Code(expectedCode)
                                       .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                       .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                            factory.Code(Environment.NewLine).AsStatement(),
                            factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                        factory.EmptyHtml()));
                };

                manager.InitializeWithDocument(edit.OldSnapshot);

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

        // [Fact] Silent skip to avoid warnings. Skipping until we can control the parser more directly.
        private void ImplicitExpressionAcceptsDotlessCommitInsertionsInStatementBlock()
        {
            var factory = new SpanFactory();
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
                    ParserTestBase.EvaluateParseTree(manager.CurrentSyntaxTree.Root, new MarkupBlock(
                        factory.EmptyHtml(),
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                            factory.Code(Environment.NewLine + "    ")
                                .AsStatement()
                                .AutoCompleteWith(autoCompleteString: null),
                            new ExpressionBlock(
                                factory.CodeTransition(),
                                factory.Code(expectedCode)
                                       .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                       .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                            factory.Code(Environment.NewLine).AsStatement(),
                            factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                        factory.EmptyHtml()));
                };

                manager.InitializeWithDocument(edit.OldSnapshot);

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

        // [Fact] Silent skip to avoid warnings. Skipping until we can control the parser more directly.
        private void ImplicitExpressionProvisionallyAcceptsDotlessCommitInsertions()
        {
            var factory = new SpanFactory();
            var changed = new StringTextSnapshot("foo @DateT. baz");
            var original = new StringTextSnapshot("foo @DateT baz");
            var edit = new TestEdit(10, 0, original, 1, changed, ".");
            using (var manager = CreateParserManager(original))
            {
                void ApplyAndVerifyPartialChange(TestEdit testEdit, string expectedCode)
                {
                    manager.ApplyEdit(testEdit);
                    Assert.Equal(1, manager.ParseCount);

                    ParserTestBase.EvaluateParseTree(manager.CurrentSyntaxTree.Root, new MarkupBlock(
                        factory.Markup("foo "),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code(expectedCode).AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                        factory.Markup(" baz")));
                };

                manager.InitializeWithDocument(edit.OldSnapshot);

                // This is the process of a dotless commit when doing "." insertions to commit intellisense changes.
                ApplyAndVerifyPartialChange(edit, "DateT.");

                original = changed;
                changed = new StringTextSnapshot("foo @DateTime. baz");
                edit = new TestEdit(10, 0, original, 3, changed, "ime");

                ApplyAndVerifyPartialChange(edit, "DateTime.");

                // Verify the reparse finally comes
                manager.WaitForReparse();

                Assert.Equal(2, manager.ParseCount);
                ParserTestBase.EvaluateParseTree(manager.CurrentSyntaxTree.Root, new MarkupBlock(
                        factory.Markup("foo "),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("DateTime").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                        factory.Markup(". baz")));
            }
        }

        // [Fact] Silent skip to avoid warnings. Skipping until we can control the parser more directly.
        private void ImplicitExpressionProvisionallyAcceptsDotlessCommitInsertionsAfterIdentifiers()
        {
            var factory = new SpanFactory();
            var changed = new StringTextSnapshot("foo @DateTime. baz");
            var original = new StringTextSnapshot("foo @DateTime baz");
            var edit = new TestEdit(13, 0, original, 1, changed, ".");
            using (var manager = CreateParserManager(original))
            {
                void ApplyAndVerifyPartialChange(TestEdit testEdit, string expectedCode)
                {
                    manager.ApplyEdit(testEdit);
                    Assert.Equal(1, manager.ParseCount);

                    ParserTestBase.EvaluateParseTree(manager.CurrentSyntaxTree.Root, new MarkupBlock(
                        factory.Markup("foo "),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code(expectedCode).AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                        factory.Markup(" baz")));
                };

                manager.InitializeWithDocument(edit.OldSnapshot);

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
                manager.WaitForReparse();

                Assert.Equal(2, manager.ParseCount);
                ParserTestBase.EvaluateParseTree(manager.CurrentSyntaxTree.Root, new MarkupBlock(
                        factory.Markup("foo "),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("DateTime.Now").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                        factory.Markup(". baz")));
            }
        }

        // [Fact] Silent skip to avoid warnings. Skipping until we can control the parser more directly.
        private void ImplicitExpressionProvisionallyAcceptsCaseInsensitiveDotlessCommitInsertions_NewRoslynIntegration()
        {
            var factory = new SpanFactory();
            var original = new StringTextSnapshot("foo @date baz");
            var changed = new StringTextSnapshot("foo @date. baz");
            var edit = new TestEdit(9, 0, original, 1, changed, ".");
            using (var manager = CreateParserManager(original))
            {
                void ApplyAndVerifyPartialChange(Action applyEdit, string expectedCode)
                {
                    applyEdit();
                    Assert.Equal(1, manager.ParseCount);

                    ParserTestBase.EvaluateParseTree(manager.CurrentSyntaxTree.Root, new MarkupBlock(
                        factory.Markup("foo "),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code(expectedCode).AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                        factory.Markup(" baz")));
                };

                manager.InitializeWithDocument(edit.OldSnapshot);

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
                manager.WaitForReparse();

                Assert.Equal(2, manager.ParseCount);
                ParserTestBase.EvaluateParseTree(manager.CurrentSyntaxTree.Root, new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("DateTime").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                    factory.Markup(". baz")));
            }
        }

        // [Fact] Silent skip to avoid warnings. Skipping until we can control the parser more directly.
        private void ImplicitExpressionRejectsChangeWhichWouldHaveBeenAcceptedIfLastChangeWasProvisionallyAcceptedOnDifferentSpan()
        {
            // Arrange
            var factory = new SpanFactory();
            var dotTyped = new TestEdit(8, 0, new StringTextSnapshot("foo @foo @bar"), 1, new StringTextSnapshot("foo @foo. @bar"), ".");
            var charTyped = new TestEdit(14, 0, new StringTextSnapshot("foo @foo. @bar"), 1, new StringTextSnapshot("foo @foo. @barb"), "b");
            using (var manager = CreateParserManager(dotTyped.OldSnapshot))
            {
                manager.InitializeWithDocument(dotTyped.OldSnapshot);

                // Apply the dot change
                manager.ApplyEditAndWaitForReparse(dotTyped);

                // Act (apply the identifier start char change)
                manager.ApplyEditAndWaitForParse(charTyped);

                // Assert
                Assert.Equal(2, manager.ParseCount);
                ParserTestBase.EvaluateParseTree(manager.CurrentSyntaxTree.Root,
                    new MarkupBlock(
                        factory.Markup("foo "),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("foo")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                   .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                        factory.Markup(". "),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("barb")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                   .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                        factory.EmptyHtml()));
            }
        }

        // [Fact] Silent skip to avoid warnings. Skipping until we can control the parser more directly.
        private void ImplicitExpressionAcceptsIdentifierTypedAfterDotIfLastChangeWasProvisionalAcceptanceOfDot()
        {
            // Arrange
            var factory = new SpanFactory();
            var dotTyped = new TestEdit(8, 0, new StringTextSnapshot("foo @foo bar"), 1, new StringTextSnapshot("foo @foo. bar"), ".");
            var charTyped = new TestEdit(9, 0, new StringTextSnapshot("foo @foo. bar"), 1, new StringTextSnapshot("foo @foo.b bar"), "b");
            using (var manager = CreateParserManager(dotTyped.OldSnapshot))
            {
                manager.InitializeWithDocument(dotTyped.OldSnapshot);

                // Apply the dot change
                manager.ApplyEdit(dotTyped);

                // Act (apply the identifier start char change)
                manager.ApplyEdit(charTyped);

                // Assert
                Assert.Equal(1, manager.ParseCount);
                ParserTestBase.EvaluateParseTree(manager.CurrentSyntaxTree.Root,
                    new MarkupBlock(
                        factory.Markup("foo "),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("foo.b")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                   .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                        factory.Markup(" bar")));
            }
        }

        // [Fact] Silent skip to avoid warnings. Skipping until we can control the parser more directly.
        private void ImplicitExpressionCorrectlyTriggersReparseIfIfKeywordTyped()
        {
            RunTypeKeywordTest("if");
        }

        // [Fact] Silent skip to avoid warnings. Skipping until we can control the parser more directly.
        private void ImplicitExpressionCorrectlyTriggersReparseIfDoKeywordTyped()
        {
            RunTypeKeywordTest("do");
        }

        // [Fact] Silent skip to avoid warnings. Skipping until we can control the parser more directly.
        private void ImplicitExpressionCorrectlyTriggersReparseIfTryKeywordTyped()
        {
            RunTypeKeywordTest("try");
        }

        // [Fact] Silent skip to avoid warnings. Skipping until we can control the parser more directly.
        private void ImplicitExpressionCorrectlyTriggersReparseIfForKeywordTyped()
        {
            RunTypeKeywordTest("for");
        }

        // [Fact] Silent skip to avoid warnings. Skipping until we can control the parser more directly.
        private void ImplicitExpressionCorrectlyTriggersReparseIfForEachKeywordTyped()
        {
            RunTypeKeywordTest("foreach");
        }

        // [Fact] Silent skip to avoid warnings. Skipping until we can control the parser more directly.
        private void ImplicitExpressionCorrectlyTriggersReparseIfWhileKeywordTyped()
        {
            RunTypeKeywordTest("while");
        }

        // [Fact] Silent skip to avoid warnings. Skipping until we can control the parser more directly.
        private void ImplicitExpressionCorrectlyTriggersReparseIfSwitchKeywordTyped()
        {
            RunTypeKeywordTest("switch");
        }

        // [Fact] Silent skip to avoid warnings. Skipping until we can control the parser more directly.
        private void ImplicitExpressionCorrectlyTriggersReparseIfLockKeywordTyped()
        {
            RunTypeKeywordTest("lock");
        }

        // [Fact] Silent skip to avoid warnings. Skipping until we can control the parser more directly.
        private void ImplicitExpressionCorrectlyTriggersReparseIfUsingKeywordTyped()
        {
            RunTypeKeywordTest("using");
        }

        // [Fact] Silent skip to avoid warnings. Skipping until we can control the parser more directly.
        private void ImplicitExpressionCorrectlyTriggersReparseIfSectionKeywordTyped()
        {
            RunTypeKeywordTest("section");
        }

        // [Fact] Silent skip to avoid warnings. Skipping until we can control the parser more directly.
        private void ImplicitExpressionCorrectlyTriggersReparseIfInheritsKeywordTyped()
        {
            RunTypeKeywordTest("inherits");
        }

        // [Fact] Silent skip to avoid warnings. Skipping until we can control the parser more directly.
        private void ImplicitExpressionCorrectlyTriggersReparseIfFunctionsKeywordTyped()
        {
            RunTypeKeywordTest("functions");
        }

        // [Fact] Silent skip to avoid warnings. Skipping until we can control the parser more directly.
        private void ImplicitExpressionCorrectlyTriggersReparseIfNamespaceKeywordTyped()
        {
            RunTypeKeywordTest("namespace");
        }

        // [Fact] Silent skip to avoid warnings. Skipping until we can control the parser more directly.
        private void ImplicitExpressionCorrectlyTriggersReparseIfClassKeywordTyped()
        {
            RunTypeKeywordTest("class");
        }

        private TestParserManager CreateParserManager(ITextSnapshot originalSnapshot, int idleDelay = 50)
        {
            var parser = new VisualStudioRazorParser(Dispatcher, new TestTextBuffer(originalSnapshot), CreateTemplateEngine(), TestLinePragmaFileName, new TestCompletionBroker());

            return new TestParserManager(parser);
        }

        private static RazorTemplateEngine CreateTemplateEngine(
            string path = TestLinePragmaFileName,
            IEnumerable<TagHelperDescriptor> tagHelpers = null)
        {
            var engine = RazorEngine.CreateDesignTime(builder =>
            {
                RazorExtensions.Register(builder);

                if (tagHelpers != null)
                {
                    builder.AddTagHelpers(tagHelpers);
                }
            });

            // GetImports on RazorTemplateEngine will at least check that the item exists, so we need to pretend
            // that it does.
            var items = new List<RazorProjectItem>();
            items.Add(new TestRazorProjectItem(path));

            var project = new TestRazorProject(items);

            var templateEngine = new RazorTemplateEngine(engine, project);
            templateEngine.Options.DefaultImports = RazorSourceDocument.Create("@addTagHelper *, Test", "_TestImports.cshtml");
            return templateEngine;
        }

        private void RunTypeKeywordTest(string keyword)
        {
            // Arrange
            var before = "@" + keyword.Substring(0, keyword.Length - 1);
            var after = "@" + keyword;
            var changed = new StringTextSnapshot(after);
            var old = new StringTextSnapshot(before);
            var change = new SourceChange(keyword.Length, 0, keyword[keyword.Length - 1].ToString());
            var edit = new TestEdit
            {
                Change = change,
                NewSnapshot = changed,
                OldSnapshot = old
            };
            using (var manager = CreateParserManager(old))
            {
                manager.InitializeWithDocument(edit.OldSnapshot);

                // Act
                manager.ApplyEditAndWaitForParse(edit);

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
                Assert.True(withTimeout((int)TimeSpan.FromSeconds(1).TotalMilliseconds), "Timeout expired!");
#if DEBUG
            }
#endif
        }

        private class TestParserManager : IDisposable
        {
            public int ParseCount;

            private readonly ManualResetEventSlim _parserComplete;
            private readonly ManualResetEventSlim _reparseComplete;
            private readonly TestTextBuffer _testBuffer;
            private readonly VisualStudioRazorParser _parser;

            public TestParserManager(VisualStudioRazorParser parser)
            {
                _parserComplete = new ManualResetEventSlim();
                _reparseComplete = new ManualResetEventSlim();
                _testBuffer = (TestTextBuffer)parser._textBuffer;
                ParseCount = 0;

                // Change idle delay to be huge in order to enable us to take control of when idle methods fire.
                parser._idleTimer.Interval = TimeSpan.FromMinutes(2).TotalMilliseconds;
                _parser = parser;
                parser.DocumentStructureChanged += (sender, args) =>
                {
                    Interlocked.Increment(ref ParseCount);
                    _parserComplete.Set();

                    if (args.SourceChange == null)
                    {
                        // Reparse occurred
                        _reparseComplete.Set();
                    }

                    CurrentSyntaxTree = args.CodeDocument.GetSyntaxTree();
                };
            }

            public RazorSyntaxTree CurrentSyntaxTree { get; private set; }

            public void InitializeWithDocument(ITextSnapshot snapshot)
            {
                var old = new StringTextSnapshot(string.Empty);
                var initialChange = new SourceChange(0, 0, snapshot.GetText());
                var edit = new TestEdit
                {
                    Change = initialChange,
                    OldSnapshot = old,
                    NewSnapshot = snapshot
                };
                ApplyEditAndWaitForParse(edit);
            }

            public void ApplyEdit(TestEdit edit)
            {
                _testBuffer.ApplyEdit(edit);
            }

            public void ApplyEditAndWaitForParse(TestEdit edit)
            {
                ApplyEdit(edit);
                WaitForParse();
            }

            public void ApplyEditAndWaitForReparse(TestEdit edit)
            {
                ApplyEdit(edit);
                WaitForReparse();
            }

            public void WaitForParse()
            {
                DoWithTimeoutIfNotDebugging(_parserComplete.Wait); // Wait for the parse to finish
                _parserComplete.Reset();
            }

            public void WaitForReparse()
            {
                Assert.True(_parser._idleTimer.Enabled, "Expected the parser to be waiting for an idle invocation but it was not.");

                _parser._idleTimer.Stop();
                _parser._idleTimer.Interval = 50;
                _parser._idleTimer.Start();
                DoWithTimeoutIfNotDebugging(_reparseComplete.Wait);
                _reparseComplete.Reset();
                Assert.False(_parser._idleTimer.Enabled);
                _parser._idleTimer.Interval = TimeSpan.FromMinutes(2).TotalMilliseconds;
            }

            public void Dispose()
            {
                _parser.Dispose();
            }
        }

        private class TextChange : ITextChange
        {
            public TextChange(TestEdit edit) : this(edit.Change)
            {
            }

            public TextChange(SourceChange change)
            {
                var changeSpan = change.Span;

                OldPosition = changeSpan.AbsoluteIndex;
                NewPosition = OldPosition;
                OldEnd = changeSpan.AbsoluteIndex + changeSpan.Length;
                NewEnd = changeSpan.AbsoluteIndex + change.NewText.Length;
            }

            public Text.Span OldSpan => throw new NotImplementedException();

            public Text.Span NewSpan => throw new NotImplementedException();

            public int OldPosition { get; }

            public int NewPosition { get; }

            public int Delta => throw new NotImplementedException();

            public int OldEnd { get; }

            public int NewEnd { get; }

            public string OldText => throw new NotImplementedException();

            public string NewText => throw new NotImplementedException();

            public int OldLength => throw new NotImplementedException();

            public int NewLength => throw new NotImplementedException();

            public int LineCountDelta => throw new NotImplementedException();
        }

        private class TestCompletionBroker : ICompletionBroker
        {
            public ICompletionSession CreateCompletionSession(ITextView textView, ITrackingPoint triggerPoint, bool trackCaret)
            {
                throw new NotImplementedException();
            }

            public void DismissAllSessions(ITextView textView)
            {
                throw new NotImplementedException();
            }

            public ReadOnlyCollection<ICompletionSession> GetSessions(ITextView textView)
            {
                throw new NotImplementedException();
            }

            public bool IsCompletionActive(ITextView textView)
            {
                return false;
            }

            public ICompletionSession TriggerCompletion(ITextView textView)
            {
                throw new NotImplementedException();
            }

            public ICompletionSession TriggerCompletion(ITextView textView, ITrackingPoint triggerPoint, bool trackCaret)
            {
                throw new NotImplementedException();
            }
        }

        private class TestTextBuffer : Text.ITextBuffer
        {
            private ITextSnapshot _currentSnapshot;

            public TestTextBuffer(ITextSnapshot initialSnapshot)
            {
                _currentSnapshot = initialSnapshot;
                ReadOnlyRegionsChanged += (sender, args) => { };
                ChangedLowPriority += (sender, args) => { };
                ChangedHighPriority += (sender, args) => { };
                Changing += (sender, args) => { };
                PostChanged += (sender, args) => { };
                ContentTypeChanged += (sender, args) => { };
            }

            public void ApplyEdit(TestEdit edit)
            {
                var args = new TextContentChangedEventArgs(edit.OldSnapshot, edit.NewSnapshot, new EditOptions(), null);
                args.Changes.Add(new TextChange(edit));
                Changed?.Invoke(this, args);

                ReadOnlyRegionsChanged?.Invoke(null, null);
                ChangedLowPriority?.Invoke(null, null);
                ChangedHighPriority?.Invoke(null, null);
                Changing?.Invoke(null, null);
                PostChanged?.Invoke(null, null);
                ContentTypeChanged?.Invoke(null, null);

                _currentSnapshot = edit.NewSnapshot;
            }

            public IContentType ContentType => throw new NotImplementedException();

            public ITextSnapshot CurrentSnapshot => _currentSnapshot;

            public bool EditInProgress => throw new NotImplementedException();

            public PropertyCollection Properties => throw new NotImplementedException();

            public event EventHandler<SnapshotSpanEventArgs> ReadOnlyRegionsChanged;
            public event EventHandler<TextContentChangedEventArgs> Changed;
            public event EventHandler<TextContentChangedEventArgs> ChangedLowPriority;
            public event EventHandler<TextContentChangedEventArgs> ChangedHighPriority;
            public event EventHandler<TextContentChangingEventArgs> Changing;
            public event EventHandler PostChanged;
            public event EventHandler<ContentTypeChangedEventArgs> ContentTypeChanged;

            public void ChangeContentType(IContentType newContentType, object editTag)
            {
                throw new NotImplementedException();
            }

            public bool CheckEditAccess()
            {
                throw new NotImplementedException();
            }

            public ITextEdit CreateEdit(EditOptions options, int? reiteratedVersionNumber, object editTag)
            {
                throw new NotImplementedException();
            }

            public ITextEdit CreateEdit()
            {
                throw new NotImplementedException();
            }

            public IReadOnlyRegionEdit CreateReadOnlyRegionEdit()
            {
                throw new NotImplementedException();
            }

            public ITextSnapshot Delete(Text.Span deleteSpan)
            {
                throw new NotImplementedException();
            }

            public NormalizedSpanCollection GetReadOnlyExtents(Text.Span span)
            {
                throw new NotImplementedException();
            }

            public ITextSnapshot Insert(int position, string text)
            {
                throw new NotImplementedException();
            }

            public bool IsReadOnly(int position)
            {
                throw new NotImplementedException();
            }

            public bool IsReadOnly(int position, bool isEdit)
            {
                throw new NotImplementedException();
            }

            public bool IsReadOnly(Text.Span span)
            {
                throw new NotImplementedException();
            }

            public bool IsReadOnly(Text.Span span, bool isEdit)
            {
                throw new NotImplementedException();
            }

            public ITextSnapshot Replace(Text.Span replaceSpan, string replaceWith)
            {
                throw new NotImplementedException();
            }

            public void TakeThreadOwnership()
            {
                throw new NotImplementedException();
            }
        }

        private class TestEdit
        {
            public TestEdit()
            {
            }

            public TestEdit(int position, int oldLength, ITextSnapshot oldSnapshot, int newLength, ITextSnapshot newSnapshot, string newText)
            {
                Change = new SourceChange(position, oldLength, newText);
                OldSnapshot = oldSnapshot;
                NewSnapshot = newSnapshot;
            }

            public SourceChange Change { get; set; }

            public ITextSnapshot OldSnapshot { get; set; }

            public ITextSnapshot NewSnapshot { get; set; }
        }
    }
}