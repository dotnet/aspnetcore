// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    /// <summary>
    /// A metadata class describing a tag helper attribute.
    /// </summary>
    public abstract class BoundAttributeDescriptor
    {
        protected BoundAttributeDescriptor(string kind)
        {
            Kind = kind;
        }

        public string Kind { get; }

        public bool IsIndexerStringProperty { get; protected set; }

        public bool IsEnum { get; protected set; }

        public bool IsStringProperty { get; protected set; }

        public string Name { get; protected set; }

        public string IndexerNamePrefix { get; set; }

        public string TypeName { get; protected set; }

        public string IndexerTypeName { get; protected set; }

        public string Documentation { get; protected set; }

        public string DisplayName { get; protected set; }

        public IReadOnlyList<RazorDiagnostic> Diagnostics { get; protected set; }

        public IReadOnlyDictionary<string, string> Metadata { get; protected set; }

        public bool HasAnyErrors
        {
            get
            {
                var anyErrors = Diagnostics.Any(diagnostic => diagnostic.Severity == RazorDiagnosticSeverity.Error);

                return anyErrors;
            }
        }
    }
}