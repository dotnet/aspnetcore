// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor
{
    /// <summary>
    /// Provides access to built-in Razor features that require a reference to <c>Microsoft.CodeAnalysis.CSharp</c>.
    /// </summary>
    public static class CompilerFeatures
    {
        /// <summary>
        /// Registers built-in Razor features that require a reference to <c>Microsoft.CodeAnalysis.CSharp</c>.
        /// </summary>
        /// <param name="builder">The <see cref="RazorProjectEngineBuilder"/>.</param>
        public static void Register(RazorProjectEngineBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            
            if (builder.Configuration.LanguageVersion.CompareTo(RazorLanguageVersion.Version_3_0) >= 0)
            {
                builder.Features.Add(new BindTagHelperDescriptorProvider());
                builder.Features.Add(new ComponentTagHelperDescriptorProvider());
                builder.Features.Add(new EventHandlerTagHelperDescriptorProvider());
                builder.Features.Add(new RefTagHelperDescriptorProvider());
                builder.Features.Add(new KeyTagHelperDescriptorProvider());
                builder.Features.Add(new SplatTagHelperDescriptorProvider());

                builder.Features.Add(new DefaultTypeNameFeature());
            }
        }
    }
}
