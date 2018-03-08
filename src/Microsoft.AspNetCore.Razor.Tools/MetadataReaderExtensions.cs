// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal static class MetadataReaderExtensions
    {
        internal static AssemblyIdentity GetAssemblyIdentity(this MetadataReader reader)
        {
            if (!reader.IsAssembly)
            {
                throw new BadImageFormatException();
            }

            var definition = reader.GetAssemblyDefinition();

            return CreateAssemblyIdentity(
                reader,
                definition.Version,
                definition.Flags,
                definition.PublicKey,
                definition.Name,
                definition.Culture,
                isReference: false);
        }
        
        internal static AssemblyIdentity[] GetReferencedAssembliesOrThrow(this MetadataReader reader)
        {
            var references = new List<AssemblyIdentity>();

            foreach (var referenceHandle in reader.AssemblyReferences)
            {
                var reference = reader.GetAssemblyReference(referenceHandle);
                references.Add(CreateAssemblyIdentity(
                    reader,
                    reference.Version,
                    reference.Flags,
                    reference.PublicKeyOrToken,
                    reference.Name,
                    reference.Culture,
                    isReference: true));
            }

            return references.ToArray();
        }
        
        private static AssemblyIdentity CreateAssemblyIdentity(
            MetadataReader reader,
            Version version,
            AssemblyFlags flags,
            BlobHandle publicKey,
            StringHandle name,
            StringHandle culture,
            bool isReference)
        {
            var publicKeyOrToken = reader.GetBlobContent(publicKey);
            bool hasPublicKey;

            if (isReference)
            {
                hasPublicKey = (flags & AssemblyFlags.PublicKey) != 0;
            }
            else
            {
                // Assembly definitions never contain a public key token, they only can have a full key or nothing,
                // so the flag AssemblyFlags.PublicKey does not make sense for them and is ignored.
                // See Ecma-335, Partition II Metadata, 22.2 "Assembly : 0x20".
                // This also corresponds to the behavior of the native C# compiler and sn.exe tool.
                hasPublicKey = !publicKeyOrToken.IsEmpty;
            }

            if (publicKeyOrToken.IsEmpty)
            {
                publicKeyOrToken = default;
            }

            return new AssemblyIdentity(
                name: reader.GetString(name),
                version: version,
                cultureName: culture.IsNil ? null : reader.GetString(culture),
                publicKeyOrToken: publicKeyOrToken,
                hasPublicKey: hasPublicKey,
                isRetargetable: (flags & AssemblyFlags.Retargetable) != 0,
                contentType: (AssemblyContentType)((int)(flags & AssemblyFlags.ContentTypeMask) >> 9));
        }
    }
}