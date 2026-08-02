// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.AspNetCore.Analyzers.Infrastructure.RoutePattern;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage;

// These tests are mirrored from component's TemplateParserTests.cs
public partial class RoutePatternParserTests
{
    [Fact]
    public void Parse_MultipleOptionalParameters()
    {
        Test(@"""{p1?}/{p2?}/{p3?}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""p1"">p1</ParameterNameToken>
        </ParameterName>
        <Optional>
          <QuestionMarkToken>?</QuestionMarkToken>
        </Optional>
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
          <ParameterNameToken value=""p2"">p2</ParameterNameToken>
        </ParameterName>
        <Optional>
          <QuestionMarkToken>?</QuestionMarkToken>
        </Optional>
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
          <ParameterNameToken value=""p3"">p3</ParameterNameToken>
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
    <Parameter Name=""p1"" IsCatchAll=""false"" IsOptional=""true"" EncodeSlashes=""true"" />
    <Parameter Name=""p2"" IsCatchAll=""false"" IsOptional=""true"" EncodeSlashes=""true"" />
    <Parameter Name=""p3"" IsCatchAll=""false"" IsOptional=""true"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>", routePatternOptions: RoutePatternOptions.ComponentsRoute);
    }

    [Fact]
    public void Parse_SingleCatchAllParameter()
    {
        Test(@"""{*p}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <CatchAll>
          <AsteriskToken>*</AsteriskToken>
        </CatchAll>
        <ParameterName>
          <ParameterNameToken value=""p"">p</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name=""p"" IsCatchAll=""true"" IsOptional=""false"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>", routePatternOptions: RoutePatternOptions.ComponentsRoute);
    }

    [Fact]
    public void Parse_MixedLiteralAndCatchAllParameter()
    {
        Test(@"""awesome/wow/{*p}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Literal>
        <Literal value=""awesome"">awesome</Literal>
      </Literal>
    </Segment>
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
    <Segment>
      <Literal>
        <Literal value=""wow"">wow</Literal>
      </Literal>
    </Segment>
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <CatchAll>
          <AsteriskToken>*</AsteriskToken>
        </CatchAll>
        <ParameterName>
          <ParameterNameToken value=""p"">p</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name=""p"" IsCatchAll=""true"" IsOptional=""false"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>", routePatternOptions: RoutePatternOptions.ComponentsRoute);
    }

    [Fact]
    public void Parse_MixedLiteralParameterAndCatchAllParameter()
    {
        Test(@"""awesome/{p1}/{*p2}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Literal>
        <Literal value=""awesome"">awesome</Literal>
      </Literal>
    </Segment>
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""p1"">p1</ParameterNameToken>
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
        <CatchAll>
          <AsteriskToken>*</AsteriskToken>
        </CatchAll>
        <ParameterName>
          <ParameterNameToken value=""p2"">p2</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name=""p1"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
    <Parameter Name=""p2"" IsCatchAll=""true"" IsOptional=""false"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>", routePatternOptions: RoutePatternOptions.ComponentsRoute, allowDiagnosticsMismatch: true);
    }

