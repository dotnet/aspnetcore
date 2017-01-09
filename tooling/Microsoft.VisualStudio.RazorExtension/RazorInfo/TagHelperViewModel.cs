// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Evolution.Legacy;

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

        public string TargetElement => _descriptor.TagName;

        public string TypeName => _descriptor.TypeName;
    }
}
