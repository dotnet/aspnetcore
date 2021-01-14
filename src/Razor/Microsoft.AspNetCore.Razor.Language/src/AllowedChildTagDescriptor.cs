// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class AllowedChildTagDescriptor : IEquatable<AllowedChildTagDescriptor>
    {
        public string Name { get; protected set; }

        public string DisplayName { get; protected set; }

        public IReadOnlyList<RazorDiagnostic> Diagnostics { get; protected set; }

        public bool HasErrors
        {
            get
            {
                var errors = Diagnostics.Any(diagnostic => diagnostic.Severity == RazorDiagnosticSeverity.Error);

                return errors;
            }
        }

        public override string ToString()
        {
            return DisplayName ?? base.ToString();
        }

        public bool Equals(AllowedChildTagDescriptor other)
        {
            return AllowedChildTagDescriptorComparer.Default.Equals(this, other);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as AllowedChildTagDescriptor);
        }

        public override int GetHashCode()
        {
            return AllowedChildTagDescriptorComparer.Default.GetHashCode(this);
        }
    }
}
