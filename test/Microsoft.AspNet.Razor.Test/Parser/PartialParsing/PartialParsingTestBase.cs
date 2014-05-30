// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Web.WebPages.TestUtils;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Test.Utils;
using Microsoft.AspNet.Razor.Text;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Parser.PartialParsing
{
    public abstract class PartialParsingTestBase<TLanguage>
        where TLanguage : RazorCodeLanguage, new()
    {
        private const string TestLinePragmaFileName = "C:\\This\\Path\\Is\\Just\\For\\Line\\Pragmas.cshtml";

        protected static void RunFullReparseTest(TextChange change, PartialParseResult additionalFlags = (PartialParseResult)0)
        {
            // Arrange
            TestParserManager manager = CreateParserManager();
            manager.InitializeWithDocument(change.OldBuffer);

            // Act
            PartialParseResult result = manager.CheckForStructureChangesAndWait(change);

            // Assert
            Assert.Equal(PartialParseResult.Rejected | additionalFlags, result);
            Assert.Equal(2, manager.ParseCount);
        }

        protected static void RunPartialParseTest(TextChange change, Block newTreeRoot, PartialParseResult additionalFlags = (PartialParseResult)0)
        {
            // Arrange
            TestParserManager manager = CreateParserManager();
            manager.InitializeWithDocument(change.OldBuffer);

            // Act
            PartialParseResult result = manager.CheckForStructureChangesAndWait(change);

            // Assert
            Assert.Equal(PartialParseResult.Accepted | additionalFlags, result);
            Assert.Equal(1, manager.ParseCount);
            ParserTestBase.EvaluateParseTree(manager.Parser.CurrentParseTree, newTreeRoot);
        }

        protected static TestParserManager CreateParserManager()
        {
            RazorEngineHost host = CreateHost();
            RazorEditorParser parser = new RazorEditorParser(host, TestLinePragmaFileName);
            return new TestParserManager(parser);
        }

        protected static RazorEngineHost CreateHost()
        {
            return new RazorEngineHost(new TLanguage())
            {
                GeneratedClassContext = new GeneratedClassContext("Execute", "Write", "WriteLiteral", "WriteTo", "WriteLiteralTo", "Template", "DefineSection"),
                DesignTimeMode = true
            };
        }

        protected static void RunTypeKeywordTest(string keyword)
        {
            string before = "@" + keyword.Substring(0, keyword.Length - 1);
            string after = "@" + keyword;
            StringTextBuffer changed = new StringTextBuffer(after);
            StringTextBuffer old = new StringTextBuffer(before);
            RunFullReparseTest(new TextChange(keyword.Length, 0, old, 1, changed), additionalFlags: PartialParseResult.SpanContextChanged);
        }

        protected class TestParserManager
        {
            public RazorEditorParser Parser;
            public ManualResetEventSlim ParserComplete;
            public int ParseCount;

            public TestParserManager(RazorEditorParser parser)
            {
                ParserComplete = new ManualResetEventSlim();
                ParseCount = 0;
                Parser = parser;
                parser.DocumentParseComplete += (sender, args) =>
                {
                    Interlocked.Increment(ref ParseCount);
                    ParserComplete.Set();
                };
            }

            public void InitializeWithDocument(ITextBuffer startDocument)
            {
                CheckForStructureChangesAndWait(new TextChange(0, 0, new StringTextBuffer(String.Empty), startDocument.Length, startDocument));
            }

            public PartialParseResult CheckForStructureChangesAndWait(TextChange change)
            {
                PartialParseResult result = Parser.CheckForStructureChanges(change);
                if (result.HasFlag(PartialParseResult.Rejected))
                {
                    WaitForParse();
                }
                return result;
            }

            public void WaitForParse()
            {
                MiscUtils.DoWithTimeoutIfNotDebugging(ParserComplete.Wait); // Wait for the parse to finish
                ParserComplete.Reset();
            }
        }
    }
}
