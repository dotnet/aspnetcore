// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    public static class SectionDirective
    {
        public static readonly DirectiveDescriptor Directive = DirectiveDescriptor.CreateDirective(
            SyntaxConstants.CSharp.SectionKeyword,
            DirectiveKind.RazorBlock,
            builder =>
            {
                builder.AddMemberToken();
                builder.Description = Resources.SectionDirective_Description;
            });

        public static void Register(IRazorEngineBuilder builder)
        {
            builder.AddDirective(Directive);
            builder.Features.Add(new SectionDirectivePass());
            builder.AddTargetExtension(new SectionTargetExtension());
        }
    }
}
