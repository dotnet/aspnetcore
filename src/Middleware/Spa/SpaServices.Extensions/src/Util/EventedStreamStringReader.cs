// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.NodeServices.Util;

/// <summary>
/// Captures the completed-line notifications from a <see cref="EventedStreamReader"/>,
/// combining the data into a single <see cref="string"/>.
/// </summary>
internal sealed class EventedStreamStringReader : IDisposable
{
    private readonly EventedStreamReader _eventedStreamReader;
    private bool _isDisposed;
    private readonly StringBuilder _stringBuilder = new StringBuilder();

    public EventedStreamStringReader(EventedStreamReader eventedStreamReader)
    {
        _eventedStreamReader = eventedStreamReader
            ?? throw new ArgumentNullException(nameof(eventedStreamReader));
        _eventedStreamReader.OnReceivedLine += OnReceivedLine;
    }

    public string ReadAsString() => _stringBuilder.ToString();

    private void OnReceivedLine(string line) => _stringBuilder.AppendLine(line);

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _eventedStreamReader.OnReceivedLine -= OnReceivedLine;
            _isDisposed = true;
        }
    }
}
