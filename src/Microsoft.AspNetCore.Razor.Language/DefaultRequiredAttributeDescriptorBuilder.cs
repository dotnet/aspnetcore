// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRequiredAttributeDescriptorBuilder : RequiredAttributeDescriptorBuilder
    {
        private RazorDiagnosticCollection _diagnostics;

        public override string Name { get; set; }

        public override RequiredAttributeDescriptor.NameComparisonMode NameComparisonMode { get; set; }

        public override string Value { get; set; }

        public override RequiredAttributeDescriptor.ValueComparisonMode ValueComparisonMode { get; set; }

        public override RazorDiagnosticCollection Diagnostics
        {
            get
            {
                if (_diagnostics == null)
                {
                    _diagnostics = new RazorDiagnosticCollection();
                }

                return _diagnostics;
            }
        }

        public RequiredAttributeDescriptor Build()
        {
            var validationDiagnostics = Validate();
            var diagnostics = new HashSet<RazorDiagnostic>(validationDiagnostics);
            if (_diagnostics != null)
            {
                diagnostics.UnionWith(_diagnostics);
            }

            var displayName = NameComparisonMode == RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch ? string.Concat(Name, "...") : Name;
            var rule = new DefaultRequiredAttributeDescriptor(
                Name,
                NameComparisonMode,
                Value,
                ValueComparisonMode,
                displayName,
                diagnostics?.ToArray() ?? Array.Empty<RazorDiagnostic>());

            return rule;
        }

        private IEnumerable<RazorDiagnostic> Validate()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidTargetedAttributeNameNullOrWhitespace();

                yield return diagnostic;
            }
            else
            {
                foreach (var character in Name)
                {
                    if (char.IsWhiteSpace(character) || HtmlConventions.InvalidNonWhitespaceHtmlCharacters.Contains(character))
                    {
                        var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidTargetedAttributeName(Name, character);

                        yield return diagnostic;
                    }
                }
            }
        }
    }
}
