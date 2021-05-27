// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRequiredAttributeDescriptorBuilder : RequiredAttributeDescriptorBuilder
    {
        private DefaultTagMatchingRuleDescriptorBuilder _parent;
        private RazorDiagnosticCollection _diagnostics;
        private readonly Dictionary<string, string> _metadata = new Dictionary<string, string>();

        public DefaultRequiredAttributeDescriptorBuilder(DefaultTagMatchingRuleDescriptorBuilder parent)
        {
            _parent = parent;
        }

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

        public override IDictionary<string, string> Metadata => _metadata;

        internal bool CaseSensitive => _parent.CaseSensitive;

        public RequiredAttributeDescriptor Build()
        {
            var validationDiagnostics = Validate();
            var diagnostics = new HashSet<RazorDiagnostic>(validationDiagnostics);
            if (_diagnostics != null)
            {
                diagnostics.UnionWith(_diagnostics);
            }

            var displayName = GetDisplayName();
            var rule = new DefaultRequiredAttributeDescriptor(
                Name,
                NameComparisonMode,
                CaseSensitive,
                Value,
                ValueComparisonMode,
                displayName,
                diagnostics.ToArray(),
                new Dictionary<string, string>(Metadata));

            return rule;
        }

        private string GetDisplayName()
        {
            return NameComparisonMode == RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch ? string.Concat(Name, "...") : Name;
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
                var name = Name;
                var isDirectiveAttribute = this.IsDirectiveAttribute();
                if (isDirectiveAttribute && name.StartsWith("@", StringComparison.Ordinal))
                {
                    name = name.Substring(1);
                }
                else if (isDirectiveAttribute)
                {
                    var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidRequiredDirectiveAttributeName(GetDisplayName(), Name);

                    yield return diagnostic;
                }

                foreach (var character in name)
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
