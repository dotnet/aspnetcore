// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyModel;

namespace Microsoft.CodeAnalysis.Razor
{
    public abstract class BaseTagHelperDescriptorProviderTest
    {
        static BaseTagHelperDescriptorProviderTest()
        {
            BaseCompilation = TestCompilation.Create(typeof(ComponentTagHelperDescriptorProviderTest).Assembly);
            CSharpParseOptions = new CSharpParseOptions(LanguageVersion.CSharp7_3);
        }

        protected static Compilation BaseCompilation { get; }

        protected static CSharpParseOptions CSharpParseOptions { get; }

        protected static CSharpSyntaxTree Parse(string text)
        {
            return (CSharpSyntaxTree)CSharpSyntaxTree.ParseText(text, CSharpParseOptions);
        }

        // For simplicity in testing, exclude the built-in components. We'll add more and we
        // don't want to update the tests when that happens.
        protected static TagHelperDescriptor[] ExcludeBuiltInComponents(TagHelperDescriptorProviderContext context)
        {
            return context.Results
                .Where(c => c.GetTypeName() != "Microsoft.AspNetCore.Components.Bind")
                .Where(c => c.GetTypeName() != "Microsoft.AspNetCore.Components.BindAttributes")
                .Where(c => c.GetTypeName() != "Microsoft.AspNetCore.Components.EventHandlers")
                .OrderBy(c => c.Name)
                .ToArray();
        }

    }
}
