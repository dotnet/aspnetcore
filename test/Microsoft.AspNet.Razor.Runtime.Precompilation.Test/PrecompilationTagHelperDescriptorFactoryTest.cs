// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.AspNet.Razor.Test.Internal;
using Xunit;

namespace Microsoft.AspNet.Razor.Runtime.Precompilation
{
    public class PrecompilationTagHelperDescriptorFactoryTest : TagHelperDescriptorFactoryTest
    {
        public override ITypeInfo GetTypeInfo(Type tagHelperType)
        {
            var paths = new[]
            {
                $"TagHelperDescriptorFactoryTagHelpers",
                $"CommonTagHelpers",
            };

            var compilation = CompilationUtility.GetCompilation(paths);
            var typeResolver = new PrecompilationTagHelperTypeResolver(compilation);

            return Assert.Single(typeResolver.GetExportedTypes(CompilationUtility.GeneratedAssemblyName),
                generatedType => string.Equals(generatedType.FullName, tagHelperType.FullName, StringComparison.Ordinal));
        }
    }
}
