// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultBoundAttributeDescriptor : BoundAttributeDescriptor
    {
        public DefaultBoundAttributeDescriptor(
            string kind,
            string name,
            string typeName,
            bool isEnum,
            bool hasIndexer,
            string indexerNamePrefix,
            string indexerTypeName,
            string documentation,
            string displayName,
            Dictionary<string, string> metadata,
            RazorDiagnostic[] diagnostics) 
            : base(kind)
        {
            Name = name;
            TypeName = typeName;
            IsEnum = isEnum;
            HasIndexer = hasIndexer;
            IndexerNamePrefix = indexerNamePrefix;
            IndexerTypeName = indexerTypeName;
            Documentation = documentation;
            DisplayName = displayName;

            Metadata = metadata;
            Diagnostics = diagnostics;

            IsIndexerStringProperty = indexerTypeName == typeof(string).FullName || indexerTypeName == "string";
            IsStringProperty = typeName == typeof(string).FullName || typeName == "string";

            IsIndexerBooleanProperty = indexerTypeName == typeof(bool).FullName || indexerTypeName == "bool";
            IsBooleanProperty = typeName == typeof(bool).FullName || typeName == "bool";
        }
    }
}