// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.Extensions;

namespace Microsoft.AspNetCore.Razor.Language.Components
{
    public static class ComponentExtensions
    {
        public static void Register(RazorProjectEngineBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            FunctionsDirective.Register(builder);
            builder.Features.Add(new ComponentDocumentClassifierPass());
        }
    }
}
