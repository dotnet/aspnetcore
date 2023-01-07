// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage;

// These tests are mirrored from routing's RoutePatternParameterParserTest.cs
public partial class RoutePatternParserTests
{
    [Fact]
    public void Parse_SingleLiteral()
    {
        Test(@"""cool""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Literal>
        <Literal value=""cool"">cool</Literal>
      </Literal>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters />
</Tree>");
    }

    [Fact]
    public void Parse_SingleParameter()
    {
        Test(@"""{p}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""p"">p</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name=""p"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void Parse_OptionalParameter()
    {
        Test(@"""{p?}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""p"">p</ParameterNameToken>
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
    <Parameter Name=""p"" IsCatchAll=""false"" IsOptional=""true"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void Parse_MultipleLiterals()
    {
        Test(@"""cool/awesome/super""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Literal>
        <Literal value=""cool"">cool</Literal>
      </Literal>
    </Segment>
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
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
        <Literal value=""super"">super</Literal>
      </Literal>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters />
</Tree>");
    }

    [Fact]
    public void Parse_MultipleParameters()
    {
        Test(@"""{p1}/{p2}/{*p3}""", @"<Tree>
  <CompilationUnit>
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
        <ParameterName>
          <ParameterNameToken value=""p2"">p2</ParameterNameToken>
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
          <ParameterNameToken value=""p3"">p3</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name=""p1"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
    <Parameter Name=""p2"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
    <Parameter Name=""p3"" IsCatchAll=""true"" IsOptional=""false"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void Parse_ComplexSegment_LP()
    {
        Test(@"""cool-{p1}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Literal>
        <Literal value=""cool-"">cool-</Literal>
      </Literal>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""p1"">p1</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name=""p1"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void Parse_ComplexSegment_PL()
    {
        Test(@"""{p1}-cool""", @"<Tree>
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
        <Literal value=""-cool"">-cool</Literal>
      </Literal>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name=""p1"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void Parse_ComplexSegment_PLP()
    {
        Test(@"""{p1}-cool-{p2}""", @"<Tree>
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
        <Literal value=""-cool-"">-cool-</Literal>
      </Literal>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
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
    <Parameter Name=""p2"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void Parse_ComplexSegment_LPL()
    {
        Test(@"""cool-{p1}-awesome""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Literal>
        <Literal value=""cool-"">cool-</Literal>
      </Literal>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""p1"">p1</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
      <Literal>
        <Literal value=""-awesome"">-awesome</Literal>
      </Literal>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name=""p1"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void Parse_ComplexSegment_OptionalParameterFollowingPeriod()
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
</Tree>");
    }

    [Fact]
    public void Parse_ComplexSegment_ParametersFollowingPeriod()
    {
        Test(@"""{p1}.{p2}""", @"<Tree>
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
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name=""p1"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
    <Parameter Name=""p2"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void Parse_ComplexSegment_OptionalParameterFollowingPeriod_ThreeParameters()
    {
        Test(@"""{p1}.{p2}.{p3?}""", @"<Tree>
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
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
      <Literal>
        <Literal value=""."">.</Literal>
      </Literal>
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
    <Parameter Name=""p1"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
    <Parameter Name=""p2"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
    <Parameter Name=""p3"" IsCatchAll=""false"" IsOptional=""true"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void Parse_ComplexSegment_ThreeParametersSeparatedByPeriod()
    {
        Test(@"""{p1}.{p2}.{p3}""", @"<Tree>
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
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
      <Literal>
        <Literal value=""."">.</Literal>
      </Literal>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""p3"">p3</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name=""p1"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
    <Parameter Name=""p2"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
    <Parameter Name=""p3"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void Parse_ComplexSegment_OptionalParameterFollowingPeriod_MiddleSegment()
    {
        Test(@"""{p1}.{p2?}/{p3}""", @"<Tree>
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
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""p3"">p3</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <EndOfFile />
  </CompilationUnit>
  <Parameters>
    <Parameter Name=""p1"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
    <Parameter Name=""p2"" IsCatchAll=""false"" IsOptional=""true"" EncodeSlashes=""true"" />
    <Parameter Name=""p3"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void Parse_ComplexSegment_OptionalParameterFollowingPeriod_LastSegment()
    {
        Test(@"""{p1}/{p2}.{p3?}""", @"<Tree>
  <CompilationUnit>
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
        <ParameterName>
          <ParameterNameToken value=""p2"">p2</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
      <Literal>
        <Literal value=""."">.</Literal>
      </Literal>
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
    <Parameter Name=""p1"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
    <Parameter Name=""p2"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
    <Parameter Name=""p3"" IsCatchAll=""false"" IsOptional=""true"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }

    [Fact]
    public void Parse_ComplexSegment_OptionalParameterFollowingPeriod_PeriodAfterSlash()
    {
        Test(@"""{p2}/.{p3?}""", @"<Tree>
  <CompilationUnit>
    <Segment>
      <Parameter>
        <OpenBraceToken>{</OpenBraceToken>
        <ParameterName>
          <ParameterNameToken value=""p2"">p2</ParameterNameToken>
        </ParameterName>
        <CloseBraceToken>}</CloseBraceToken>
      </Parameter>
    </Segment>
    <Separator>
      <SlashToken>/</SlashToken>
    </Separator>
    <Segment>
      <Literal>
        <Literal value=""."">.</Literal>
      </Literal>
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
    <Parameter Name=""p2"" IsCatchAll=""false"" IsOptional=""false"" EncodeSlashes=""true"" />
    <Parameter Name=""p3"" IsCatchAll=""false"" IsOptional=""true"" EncodeSlashes=""true"" />
  </Parameters>
</Tree>");
    }

    [Theory]
    [InlineData(@"{p1:regex(^\d{{3}}-\d{{3}}-\d{{4}}$)}", @"regex(^\d{3}-\d{3}-\d{4}$)")] // ssn
    [InlineData(@"{p1:regex(^\d{{1,2}}\/\d{{1,2}}\/\d{{4}}$)}", @"regex(^\d{1,2}\/\d{1,2}\/\d{4}$)")] // date
    [InlineData(@"{p1:regex(^\w+\@\w+\.\w+)}", @"regex(^\w+\@\w+\.\w+)")] // email
    [InlineData(@"{p1:regex(([}}])\w+)}", @"regex(([}])\w+)")] // Not balanced }
    [InlineData(@"{p1:regex(([{{(])\w+)}", @"regex(([{(])\w+)")] // Not balanced {
    public void Parse_RegularExpressions(string template, string constraint)
    {
        var tree = Test(@"""" + template.Replace(@"\", @"\\") + @"""");
        var parameter = tree.GetRouteParameter("p1");
        Assert.Collection(parameter.Policies, p => Assert.Equal(":" + constraint.Replace("{", "{{").Replace("}", "}}"), p));
    }

    [Theory]
    [InlineData(@"{p1:regex(^\d{{3}}-\d{{3}}-\d{{4}}}$)}")] // extra }
    [InlineData(@"{p1:regex(^\d{{3}}-\d{{3}}-\d{{4}}$)}}")] // extra } at the end
    [InlineData(@"{{p1:regex(^\d{{3}}-\d{{3}}-\d{{4}}$)}")] // extra { at the beginning
    [InlineData(@"{p1:regex(([}])\w+}")] // Not escaped }
    [InlineData(@"{p1:regex(^\d{{3}}-\d{{3}}-\d{{4}$)}")] // Not escaped }
    [InlineData(@"{p1:regex(abc)")]
    public void Parse_RegularExpressions_Invalid(string template)
    {
        var tree = Test(@"""" + template.Replace(@"\", @"\\") + @"""");
        Assert.Collection(tree.Diagnostics, p => Assert.Equal(Resources.TemplateRoute_MismatchedParameter, p.Message));
    }

    [Theory]
    [InlineData(@"{p1:regex(^\d{{3}}-\d{{3}}-\d{{{4}}$)}")] // extra {
    [InlineData(@"{p1:regex(^\d{{3}}-\d{{3}}-\d{4}}$)}")] // Not escaped {
    public void Parse_RegularExpressions_Unescaped(string template)
    {
        var tree = Test(@"""" + template.Replace(@"\", @"\\") + @"""");
        Assert.Collection(tree.Diagnostics, p => Assert.Equal(Resources.TemplateRoute_UnescapedBrace, p.Message));
    }

    [Theory]
    [InlineData("{p1}.{p2?}.{p3}", "p2", ".")]
    [InlineData("{p1?}{p2}", "p1", "{p2}")]
    [InlineData("{p1?}{p2?}", "p1", "{p2?}")]
    [InlineData("{p1}.{p2?})", "p2", ")")]
    [InlineData("{foorb?}-bar-{z}", "foorb", "-bar-")]
    public void Parse_ComplexSegment_OptionalParameter_NotTheLastPart(
        string template,
        string parameter,
        string invalid)
    {
        var tree = Test(@"""" + template.Replace(@"\", @"\\") + @"""");
        // Use contains because other diagnostics can be recorded.
        Assert.Contains(tree.Diagnostics, p => p.Message == Resources.FormatTemplateRoute_OptionalParameterHasTobeTheLast(template, parameter, invalid));
    }

    [Theory]
    [InlineData("{p1}-{p2?}", "-")]
    [InlineData("{p1}..{p2?}", "..")]
    [InlineData("..{p2?}", "..")]
    [InlineData("{p1}.abc.{p2?}", ".abc.")]
    [InlineData("{p1}{p2?}", "{p1}")]
    public void Parse_ComplexSegment_OptionalParametersSeparatedByPeriod_Invalid(string template, string parameter)
    {
        var tree = Test(@"""" + template.Replace(@"\", @"\\") + @"""");
        Assert.Collection(tree.Diagnostics, p => Assert.Equal(Resources.FormatTemplateRoute_OptionalParameterCanbBePrecededByPeriod(template, "p2", parameter), p.Message));
    }

    [Fact]
    public void InvalidTemplate_WithRepeatedParameter()
    {
        var tree = Test(@"""{Controller}.mvc/{id}/{controller}""");
        Assert.Collection(tree.Diagnostics, p => Assert.Equal(Resources.FormatTemplateRoute_RepeatedParameter("controller"), p.Message));
    }

    [Theory]
    [InlineData("123{a}abc{")]
    [InlineData("123{a}abc}")]
    [InlineData("xyz}123{a}abc}")]
    [InlineData("{{p1}")]
    [InlineData("{p1}}")]
    [InlineData("p1}}p2{")]
    public void InvalidTemplate_WithMismatchedBraces(string template)
    {
        var tree = Test(@"""" + template.Replace(@"\", @"\\") + @"""");
        // Use contains because other diagnostics can be recorded.
        Assert.Contains(tree.Diagnostics, p => p.Message == Resources.TemplateRoute_MismatchedParameter);
    }

    [Fact]
    public void InvalidTemplate_CannotHaveCatchAllInMultiSegment()
    {
        var tree = Test(@"""123{a}abc{*moo}""");
        Assert.Collection(tree.Diagnostics, p => Assert.Equal(Resources.TemplateRoute_CannotHaveCatchAllInMultiSegment, p.Message));
    }

    [Fact]
    public void InvalidTemplate_CannotHaveMoreThanOneCatchAll()
    {
        var tree = Test(@"""{*p1}/{*p2}""");
        Assert.Collection(tree.Diagnostics, p => Assert.Equal(Resources.TemplateRoute_CatchAllMustBeLast, p.Message));
    }

    [Fact]
    public void InvalidTemplate_CannotHaveMoreThanOneCatchAllInMultiSegment()
    {
        var tree = Test(@"""{*p1}abc{*p2}""");
        Assert.Collection(
            tree.Diagnostics,
            p => Assert.Equal(Resources.TemplateRoute_CannotHaveCatchAllInMultiSegment, p.Message),
            p => Assert.Equal(Resources.TemplateRoute_CannotHaveCatchAllInMultiSegment, p.Message));
    }

    [Fact]
    public void InvalidTemplate_CannotHaveCatchAllWithNoName()
    {
        var tree = Test(@"""foo/{*}""");
        Assert.Collection(tree.Diagnostics, p => Assert.Equal(Resources.FormatTemplateRoute_InvalidParameterName(""), p.Message));
    }

    [Theory]
    [InlineData("{a*}", "a*")]
    [InlineData("{*a*}", "a*")]
    [InlineData("{*a*:int}", "a*")]
    [InlineData("{*a*=5}", "a*")]
    [InlineData("{*a*b=5}", "a*b")]
    [InlineData("{p1?}.{p2/}/{p3}", "p2/")]
    [InlineData("{p{{}", "p{")]
    [InlineData("{p}}}", "p}")]
    [InlineData("{p/}", "p/")]
    public void ParseRouteParameter_ThrowsIf_ParameterContainsSpecialCharacters(
        string template,
        string parameterName)
    {
        var tree = Test(@"""" + template.Replace(@"\", @"\\") + @"""");
        // Use contains because other diagnostics can be recorded.
        Assert.Contains(tree.Diagnostics, p => p.Message == Resources.FormatTemplateRoute_InvalidParameterName(parameterName));
    }

    [Fact]
    public void InvalidTemplate_CannotHaveConsecutiveOpenBrace()
    {
        var tree = Test(@"""foo/{{p1}""");
        Assert.Collection(tree.Diagnostics, p => Assert.Equal(Resources.TemplateRoute_MismatchedParameter, p.Message));
    }

    [Fact]
    public void InvalidTemplate_CannotHaveConsecutiveCloseBrace()
    {
        var tree = Test(@"""foo/{p1}}""");
        // Use contains because other diagnostics can be recorded.
        Assert.Contains(tree.Diagnostics, p => p.Message == Resources.TemplateRoute_MismatchedParameter);
    }

    [Fact]
    public void InvalidTemplate_SameParameterTwiceThrows()
    {
        var tree = Test(@"""{aaa}/{AAA}""");
        Assert.Collection(tree.Diagnostics, p => Assert.Equal(Resources.FormatTemplateRoute_RepeatedParameter("AAA"), p.Message));
    }

    [Fact]
    public void InvalidTemplate_SameParameterTwiceAndOneCatchAllThrows()
    {
        var tree = Test(@"""{aaa}/{*AAA}""");
        Assert.Collection(tree.Diagnostics, p => Assert.Equal(Resources.FormatTemplateRoute_RepeatedParameter("AAA"), p.Message));
    }

    [Fact]
    public void InvalidTemplate_InvalidParameterNameWithCloseBracketThrows()
    {
        var tree = Test(@"""{a}/{aa}a}/{z}""");
        Assert.Collection(tree.Diagnostics, p => Assert.Equal(Resources.TemplateRoute_MismatchedParameter, p.Message));
    }

    [Fact]
    public void InvalidTemplate_InvalidParameterNameWithOpenBracketThrows()
    {
        var tree = Test(@"""{a}/{a{aa}/{z}""");
        Assert.Collection(tree.Diagnostics, p => Assert.Equal(Resources.TemplateRoute_UnescapedBrace, p.Message));
    }

    [Fact]
    public void InvalidTemplate_InvalidParameterNameWithEmptyNameThrows()
    {
        var tree = Test(@"""{a}/{}/{z}""");
        Assert.Collection(tree.Diagnostics, p => Assert.Equal(Resources.FormatTemplateRoute_InvalidParameterName(""), p.Message));
    }

    [Fact]
    public void InvalidTemplate_InvalidParameterNameWithQuestionThrows()
    {
        var tree = Test(@"""{Controller}.mvc/{?}""");
        Assert.Collection(tree.Diagnostics, p => Assert.Equal(Resources.FormatTemplateRoute_InvalidParameterName(""), p.Message));
    }

    [Fact]
    public void InvalidTemplate_ConsecutiveSeparatorsSlashSlashThrows()
    {
        var tree = Test(@"""{a}//{z}""");
        Assert.Collection(tree.Diagnostics, p => Assert.Equal(Resources.TemplateRoute_CannotHaveConsecutiveSeparators, p.Message));
    }

    [Fact]
    public void InvalidTemplate_WithCatchAllNotAtTheEndThrows()
    {
        var tree = Test(@"""foo/{p1}/{*p2}/{p3}""");
        Assert.Collection(tree.Diagnostics, p => Assert.Equal(Resources.TemplateRoute_CatchAllMustBeLast, p.Message));
    }

    [Fact]
    public void InvalidTemplate_RepeatedParametersThrows()
    {
        var tree = Test(@"""foo/aa{p1}{p2}""");
        Assert.Collection(tree.Diagnostics, p => Assert.Equal(Resources.TemplateRoute_CannotHaveConsecutiveParameters, p.Message));
    }

    [Theory]
    [InlineData("/foo")]
    [InlineData("~/foo")]
    public void ValidTemplate_CanStartWithSlashOrTildeSlash(string routePattern)
    {
        var tree = Test(@"""" + routePattern.Replace(@"\", @"\\") + @"""");
        Assert.Empty(tree.Diagnostics);
    }

    [Fact]
    public void InvalidTemplate_CannotStartWithTilde()
    {
        var tree = Test(@"""~foo""");
        Assert.Collection(tree.Diagnostics, p => Assert.Equal(Resources.TemplateRoute_InvalidRouteTemplate, p.Message));
    }

    [Fact]
    public void InvalidTemplate_CannotContainQuestionMark()
    {
        var tree = Test(@"""foor?bar""");
        Assert.Collection(tree.Diagnostics, p => Assert.Equal(Resources.FormatTemplateRoute_InvalidLiteral("foor?bar"), p.Message));
    }

    [Fact]
    public void InvalidTemplate_ParameterCannotContainQuestionMark_UnlessAtEnd()
    {
        var tree = Test(@"""{foor?b}""");
        Assert.Collection(tree.Diagnostics, p => Assert.Equal(Resources.FormatTemplateRoute_InvalidParameterName("foor?b"), p.Message));
    }

    [Fact]
    public void InvalidTemplate_CatchAllMarkedOptional()
    {
        var tree = Test(@"""{a}/{*b?}""");
        Assert.Collection(tree.Diagnostics, p => Assert.Equal(Resources.TemplateRoute_CatchAllCannotBeOptional, p.Message));
    }

    [Theory]
    [InlineData("{id}", new[] { "id" }, new[] { "" })]
    [InlineData("{category}/product/{group}", new[] { "category", "group" }, new[] { "", "" })]
    [InlineData("{category:int}/product/{group:range(10, 20)}?", new[] { "category", "group" }, new[] { ":int", ":range(10, 20)" })]
    [InlineData("{person:int}/{ssn:regex(^\\\\d{{3}}-\\\\d{{2}}-\\\\d{{4}}$)}", new[] { "person", "ssn" }, new[] { ":int", ":regex(^\\d{{3}}-\\d{{2}}-\\d{{4}}$)" })]
    [InlineData("{area=Home}/{controller:required}/{id:int=0}", new[] { "area", "controller", "id" }, new[] { "=Home", ":required", ":int=0" })]
    [InlineData("{category}/product/{group?}", new[] { "category", "group" }, new[] { "", "?" })]
    [InlineData("{category}/{product}/{*sku}", new[] { "category", "product", "sku" }, new[] { "", "", "" })]
    [InlineData("{category}-product-{sku}", new[] { "category", "sku" }, new[] { "", "" })]
    [InlineData("category-{product}-sku", new[] { "product" }, new[] { "" })]
    [InlineData("{category}.{sku?}", new[] { "category", "sku" }, new[] { "", "?" })]
    [InlineData("{category}.{product?}/{sku}", new[] { "category", "product", "sku" }, new[] { "", "?", "" })]
    public void RouteTokenizer_Works_ForSimpleRouteTemplates(string template, string[] expectedNames, string[] expectedQualifiers)
    {
        var tree = Test(@"""" + template + @"""", runSubTreeTests: false);

        Assert.Equal(expectedNames.Length, tree.RouteParameters.Length);
        Assert.Equal(expectedQualifiers.Length, tree.RouteParameters.Length);

        for (var i = 0; i < expectedNames.Length; i++)
        {
            var expectedName = expectedNames[i];
            var expectedQualifier = expectedQualifiers[i];

            if (!tree.TryGetRouteParameter(expectedName, out var routeParameter))
            {
                throw new Exception($"Couldn't find expected route parameter: {expectedName}");
            }

            var qualifier = string.Join(string.Empty, routeParameter.Policies);
            if (routeParameter.DefaultValue != null)
            {
                qualifier += "=" + routeParameter.DefaultValue;
            }
            if (routeParameter.IsOptional)
            {
                qualifier += "?";
            }

            Assert.Equal(expectedQualifier, qualifier);
        }
    }
}
