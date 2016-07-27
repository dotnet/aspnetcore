// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.Internal.ModRewrite;
using Microsoft.AspNetCore.Rewrite.Internal.ModRewrite.Operands;
using Microsoft.AspNetCore.Rewrite.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.ModRewrite
{
    public class ModRewriteFlagTest
    {
        // Flag tests
        [Fact]
        public void ModRewriteRule_Check403OnForbiddenFlag()
        {
            var context = new RewriteContext { HttpContext = CreateRequest("/", "/hey/hello") };
            var rule = new ModRewriteRule
            {
                InitialRule = new RuleExpression { Operand = new RegexOperand(new Regex("/hey/(.*)")) , Invert = false },
                Transform = ConditionTestStringParser.ParseConditionTestString("/$1"),
                Flags = FlagParser.ParseRuleFlags("[F]")
            };
            var res = rule.ApplyRule(context);
            Assert.True(res.Result == RuleTerminiation.ResponseComplete);
            Assert.True(context.HttpContext.Response.StatusCode == 403);
        }

        [Fact]
        public void ModRewriteRule_Check410OnGoneFlag()
        {
            var context = new RewriteContext { HttpContext = CreateRequest("/", "/hey/hello") };
            var rule = new ModRewriteRule
            {
                InitialRule = new RuleExpression { Operand = new RegexOperand(new Regex("/hey/(.*)")), Invert = false },
                Transform = ConditionTestStringParser.ParseConditionTestString("/$1"),
                Flags = FlagParser.ParseRuleFlags("[G]")
            };
            var res = rule.ApplyRule(context);
            Assert.True(res.Result == RuleTerminiation.ResponseComplete);
            Assert.True(context.HttpContext.Response.StatusCode == 410);
        }

        [Fact]
        public void ModRewriteRule_CheckLastFlag()
        {
            var context = new RewriteContext { HttpContext = CreateRequest("/", "/hey/hello") };
            var rule = new ModRewriteRule
            {
                InitialRule = new RuleExpression { Operand = new RegexOperand(new Regex("/hey/(.*)")), Invert = false },
                Transform = ConditionTestStringParser.ParseConditionTestString("/$1"),
                Flags = FlagParser.ParseRuleFlags("[L]")
            };
            var res = rule.ApplyRule(context);
            Assert.True(res.Result == RuleTerminiation.StopRules);
            Assert.True(context.HttpContext.Request.Path.Equals(new PathString("/hello")));
        }


        [Fact]
        public void ModRewriteRule_CheckRedirectFlag()
        {
            // TODO fix this test.
            var context = new RewriteContext { HttpContext = CreateRequest("/", "/hey/hello") };
            var rule = new ModRewriteRule
            {
                InitialRule = new RuleExpression { Operand = new RegexOperand(new Regex("/hey/(.*)")), Invert = false },
                Transform = ConditionTestStringParser.ParseConditionTestString("/$1"),
                Flags = FlagParser.ParseRuleFlags("[G]")
            };
            var res = rule.ApplyRule(context);
            Assert.True(res.Result == RuleTerminiation.ResponseComplete);
            Assert.True(context.HttpContext.Response.StatusCode == 410);
        }

        private HttpContext CreateRequest(string basePath, string requestPath, string requestQuery = "", string hostName = "")
        {
            HttpContext context = new DefaultHttpContext();
            context.Request.PathBase = new PathString(basePath);
            context.Request.Path = new PathString(requestPath);
            context.Request.QueryString = new QueryString(requestQuery);
            context.Request.Host = new HostString(hostName);
            return context;
        }
    }
}
