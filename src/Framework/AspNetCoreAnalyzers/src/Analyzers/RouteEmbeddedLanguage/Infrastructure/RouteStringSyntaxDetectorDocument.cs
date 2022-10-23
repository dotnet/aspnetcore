// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;

/// <summary>
/// This type is seperate from <see cref="RouteStringSyntaxDetector"/> to avoid RS1022 warning in analyzer.
/// It doesn't like analyzers referencing types that might use Document.
/// </summary>
internal static class RouteStringSyntaxDetectorDocument
{
    internal static async ValueTask<(bool success, SyntaxToken token, SemanticModel? model)> TryGetStringSyntaxTokenAtPositionAsync(
        Document document, int position, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return default;
        }
        var token = root.FindToken(position);

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
        {
            return default;
        }

        if (!RouteStringSyntaxDetector.IsRouteStringSyntaxToken(token, semanticModel, cancellationToken))
        {
            return default;
        }

        return (true, token, semanticModel);
    }
}
