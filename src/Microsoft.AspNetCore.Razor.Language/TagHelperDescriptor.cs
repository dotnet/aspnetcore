// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class TagHelperDescriptor : IEquatable<TagHelperDescriptor>
    {
        private IEnumerable<RazorDiagnostic> _allDiagnostics;

        protected TagHelperDescriptor(string kind)
        {
            Kind = kind;
        }

        public string Kind { get; }

        public string Name { get; protected set; }

        public IEnumerable<TagMatchingRule> TagMatchingRules { get; protected set; }

        public string AssemblyName { get; protected set; }

        public IEnumerable<BoundAttributeDescriptor> BoundAttributes { get; protected set; }

        public IEnumerable<string> AllowedChildTags { get; protected set; }

        public string Documentation { get; protected set; }

        public string DisplayName { get; protected set; }

        public string TagOutputHint { get; protected set; }

        public IReadOnlyList<RazorDiagnostic> Diagnostics { get; protected set; }

        public IReadOnlyDictionary<string, string> Metadata { get; protected set; }

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
                var attributeDiagnostics = BoundAttributes.SelectMany(attribute => attribute.Diagnostics);
                var ruleDiagnostics = TagMatchingRules.SelectMany(rule => rule.GetAllDiagnostics());
                var combinedDiagnostics = attributeDiagnostics.Concat(ruleDiagnostics).Concat(Diagnostics);
                _allDiagnostics = combinedDiagnostics.ToArray();
            }

            return _allDiagnostics;
        }

        public bool Equals(TagHelperDescriptor other)
        {
            return TagHelperDescriptorComparer.Default.Equals(this, other);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TagHelperDescriptor);
        }

        public override int GetHashCode()
        {
            return TagHelperDescriptorComparer.Default.GetHashCode(this);
        }
    }
}
