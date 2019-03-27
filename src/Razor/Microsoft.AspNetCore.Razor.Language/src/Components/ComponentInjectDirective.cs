// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Components
{
    // Much of the following is equivalent to Microsoft.AspNetCore.Mvc.Razor.Extensions's InjectDirective,
    // but this one outputs properties annotated for Components's property injector, plus it doesn't need to
    // support multiple CodeTargets.

    internal class ComponentInjectDirective
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
}
