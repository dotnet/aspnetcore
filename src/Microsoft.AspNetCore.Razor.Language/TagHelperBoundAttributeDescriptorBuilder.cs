// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    public sealed class TagHelperBoundAttributeDescriptorBuilder
    {
        internal const string DescriptorKind = "ITagHelper";
        internal const string PropertyNameKey = "ITagHelper.PropertyName";

        private static readonly IReadOnlyDictionary<string, string> PrimitiveDisplayTypeNameLookups = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [typeof(byte).FullName] = "byte",
            [typeof(sbyte).FullName] = "sbyte",
            [typeof(int).FullName] = "int",
            [typeof(uint).FullName] = "uint",
            [typeof(short).FullName] = "short",
            [typeof(ushort).FullName] = "ushort",
            [typeof(long).FullName] = "long",
            [typeof(ulong).FullName] = "ulong",
            [typeof(float).FullName] = "float",
            [typeof(double).FullName] = "double",
            [typeof(char).FullName] = "char",
            [typeof(bool).FullName] = "bool",
            [typeof(object).FullName] = "object",
            [typeof(string).FullName] = "string",
            [typeof(decimal).FullName] = "decimal",
        };

        private static ICollection<char> InvalidNonWhitespaceAttributeNameCharacters { get; } = new HashSet<char>(
            new[] { '@', '!', '<', '/', '?', '[', '>', ']', '=', '"', '\'', '*' });

        private bool _isEnum;
        private bool _hasIndexer;
        private string _indexerValueTypeName;
        private string _name;
        private string _propertyName;
        private string _typeName;
        private string _documentation;
        private string _indexerNamePrefix;
        private readonly string _containingTypeName;
        private readonly Dictionary<string, string> _metadata;
        private HashSet<RazorDiagnostic> _diagnostics;

        private TagHelperBoundAttributeDescriptorBuilder(string containingTypeName)
        {
            _containingTypeName = containingTypeName;
            _metadata = new Dictionary<string, string>();
        }

        public static TagHelperBoundAttributeDescriptorBuilder Create(string containingTypeName)
        {
            return new TagHelperBoundAttributeDescriptorBuilder(containingTypeName);
        }

        public TagHelperBoundAttributeDescriptorBuilder Name(string name)
        {
            _name = name;

            return this;
        }

        public TagHelperBoundAttributeDescriptorBuilder PropertyName(string propertyName)
        {
            _propertyName = propertyName;

            return this;
        }

        public TagHelperBoundAttributeDescriptorBuilder TypeName(string typeName)
        {
            _typeName = typeName;

            return this;
        }

        public TagHelperBoundAttributeDescriptorBuilder AsEnum()
        {
            _isEnum = true;

            return this;
        }

        public TagHelperBoundAttributeDescriptorBuilder AsDictionary(string attributeNamePrefix, string valueTypeName)
        {
            _indexerNamePrefix = attributeNamePrefix;
            _indexerValueTypeName = valueTypeName;
            _hasIndexer = true;

            return this;
        }

        public TagHelperBoundAttributeDescriptorBuilder Documentation(string documentation)
        {
            _documentation = documentation;

            return this;
        }

        public TagHelperBoundAttributeDescriptorBuilder AddMetadata(string key, string value)
        {
            _metadata[key] = value;

            return this;
        }

        public TagHelperBoundAttributeDescriptorBuilder AddDiagnostic(RazorDiagnostic diagnostic)
        {
            EnsureDiagnostics();
            _diagnostics.Add(diagnostic);

            return this;
        }

        public BoundAttributeDescriptor Build()
        {
            var validationDiagnostics = Validate();
            var diagnostics = new HashSet<RazorDiagnostic>(validationDiagnostics);
            if (_diagnostics != null)
            {
                diagnostics.UnionWith(_diagnostics);
            }

            if (!PrimitiveDisplayTypeNameLookups.TryGetValue(_typeName, out var simpleName))
            {
                simpleName = _typeName;
            }

            var displayName = $"{simpleName} {_containingTypeName}.{_propertyName}";
            var descriptor = new ITagHelperBoundAttributeDescriptor(
                _isEnum,
                _name,
                _propertyName,
                _typeName,
                _indexerNamePrefix,
                _indexerValueTypeName,
                _hasIndexer,
                _documentation,
                displayName,
                _metadata,
                diagnostics);

            return descriptor;
        }

        public void Reset()
        {
            _name = null;
            _propertyName = null;
            _typeName = null;
            _documentation = null;
            _isEnum = false;
            _indexerNamePrefix = null;
            _indexerValueTypeName = null;
            _metadata.Clear();
            _diagnostics?.Clear();
        }

        private IEnumerable<RazorDiagnostic> Validate()
        {
            // data-* attributes are explicitly not implemented by user agents and are not intended for use on
            // the server; therefore it's invalid for TagHelpers to bind to them.
            const string DataDashPrefix = "data-";

            if (string.IsNullOrWhiteSpace(_name))
            {
                if (_indexerNamePrefix == null)
                {
                    var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidBoundAttributeNullOrWhitespace(
                        _containingTypeName,
                        _propertyName);

                    yield return diagnostic;
                }
            }
            else
            {
                if (_name.StartsWith(DataDashPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidBoundAttributeNameStartsWith(
                        _containingTypeName,
                        _propertyName,
                        _name);

                    yield return diagnostic;
                }

                foreach (var character in _name)
                {
                    if (char.IsWhiteSpace(character) || InvalidNonWhitespaceAttributeNameCharacters.Contains(character))
                    {
                        var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidBoundAttributeName(
                            _containingTypeName,
                            _propertyName,
                            _name,
                            character);

                        yield return diagnostic;
                    }
                }
            }

            if (_indexerNamePrefix != null)
            {
                if (_indexerNamePrefix.StartsWith(DataDashPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidBoundAttributePrefixStartsWith(
                        _containingTypeName,
                        _propertyName,
                        _indexerNamePrefix);

                    yield return diagnostic;
                }
                else if (_indexerNamePrefix.Length > 0 && string.IsNullOrWhiteSpace(_indexerNamePrefix))
                {
                    var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidBoundAttributeNullOrWhitespace(
                        _containingTypeName,
                        _propertyName);

                    yield return diagnostic;
                }
                else
                {
                    foreach (var character in _indexerNamePrefix)
                    {
                        if (char.IsWhiteSpace(character) || InvalidNonWhitespaceAttributeNameCharacters.Contains(character))
                        {
                            var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidBoundAttributePrefix(
                                _containingTypeName,
                                _propertyName,
                                _indexerNamePrefix,
                                character);

                            yield return diagnostic;
                        }
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

        private class ITagHelperBoundAttributeDescriptor : BoundAttributeDescriptor
        {
            public ITagHelperBoundAttributeDescriptor(
                bool isEnum,
                string name,
                string propertyName,
                string typeName,
                string dictionaryAttributeNamePrefix,
                string dictionaryValueTypeName,
                bool hasIndexer,
                string documentation,
                string displayName,
                Dictionary<string, string> metadata,
                IEnumerable<RazorDiagnostic> diagnostics) : base(DescriptorKind)
            {
                IsEnum = isEnum;
                IsIndexerStringProperty = dictionaryValueTypeName == typeof(string).FullName || dictionaryValueTypeName == "string";
                IsStringProperty = typeName == typeof(string).FullName || typeName == "string";
                Name = name;
                TypeName = typeName;
                IndexerNamePrefix = dictionaryAttributeNamePrefix;
                IndexerTypeName = dictionaryValueTypeName;
                HasIndexer = hasIndexer;
                Documentation = documentation;
                DisplayName = displayName;
                Diagnostics = new List<RazorDiagnostic>(diagnostics);
                Metadata = new Dictionary<string, string>(metadata)
                {
                    [PropertyNameKey] = propertyName
                };
            }
        }
    }
}
