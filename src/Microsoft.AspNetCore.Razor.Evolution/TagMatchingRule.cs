// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public abstract class TagMatchingRule
    {
        private IEnumerable<RazorDiagnostic> _allDiagnostics;

        public string TagName { get; protected set; }

        public IEnumerable<RequiredAttributeDescriptor> Attributes { get; protected set; }

        public string ParentTag { get; protected set; }

        public TagStructure TagStructure { get; protected set; }

        public IReadOnlyList<RazorDiagnostic> Diagnostics { get; protected set; }

        public bool HasAnyErrors
        {
            get
            {
                var allDiagnostics = GetAllDiagnostics();
                var anyErrors = allDiagnostics.Any(diagnostic => diagnostic.Severity == RazorDiagnosticSeverity.Error);

                return anyErrors;
            }
        }

        public virtual IEnumerable<RazorDiagnostic> GetAllDiagnostics()
        {
            if (_allDiagnostics == null)
            {
                var attributeDiagnostics = Attributes.SelectMany(attribute => attribute.Diagnostics);
                var combinedDiagnostics = Diagnostics.Concat(attributeDiagnostics);
                _allDiagnostics = combinedDiagnostics.ToArray();
            }

            return _allDiagnostics;
        }
    }
}
