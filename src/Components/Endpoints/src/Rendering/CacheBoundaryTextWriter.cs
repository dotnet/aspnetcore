// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class CacheBoundaryTextWriter : TextWriter
{
    private readonly TextWriter _innerWriter;
    private readonly CacheBoundaryJson _segments = new();
    private readonly StringBuilder _buffer = new();
    private bool _capturing;

    public CacheBoundaryTextWriter(TextWriter inner, CacheBoundaryVaryBy varyBy)
    {
        _innerWriter = inner;
        VaryBy = varyBy;
    }

    public CacheBoundaryVaryBy VaryBy { get; set; }

    public bool IsCapturing => _capturing;

    public override Encoding Encoding => _innerWriter.Encoding;

    public override void Write(char value)
    {
        _innerWriter.Write(value);
        if (_capturing)
        {
            _buffer.Append(value);
        }
    }

    public override void Write(string? value)
    {
        _innerWriter.Write(value);
        if (_capturing)
        {
            _buffer.Append(value);
        }
    }

    public void PauseCapture()
    {
        if (_buffer.Length > 0)
        {
            _segments.AddHtml(_buffer.ToString());
            _buffer.Clear();
        }
        _capturing = false;
    }

    public void StartCapture()
    {
        _capturing = true;
    }

    public void CreateHole(Type componentType, string? renderModeName = null, object? componentKey = null)
    {
        _segments.AddHole(componentType, renderModeName, componentKey);
    }

    public CacheBoundaryJson StopCapture()
    {
        _capturing = false;

        if (_buffer.Length > 0)
        {
            _segments.AddHtml(_buffer.ToString());
            _buffer.Clear();
        }
        return _segments;
    }
}
