// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
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
        public CSharpStatementTest()
        {
            UseBaselineTests = true;
        }

        [Fact]
        public void ForStatement()
        {
            ParseBlockTest("@for(int i = 0; i++; i < length) { foo(); }");
        }

        [Fact]
        public void ForEachStatement()
        {
            ParseBlockTest("@foreach(var foo in bar) { foo(); }");
        }

        [Fact]
        public void WhileStatement()
        {
            ParseBlockTest("@while(true) { foo(); }");
        }

        [Fact]
        public void SwitchStatement()
        {
            ParseBlockTest("@switch(foo) { foo(); }");
        }

        [Fact]
        public void LockStatement()
        {
            ParseBlockTest("@lock(baz) { foo(); }");
        }

        [Fact]
        public void IfStatement()
        {
            ParseBlockTest("@if(true) { foo(); }");
        }

        [Fact]
        public void ElseIfClause()
        {
            ParseBlockTest("@if(true) { foo(); } else if(false) { foo(); } else if(!false) { foo(); }");
        }

        [Fact]
        public void ElseClause()
        {
            ParseBlockTest("@if(true) { foo(); } else { foo(); }");
        }

        [Fact]
        public void TryStatement()
        {
            ParseBlockTest("@try { foo(); }");
        }

        [Fact]
        public void CatchClause()
        {
            ParseBlockTest("@try { foo(); } catch(IOException ioex) { handleIO(); } catch(Exception ex) { handleOther(); }");
        }

        [Fact]
        public void ExceptionFilter_TryCatchWhenComplete_SingleLine()
        {
            ParseBlockTest("@try { someMethod(); } catch(Exception) when (true) { handleIO(); }");
        }

        [Fact]
        public void ExceptionFilter_TryCatchWhenFinallyComplete_SingleLine()
        {
            ParseBlockTest("@try { A(); } catch(Exception) when (true) { B(); } finally { C(); }");
        }

        [Fact]
        public void ExceptionFilter_TryCatchWhenCatchWhenComplete_SingleLine()
        {
            ParseBlockTest("@try { A(); } catch(Exception) when (true) { B(); } catch(IOException) when (false) { C(); }");
        }

        [Fact]
        public void ExceptionFilter_MultiLine()
        {
            ParseBlockTest(
@"@try
{
A();
}
catch(Exception) when (true)
{
B();
}
catch(IOException) when (false)
{
C();
}");
        }

        [Fact]
        public void ExceptionFilter_NestedTryCatchWhen()
        {
            ParseBlockTest("@{try { someMethod(); } catch(Exception) when (true) { handleIO(); }}");
        }

        [Fact]
        public void ExceptionFilter_IncompleteTryCatchWhen()
        {
            ParseBlockTest("@try { someMethod(); } catch(Exception) when");
        }

        [Fact]
        public void ExceptionFilter_IncompleteTryWhen()
        {
            ParseBlockTest("@try { someMethod(); } when");
        }

        [Fact]
        public void ExceptionFilter_IncompleteTryCatchNoBodyWhen()
        {
            ParseBlockTest("@try { someMethod(); } catch(Exception) when { anotherMethod(); }");
        }

        [Fact]
        public void ExceptionFilter_IncompleteTryCatchWhenNoBodies()
        {
            ParseBlockTest("@try { someMethod(); } catch(Exception) when (true)");
        }

        [Fact]
        public void ExceptionFilterError_TryCatchWhen_InCompleteCondition()
        {
            ParseBlockTest("@try { someMethod(); } catch(Exception) when (");
        }

        [Fact]
        public void ExceptionFilterError_TryCatchWhen_InCompleteBody()
        {
            ParseBlockTest("@try { someMethod(); } catch(Exception) when (true) {");
        }

        [Fact]
        public void FinallyClause()
        {
            ParseBlockTest("@try { foo(); } finally { Dispose(); }");
        }

        [Fact]
        public void StaticUsing_NoUsing()
        {
            ParseBlockTest("@using static");
        }

        [Fact]
        public void StaticUsing_SingleIdentifier()
        {
            ParseBlockTest("@using static System");
        }

        [Fact]
        public void StaticUsing_MultipleIdentifiers()
        {
            ParseBlockTest("@using static System.Console");
        }

        [Fact]
        public void StaticUsing_GlobalPrefix()
        {
            ParseBlockTest("@using static global::System.Console");
        }

        [Fact]
        public void StaticUsing_Complete_Spaced()
        {
            ParseBlockTest("@using   static   global::System.Console  ");
        }

        [Fact]
        public void UsingStatement()
        {
            ParseBlockTest("@using(var foo = new Foo()) { foo.Bar(); }");
        }

        [Fact]
        public void UsingTypeAlias()
        {
            ParseBlockTest("@using StringDictionary = System.Collections.Generic.Dictionary<string, string>");
        }

        [Fact]
        public void UsingNamespaceImport()
        {
            ParseBlockTest("@using System.Text.Encoding.ASCIIEncoding");
        }

        [Fact]
        public void DoStatement()
        {
            ParseBlockTest("@do { foo(); } while(true);");
        }

        [Fact]
        public void NonBlockKeywordTreatedAsImplicitExpression()
        {
            ParseBlockTest("@is foo");
        }
    }
}
