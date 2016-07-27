// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Rewrite.UrlRewrite;
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

            var expected = new List<UrlRewriteRule>();
            expected.Add(CreateTestRule(new List<Condition>(),
                Url: "^article/([0-9]+)/([_0-9a-z-]+)",
                name: "Rewrite to article.aspx",
                actionType: ActionType.Rewrite,
                pattern: "article.aspx?id={R:1}&amp;title={R:2}"));

            // act
            var res = UrlRewriteFileParser.Parse(new StringReader(xml));

            // assert
           AssertUrlRewriteRuleEquality(res, expected);
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

            var condList = new List<Condition>();
            condList.Add(new Condition
            {
                Input = InputParser.ParseInputString("{HTTPS}"),
                MatchPattern = new Regex("^OFF$")
            });

            var expected = new List<UrlRewriteRule>();
            expected.Add(CreateTestRule(condList,
                Url: "^article/([0-9]+)/([_0-9a-z-]+)",
                name: "Rewrite to article.aspx",
                actionType: ActionType.Rewrite,
                pattern: "article.aspx?id={R:1}&amp;title={R:2}"));

            // act
            var res = UrlRewriteFileParser.Parse(new StringReader(xml));
            
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
                                <rule name=""Rewrite to article.aspx"">
                                    <match url = ""^article/([0-9]+)/([_0-9a-z-]+)"" />
                                    <conditions>  
                                        <add input=""{HTTPS}"" pattern=""^OFF$"" />  
                                    </conditions>  
                                    <action type=""Redirect"" url =""article.aspx?id={R:1}&amp;title={R:2}"" />
                                </rule>
                            </rules>
                        </rewrite>";

            var condList = new List<Condition>();
            condList.Add(new Condition
            {
                Input = InputParser.ParseInputString("{HTTPS}"),
                MatchPattern = new Regex("^OFF$")
            });

            var expected = new List<UrlRewriteRule>();
            expected.Add(CreateTestRule(condList,
                Url: "^article/([0-9]+)/([_0-9a-z-]+)",
                name: "Rewrite to article.aspx",
                actionType: ActionType.Rewrite,
                pattern: "article.aspx?id={R:1}&amp;title={R:2}"));
            expected.Add(CreateTestRule(condList,
                Url: "^article/([0-9]+)/([_0-9a-z-]+)",
                name: "Rewrite to article.aspx",
                actionType: ActionType.Redirect,
                pattern: "article.aspx?id={R:1}&amp;title={R:2}"));

            // act
            var res = UrlRewriteFileParser.Parse(new StringReader(xml));

            // assert
            AssertUrlRewriteRuleEquality(expected, res);
        }

        // Creates a rule with appropriate default values of the url rewrite rule.
        private UrlRewriteRule CreateTestRule(List<Condition> conditions,
            LogicalGrouping condGrouping = LogicalGrouping.MatchAll,
            bool condTracking = false,
            string name = "",
            bool enabled = true,
            PatternSyntax patternSyntax = PatternSyntax.ECMAScript,
            bool stopProcessing = false,
            string Url = "",
            bool ignoreCase = true,
            bool negate = false,
            ActionType actionType = ActionType.None,
            string pattern = "",
            bool appendQueryString = false,
            bool rewrittenUrl = false,
            RedirectType redirectType = RedirectType.Permanent
            )
        {
            return new UrlRewriteRule
            {
                Action = new UrlAction
                {
                    Url = InputParser.ParseInputString(pattern),
                    Type = actionType,
                    AppendQueryString = appendQueryString,
                    LogRewrittenUrl = rewrittenUrl,
                    RedirectType = redirectType
                },
                Name = name,
                Enabled = enabled,
                StopProcessing = stopProcessing,
                PatternSyntax = patternSyntax,
                Match = new InitialMatch
                {
                    Url = new Regex(Url),
                    IgnoreCase = ignoreCase,
                    Negate = negate
                },
                Conditions = new Conditions
                {
                    ConditionList = conditions,
                    MatchType = condGrouping,
                    TrackingAllCaptures = condTracking
                }
            };
        }

        private void AssertUrlRewriteRuleEquality(List<UrlRewriteRule> expected, List<UrlRewriteRule> actual)
        {
            Assert.Equal(expected.Count, actual.Count);
            for (var i = 0; i < expected.Count; i++)
            {
                var r1 = expected[i];
                var r2 = actual[i];

                Assert.Equal(r1.Name, r2.Name);
                Assert.Equal(r1.Enabled, r2.Enabled);
                Assert.Equal(r1.StopProcessing, r2.StopProcessing);
                Assert.Equal(r1.PatternSyntax, r2.PatternSyntax);

                Assert.Equal(r1.Match.IgnoreCase, r2.Match.IgnoreCase);
                Assert.Equal(r1.Match.Negate, r2.Match.Negate);

                Assert.Equal(r1.Action.Type, r2.Action.Type);
                Assert.Equal(r1.Action.AppendQueryString, r2.Action.AppendQueryString);
                Assert.Equal(r1.Action.RedirectType, r2.Action.RedirectType);
                Assert.Equal(r1.Action.LogRewrittenUrl, r2.Action.LogRewrittenUrl);

                // TODO conditions, url pattern, initial match regex
                Assert.Equal(r1.Conditions.MatchType, r2.Conditions.MatchType);
                Assert.Equal(r1.Conditions.TrackingAllCaptures, r2.Conditions.TrackingAllCaptures);
                Assert.Equal(r1.Conditions.ConditionList.Count, r2.Conditions.ConditionList.Count);

                for (var j = 0; j < r1.Conditions.ConditionList.Count; j++)
                {
                    var c1 = r1.Conditions.ConditionList[j];
                    var c2 = r2.Conditions.ConditionList[j];
                    Assert.Equal(c1.IgnoreCase, c2.IgnoreCase);
                    Assert.Equal(c1.Negate, c2.Negate);
                    Assert.Equal(c1.MatchType, c2.MatchType);
                    Assert.Equal(c1.Input.PatternSegments.Count, c2.Input.PatternSegments.Count);
                }
            }
        }
    }
}
