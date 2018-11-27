// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if RAZOR_EXTENSION_DEVELOPER_MODE
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.VisualStudio.RazorExtension.RazorInfo
{
    public class TagHelperViewModel : NotifyPropertyChanged
    {
        private readonly TagHelperDescriptor _descriptor;

        internal TagHelperViewModel(TagHelperDescriptor descriptor)
        {
            _descriptor = descriptor;
        }

        public string AssemblyName => _descriptor.AssemblyName;

        public string TargetElement => string.Join(", ", _descriptor.TagMatchingRules.Select(rule => rule.TagName));

        public string TypeName => _descriptor.GetTypeName();
    }
}
#endif