// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Web.WebPages.TestUtils;
using Microsoft.AspNetCore.Razor.CodeGenerators;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;
using Microsoft.AspNetCore.Razor.Test.Framework;
using Microsoft.AspNetCore.Razor.Test.Utils;
using Microsoft.AspNetCore.Razor.Text;
using Xunit;

namespace Microsoft.AspNetCore.Razor
{
    public abstract class PartialParsingTestBase<TLanguage>
        where TLanguage : RazorCodeLanguage, new()
    {
        private const string TestLinePragmaFileName = "C:\\This\\Path\\Is\\Just\\For\\Line\\Pragmas.cshtml";

        protected static void RunFullReparseTest(TextChange change, PartialParseResult additionalFlags = (PartialParseResult)0)
        {
            // Arrange
            using (var manager = CreateParserManager())
            {
                manager.InitializeWithDocument(change.OldBuffer);

                // Act
                var result = manager.CheckForStructureChangesAndWait(change);

                // Assert
                Assert.Equal(PartialParseResult.Rejected | additionalFlags, result);
                Assert.Equal(2, manager.ParseCount);
            }
        }

        protected static void RunPartialParseTest(TextChange change, Block newTreeRoot, PartialParseResult additionalFlags = (PartialParseResult)0)
        {
            // Arrange
            using (var manager = CreateParserManager())
            {
                manager.InitializeWithDocument(change.OldBuffer);

                // Act
                var result = manager.CheckForStructureChangesAndWait(change);

                // Assert
                Assert.Equal(PartialParseResult.Accepted | additionalFlags, result);
                Assert.Equal(1, manager.ParseCount);
                ParserTestBase.EvaluateParseTree(manager.Parser.CurrentParseTree, newTreeRoot);
            }
        }

        protected static TestParserManager CreateParserManager()
        {
            var host = CreateHost();
            var parser = new RazorEditorParser(host, TestLinePragmaFileName);
            return new TestParserManager(parser);
        }

        protected static RazorEngineHost CreateHost()
        {
            return new RazorEngineHost(new TLanguage())
            {
                GeneratedClassContext = new GeneratedClassContext(
                    "Execute",
                    "Write",
                    "WriteLiteral",
                    "WriteTo",
                    "WriteLiteralTo",
                    "Template",
                    "DefineSection",
                    new GeneratedTagHelperContext()),
                DesignTimeMode = true
            };
        }

        protected static void RunTypeKeywordTest(string keyword)
        {
            var before = "@" + keyword.Substring(0, keyword.Length - 1);
            var after = "@" + keyword;
            var changed = new StringTextBuffer(after);
            var old = new StringTextBuffer(before);
            RunFullReparseTest(new TextChange(keyword.Length, 0, old, 1, changed), additionalFlags: PartialParseResult.SpanContextChanged);
        }

        protected class TestParserManager : IDisposable
        {
            public int ParseCount;

            private readonly ManualResetEventSlim _parserComplete;

            public TestParserManager(RazorEditorParser parser)
            {
                _parserComplete = new ManualResetEventSlim();
                ParseCount = 0;
                Parser = parser;
                parser.DocumentParseComplete += (sender, args) =>
                {
                    Interlocked.Increment(ref ParseCount);
                    _parserComplete.Set();
                };
            }

            public RazorEditorParser Parser { get; }

            public void InitializeWithDocument(ITextBuffer startDocument)
            {
                CheckForStructureChangesAndWait(new TextChange(0, 0, new StringTextBuffer(string.Empty), startDocument.Length, startDocument));
            }

            public PartialParseResult CheckForStructureChangesAndWait(TextChange change)
            {
                var result = Parser.CheckForStructureChanges(change);
                if (result.HasFlag(PartialParseResult.Rejected))
                {
                    WaitForParse();
                }
                return result;
            }

            public void WaitForParse()
            {
                MiscUtils.DoWithTimeoutIfNotDebugging(_parserComplete.Wait); // Wait for the parse to finish
                _parserComplete.Reset();
            }

            public void Dispose()
            {
                Parser.Dispose();
            }
        }
    }
}