    [Theory]
    // * is only allowed at beginning for catch-all parameters
    [InlineData("{p*}")]
    [InlineData("{{}")]
    [InlineData("{}}")]
    public void Components_ParseRouteParameter_ThrowsIf_ParameterContainsSpecialCharacters(string template)
    {
        var tree = Test(@"""" + template + @"""", routePatternOptions: RoutePatternOptions.ComponentsRoute, allowDiagnosticsMismatch: true);
        Assert.NotEmpty(tree.Diagnostics);
    }

    [Fact]
    public void InvalidTemplate_LiteralAfterOptionalParam()
    {
        Test(@"""/test/{a?}/test""", @"<Tree>
  <CompilationUnit>
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
    <Segment>
      <Literal>
        <Literal value=""test"">test</Literal>
      </Literal>
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
        <Optional>
          <QuestionMarkToken>?</QuestionMarkToken>
        </Optional>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
    <Segment>
      <Literal>
        <Literal value=""test"">test</Literal>
      </Literal>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name=""a"" IsCatchAll=""false"" IsOptional=""true"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>", routePatternOptions: RoutePatternOptions.ComponentsRoute);
    }

    [Fact]
    public void InvalidTemplate_NonOptionalParamAfterOptionalParam()
    {
        Test(@"""/test/{a?}/{b}""", @"<Tree>
  <CompilationUnit>
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
    <Segment>
      <Literal>
        <Literal value=""test"">test</Literal>
      </Literal>
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
        <Optional>
          <QuestionMarkToken>?</QuestionMarkToken>
        </Optional>
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
    <Parameter Name=""a"" IsCatchAll=""false"" IsOptional=""true"" EncodeSlashes=""true"" />
    <Parameter Name=""b"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>", routePatternOptions: RoutePatternOptions.ComponentsRoute);
    }

    [Fact]
    public void Template_CatchAllParamWithMultipleAsterisks()
    {
        Test(@"""/test/{a}/{**b}""", @"<Tree>
  <CompilationUnit>
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
    <Segment>
      <Literal>
        <Literal value=""test"">test</Literal>
      </Literal>
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
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <CatchAll>
          <AsteriskToken>**</AsteriskToken>
        </CatchAll>
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
    <Parameter Name=""b"" IsCatchAll=""true"" IsOptional=""false"" EncodeSlashes=""false"" />
  </Parameters>
</Tree>", routePatternOptions: RoutePatternOptions.ComponentsRoute, allowDiagnosticsMismatch: true);
    }

    [Fact]
    public void InvalidTemplate_CatchAllParamNotLast()
    {
        Test(@"""/test/{*a}/{b}""", @"<Tree>
  <CompilationUnit>
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
    <Segment>
      <Literal>
        <Literal value=""test"">test</Literal>
      </Literal>
    </Segment>
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
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
    <Diagnostic Message=""A catch-all parameter can only appear as the last segment of the route template."" Span=""[15..19)"" Text=""{*a}"" />
  </Diagnostics>
  <Parameters>
    <Parameter Name=""a"" IsCatchAll=""true"" IsOptional=""false"" EncodeSlashes=""true"" />
    <Parameter Name=""b"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>", routePatternOptions: RoutePatternOptions.ComponentsRoute);
    }

    [Fact]
    public void InvalidTemplate_BadOptionalCharacterPosition()
    {
        Test(@"""/test/{a?bc}/{b}""", @"<Tree>
  <CompilationUnit>
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
    <Segment>
      <Literal>
        <Literal value=""test"">test</Literal>
      </Literal>
    </Segment>
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""a?bc"">a?bc</ParameterNameToken>
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
    <Diagnostic Message=""The route parameter name 'a?bc' is invalid. Route parameter names must be non-empty and cannot contain these characters: '{', '}', '/'. The '?' character marks a parameter as optional, and can occur only at the end of the parameter. The '*' character marks a parameter as catch-all, and can occur only at the start of the parameter."" Span=""[16..20)"" Text=""a?bc"" />
  </Diagnostics>
  <Parameters>
    <Parameter Name=""a?bc"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
    <Parameter Name=""b"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>", routePatternOptions: RoutePatternOptions.ComponentsRoute);
    }

    [Fact]
    public void Components_TestParameterWithDefault()
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
</Tree>", routePatternOptions: RoutePatternOptions.ComponentsRoute, allowDiagnosticsMismatch: true);
    }

    [Fact]
    public void Components_Parse_ComplexSegment_OptionalParameterFollowingPeriod()
    {
        Test(@"""{p1}.{p2?}""", @"<Tree>
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
        <Literal value=""."">.</Literal>
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
  <Parameters>
    <Parameter Name=""p1"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
    <Parameter Name=""p2"" IsCatchAll=""false"" IsOptional=""true"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>", routePatternOptions: RoutePatternOptions.ComponentsRoute, allowDiagnosticsMismatch: true);
    }

}
