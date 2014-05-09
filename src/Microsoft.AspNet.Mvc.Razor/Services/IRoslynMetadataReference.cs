// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.Framework.Runtime
{
    [AssemblyNeutral]
    public interface IRoslynMetadataReference : IMetadataReference
    {
        MetadataReference MetadataReference { get; }
    }
}