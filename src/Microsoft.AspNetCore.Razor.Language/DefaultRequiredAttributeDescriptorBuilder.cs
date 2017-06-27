// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRequiredAttributeDescriptorBuilder : RequiredAttributeDescriptorBuilder
    {
        private string _name;
        private RequiredAttributeDescriptor.NameComparisonMode _nameComparison;
        private string _value;
        private RequiredAttributeDescriptor.ValueComparisonMode _valueComparison;
        private HashSet<RazorDiagnostic> _diagnostics;

        public override RequiredAttributeDescriptorBuilder Name(string name)
        {
            _name = name;

            return this;
        }

        public override RequiredAttributeDescriptorBuilder NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode nameComparison)
        {
            _nameComparison = nameComparison;

            return this;
        }

        public override RequiredAttributeDescriptorBuilder Value(string value)
        {
            _value = value;

            return this;
        }

        public override RequiredAttributeDescriptorBuilder ValueComparisonMode(RequiredAttributeDescriptor.ValueComparisonMode valueComparison)
        {
            _valueComparison = valueComparison;

            return this;
        }

        public override RequiredAttributeDescriptorBuilder AddDiagnostic(RazorDiagnostic diagnostic)
        {
            EnsureDiagnostics();
            _diagnostics.Add(diagnostic);

            return this;
        }

        public RequiredAttributeDescriptor Build()
        {
            var validationDiagnostics = Validate();
            var diagnostics = new HashSet<RazorDiagnostic>(validationDiagnostics);
            if (_diagnostics != null)
            {
                diagnostics.UnionWith(_diagnostics);
            }

            var displayName = _nameComparison == RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch ? string.Concat(_name, "...") : _name;
            var rule = new DefaultRequiredAttributeDescriptor(
                _name,
                _nameComparison,
                _value,
                _valueComparison,
                displayName,
                diagnostics?.ToArray() ?? Array.Empty<RazorDiagnostic>());

            return rule;
        }

        private IEnumerable<RazorDiagnostic> Validate()
        {
            if (string.IsNullOrWhiteSpace(_name))
            {
                var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidTargetedAttributeNameNullOrWhitespace();

                yield return diagnostic;
            }
            else
            {
                foreach (var character in _name)
                {
                    if (char.IsWhiteSpace(character) || HtmlConventions.InvalidNonWhitespaceHtmlCharacters.Contains(character))
                    {
                        var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidTargetedAttributeName(_name, character);

                        yield return diagnostic;
                    }
                }
            }
        }

        private void EnsureDiagnostics()
        {
            if (_diagnostics == null)
            {
                _diagnostics = new HashSet<RazorDiagnostic>();
            }
        }
    }
}
