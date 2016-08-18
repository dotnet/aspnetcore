// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Rewrite.Internal;
using Microsoft.AspNetCore.Rewrite.Internal.UrlActions;
using Microsoft.AspNetCore.Rewrite.Internal.UrlMatches;
using Microsoft.AspNetCore.Rewrite.Internal.UrlRewrite;
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
            var res = new FileParser().Parse(new StringReader(xml));

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
                Input = new InputParser().ParseInputString("{HTTPS}"),
                Match = new RegexMatch(new Regex("^OFF$"), false)
            });

            var expected = new List<UrlRewriteRule>();
            expected.Add(CreateTestRule(condList,
                Url: "^article/([0-9]+)/([_0-9a-z-]+)",
                name: "Rewrite to article.aspx",
                actionType: ActionType.Rewrite,
                pattern: "article.aspx?id={R:1}&amp;title={R:2}"));

            // act
            var res = new FileParser().Parse(new StringReader(xml));

            // assert
            AssertUrlRewriteRuleEquality(res, expected);
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

            var condList = new List<Condition>();
            condList.Add(new Condition
            {
                Input = new InputParser().ParseInputString("{HTTPS}"),
                Match = new RegexMatch(new Regex("^OFF$"), false)
            });

            var expected = new List<UrlRewriteRule>();
            expected.Add(CreateTestRule(condList,
                Url: "^article/([0-9]+)/([_0-9a-z-]+)",
                name: "Rewrite to article.aspx",
                actionType: ActionType.Rewrite,
                pattern: "article.aspx?id={R:1}&amp;title={R:2}"));
            expected.Add(CreateTestRule(condList,
                Url: "^article/([0-9]+)/([_0-9a-z-]+)",
                name: "Rewrite to another article.aspx",
                actionType: ActionType.Rewrite,
                pattern: "article.aspx?id={R:1}&amp;title={R:2}"));

            // act
            var res = new FileParser().Parse(new StringReader(xml));

            // assert
            AssertUrlRewriteRuleEquality(res, expected);
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
                Action = new RewriteAction(RuleTerminiation.Continue, new InputParser().ParseInputString(Url), clearQuery: false),
                Name = name,
                Enabled = enabled,
                InitialMatch = new RegexMatch(new Regex("^OFF$"), false)
                {
                },
                Conditions = new Conditions
                {
                    ConditionList = conditions
                }
            };
        }

        private void AssertUrlRewriteRuleEquality(List<UrlRewriteRule> actual, List<UrlRewriteRule> expected)
        {
            Assert.Equal(actual.Count, expected.Count);
            for (var i = 0; i < actual.Count; i++)
            {
                var r1 = actual[i];
                var r2 = expected[i];

                Assert.Equal(r1.Name, r2.Name);
                Assert.Equal(r1.Enabled, r2.Enabled);

                // TODO conditions, url pattern, initial match regex
                if (r1.Conditions == null)
                {
                    Assert.Equal(r2.Conditions.ConditionList.Count, 0);
                }
                else if (r2.Conditions == null)
                {
                    Assert.Equal(r1.Conditions.ConditionList.Count, 0);
                }
                else
                {
                    Assert.Equal(r1.Conditions.ConditionList.Count, r2.Conditions.ConditionList.Count);
                    for (var j = 0; j < r1.Conditions.ConditionList.Count; j++)
                    {
                        var c1 = r1.Conditions.ConditionList[j];
                        var c2 = r2.Conditions.ConditionList[j];
                        Assert.Equal(c1.Input.PatternSegments.Count, c2.Input.PatternSegments.Count);
                    }
                }

                Assert.Equal(r1.Action.GetType(), r2.Action.GetType());
                Assert.Equal(r1.InitialMatch.GetType(), r2.InitialMatch.GetType());
            }
        }
    }
}