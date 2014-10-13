// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.TagHelpers;

namespace Microsoft.AspNet.Razor.Test.Generator
{
    public class TagHelperTestBase : CSharpRazorCodeGeneratorTest
    {
        protected void RunTagHelperTest(string testName,
                                        string baseLineName = null,
                                        bool designTimeMode = false,
                                        IEnumerable<TagHelperDescriptor> tagHelperDescriptors = null,
                                        Func<RazorEngineHost, RazorEngineHost> hostConfig = null,
                                        IList<LineMapping> expectedDesignTimePragmas = null)
        {
            RunTest(name: testName,
                    baselineName: baseLineName,
                    designTimeMode: designTimeMode,
                    tabTest: TabTest.NoTabs,
                    templateEngineConfig: (engine) =>
                    {
                        return new TagHelperTemplateEngine(engine, tagHelperDescriptors);
                    },
                    hostConfig: hostConfig,
                    expectedDesignTimePragmas: expectedDesignTimePragmas);
        }

        private class CustomTagHelperDescriptorResolver : ITagHelperDescriptorResolver
        {
            private IEnumerable<TagHelperDescriptor> _tagHelperDescriptors;

            public CustomTagHelperDescriptorResolver(IEnumerable<TagHelperDescriptor> tagHelperDescriptors)
            {
                _tagHelperDescriptors = tagHelperDescriptors ?? Enumerable.Empty<TagHelperDescriptor>();
            }

            public IEnumerable<TagHelperDescriptor> Resolve(string lookupText)
            {
                return _tagHelperDescriptors;
            }
        }

        private class TagHelperTemplateEngine : RazorTemplateEngine
        {
            private IEnumerable<TagHelperDescriptor> _tagHelperDescriptors;

            public TagHelperTemplateEngine(RazorTemplateEngine engine,
                                           IEnumerable<TagHelperDescriptor> tagHelperDescriptors)
                : base(engine.Host)
            {
                _tagHelperDescriptors = tagHelperDescriptors;
            }

            protected internal override RazorParser CreateParser(string fileName)
            {
                var parser = base.CreateParser(fileName);

                return new RazorParser(parser.CodeParser,
                                       parser.MarkupParser,
                                       new CustomTagHelperDescriptorResolver(_tagHelperDescriptors));
            }
        }
    }
}