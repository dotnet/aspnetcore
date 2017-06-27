// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class BoundAttributeDescriptorBuilder
    {
        public abstract BoundAttributeDescriptorBuilder Name(string name);

        public abstract BoundAttributeDescriptorBuilder PropertyName(string propertyName);

        public abstract BoundAttributeDescriptorBuilder TypeName(string typeName);

        public abstract BoundAttributeDescriptorBuilder AsEnum();

        public abstract BoundAttributeDescriptorBuilder AsDictionary(string attributeNamePrefix, string valueTypeName);

        public abstract BoundAttributeDescriptorBuilder Documentation(string documentation);

        public abstract BoundAttributeDescriptorBuilder AddMetadata(string key, string value);

        public abstract BoundAttributeDescriptorBuilder AddDiagnostic(RazorDiagnostic diagnostic);

        public abstract BoundAttributeDescriptorBuilder DisplayName(string displayName);

    }
}
