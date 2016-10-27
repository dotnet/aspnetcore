// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    // Basic Tests for C# Statements:
    //  * Basic case for each statement
    //  * Basic case for ALL clauses

    // This class DOES NOT contain
    //  * Error cases
    //  * Tests for various types of nested statements
    //  * Comment tests

    public class CSharpStatementTest : CsHtmlCodeParserTestBase
    {
        [Fact]
        public void ForStatement()
        {
            ParseBlockTest("@for(int i = 0; i++; i < length) { foo(); }",
                           new StatementBlock(
                               Factory.CodeTransition(),
                               Factory.Code("for(int i = 0; i++; i < length) { foo(); }")
                                   .AsStatement()
                                   .Accepts(AcceptedCharacters.None)
                               ));
        }

        [Fact]
        public void ForEachStatement()
        {
            ParseBlockTest("@foreach(var foo in bar) { foo(); }",
                           new StatementBlock(
                               Factory.CodeTransition(),
                               Factory.Code("foreach(var foo in bar) { foo(); }")
                                   .AsStatement()
                                   .Accepts(AcceptedCharacters.None)
                               ));
        }

        [Fact]
        public void WhileStatement()
        {
            ParseBlockTest("@while(true) { foo(); }",
                           new StatementBlock(
                               Factory.CodeTransition(),
                               Factory.Code("while(true) { foo(); }")
                                   .AsStatement()
                                   .Accepts(AcceptedCharacters.None)
                               ));
        }

        [Fact]
        public void SwitchStatement()
        {
            ParseBlockTest("@switch(foo) { foo(); }",
                           new StatementBlock(
                               Factory.CodeTransition(),
                               Factory.Code("switch(foo) { foo(); }")
                                   .AsStatement()
                                   .Accepts(AcceptedCharacters.None)
                               ));
        }

        [Fact]
        public void LockStatement()
        {
            ParseBlockTest("@lock(baz) { foo(); }",
                           new StatementBlock(
                               Factory.CodeTransition(),
                               Factory.Code("lock(baz) { foo(); }")
                                   .AsStatement()
                                   .Accepts(AcceptedCharacters.None)
                               ));
        }

        [Fact]
        public void IfStatement()
        {
            ParseBlockTest("@if(true) { foo(); }",
                           new StatementBlock(
                               Factory.CodeTransition(),
                               Factory.Code("if(true) { foo(); }")
                                   .AsStatement()
                               ));
        }

        [Fact]
        public void ElseIfClause()
        {
            ParseBlockTest("@if(true) { foo(); } else if(false) { foo(); } else if(!false) { foo(); }",
                           new StatementBlock(
                               Factory.CodeTransition(),
                               Factory.Code("if(true) { foo(); } else if(false) { foo(); } else if(!false) { foo(); }")
                                   .AsStatement()
                               ));
        }

        [Fact]
        public void ElseClause()
        {
            ParseBlockTest("@if(true) { foo(); } else { foo(); }",
                           new StatementBlock(
                               Factory.CodeTransition(),
                               Factory.Code("if(true) { foo(); } else { foo(); }")
                                   .AsStatement()
                                   .Accepts(AcceptedCharacters.None)
                               ));
        }

        [Fact]
        public void TryStatement()
        {
            ParseBlockTest("@try { foo(); }",
                           new StatementBlock(
                               Factory.CodeTransition(),
                               Factory.Code("try { foo(); }")
                                   .AsStatement()
                               ));
        }

        [Fact]
        public void CatchClause()
        {
            ParseBlockTest("@try { foo(); } catch(IOException ioex) { handleIO(); } catch(Exception ex) { handleOther(); }",
                           new StatementBlock(
                               Factory.CodeTransition(),
                               Factory.Code("try { foo(); } catch(IOException ioex) { handleIO(); } catch(Exception ex) { handleOther(); }")
                                   .AsStatement()
                               ));
        }

        public static TheoryData ExceptionFilterData
        {
            get
            {
                var factory = new SpanFactory();

                // document, expectedStatement
                return new TheoryData<string, StatementBlock>
                {
                    {
                        "@try { someMethod(); } catch(Exception) when (true) { handleIO(); }",
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory
                                .Code("try { someMethod(); } catch(Exception) when (true) { handleIO(); }")
                                .AsStatement())
                    },
                    {
                        "@try { A(); } catch(Exception) when (true) { B(); } finally { C(); }",
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory
                                .Code("try { A(); } catch(Exception) when (true) { B(); } finally { C(); }")
                                .AsStatement()
                                .Accepts(AcceptedCharacters.None))
                    },
                    {
                        "@try { A(); } catch(Exception) when (true) { B(); } catch(IOException) when (false) { C(); }",
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory
                                .Code("try { A(); } catch(Exception) when (true) { B(); } catch(IOException) " +
                                    "when (false) { C(); }")
                                .AsStatement())
                    },
                    {
                        string.Format("@try{0}{{{0}   A();{0}}}{0}catch(Exception) when (true)", Environment.NewLine) +
                        string.Format("{0}{{{0}    B();{0}}}{0}catch(IOException) when (false)", Environment.NewLine) +
                        string.Format("{0}{{{0}    C();{0}}}", Environment.NewLine),
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory
                                .Code(
                                    string.Format("try{0}{{{0}   A();{0}}}{0}catch(Exception) ", Environment.NewLine) +
                                    string.Format("when (true){0}{{{0}    B();{0}}}{0}", Environment.NewLine) +
                                    string.Format("catch(IOException) when (false){0}{{{0}    ", Environment.NewLine) +
                                    string.Format("C();{0}}}", Environment.NewLine))
                                .AsStatement())
                    },

                    // Wrapped in @{ block.
                    {
                        "@{try { someMethod(); } catch(Exception) when (true) { handleIO(); }}",
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                            factory
                                .Code("try { someMethod(); } catch(Exception) when (true) { handleIO(); }")
                                .AsStatement()
                                .AutoCompleteWith(autoCompleteString: null),
                            factory.MetaCode("}").Accepts(AcceptedCharacters.None))
                    },

                    // Partial exception filter data
                    {
                        "@try { someMethod(); } catch(Exception) when",
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory
                                .Code("try { someMethod(); } catch(Exception) when")
                                .AsStatement())
                    },
                    {
                        "@try { someMethod(); } when",
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory
                                .Code("try { someMethod(); }")
                                .AsStatement())
                    },
                    {
                        "@try { someMethod(); } catch(Exception) when { anotherMethod(); }",
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory
                                .Code("try { someMethod(); } catch(Exception) when { anotherMethod(); }")
                                .AsStatement())
                    },
                    {
                        "@try { someMethod(); } catch(Exception) when (true)",
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory
                                .Code("try { someMethod(); } catch(Exception) when (true)")
                                .AsStatement())
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(ExceptionFilterData))]
        public void ExceptionFilters(string document, object expectedStatement)
        {
            // Act & Assert
            ParseBlockTest(document, (StatementBlock)expectedStatement);
        }

        public static TheoryData ExceptionFilterErrorData
        {
            get
            {
                var factory = new SpanFactory();
                var unbalancedParenErrorString = "An opening \"(\" is missing the corresponding closing \")\".";
                var unbalancedBracketCatchErrorString = "The catch block is missing a closing \"}\" character.  " +
                    "Make sure you have a matching \"}\" character for all the \"{\" characters within this block, " +
                    "and that none of the \"}\" characters are being interpreted as markup.";

                // document, expectedStatement, expectedErrors
                return new TheoryData<string, StatementBlock, RazorError[]>
                {
                    {
                        "@try { someMethod(); } catch(Exception) when (",
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory
                                .Code("try { someMethod(); } catch(Exception) when (")
                                .AsStatement()),
                        new[] { new RazorError(unbalancedParenErrorString, 45, 0, 45, 1) }
                    },
                    {
                        "@try { someMethod(); } catch(Exception) when (someMethod(",
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory
                                .Code("try { someMethod(); } catch(Exception) when (someMethod(")
                                .AsStatement()),
                        new[] { new RazorError(unbalancedParenErrorString, 45, 0, 45, 1) }
                    },
                    {
                        "@try { someMethod(); } catch(Exception) when (true) {",
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory
                                .Code("try { someMethod(); } catch(Exception) when (true) {")
                                .AsStatement()),
                        new[] { new RazorError(unbalancedBracketCatchErrorString, 23, 0, 23, 1) }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(ExceptionFilterErrorData))]
        public void ExceptionFilterErrors(
            string document,
            object expectedStatement,
            object expectedErrors)
        {
            // Act & Assert
            ParseBlockTest(document, (StatementBlock)expectedStatement, (RazorError[])expectedErrors);
        }

        [Fact]
        public void FinallyClause()
        {
            ParseBlockTest("@try { foo(); } finally { Dispose(); }",
                           new StatementBlock(
                               Factory.CodeTransition(),
                               Factory.Code("try { foo(); } finally { Dispose(); }")
                                   .AsStatement()
                                   .Accepts(AcceptedCharacters.None)
                               ));
        }

        public static TheoryData StaticUsingData
        {
            get
            {
                var factory = new SpanFactory();
                Func<string, string, DirectiveBlock> createUsing = (code, import) =>
                    new DirectiveBlock(
                        factory.CodeTransition(),
                        factory.Code(code)
                            .AsNamespaceImport(import)
                            .Accepts(AcceptedCharacters.AnyExceptNewline));

                // document, expectedResult
                return new TheoryData<string, DirectiveBlock>
                {
                    { "@using static", createUsing("using static", " static") },
                    { "@using static    ", createUsing("using static    ", " static    ") },
                    { "@using         static    ", createUsing("using         static    ", "         static    ") },
                    { "@using static System", createUsing("using static System", " static System") },
                    {
                        "@using static         System",
                        createUsing("using static         System", " static         System")
                    },
                    {
                        "@using static System.Console",
                        createUsing("using static System.Console", " static System.Console")
                    },
                    {
                        "@using static global::System.Console",
                        createUsing("using static global::System.Console", " static global::System.Console")
                    },
                    {
                        "@using   static   global::System.Console  ",
                        createUsing("using   static   global::System.Console", "   static   global::System.Console")
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(StaticUsingData))]
        public void StaticUsingImport(string document, object expectedResult)
        {
            // Act & Assert
            ParseBlockTest(document, (DirectiveBlock)expectedResult);
        }

        [Fact]
        public void UsingStatement()
        {
            ParseBlockTest("@using(var foo = new Foo()) { foo.Bar(); }",
                           new StatementBlock(
                               Factory.CodeTransition(),
                               Factory.Code("using(var foo = new Foo()) { foo.Bar(); }")
                                   .AsStatement()
                                   .Accepts(AcceptedCharacters.None)
                               ));
        }

        [Fact]
        public void UsingTypeAlias()
        {
            ParseBlockTest("@using StringDictionary = System.Collections.Generic.Dictionary<string, string>",
                           new DirectiveBlock(
                               Factory.CodeTransition(),
                               Factory.Code("using StringDictionary = System.Collections.Generic.Dictionary<string, string>")
                                   .AsNamespaceImport(" StringDictionary = System.Collections.Generic.Dictionary<string, string>")
                                   .Accepts(AcceptedCharacters.AnyExceptNewline)
                               ));
        }

        [Fact]
        public void UsingNamespaceImport()
        {
            ParseBlockTest("@using System.Text.Encoding.ASCIIEncoding",
                           new DirectiveBlock(
                               Factory.CodeTransition(),
                               Factory.Code("using System.Text.Encoding.ASCIIEncoding")
                                   .AsNamespaceImport(" System.Text.Encoding.ASCIIEncoding")
                                   .Accepts(AcceptedCharacters.AnyExceptNewline)
                               ));
        }

        [Fact]
        public void DoStatement()
        {
            ParseBlockTest("@do { foo(); } while(true);",
                           new StatementBlock(
                               Factory.CodeTransition(),
                               Factory.Code("do { foo(); } while(true);")
                                   .AsStatement()
                                   .Accepts(AcceptedCharacters.None)
                               ));
        }

        [Fact]
        public void NonBlockKeywordTreatedAsImplicitExpression()
        {
            ParseBlockTest("@is foo",
                           new ExpressionBlock(new ExpressionChunkGenerator(),
                                               Factory.CodeTransition(),
                                               Factory.Code("is")
                                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                   .Accepts(AcceptedCharacters.NonWhiteSpace)
                               ));
        }
    }
}
