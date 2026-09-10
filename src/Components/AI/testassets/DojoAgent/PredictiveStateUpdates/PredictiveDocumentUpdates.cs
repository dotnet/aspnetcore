// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace DojoAgent.PredictiveStateUpdates;

internal sealed class PredictiveDocumentUpdates
{
    private string? _lastDocument;
    private bool _hasEmittedConfirmation;

    internal DocumentChange? Create(FunctionCallContent call)
    {
        if (call.Name != "write_document_local" ||
            call.Arguments?.TryGetValue("document", out var value) != true ||
            value?.ToString() is not { } document ||
            document == _lastDocument)
        {
            return null;
        }

        var startIndex = _lastDocument is not null &&
            document.StartsWith(_lastDocument, StringComparison.Ordinal)
                ? _lastDocument.Length
                : 0;
        FunctionCallContent? confirmation = null;
        if (!_hasEmittedConfirmation)
        {
            _hasEmittedConfirmation = true;
            confirmation = new FunctionCallContent(
                Guid.NewGuid().ToString("N"), "confirm_changes", new Dictionary<string, object?>());
        }

        _lastDocument = document;

        return new DocumentChange(GetSnapshots(document, startIndex), confirmation);
    }

    private static IEnumerable<DocumentState> GetSnapshots(string document, int startIndex)
    {
        if (document.Length == 0)
        {
            yield return new DocumentState { Document = "" };
            yield break;
        }

        const int chunkSize = 10;
        for (var index = startIndex; index < document.Length; index += chunkSize)
        {
            yield return new DocumentState { Document = document[..Math.Min(index + chunkSize, document.Length)] };
        }
    }

    internal sealed record DocumentChange(
        IEnumerable<DocumentState> Snapshots,
        FunctionCallContent? Confirmation);
}
