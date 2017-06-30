// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class AllowedChildTagDescriptorBuilder
    {
        public abstract string Name { get; set; }

        public abstract string DisplayName { get; set; }

        public abstract RazorDiagnosticCollection Diagnostics { get; }
        
    }
}
