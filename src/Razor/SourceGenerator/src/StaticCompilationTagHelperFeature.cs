// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor
{
    internal sealed class StaticCompilationTagHelperFeature : RazorEngineFeatureBase, ITagHelperFeature
    {
        private ITagHelperDescriptorProvider[] _providers;

        public IReadOnlyList<TagHelperDescriptor> GetDescriptors()
        {
            if (Compilation is null)
            {
                return Array.Empty<TagHelperDescriptor>();
            }

            var results = new List<TagHelperDescriptor>();


            var context = TagHelperDescriptorProviderContext.Create(results);
            context.SetCompilation(Compilation);
            context.DiscoveryMode = DiscoveryMode;

            for (var i = 0; i < _providers.Length; i++)
            {
                _providers[i].Execute(context);
            }

            return results;
        }

        public Compilation Compilation { get; set; }

        public TagHelperDiscoveryMode DiscoveryMode { get; set; }

        protected override void OnInitialized()
        {
            _providers = Engine.Features.OfType<ITagHelperDescriptorProvider>().OrderBy(f => f.Order).ToArray();
        }

        internal static bool IsValidCompilation(Compilation compilation)
        {
            var @string = compilation.GetSpecialType(SpecialType.System_String);

            // Do some minimal tests to verify the compilation is valid. If symbols for System.String
            // is missing or errored, the compilation may be missing references.
            return @string != null && @string.TypeKind != TypeKind.Error;
        }
    }
}
