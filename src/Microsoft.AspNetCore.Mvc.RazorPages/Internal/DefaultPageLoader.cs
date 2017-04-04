// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class DefaultPageLoader : IPageLoader
    {
        private const string ModelPropertyName = "Model";
        
        private readonly RazorCompiler _compiler;

        public DefaultPageLoader(RazorCompiler compiler)
        {
            _compiler = compiler;
        }

        public CompiledPageActionDescriptor Load(PageActionDescriptor actionDescriptor)
        {
            var compilationResult = _compiler.Compile(actionDescriptor.RelativePath);
            var compiledTypeInfo = compilationResult.CompiledType.GetTypeInfo();
            // If a model type wasn't set in code then the model property's type will be the same
            // as the compiled type.
            var modelTypeInfo = compiledTypeInfo.GetProperty(ModelPropertyName)?.PropertyType.GetTypeInfo();
            if (modelTypeInfo == compiledTypeInfo)
            {
                modelTypeInfo = null;
            }

            return new CompiledPageActionDescriptor(actionDescriptor)
            {
                PageTypeInfo = compiledTypeInfo,
                ModelTypeInfo = modelTypeInfo,
            };
        }
    }
}