// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language.IntegrationTests;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language
{
    public static class RazorEngineBuilderExtensions
    {
        public static IRazorEngineBuilder AddTagHelpers(this IRazorEngineBuilder builder, params TagHelperDescriptor[] tagHelpers)
        {
            return AddTagHelpers(builder, (IEnumerable<TagHelperDescriptor>)tagHelpers);
        }

        public static IRazorEngineBuilder AddTagHelpers(this IRazorEngineBuilder builder, IEnumerable<TagHelperDescriptor> tagHelpers)
        {
            var feature = (TestTagHelperFeature)builder.Features.OfType<ITagHelperFeature>().FirstOrDefault();
            if (feature == null)
            {
                feature = new TestTagHelperFeature();
                builder.Features.Add(feature);
            }

            feature.TagHelpers.AddRange(tagHelpers);
            return builder;
        }

        public static IRazorEngineBuilder ConfigureDocumentClassifier(this IRazorEngineBuilder builder)
        {
            var feature = builder.Features.OfType<DefaultDocumentClassifierPassFeature>().FirstOrDefault();
            if (feature == null)
            {
                feature = new DefaultDocumentClassifierPassFeature();
                builder.Features.Add(feature);
            }

            feature.ConfigureNamespace.Clear();
            feature.ConfigureClass.Clear();
            feature.ConfigureMethod.Clear();

            feature.ConfigureNamespace.Add((RazorCodeDocument codeDocument, NamespaceDeclarationIntermediateNode node) =>
            {
                node.Content = "Microsoft.AspNetCore.Razor.Language.IntegrationTests.TestFiles";
            });

            feature.ConfigureClass.Add((RazorCodeDocument codeDocument, ClassDeclarationIntermediateNode node) =>
            {
                node.ClassName = IntegrationTestBase.FileName.Replace('/', '_');
                node.Modifiers.Clear();
                node.Modifiers.Add("public");
            });

            feature.ConfigureMethod.Add((RazorCodeDocument codeDocument, MethodDeclarationIntermediateNode node) =>
            {
                node.Modifiers.Clear();
                node.Modifiers.Add("public");
                node.Modifiers.Add("async");
                node.MethodName = "ExecuteAsync";
                node.ReturnType = typeof(Task).FullName;
            });

            return builder;
        }
    }
}
