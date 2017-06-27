// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultTagMatchingRuleDescriptorBuilder : TagMatchingRuleDescriptorBuilder
    {
        private string _tagName;
        private string _parentTag;
        private TagStructure _tagStructure;
        private List<DefaultRequiredAttributeDescriptorBuilder> _requiredAttributeBuilders;
        private HashSet<RazorDiagnostic> _diagnostics;

        internal DefaultTagMatchingRuleDescriptorBuilder()
        {
        }

        public override TagMatchingRuleDescriptorBuilder RequireTagName(string tagName)
        {
            _tagName = tagName;

            return this;
        }

        public override TagMatchingRuleDescriptorBuilder RequireParentTag(string parentTag)
        {
            _parentTag = parentTag;

            return this;
        }

        public override TagMatchingRuleDescriptorBuilder RequireTagStructure(TagStructure tagStructure)
        {
            _tagStructure = tagStructure;

            return this;
        }

        public override TagMatchingRuleDescriptorBuilder RequireAttribute(Action<RequiredAttributeDescriptorBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            EnsureRequiredAttributeBuilders();

            var builder = new DefaultRequiredAttributeDescriptorBuilder();
            configure(builder);
            _requiredAttributeBuilders.Add(builder);

            return this;
        }

        public override TagMatchingRuleDescriptorBuilder AddDiagnostic(RazorDiagnostic diagnostic)
        {
            EnsureDiagnostics();
            _diagnostics.Add(diagnostic);

            return this;
        }

        public TagMatchingRuleDescriptor Build()
        {
            var validationDiagnostics = Validate();
            var diagnostics = new HashSet<RazorDiagnostic>(validationDiagnostics);
            if (_diagnostics != null)
            {
                diagnostics.UnionWith(_diagnostics);
            }

            var requiredAttributes = Array.Empty<RequiredAttributeDescriptor>();
            if (_requiredAttributeBuilders != null)
            {
                var requiredAttributeSet = new HashSet<RequiredAttributeDescriptor>(RequiredAttributeDescriptorComparer.Default);
                for (var i = 0; i < _requiredAttributeBuilders.Count; i++)
                {
                    requiredAttributeSet.Add(_requiredAttributeBuilders[i].Build());
                }

                requiredAttributes = requiredAttributeSet.ToArray();
            }

            var rule = new DefaultTagMatchingRuleDescriptor(
                _tagName,
                _parentTag,
                _tagStructure,
                requiredAttributes,
                diagnostics.ToArray());

            return rule;
        }

        private IEnumerable<RazorDiagnostic> Validate()
        {
            if (string.IsNullOrWhiteSpace(_tagName))
            {
                var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidTargetedTagNameNullOrWhitespace();

                yield return diagnostic;
            }
            else if (_tagName != TagHelperMatchingConventions.ElementCatchAllName)
            {
                foreach (var character in _tagName)
                {
                    if (char.IsWhiteSpace(character) || HtmlConventions.InvalidNonWhitespaceHtmlCharacters.Contains(character))
                    {
                        var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidTargetedTagName(_tagName, character);

                        yield return diagnostic;
                    }
                }
            }

            if (_parentTag != null)
            {
                if (string.IsNullOrWhiteSpace(_parentTag))
                {
                    var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidTargetedParentTagNameNullOrWhitespace();

                    AddDiagnostic(diagnostic);
                }
                else
                {
                    foreach (var character in _parentTag)
                    {
                        if (char.IsWhiteSpace(character) || HtmlConventions.InvalidNonWhitespaceHtmlCharacters.Contains(character))
                        {
                            var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidTargetedParentTagName(_parentTag, character);

                            AddDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }

        private void EnsureRequiredAttributeBuilders()
        {
            if (_requiredAttributeBuilders == null)
            {
                _requiredAttributeBuilders = new List<DefaultRequiredAttributeDescriptorBuilder>();
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
