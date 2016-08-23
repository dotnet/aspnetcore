// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.Internal;
using Microsoft.AspNetCore.Rewrite.Internal.UrlRewrite;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.UrlRewrite
{
    // TODO add more of these 
    public class UrlRewriteApplicationTests
    {
        [Fact]
        public void ApplyRule_AssertStopProcessingFlagWillTerminateOnNoAction()
        {
            var xml = new StringReader(@"<rewrite>
                <rules>
                <rule name=""Test"" stopProcessing=""true"">  
                <match url = ""(.*)""/>
                <action type = ""None""/>
                </rule>  
                </rules>
                </rewrite>");
            var rules = new FileParser().Parse(xml);
            
            Assert.Equal(rules.Count, 1);
            var ruleResults = rules.FirstOrDefault().ApplyRule(new RewriteContext {HttpContext = new DefaultHttpContext()});
            Assert.Equal(ruleResults.Result, RuleTerminiation.StopRules);
        }

        [Fact]
        public void ApplyRule_AssertNoTerminateFlagWillNotTerminateOnNoAction()
        {
            var xml = new StringReader(@"<rewrite>
                <rules>
                <rule name=""Test"">
                <match url = ""(.*)"" ignoreCase=""false"" />
                <action type = ""None""/>
                </rule>  
                </rules>
                </rewrite>");
            var rules = new FileParser().Parse(xml);

            Assert.Equal(rules.Count, 1);
            var ruleResults = rules.FirstOrDefault().ApplyRule(new RewriteContext { HttpContext = new DefaultHttpContext() });
            Assert.Equal(ruleResults.Result, RuleTerminiation.Continue);
        }
    }
}
