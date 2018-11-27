// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if RAZOR_EXTENSION_DEVELOPER_MODE

using System.Linq;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.VisualStudio.RazorExtension.RazorInfo
{
    public class TagHelperItemViewModel : NotifyPropertyChanged
    {
        private readonly TagHelperDescriptor _tagHelper;

        internal TagHelperItemViewModel(TagHelperDescriptor tagHelper)
        {
            _tagHelper = tagHelper;
        }

        public string AssemblyName => _tagHelper.AssemblyName;

        public string TargetElement => string.Join(", ", _tagHelper.TagMatchingRules.Select(rule => rule.TagName));

        public string TypeName => _tagHelper.GetTypeName();
    }
}

#endif