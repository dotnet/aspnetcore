// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    internal readonly struct DeclaredApiResponseMetadata
    {
        public DeclaredApiResponseMetadata(int statusCode, AttributeData attributeData, IMethodSymbol convention)
        {
            StatusCode = statusCode;
            Attribute = attributeData;
            Convention = convention;
        }

        public int StatusCode { get; }

        public AttributeData Attribute { get; }

        public IMethodSymbol Convention { get; }
    }
}
