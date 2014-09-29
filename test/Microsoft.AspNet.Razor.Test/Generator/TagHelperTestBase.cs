// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.TagHelpers.Internal;
using Microsoft.AspNet.Razor.TagHelpers;

namespace Microsoft.AspNet.Razor.Test.Generator
{
    public class TagHelperTestBase : CSharpRazorCodeGeneratorTest
    {
        protected void RunTagHelperTest(string testName,
                                        string baseLineName = null,
                                        TagHelperDescriptorProvider tagHelperDescriptorProvider = null,
                                        Func<RazorEngineHost, RazorEngineHost> hostConfig = null)
        {
            RunTest(name: testName,
                    baselineName: baseLineName,
                    templateEngineConfig: (engine) =>
                    {
                        return new TagHelperTemplateEngine(engine, tagHelperDescriptorProvider);
                    },
                    hostConfig: hostConfig);
        }

        private class TagHelperTemplateEngine : RazorTemplateEngine
        {
            private TagHelperDescriptorProvider _tagHelperDescriptorProvider;

            public TagHelperTemplateEngine(RazorTemplateEngine engine, TagHelperDescriptorProvider tagHelperDescriptorProvider)
                : base(engine.Host)
            {
                _tagHelperDescriptorProvider = tagHelperDescriptorProvider;
            }

            protected internal override RazorParser CreateParser()
            {
                var parser = base.CreateParser();
                var tagHelperParseTreeRewriter = new TagHelperParseTreeRewriter(_tagHelperDescriptorProvider);

                for (var i = 0; i < parser.Optimizers.Count; i++)
                {
                    if (parser.Optimizers[i] is TagHelperParseTreeRewriter)
                    {
                        parser.Optimizers[i] = tagHelperParseTreeRewriter;
                        break;
                    }
                }

                return parser;
            }
        }
    }
}