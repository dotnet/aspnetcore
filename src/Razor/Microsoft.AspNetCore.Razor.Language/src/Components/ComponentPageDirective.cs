// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components
{
    internal class ComponentPageDirective
    {
        public static readonly DirectiveDescriptor Directive = DirectiveDescriptor.CreateDirective(
            "page",
            DirectiveKind.SingleLine,
            builder =>
            {
                builder.AddStringToken(ComponentResources.PageDirective_RouteToken_Name, ComponentResources.PageDirective_RouteToken_Description);
                builder.Usage = DirectiveUsage.FileScopedMultipleOccurring;
                builder.Description = ComponentResources.PageDirective_Description;
            });

        private ComponentPageDirective(string routeTemplate, IntermediateNode directiveNode)
        {
            RouteTemplate = routeTemplate;
            DirectiveNode = directiveNode;
        }

        public string RouteTemplate { get; }

        public IntermediateNode DirectiveNode { get; }

        public static RazorProjectEngineBuilder Register(RazorProjectEngineBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddDirective(Directive, FileKinds.Component, FileKinds.ComponentImport);
            builder.Features.Add(new ComponentPageDirectivePass());
            return builder;
        }
    }
}