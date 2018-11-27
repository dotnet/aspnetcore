// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if RAZOR_EXTENSION_DEVELOPER_MODE

namespace Microsoft.VisualStudio.RazorExtension.RazorInfo
{
    public class PropertyViewModel : NotifyPropertyChanged
    {
        internal PropertyViewModel(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }

        public string Value { get; }
    }
}
#endif