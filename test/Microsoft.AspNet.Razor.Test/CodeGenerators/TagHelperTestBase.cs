// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.CodeGenerators;
using Microsoft.AspNet.Razor.CodeGenerators.Visitors;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.TagHelpers;

namespace Microsoft.AspNet.Razor.Test.Generator
{
    public class TagHelperTestBase : RazorCodeGeneratorTest
    {
        protected void RunTagHelperTest(string testName,
                                        string baseLineName = null,
                                        bool designTimeMode = false,
                                        IEnumerable<TagHelperDescriptor> tagHelperDescriptors = null,
                                        Func<RazorEngineHost, RazorEngineHost> hostConfig = null,
                                        IList<LineMapping> expectedDesignTimePragmas = null,
                                        Action<GeneratorResults> onResults = null)
        {
            RunTest(name: testName,
                    baselineName: baseLineName,
                    designTimeMode: designTimeMode,
                    tabTest: TabTest.NoTabs,
                    templateEngineConfig: (engine) =>
                    {
                        return new TagHelperTemplateEngine(engine, tagHelperDescriptors);
                    },
                    onResults: onResults,
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

            public IEnumerable<TagHelperDescriptor> Resolve(TagHelperDescriptorResolutionContext resolutionContext)
            {
                IEnumerable<TagHelperDescriptor> descriptors = null;

                foreach (var directiveDescriptor in resolutionContext.DirectiveDescriptors)
                {
                    if (directiveDescriptor.DirectiveType == TagHelperDirectiveType.RemoveTagHelper)
                    {
                        // We don't yet support "typeName, assemblyName" for @removeTagHelper in this test class. Will
                        // add that ability and add the corresponding end-to-end test verification in:
                        // https://github.com/aspnet/Razor/issues/222
                        descriptors = null;
                    }
                    else if (directiveDescriptor.DirectiveType == TagHelperDirectiveType.AddTagHelper)
                    {
                        descriptors = _tagHelperDescriptors;
                    }
                }

                return descriptors ?? Enumerable.Empty<TagHelperDescriptor>();
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

            protected internal override CodeGenerator CreateCodeGenerator(CodeGeneratorContext context)
            {
                return Host.DecorateCodeGenerator(new TestCSharpCodeGenerator(context), context);
            }

            protected internal override RazorParser CreateParser(string fileName)
            {
                var parser = base.CreateParser(fileName);

                return new RazorParser(parser.CodeParser,
                                       parser.MarkupParser,
                                       new CustomTagHelperDescriptorResolver(_tagHelperDescriptors));
            }
        }

        protected class TestCSharpCodeGenerator : CodeGenTestCodeGenerator
        {
            public TestCSharpCodeGenerator(CodeGeneratorContext context)
                : base(context)
            {

            }

            protected override CSharpCodeVisitor CreateCSharpCodeVisitor(
                CSharpCodeWriter writer,
                CodeGeneratorContext context)
            {
                var visitor = base.CreateCSharpCodeVisitor(writer, context);
                visitor.TagHelperRenderer = new NoUniqueIdsTagHelperCodeRenderer(visitor, writer, context);
                return visitor;
            }

            private class NoUniqueIdsTagHelperCodeRenderer : CSharpTagHelperCodeRenderer
            {
                public NoUniqueIdsTagHelperCodeRenderer(IChunkVisitor bodyVisitor,
                                                        CSharpCodeWriter writer,
                                                        CodeGeneratorContext context)
                    : base(bodyVisitor, writer, context)
                {

                }

                protected override string GenerateUniqueId()
                {
                    return "test";
                }
            }
        }
    }
}