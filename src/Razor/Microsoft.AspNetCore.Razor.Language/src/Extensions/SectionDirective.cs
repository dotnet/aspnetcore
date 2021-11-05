// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

public static class SectionDirective
{
    public static readonly DirectiveDescriptor Directive = DirectiveDescriptor.CreateDirective(
        SyntaxConstants.CSharp.SectionKeyword,
        DirectiveKind.RazorBlock,
        builder =>
        {
            builder.AddMemberToken(Resources.SectionDirective_NameToken_Name, Resources.SectionDirective_NameToken_Description);
            builder.Description = Resources.SectionDirective_Description;
        });

    public static void Register(RazorProjectEngineBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.AddDirective(Directive, FileKinds.Legacy, FileKinds.Component);
        builder.Features.Add(new SectionDirectivePass());
        builder.AddTargetExtension(new SectionTargetExtension());
    }

    #region Obsolete
    [Obsolete("This method is obsolete and will be removed in a future version.")]
    public static void Register(IRazorEngineBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.AddDirective(Directive);
        builder.Features.Add(new SectionDirectivePass());
        builder.AddTargetExtension(new SectionTargetExtension());
    }
    #endregion
}
