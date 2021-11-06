// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers;

internal readonly struct DeclaredApiResponseMetadata
{
    public static DeclaredApiResponseMetadata ImplicitResponse { get; } =
        new DeclaredApiResponseMetadata(statusCode: 200, attributeData: null, attributeSource: null, @implicit: true, @default: false);

    public static DeclaredApiResponseMetadata ForProducesResponseType(int statusCode, AttributeData attributeData, IMethodSymbol attributeSource)
    {
        return new DeclaredApiResponseMetadata(statusCode, attributeData, attributeSource, @implicit: false, @default: false);
    }

    public static DeclaredApiResponseMetadata ForProducesDefaultResponse(AttributeData attributeData, IMethodSymbol attributeSource)
    {
        return new DeclaredApiResponseMetadata(statusCode: 0, attributeData, attributeSource, @implicit: false, @default: true);
    }

    private DeclaredApiResponseMetadata(
        int statusCode,
        AttributeData? attributeData,
        IMethodSymbol? attributeSource,
        bool @implicit,
        bool @default)
    {
        StatusCode = statusCode;
        Attribute = attributeData;
        AttributeSource = attributeSource;
        IsImplicit = @implicit;
        IsDefault = @default;
    }

    public int StatusCode { get; }

    public AttributeData? Attribute { get; }

    public IMethodSymbol? AttributeSource { get; }

    /// <summary>
    /// <c>True</c> if this <see cref="DeclaredApiResponseMetadata" /> is the implicit 200 associated with an
    /// action specifying no metadata.
    /// </summary>
    public bool IsImplicit { get; }

    /// <summary>
    /// <c>True</c> if this <see cref="DeclaredApiResponseMetadata" /> is from a <c>ProducesDefaultResponseTypeAttribute</c>.
    /// Matches all failure (400 and above) status codes.
    /// </summary>
    public bool IsDefault { get; }

    internal static bool Contains(IList<DeclaredApiResponseMetadata> declaredApiResponseMetadata, ActualApiResponseMetadata actualMetadata)
    {
        return TryGetDeclaredMetadata(declaredApiResponseMetadata, actualMetadata, out _);
    }

    internal static bool TryGetDeclaredMetadata(
        IList<DeclaredApiResponseMetadata> declaredApiResponseMetadata,
        ActualApiResponseMetadata actualMetadata,
        out DeclaredApiResponseMetadata result)
    {
        for (var i = 0; i < declaredApiResponseMetadata.Count; i++)
        {
            var declaredMetadata = declaredApiResponseMetadata[i];

            if (declaredMetadata.Matches(actualMetadata))
            {
                result = declaredMetadata;
                return true;
            }
        }

        result = default;
        return false;
    }

    internal bool Matches(ActualApiResponseMetadata actualMetadata)
    {
        if (actualMetadata.IsDefaultResponse)
        {
            return IsImplicit || StatusCode == 200 || StatusCode == 201;
        }
        else if (actualMetadata.StatusCode == StatusCode)
        {
            return true;
        }
        else if (actualMetadata.StatusCode >= 400 && IsDefault)
        {
            // ProducesDefaultResponse matches any failure code
            return true;
        }

        return false;
    }
}
