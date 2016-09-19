// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    public class ViewComponentTagHelperDescriptorResolver : TagHelperDescriptorResolver
    {
        private readonly ViewComponentTagHelperDescriptorFactory _descriptorFactory;

        public ViewComponentTagHelperDescriptorResolver(
            IViewComponentDescriptorProvider viewComponentDescriptorProvider)
            : base(designTime: false)
        {
            _descriptorFactory = new ViewComponentTagHelperDescriptorFactory(viewComponentDescriptorProvider);
        }

        protected override IEnumerable<TagHelperDescriptor> ResolveDescriptorsInAssembly(
            string assemblyName,
            SourceLocation documentLocation,
            ErrorSink errorSink)
        {
            return _descriptorFactory.CreateDescriptors(assemblyName);
        }
    }
}