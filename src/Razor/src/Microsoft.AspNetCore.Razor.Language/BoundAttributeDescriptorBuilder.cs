// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class BoundAttributeDescriptorBuilder
    {
        public abstract string Name { get; set; }

        public abstract string TypeName { get; set; }

        public abstract bool IsEnum { get; set; }

        public abstract bool IsDictionary { get; set; }

        public abstract string IndexerAttributeNamePrefix { get; set; }

        public abstract string IndexerValueTypeName { get; set; }

        public abstract string Documentation { get; set; }

        public abstract string DisplayName { get; set; }

        public abstract IDictionary<string, string> Metadata { get; }

        public abstract RazorDiagnosticCollection Diagnostics { get; }
    }
}
