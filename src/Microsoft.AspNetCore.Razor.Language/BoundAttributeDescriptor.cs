// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    /// <summary>
    /// A metadata class describing a tag helper attribute.
    /// </summary>
    [DebuggerDisplay("{DisplayName,nq}")]
    public abstract class BoundAttributeDescriptor : IEquatable<BoundAttributeDescriptor>
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

        public string IndexerNamePrefix { get; protected set; }

        public string TypeName { get; protected set; }

        public string IndexerTypeName { get; protected set; }

        public bool HasIndexer { get; protected set; }

        public string Documentation { get; protected set; }

        public string DisplayName { get; protected set; }

        public IReadOnlyList<RazorDiagnostic> Diagnostics { get; protected set; }

        public IReadOnlyDictionary<string, string> Metadata { get; protected set; }

        public bool HasErrors
        {
            get
            {
                var errors = Diagnostics.Any(diagnostic => diagnostic.Severity == RazorDiagnosticSeverity.Error);

                return errors;
            }
        }

        public bool Equals(BoundAttributeDescriptor other)
        {
            return BoundAttributeDescriptorComparer.Default.Equals(this, other);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as BoundAttributeDescriptor);
        }

        public override int GetHashCode()
        {
            return BoundAttributeDescriptorComparer.Default.GetHashCode(this);
        }
    }
}