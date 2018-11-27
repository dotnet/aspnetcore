// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal abstract class ProjectSnapshot
    {
        public abstract RazorConfiguration Configuration { get; }

        public abstract string FilePath { get; }

        public abstract bool IsInitialized { get; }

        public abstract IReadOnlyList<TagHelperDescriptor> TagHelpers { get; }

        public abstract VersionStamp Version { get; }

        public abstract Project WorkspaceProject { get; }

        public abstract HostProject HostProject { get; }
    }
}