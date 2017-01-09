// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.LanguageServices.Razor;

namespace Microsoft.VisualStudio.RazorExtension.RazorInfo
{
    public class AssemblyViewModel : NotifyPropertyChanged
    {
        private readonly RazorEngineAssembly _assembly;

        internal AssemblyViewModel(RazorEngineAssembly assembly)
        {
            _assembly = assembly;

            Name = _assembly.Identity.GetDisplayName();
            FilePath = assembly.FilePath;
        }

        public string Name { get; }

        public string FilePath { get; }
    }
}
