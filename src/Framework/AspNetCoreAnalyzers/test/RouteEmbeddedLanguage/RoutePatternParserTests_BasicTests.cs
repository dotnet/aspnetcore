// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage;

// These tests were created by trying to enumerate all codepaths in the lexer/parser.
public partial class RoutePatternParserTests
{
    [Fact]
    public void TestEmpty()
    {
        Test(@"""""", @"<Tree>
  <CompilationUnit>
    <EndOfFile />
  </CompilationUnit>
  <Parameters />
</Tree>");
    }

    [Fact]
    public void TestSingleLiteral()
    {
        Test(@"""hello""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Literal>
        <Literal value=""hello"">hello</Literal>
      </Literal>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters />
</Tree>");
    }

    [Fact]
    public void TestSingleLiteralWithQuestionMark()
    {
        Test(@"""hel?lo""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Literal>
        <Literal value=""hel?lo"">hel?lo</Literal>
      </Literal>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""The literal section 'hel?lo' is invalid. Literal sections cannot contain the '?' character."" Span=""[9..15)"" Text=""hel?lo"" />
  </Diagnostics>
  <Parameters />
</Tree>");
    }

    [Fact]
    public void TestSlashSeperatedLiterals()
    {
        Test(@"""hello/world""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Literal>
        <Literal value=""hello"">hello</Literal>
      </Literal>
    </Segment>
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
    <Segment>
      <Literal>
        <Literal value=""world"">world</Literal>
      </Literal>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters />
</Tree>");
    }

    [Fact]
    public void TestDuplicateParameterNames()
    {
        Test(@"""{a}/{a}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""a"">a</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""a"">a</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""The route parameter name 'a' appears more than one time in the route template."" Span=""[13..16)"" Text=""{a}"" />
  </Diagnostics>
  <Parameters>
    <Parameter Name=""a"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestSlashSeperatedSegments()
    {
        Test(@"""{a}/{b}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""a"">a</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""b"">b</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name=""a"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
    <Parameter Name=""b"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestCatchAllParameterFollowedBySlash()
    {
        Test(@"""{*a}/""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <CatchAll>
          <AsteriskToken>*</AsteriskToken>
        </CatchAll>
        <ParameterName>
          <ParameterNameToken value=""a"">a</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name=""a"" IsCatchAll=""true"" IsOptional=""false"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestCatchAllParameterNotLast()
    {
        Test(@"""{*a}/{b}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <CatchAll>
          <AsteriskToken>*</AsteriskToken>
        </CatchAll>
        <ParameterName>
          <ParameterNameToken value=""a"">a</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""b"">b</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""A catch-all parameter can only appear as the last segment of the route template."" Span=""[9..13)"" Text=""{*a}"" />
  </Diagnostics>
  <Parameters>
    <Parameter Name=""a"" IsCatchAll=""true"" IsOptional=""false"" EncodeSlashes=""true"" />
    <Parameter Name=""b"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestCatchAllAndOptional()
    {
        Test(@"""{*a?}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <CatchAll>
          <AsteriskToken>*</AsteriskToken>
        </CatchAll>
        <ParameterName>
          <ParameterNameToken value=""a"">a</ParameterNameToken>
        </ParameterName>
        <Optional>
          <QuestionMarkToken>?</QuestionMarkToken>
        </Optional>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""A catch-all parameter cannot be marked optional."" Span=""[9..14)"" Text=""{*a?}"" />
  </Diagnostics>
  <Parameters>
    <Parameter Name=""a"" IsCatchAll=""true"" IsOptional=""true"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestCatchAllParameterComplexSegment()
    {
        Test(@"""a{*a}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Literal>
        <Literal value=""a"">a</Literal>
      </Literal>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <CatchAll>
          <AsteriskToken>*</AsteriskToken>
        </CatchAll>
        <ParameterName>
          <ParameterNameToken value=""a"">a</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""A path segment that contains more than one section, such as a literal section or a parameter, cannot contain a catch-all parameter."" Span=""[10..14)"" Text=""{*a}"" />
  </Diagnostics>
  <Parameters>
    <Parameter Name=""a"" IsCatchAll=""true"" IsOptional=""false"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestPeriodSeperatedLiterals()
    {
        Test(@"""hello.world""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Literal>
        <Literal value=""hello.world"">hello.world</Literal>
      </Literal>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters />
</Tree>");
    }

    [Fact]
    public void TestSimpleParameter()
    {
        Test(@"""{id}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""id"">id</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name=""id"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestParameterWithPolicy()
    {
        Test(@"""{id:foo}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""id"">id</ParameterNameToken>
        </ParameterName>
        <ParameterPolicy>
          <ColonToken>:</ColonToken>
          <PolicyFragment>
            <PolicyFragmentToken value=""foo"">foo</PolicyFragmentToken>
          </PolicyFragment>
        </ParameterPolicy>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name=""id"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"">
      <Policy>:foo</Policy>
    </Parameter>
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestParameterWithDefault()
    {
        Test(@"""{id=Home}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""id"">id</ParameterNameToken>
        </ParameterName>
        <DefaultValue>
          <EqualsToken>=</EqualsToken>
          <DefaultValueToken value=""Home"">Home</DefaultValueToken>
        </DefaultValue>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name=""id"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" DefaultValue=""Home"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestParameterWithDefaultContainingPolicyChars()
    {
        Test(@"""{id=Home=Controller:int()}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""id"">id</ParameterNameToken>
        </ParameterName>
        <DefaultValue>
          <EqualsToken>=</EqualsToken>
          <DefaultValueToken value=""Home=Controller:int()"">Home=Controller:int()</DefaultValueToken>
        </DefaultValue>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name=""id"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" DefaultValue=""Home=Controller:int()"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestParameterWithPolicyArgument()
    {
        Test(@"""{id:foo(wee)}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""id"">id</ParameterNameToken>
        </ParameterName>
        <ParameterPolicy>
          <ColonToken>:</ColonToken>
          <PolicyFragment>
            <PolicyFragmentToken value=""foo"">foo</PolicyFragmentToken>
          </PolicyFragment>
          <PolicyFragmentEscaped>
            <OpenParenToken>(</OpenParenToken>
            <PolicyFragmentToken value=""wee"">wee</PolicyFragmentToken>
            <CloseParenToken>)</CloseParenToken>
          </PolicyFragmentEscaped>
        </ParameterPolicy>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name=""id"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"">
      <Policy>:foo(wee)</Policy>
    </Parameter>
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestParameterWithPolicyArgumentEmpty()
    {
        Test(@"""{id:foo()}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""id"">id</ParameterNameToken>
        </ParameterName>
        <ParameterPolicy>
          <ColonToken>:</ColonToken>
          <PolicyFragment>
            <PolicyFragmentToken value=""foo"">foo</PolicyFragmentToken>
          </PolicyFragment>
          <PolicyFragmentEscaped>
            <OpenParenToken>(</OpenParenToken>
            <PolicyFragmentToken value="""" />
            <CloseParenToken>)</CloseParenToken>
          </PolicyFragmentEscaped>
        </ParameterPolicy>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name=""id"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"">
      <Policy>:foo()</Policy>
    </Parameter>
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestParameterOptional()
    {
        Test(@"""{id?}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""id"">id</ParameterNameToken>
        </ParameterName>
        <Optional>
          <QuestionMarkToken>?</QuestionMarkToken>
        </Optional>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name=""id"" IsCatchAll=""false"" IsOptional=""true"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestParameterDefaultValue()
    {
        Test(@"""{id=Home}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""id"">id</ParameterNameToken>
        </ParameterName>
        <DefaultValue>
          <EqualsToken>=</EqualsToken>
          <DefaultValueToken value=""Home"">Home</DefaultValueToken>
        </DefaultValue>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name=""id"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" DefaultValue=""Home"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestParameterDefaultValueAndOptional()
    {
        Test(@"""{id=Home?}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""id"">id</ParameterNameToken>
        </ParameterName>
        <DefaultValue>
          <EqualsToken>=</EqualsToken>
          <DefaultValueToken value=""Home"">Home</DefaultValueToken>
        </DefaultValue>
        <Optional>
          <QuestionMarkToken>?</QuestionMarkToken>
        </Optional>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""An optional parameter cannot have default value."" Span=""[9..19)"" Text=""{id=Home?}"" />
  </Diagnostics>
  <Parameters>
    <Parameter Name=""id"" IsCatchAll=""false"" IsOptional=""true"" EncodeSlashes=""true"" DefaultValue=""Home"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestParameterQuestionMarkBeforeEscapedClose()
    {
        Test(@"""{id?}}}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""id?}}"">id?}}</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""The route parameter name 'id?}' is invalid. Route parameter names must be non-empty and cannot contain these characters: '{', '}', '/'. The '?' character marks a parameter as optional, and can occur only at the end of the parameter. The '*' character marks a parameter as catch-all, and can occur only at the start of the parameter."" Span=""[10..15)"" Text=""id?}}"" />
  </Diagnostics>
  <Parameters>
    <Parameter Name=""id?}}"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestUnbalancedBracesInComplexSegment()
    {
        Test(@"""a{foob{bar}c""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Literal>
        <Literal value=""a"">a</Literal>
      </Literal>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""foob{bar"">foob{bar</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
      <Literal>
        <Literal value=""c"">c</Literal>
      </Literal>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""In a route parameter, '{' and '}' must be escaped with '{{' and '}}'."" Span=""[11..19)"" Text=""foob{bar"" />
  </Diagnostics>
  <Parameters>
    <Parameter Name=""foob{bar"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestComplexSegment()
    {
        Test(@"""a{foo}b{bar}c""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Literal>
        <Literal value=""a"">a</Literal>
      </Literal>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""foo"">foo</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
      <Literal>
        <Literal value=""b"">b</Literal>
      </Literal>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""bar"">bar</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
      <Literal>
        <Literal value=""c"">c</Literal>
      </Literal>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name=""bar"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
    <Parameter Name=""foo"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestConsecutiveParameters()
    {
        Test(@"""{a}{b}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""a"">a</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""b"">b</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""A path segment cannot contain two consecutive parameters. They must be separated by a '/' or by a literal string."" Span=""[12..15)"" Text=""{b}"" />
  </Diagnostics>
  <Parameters>
    <Parameter Name=""a"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
    <Parameter Name=""b"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestUnescapedOpenBrace()
    {
        Test(@"""{a{b}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""a{b"">a{b</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""In a route parameter, '{' and '}' must be escaped with '{{' and '}}'."" Span=""[10..13)"" Text=""a{b"" />
  </Diagnostics>
  <Parameters>
    <Parameter Name=""a{b"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestInvalidCharsAndUnescapedOpenBrace()
    {
        Test(@"""{a/{b}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""a/{b"">a/{b</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""In a route parameter, '{' and '}' must be escaped with '{{' and '}}'."" Span=""[10..14)"" Text=""a/{b"" />
  </Diagnostics>
  <Parameters>
    <Parameter Name=""a/{b"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestParameterWithPolicyAndOptional()
    {
        Test(@"""{id:foo?}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""id"">id</ParameterNameToken>
        </ParameterName>
        <ParameterPolicy>
          <ColonToken>:</ColonToken>
          <PolicyFragment>
            <PolicyFragmentToken value=""foo"">foo</PolicyFragmentToken>
          </PolicyFragment>
        </ParameterPolicy>
        <Optional>
          <QuestionMarkToken>?</QuestionMarkToken>
        </Optional>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name=""id"" IsCatchAll=""false"" IsOptional=""true"" EncodeSlashes=""true"">
      <Policy>:foo</Policy>
    </Parameter>
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestParameterWithMultiplePolicies()
    {
        Test(@"""{id:foo:bar}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""id"">id</ParameterNameToken>
        </ParameterName>
        <ParameterPolicy>
          <ColonToken>:</ColonToken>
          <PolicyFragment>
            <PolicyFragmentToken value=""foo"">foo</PolicyFragmentToken>
          </PolicyFragment>
        </ParameterPolicy>
        <ParameterPolicy>
          <ColonToken>:</ColonToken>
          <PolicyFragment>
            <PolicyFragmentToken value=""bar"">bar</PolicyFragmentToken>
          </PolicyFragment>
        </ParameterPolicy>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name=""id"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"">
      <Policy>:foo</Policy>
      <Policy>:bar</Policy>
    </Parameter>
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestPolicyWithEscapedFragmentParameterIncomplete()
    {
        Test(@"""{id:foo(hi""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""id"">id</ParameterNameToken>
        </ParameterName>
        <ParameterPolicy>
          <ColonToken>:</ColonToken>
          <PolicyFragment>
            <PolicyFragmentToken value=""foo(hi"">foo(hi</PolicyFragmentToken>
          </PolicyFragment>
        </ParameterPolicy>
        <CloseBraceToken />
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""There is an incomplete parameter in the route template. Check that each '{' character has a matching '}' character."" Span=""[19..19)"" Text="""" />
  </Diagnostics>
  <Parameters>
    <Parameter Name=""id"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"">
      <Policy>:foo(hi</Policy>
    </Parameter>
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestPolicyWithEscapedFragmentIncomplete()
    {
        Test(@"""{id:foo(hi}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""id"">id</ParameterNameToken>
        </ParameterName>
        <ParameterPolicy>
          <ColonToken>:</ColonToken>
          <PolicyFragment>
            <PolicyFragmentToken value=""foo(hi"">foo(hi</PolicyFragmentToken>
          </PolicyFragment>
        </ParameterPolicy>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name=""id"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"">
      <Policy>:foo(hi</Policy>
    </Parameter>
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestPolicyWithMultipleFragments()
    {
        Test(@"""{id:foo(hi)bar}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""id"">id</ParameterNameToken>
        </ParameterName>
        <ParameterPolicy>
          <ColonToken>:</ColonToken>
          <PolicyFragment>
            <PolicyFragmentToken value=""foo"">foo</PolicyFragmentToken>
          </PolicyFragment>
          <PolicyFragmentEscaped>
            <OpenParenToken>(</OpenParenToken>
            <PolicyFragmentToken value=""hi"">hi</PolicyFragmentToken>
            <CloseParenToken>)</CloseParenToken>
          </PolicyFragmentEscaped>
          <PolicyFragment>
            <PolicyFragmentToken value=""bar"">bar</PolicyFragmentToken>
          </PolicyFragment>
        </ParameterPolicy>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name=""id"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"">
      <Policy>:foo(hi)bar</Policy>
    </Parameter>
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestCatchAllParameter()
    {
        Test(@"""{*id}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <CatchAll>
          <AsteriskToken>*</AsteriskToken>
        </CatchAll>
        <ParameterName>
          <ParameterNameToken value=""id"">id</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name=""id"" IsCatchAll=""true"" IsOptional=""false"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestCatchAllUnescapedParameter()
    {
        Test(@"""{**id}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <CatchAll>
          <AsteriskToken>**</AsteriskToken>
        </CatchAll>
        <ParameterName>
          <ParameterNameToken value=""id"">id</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name=""id"" IsCatchAll=""true"" IsOptional=""false"" EncodeSlashes=""false"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestEmptyParameter()
    {
        Test(@"""{}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken />
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""The route parameter name '' is invalid. Route parameter names must be non-empty and cannot contain these characters: '{', '}', '/'. The '?' character marks a parameter as optional, and can occur only at the end of the parameter. The '*' character marks a parameter as catch-all, and can occur only at the start of the parameter."" Span=""[10..11)"" Text=""}"" />
  </Diagnostics>
  <Parameters />
</Tree>");
    }

    [Fact]
    public void TestOptionalOnlyParameter()
    {
        Test(@"""{?}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken />
        </ParameterName>
        <Optional>
          <QuestionMarkToken>?</QuestionMarkToken>
        </Optional>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""The route parameter name '' is invalid. Route parameter names must be non-empty and cannot contain these characters: '{', '}', '/'. The '?' character marks a parameter as optional, and can occur only at the end of the parameter. The '*' character marks a parameter as catch-all, and can occur only at the start of the parameter."" Span=""[10..11)"" Text=""?"" />
  </Diagnostics>
  <Parameters />
</Tree>");
    }

    [Fact]
    public void TestCatchallEscapeOnlyParameter()
    {
        Test(@"""{*}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <CatchAll>
          <AsteriskToken>*</AsteriskToken>
        </CatchAll>
        <ParameterName>
          <ParameterNameToken />
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""The route parameter name '' is invalid. Route parameter names must be non-empty and cannot contain these characters: '{', '}', '/'. The '?' character marks a parameter as optional, and can occur only at the end of the parameter. The '*' character marks a parameter as catch-all, and can occur only at the start of the parameter."" Span=""[11..12)"" Text=""}"" />
  </Diagnostics>
  <Parameters />
</Tree>");
    }

    [Fact]
    public void TestCatchallOnlyParameter()
    {
        Test(@"""{**}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <CatchAll>
          <AsteriskToken>**</AsteriskToken>
        </CatchAll>
        <ParameterName>
          <ParameterNameToken />
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""The route parameter name '' is invalid. Route parameter names must be non-empty and cannot contain these characters: '{', '}', '/'. The '?' character marks a parameter as optional, and can occur only at the end of the parameter. The '*' character marks a parameter as catch-all, and can occur only at the start of the parameter."" Span=""[12..13)"" Text=""}"" />
  </Diagnostics>
  <Parameters />
</Tree>");
    }

    [Fact]
    public void TestCatchallPolicyParameter()
    {
        Test(@"""{**:int}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <CatchAll>
          <AsteriskToken>**</AsteriskToken>
        </CatchAll>
        <ParameterName>
          <ParameterNameToken value="":int"">:int</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name="":int"" IsCatchAll=""true"" IsOptional=""false"" EncodeSlashes=""false"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestParameterWithEscapedPolicyArgument()
    {
        Test(@"""{ssn:regex(^\\d{{3}}-\\d{{2}}-\\d{{4}}$)}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""ssn"">ssn</ParameterNameToken>
        </ParameterName>
        <ParameterPolicy>
          <ColonToken>:</ColonToken>
          <PolicyFragment>
            <PolicyFragmentToken value=""regex"">regex</PolicyFragmentToken>
          </PolicyFragment>
          <PolicyFragmentEscaped>
            <OpenParenToken>(</OpenParenToken>
            <PolicyFragmentToken value=""^\d{{3}}-\d{{2}}-\d{{4}}$"">^\d{{3}}-\d{{2}}-\d{{4}}$</PolicyFragmentToken>
            <CloseParenToken>)</CloseParenToken>
          </PolicyFragmentEscaped>
        </ParameterPolicy>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name=""ssn"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"">
      <Policy>:regex(^\d{{3}}-\d{{2}}-\d{{4}}$)</Policy>
    </Parameter>
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestParameterWithEscapedPolicyArgumentIncomplete()
    {
        Test(@"""{ssn:regex(^\\d{{3}}-\\d{{2}}-\\d{{4}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""ssn"">ssn</ParameterNameToken>
        </ParameterName>
        <ParameterPolicy>
          <ColonToken>:</ColonToken>
          <PolicyFragment>
            <PolicyFragmentToken value=""regex(^\d{{3}}-\d{{2}}-\d{{4"">regex(^\d{{3}}-\d{{2}}-\d{{4</PolicyFragmentToken>
          </PolicyFragment>
        </ParameterPolicy>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name=""ssn"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"">
      <Policy>:regex(^\d{{3}}-\d{{2}}-\d{{4</Policy>
    </Parameter>
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestParameterWithOpenBraceInEscapedPolicyArgument()
    {
        Test(@"""{ssn:regex(^\\d{3}})}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""ssn"">ssn</ParameterNameToken>
        </ParameterName>
        <ParameterPolicy>
          <ColonToken>:</ColonToken>
          <PolicyFragment>
            <PolicyFragmentToken value=""regex"">regex</PolicyFragmentToken>
          </PolicyFragment>
          <PolicyFragmentEscaped>
            <OpenParenToken>(</OpenParenToken>
            <PolicyFragmentToken value=""^\d{3}}"">^\d{3}}</PolicyFragmentToken>
            <CloseParenToken>)</CloseParenToken>
          </PolicyFragmentEscaped>
        </ParameterPolicy>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""In a route parameter, '{' and '}' must be escaped with '{{' and '}}'."" Span=""[20..28)"" Text=""^\\d{3}}"" />
  </Diagnostics>
  <Parameters>
    <Parameter Name=""ssn"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"">
      <Policy>:regex(^\d{3}})</Policy>
    </Parameter>
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestParameterWithInvalidName()
    {
        Test(@"""{3}}-\\d{{2}}-\\d{{4}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""3}}-\d{{2}}-\d{{4"">3}}-\d{{2}}-\d{{4</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""The route parameter name '3}-\d{2}-\d{4' is invalid. Route parameter names must be non-empty and cannot contain these characters: '{', '}', '/'. The '?' character marks a parameter as optional, and can occur only at the end of the parameter. The '*' character marks a parameter as catch-all, and can occur only at the start of the parameter."" Span=""[10..29)"" Text=""3}}-\\d{{2}}-\\d{{4"" />
  </Diagnostics>
  <Parameters>
    <Parameter Name=""3}}-\d{{2}}-\d{{4"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestInvalidCloseBrace()
    {
        Test(@"""-\\d{{2}}-\\d{{4}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Literal>
        <Literal value=""-\d{{2}}-\d{{4}"">-\d{{2}}-\d{{4}</Literal>
      </Literal>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""There is an incomplete parameter in the route template. Check that each '{' character has a matching '}' character."" Span=""[9..26)"" Text=""-\\d{{2}}-\\d{{4}"" />
  </Diagnostics>
  <Parameters />
</Tree>");
    }

    [Fact]
    public void TestEscapedBraces()
    {
        Test(@"""{{2}}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Literal>
        <Literal value=""{{2}}"">{{2}}</Literal>
      </Literal>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters />
</Tree>");
    }

    [Fact]
    public void TestInvalidCloseBrace2()
    {
        Test(@"""{2}}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""2}}"">2}}</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken />
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""The route parameter name '2}' is invalid. Route parameter names must be non-empty and cannot contain these characters: '{', '}', '/'. The '?' character marks a parameter as optional, and can occur only at the end of the parameter. The '*' character marks a parameter as catch-all, and can occur only at the start of the parameter."" Span=""[10..13)"" Text=""2}}"" />
    <Diagnostic Message=""There is an incomplete parameter in the route template. Check that each '{' character has a matching '}' character."" Span=""[13..13)"" Text="""" />
  </Diagnostics>
  <Parameters>
    <Parameter Name=""2}}"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestOptionalParameterPrecededByParameter()
    {
        Test(@"""{p1}{p2?}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""p1"">p1</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""p2"">p2</ParameterNameToken>
        </ParameterName>
        <Optional>
          <QuestionMarkToken>?</QuestionMarkToken>
        </Optional>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""In the segment '{p1}{p2?}', the optional parameter 'p2' is preceded by an invalid segment '{p1}'. Only a period (.) can precede an optional parameter."" Span=""[13..18)"" Text=""{p2?}"" />
  </Diagnostics>
  <Parameters>
    <Parameter Name=""p1"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
    <Parameter Name=""p2"" IsCatchAll=""false"" IsOptional=""true"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestOptionalParameterPrecededByLiteral()
    {
        Test(@"""{p1}-{p2?}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""p1"">p1</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
      <Literal>
        <Literal value=""-"">-</Literal>
      </Literal>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""p2"">p2</ParameterNameToken>
        </ParameterName>
        <Optional>
          <QuestionMarkToken>?</QuestionMarkToken>
        </Optional>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""In the segment '{p1}-{p2?}', the optional parameter 'p2' is preceded by an invalid segment '-'. Only a period (.) can precede an optional parameter."" Span=""[14..19)"" Text=""{p2?}"" />
  </Diagnostics>
  <Parameters>
    <Parameter Name=""p1"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
    <Parameter Name=""p2"" IsCatchAll=""false"" IsOptional=""true"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestParameterColonStart()
    {
        Test(@"""{:hi}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value="":hi"">:hi</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name="":hi"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestParameterCatchAllColonStart()
    {
        Test(@"""{**:hi}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <CatchAll>
          <AsteriskToken>**</AsteriskToken>
        </CatchAll>
        <ParameterName>
          <ParameterNameToken value="":hi"">:hi</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name="":hi"" IsCatchAll=""true"" IsOptional=""false"" EncodeSlashes=""false"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void TestTilde()
    {
        Test(@"""~""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Literal>
        <Literal value=""~"">~</Literal>
      </Literal>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""The route template cannot start with a '~' character unless followed by a '/'."" Span=""[9..10)"" Text=""~"" />
  </Diagnostics>
  <Parameters />
</Tree>");
    }

    [Fact]
    public void TestTwoTildes()
    {
        Test(@"""~~""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Literal>
        <Literal value=""~~"">~~</Literal>
      </Literal>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""The route template cannot start with a '~' character unless followed by a '/'."" Span=""[9..11)"" Text=""~~"" />
  </Diagnostics>
  <Parameters />
</Tree>");
    }

    [Fact]
    public void TestTildeSlash()
    {
        Test(@"""~/""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Literal>
        <Literal value=""~"">~</Literal>
      </Literal>
    </Segment>
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
    <EndOfFile />
  </CompilationUnit>
  <Parameters />
</Tree>");
    }

    [Fact]
    public void TestTildeParameter()
    {
        Test(@"""~{id}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Literal>
        <Literal value=""~"">~</Literal>
      </Literal>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""id"">id</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""The route template cannot start with a '~' character unless followed by a '/'."" Span=""[9..10)"" Text=""~"" />
  </Diagnostics>
  <Parameters>
    <Parameter Name=""id"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }
}
