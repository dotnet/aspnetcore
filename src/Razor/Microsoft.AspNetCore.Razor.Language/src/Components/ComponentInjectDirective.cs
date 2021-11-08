// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Components;

// Much of the following is equivalent to Microsoft.AspNetCore.Mvc.Razor.Extensions's InjectDirective,
// but this one outputs properties annotated for Components's property injector, plus it doesn't need to
// support multiple CodeTargets.

internal static class ComponentInjectDirective
{
    public static readonly DirectiveDescriptor Directive = DirectiveDescriptor.CreateDirective(
        "inject",
        DirectiveKind.SingleLine,
        builder =>
        {
            builder.AddTypeToken("TypeName", "The type of the service to inject.");
            builder.AddMemberToken("PropertyName", "The name of the property.");
            builder.Usage = DirectiveUsage.FileScopedMultipleOccurring;
            builder.Description = "Inject a service from the application's service container into a property.";
        });

    public static void Register(RazorProjectEngineBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.AddDirective(Directive, FileKinds.Component, FileKinds.ComponentImport);
        builder.Features.Add(new ComponentInjectDirectivePass());
    }
}
