// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using MonoDevelop.Projects;

namespace Microsoft.VisualStudio.Mac.LanguageServices.Razor.ProjectSystem
{
    internal abstract class DotNetProjectHost
    {
        public abstract DotNetProject Project { get; }

        public abstract void Subscribe();
    }
}
