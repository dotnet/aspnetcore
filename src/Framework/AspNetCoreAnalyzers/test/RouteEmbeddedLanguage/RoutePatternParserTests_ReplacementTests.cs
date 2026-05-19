// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.AspNetCore.Analyzers.Infrastructure.RoutePattern;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage;

// These tests are mirrored from routing's AttributeRouteModelTests.cs
public partial class RoutePatternParserTests
{
    [Fact]
    public void TestReplacement()
    {
        Test(@"""[controller]""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Replacement>
        <OpenBracketToken>[</OpenBracketToken>
        <ReplacementToken value=""controller"">controller</ReplacementToken>
        <CloseBracketToken>]</CloseBracketToken>
      </Replacement>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters />
</Tree>", routePatternOptions: RoutePatternOptions.MvcAttributeRoute);
    }

    [Fact]
    public void TestEscapedReplacement()
    {
        Test(@"""[[controller]]""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Literal>
        <Literal value=""[[controller]]"">[[controller]]</Literal>
      </Literal>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters />
</Tree>", routePatternOptions: RoutePatternOptions.MvcAttributeRoute);
    }

    [Fact]
    public void TestIncompleteReplacement()
    {
        Test(@"""[controller""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Replacement>
        <OpenBracketToken>[</OpenBracketToken>
        <ReplacementToken value=""controller"">controller</ReplacementToken>
        <CloseBracketToken />
      </Replacement>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""A replacement token is not closed."" Span=""[20..20)"" Text="""" />
  </Diagnostics>
  <Parameters />
</Tree>", routePatternOptions: RoutePatternOptions.MvcAttributeRoute);
    }

    [Fact]
    public void TestOpenBracketInReplacement()
    {
        Test(@"""[cont[controller]""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Replacement>
        <OpenBracketToken>[</OpenBracketToken>
        <ReplacementToken value=""cont[controller"">cont[controller</ReplacementToken>
        <CloseBracketToken>]</CloseBracketToken>
      </Replacement>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""An unescaped '[' token is not allowed inside of a replacement token. Use '[[' to escape."" Span=""[10..25)"" Text=""cont[controller"" />
  </Diagnostics>
  <Parameters />
</Tree>", routePatternOptions: RoutePatternOptions.MvcAttributeRoute);
    }

    [Fact]
    public void TestEmptyReplacement()
    {
        Test(@"""[]""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Replacement>
        <OpenBracketToken>[</OpenBracketToken>
        <ReplacementToken />
        <CloseBracketToken>]</CloseBracketToken>
      </Replacement>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""An empty replacement token ('[]') is not allowed."" Span=""[10..11)"" Text=""]"" />
  </Diagnostics>
  <Parameters />
</Tree>", routePatternOptions: RoutePatternOptions.MvcAttributeRoute);
    }

    [Fact]
    public void TestEndReplacement()
    {
        Test(@"""]""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Literal>
        <Literal value=""]"">]</Literal>
      </Literal>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Token delimiters ('[', ']') are imbalanced."" Span=""[9..10)"" Text=""]"" />
  </Diagnostics>
  <Parameters />
</Tree>", routePatternOptions: RoutePatternOptions.MvcAttributeRoute);
    }

    [Fact]
    public void TestRepeatedReplacement()
    {
        Test(@"""[one][two]""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Replacement>
        <OpenBracketToken>[</OpenBracketToken>
        <ReplacementToken value=""one"">one</ReplacementToken>
        <CloseBracketToken>]</CloseBracketToken>
      </Replacement>
      <Replacement>
        <OpenBracketToken>[</OpenBracketToken>
        <ReplacementToken value=""two"">two</ReplacementToken>
        <CloseBracketToken>]</CloseBracketToken>
      </Replacement>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters />
</Tree>", routePatternOptions: RoutePatternOptions.MvcAttributeRoute);
    }

    [Fact]
    public void TestMultipleReplacements()
    {
        Test(@"""[controller]/[action]""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Replacement>
        <OpenBracketToken>[</OpenBracketToken>
        <ReplacementToken value=""controller"">controller</ReplacementToken>
        <CloseBracketToken>]</CloseBracketToken>
      </Replacement>
    </Segment>
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
    <Segment>
      <Replacement>
        <OpenBracketToken>[</OpenBracketToken>
        <ReplacementToken value=""action"">action</ReplacementToken>
        <CloseBracketToken>]</CloseBracketToken>
      </Replacement>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters />
</Tree>", routePatternOptions: RoutePatternOptions.MvcAttributeRoute);
    }

    [Fact]
    public void TestReplacementThenEscapedBracket()
    {
        Test(@"""[controller][[""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Replacement>
        <OpenBracketToken>[</OpenBracketToken>
        <ReplacementToken value=""controller"">controller</ReplacementToken>
        <CloseBracketToken>]</CloseBracketToken>
      </Replacement>
      <Literal>
        <Literal value=""[["">[[</Literal>
      </Literal>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters />
</Tree>", routePatternOptions: RoutePatternOptions.MvcAttributeRoute);
    }

    [Fact]
    public void TestLiteralThenReplacement()
    {
        Test(@"""thisisSomeText[action]""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Literal>
        <Literal value=""thisisSomeText"">thisisSomeText</Literal>
      </Literal>
      <Replacement>
        <OpenBracketToken>[</OpenBracketToken>
        <ReplacementToken value=""action"">action</ReplacementToken>
        <CloseBracketToken>]</CloseBracketToken>
      </Replacement>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters />
</Tree>", routePatternOptions: RoutePatternOptions.MvcAttributeRoute);
    }

    [Fact]
    public void TestMultipleTokenEscapes()
    {
        Test(@"""[[-]][[/[[controller]]""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Literal>
        <Literal value=""[[-]][["">[[-]][[</Literal>
      </Literal>
    </Segment>
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
    <Segment>
      <Literal>
        <Literal value=""[[controller]]"">[[controller]]</Literal>
      </Literal>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters />
</Tree>", routePatternOptions: RoutePatternOptions.MvcAttributeRoute, allowDiagnosticsMismatch: true);
    }

    [Fact]
    public void TestReplacementContainingEscapedBackets()
    {
        Test(@"""[contr[[oller]/[act]]ion]""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Replacement>
        <OpenBracketToken>[</OpenBracketToken>
        <ReplacementToken value=""contr[[oller"">contr[[oller</ReplacementToken>
        <CloseBracketToken>]</CloseBracketToken>
      </Replacement>
    </Segment>
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
    <Segment>
      <Replacement>
        <OpenBracketToken>[</OpenBracketToken>
        <ReplacementToken value=""act]]ion"">act]]ion</ReplacementToken>
        <CloseBracketToken>]</CloseBracketToken>
      </Replacement>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters />
</Tree>", routePatternOptions: RoutePatternOptions.MvcAttributeRoute, allowDiagnosticsMismatch: true);
    }

    [Fact]
    public void TestReplacementContainingBraces()
    {
        Test(@"""[contr}oller]/[act{ion]/{id}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Replacement>
        <OpenBracketToken>[</OpenBracketToken>
        <ReplacementToken value=""contr}oller"">contr}oller</ReplacementToken>
        <CloseBracketToken>]</CloseBracketToken>
      </Replacement>
    </Segment>
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
    <Segment>
      <Replacement>
        <OpenBracketToken>[</OpenBracketToken>
        <ReplacementToken value=""act{ion"">act{ion</ReplacementToken>
        <CloseBracketToken>]</CloseBracketToken>
      </Replacement>
    </Segment>
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
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
</Tree>", routePatternOptions: RoutePatternOptions.MvcAttributeRoute, allowDiagnosticsMismatch: true);
    }

    [Fact]
    public void TestReplacementInEscapedBrackets()
    {
        Test(@"""[controller]/[[[action]]]/id""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Replacement>
        <OpenBracketToken>[</OpenBracketToken>
        <ReplacementToken value=""controller"">controller</ReplacementToken>
        <CloseBracketToken>]</CloseBracketToken>
      </Replacement>
    </Segment>
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
    <Segment>
      <Literal>
        <Literal value=""[["">[[</Literal>
      </Literal>
      <Replacement>
        <OpenBracketToken>[</OpenBracketToken>
        <ReplacementToken value=""action"">action</ReplacementToken>
        <CloseBracketToken>]</CloseBracketToken>
      </Replacement>
      <Literal>
        <Literal value=""]]"">]]</Literal>
      </Literal>
    </Segment>
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
    <Segment>
      <Literal>
        <Literal value=""id"">id</Literal>
      </Literal>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters />
</Tree>", routePatternOptions: RoutePatternOptions.MvcAttributeRoute, allowDiagnosticsMismatch: true);
    }

    [Fact]
    public void TestReplacementInEscapedBrackets2()
    {
        Test(@"""[controller]/[[[[[action]]]]]/id""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Replacement>
        <OpenBracketToken>[</OpenBracketToken>
        <ReplacementToken value=""controller"">controller</ReplacementToken>
        <CloseBracketToken>]</CloseBracketToken>
      </Replacement>
    </Segment>
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
    <Segment>
      <Literal>
        <Literal value=""[[[["">[[[[</Literal>
      </Literal>
      <Replacement>
        <OpenBracketToken>[</OpenBracketToken>
        <ReplacementToken value=""action"">action</ReplacementToken>
        <CloseBracketToken>]</CloseBracketToken>
      </Replacement>
      <Literal>
        <Literal value=""]]]]"">]]]]</Literal>
      </Literal>
    </Segment>
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
    <Segment>
      <Literal>
        <Literal value=""id"">id</Literal>
      </Literal>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters />
</Tree>", routePatternOptions: RoutePatternOptions.MvcAttributeRoute, allowDiagnosticsMismatch: true);
    }

    [Fact]
    public void TestReplacementInEscapedBrackets3()
    {
        Test(@"""[controller]/[[[[[[[action]]]]]]]/id""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Replacement>
        <OpenBracketToken>[</OpenBracketToken>
        <ReplacementToken value=""controller"">controller</ReplacementToken>
        <CloseBracketToken>]</CloseBracketToken>
      </Replacement>
    </Segment>
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
    <Segment>
      <Literal>
        <Literal value=""[[[[[["">[[[[[[</Literal>
      </Literal>
      <Replacement>
        <OpenBracketToken>[</OpenBracketToken>
        <ReplacementToken value=""action"">action</ReplacementToken>
        <CloseBracketToken>]</CloseBracketToken>
      </Replacement>
      <Literal>
        <Literal value=""]]]]]]"">]]]]]]</Literal>
      </Literal>
    </Segment>
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
    <Segment>
      <Literal>
        <Literal value=""id"">id</Literal>
      </Literal>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters />
</Tree>", routePatternOptions: RoutePatternOptions.MvcAttributeRoute, allowDiagnosticsMismatch: true);
    }

    [Fact]
    public void TestReplacementInEscapedBrackets4()
    {
        Test(@"""[controller]/[[[[[action]]]]]]]/id""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Replacement>
        <OpenBracketToken>[</OpenBracketToken>
        <ReplacementToken value=""controller"">controller</ReplacementToken>
        <CloseBracketToken>]</CloseBracketToken>
      </Replacement>
    </Segment>
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
    <Segment>
      <Literal>
        <Literal value=""[[[["">[[[[</Literal>
      </Literal>
      <Replacement>
        <OpenBracketToken>[</OpenBracketToken>
        <ReplacementToken value=""action"">action</ReplacementToken>
        <CloseBracketToken>]</CloseBracketToken>
      </Replacement>
      <Literal>
        <Literal value=""]]]]]]"">]]]]]]</Literal>
      </Literal>
    </Segment>
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
    <Segment>
      <Literal>
        <Literal value=""id"">id</Literal>
      </Literal>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters />
</Tree>", routePatternOptions: RoutePatternOptions.MvcAttributeRoute, allowDiagnosticsMismatch: true);
    }

    [Fact]
    public void TestOpenBracketInLiteral()
    {
        Test(@"""controller]""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Literal>
        <Literal value=""controller]"">controller]</Literal>
      </Literal>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Diagnostics>
    <Diagnostic Message=""Token delimiters ('[', ']') are imbalanced."" Span=""[9..20)"" Text=""controller]"" />
  </Diagnostics>
  <Parameters />
</Tree>", routePatternOptions: RoutePatternOptions.MvcAttributeRoute);
    }
}
