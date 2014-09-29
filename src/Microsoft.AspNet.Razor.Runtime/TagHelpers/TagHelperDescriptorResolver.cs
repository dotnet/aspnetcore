// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.TagHelpers;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Used to resolve <see cref="TagHelperDescriptor"/>s.
    /// </summary>
    public class TagHelperDescriptorResolver : ITagHelperDescriptorResolver
    {
        private readonly TagHelperTypeResolver _typeResolver;

        // internal for testing
        internal TagHelperDescriptorResolver(TagHelperTypeResolver typeResolver)
        {
            _typeResolver = typeResolver;
        }

        /// <summary>
        /// Instantiates a new instance of the <see cref="TagHelperDescriptorResolver"/> class.
        /// </summary>
        public TagHelperDescriptorResolver()
            : this(new TagHelperTypeResolver())
        {
        }

        /// <inheritdoc />
        public IEnumerable<TagHelperDescriptor> Resolve(string lookupText)
        {
            var lookupStrings = lookupText?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            // Ensure that we have valid lookupStrings to work with. Valid formats are:
            // "assemblyName"
            // "typeName, assemblyName"
            if (string.IsNullOrEmpty(lookupText) ||
                (lookupStrings.Length != 1 && lookupStrings.Length != 2))
            {
                throw new ArgumentException(
                    Resources.FormatTagHelperDescriptorResolver_InvalidTagHelperLookupText(lookupText),
                    nameof(lookupText));
            }

            var tagHelperTypes = ResolveTagHelperTypes(lookupStrings);

            // Convert types to TagHelperDescriptors
            var descriptors = tagHelperTypes.SelectMany(TagHelperDescriptorFactory.CreateDescriptors);

            return descriptors;
        }

        private IEnumerable<Type> ResolveTagHelperTypes(string[] lookupStrings)
        {
            // Grab the assembly name from the lookup text strings. Due to our supported lookupText formats it will 
            // always be the last element provided.
            var assemblyName = lookupStrings.Last().Trim();

            // Resolve valid tag helper types from the assembly.
            var types = _typeResolver.Resolve(assemblyName);

            // Check if the lookupText specifies a type to search for.
            if (lookupStrings.Length == 2)
            {
                // The user provided a type name retrieve the value and trim it.
                var typeName = lookupStrings[0].Trim();

                types = types.Where(type => 
                    string.Equals(type.Namespace + "." + type.Name, typeName, StringComparison.Ordinal));
            }

            return types;
        }
    }
}