// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Rewrite.UrlActions;
using Microsoft.AspNetCore.Rewrite.UrlMatches;
using Microsoft.AspNetCore.Rewrite.IISUrlRewrite;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.UrlRewrite
{
    public class FileParserTests
    {
        [Fact]
        public void RuleParse_ParseTypicalRule()
        {
            // arrange
            var xml = @"<rewrite>
                            <rules>
                                <rule name=""Rewrite to article.aspx"">
                                    <match url = ""^article/([0-9]+)/([_0-9a-z-]+)"" />
                                    <action type=""Rewrite"" url =""article.aspx?id={R:1}&amp;title={R:2}"" />
                                </rule>
                            </rules>
                        </rewrite>";

            var expected = new List<IISUrlRewriteRule>();
            expected.Add(CreateTestRule(new ConditionCollection(),
                url: "^article/([0-9]+)/([_0-9a-z-]+)",
                name: "Rewrite to article.aspx",
                actionType: ActionType.Rewrite,
                pattern: "article.aspx?id={R:1}&amp;title={R:2}"));

            // act
            var res = new UrlRewriteFileParser().Parse(new StringReader(xml), false);

            // assert
            AssertUrlRewriteRuleEquality(expected, res);
        }

        [Fact]
        public void RuleParse_ParseSingleRuleWithSingleCondition()
        {
            // arrange
            var xml = @"<rewrite>
                            <rules>
                                <rule name=""Rewrite to article.aspx"">
                                    <match url = ""^article/([0-9]+)/([_0-9a-z-]+)"" />
                                    <conditions>
                                        <add input=""{HTTPS}"" pattern=""^OFF$"" />
                                    </conditions>
                                    <action type=""Rewrite"" url =""article.aspx?id={R:1}&amp;title={R:2}"" />
                                </rule>
                            </rules>
                        </rewrite>";

            var condList = new ConditionCollection();
            condList.Add(new Condition
            {
                Input = new InputParser().ParseInputString("{HTTPS}"),
                Match = new RegexMatch(new Regex("^OFF$"), false)
            });

            var expected = new List<IISUrlRewriteRule>();
            expected.Add(CreateTestRule(condList,
                url: "^article/([0-9]+)/([_0-9a-z-]+)",
                name: "Rewrite to article.aspx",
                actionType: ActionType.Rewrite,
                pattern: "article.aspx?id={R:1}&amp;title={R:2}"));

            // act
            var res = new UrlRewriteFileParser().Parse(new StringReader(xml), false);

            // assert
            AssertUrlRewriteRuleEquality(expected, res);
        }

        [Fact]
        public void RuleParse_ParseMultipleRules()
        {
            // arrange
            var xml = @"<rewrite>
                            <rules>
                                <rule name=""Rewrite to article.aspx"">
                                    <match url = ""^article/([0-9]+)/([_0-9a-z-]+)"" />
                                    <conditions>
                                        <add input=""{HTTPS}"" pattern=""^OFF$"" />
                                    </conditions>
                                    <action type=""Rewrite"" url =""article.aspx?id={R:1}&amp;title={R:2}"" />
                                </rule>
                                <rule name=""Rewrite to another article.aspx"">
                                    <match url = ""^article/([0-9]+)/([_0-9a-z-]+)"" />
                                    <conditions>
                                        <add input=""{HTTPS}"" pattern=""^OFF$"" />
                                    </conditions>
                                    <action type=""Rewrite"" url =""article.aspx?id={R:1}&amp;title={R:2}"" />
                                </rule>
                            </rules>
                        </rewrite>";

            var condList = new ConditionCollection();
            condList.Add(new Condition
            {
                Input = new InputParser().ParseInputString("{HTTPS}"),
                Match = new RegexMatch(new Regex("^OFF$"), false)
            });

            var expected = new List<IISUrlRewriteRule>();
            expected.Add(CreateTestRule(condList,
                url: "^article/([0-9]+)/([_0-9a-z-]+)",
                name: "Rewrite to article.aspx",
                actionType: ActionType.Rewrite,
                pattern: "article.aspx?id={R:1}&amp;title={R:2}"));
            expected.Add(CreateTestRule(condList,
                url: "^article/([0-9]+)/([_0-9a-z-]+)",
                name: "Rewrite to another article.aspx",
                actionType: ActionType.Rewrite,
                pattern: "article.aspx?id={R:1}&amp;title={R:2}"));

            // act
            var res = new UrlRewriteFileParser().Parse(new StringReader(xml), false);

            // assert
            AssertUrlRewriteRuleEquality(expected, res);
        }

        [Fact]
        public void Should_parse_global_rules()
        {
            // arrange
            var xml = @"<rewrite>
                            <globalRules>
                                <rule name=""httpsOnly"" patternSyntax=""ECMAScript"" stopProcessing=""true"">
                                    <match url="".*"" />
                                    <conditions logicalGrouping=""MatchAll"" trackAllCaptures=""false"">
                                        <add input=""{HTTPS}"" pattern=""off"" />
                                    </conditions>
                                    <action type=""Redirect"" url=""https://{HTTP_HOST}{REQUEST_URI}"" />
                                </rule>
                            </globalRules>
                            <rules>
                                <rule name=""Rewrite to article.aspx"">
                                    <match url = ""^article/([0-9]+)/([_0-9a-z-]+)"" />
                                    <action type=""Rewrite"" url =""article.aspx?id={R:1}&amp;title={R:2}"" />
                                </rule>
                            </rules>
                        </rewrite>";

            // act
            var rules = new UrlRewriteFileParser().Parse(new StringReader(xml), false);

            // assert
            Assert.Equal(2, rules.Count);
            Assert.True(rules[0].Global);
            Assert.False(rules[1].Global);
        }

        [Fact]
        public void Should_skip_empty_conditions()
        {
            // arrange
            var xml = @"<rewrite>
                            <rules>
                                <rule name=""redirect-aspnet-mvc"" enabled=""true"" stopProcessing=""true"">
                                    <match url=""^aspnet/Mvc"" />
                                    <conditions logicalGrouping=""MatchAll"" trackAllCaptures=""false"" />
                                    <action type=""Redirect"" url=""https://github.com/dotnet/aspnetcore"" />
                                </rule>
                            </rules>
                        </rewrite>";

            // act
            var rules = new UrlRewriteFileParser().Parse(new StringReader(xml), false);

            // assert
            Assert.Null(rules[0].Conditions);
        }

        // Creates a rule with appropriate default values of the url rewrite rule.
        private IISUrlRewriteRule CreateTestRule(ConditionCollection conditions,
            string name = "",
            bool enabled = true,
            PatternSyntax patternSyntax = PatternSyntax.ECMAScript,
            bool stopProcessing = false,
            string url = "",
            bool ignoreCase = true,
            bool negate = false,
            ActionType actionType = ActionType.None,
            string pattern = "",
            bool appendQueryString = false,
            bool rewrittenUrl = false,
            bool global = false,
            UriMatchPart uriMatchPart = UriMatchPart.Path,
            RedirectType redirectType = RedirectType.Permanent
            )
        {
            return new IISUrlRewriteRule(
                name,
                new RegexMatch(new Regex("^OFF$"), negate),
                conditions,
                new RewriteAction(RuleResult.ContinueRules, new InputParser().ParseInputString(url, uriMatchPart), queryStringAppend: false),
                global);
        }

        // TODO make rules comparable?
        private void AssertUrlRewriteRuleEquality(IList<IISUrlRewriteRule> actual, IList<IISUrlRewriteRule> expected)
        {
            Assert.Equal(actual.Count, expected.Count);
            for (var i = 0; i < actual.Count; i++)
            {
                var r1 = actual[i];
                var r2 = expected[i];

                Assert.Equal(r1.Name, r2.Name);

                if (r1.Conditions == null)
                {
                    Assert.Equal(0, r2.Conditions.Count);
                }
                else if (r2.Conditions == null)
                {
                    Assert.Equal(0, r1.Conditions.Count);
                }
                else
                {
                    Assert.Equal(r1.Conditions.Count, r2.Conditions.Count);
                    for (var j = 0; j < r1.Conditions.Count; j++)
                    {
                        var c1 = r1.Conditions[j];
                        var c2 = r2.Conditions[j];
                        Assert.Equal(c1.Input.PatternSegments.Count, c2.Input.PatternSegments.Count);
                    }
                }

                Assert.Equal(r1.Action.GetType(), r2.Action.GetType());
                Assert.Equal(r1.InitialMatch.GetType(), r2.InitialMatch.GetType());
            }
        }
    }
}
