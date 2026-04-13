// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class CacheComponentTextWriter : TextWriter
{
    private readonly TextWriter _inner;
    private readonly CacheComponentJson _segments = new();
    private readonly StringBuilder _buffer = new();
    private bool _capturing;

    public CacheComponentTextWriter(TextWriter inner, CacheComponentVaryBy varyBy)
    {
        _inner = inner;
        VaryBy = varyBy;
    }

    public CacheComponentVaryBy VaryBy { get; set; }

    public bool IsCapturing => _capturing;

    public override Encoding Encoding => _inner.Encoding;

    public override void Write(char value)
    {
        _inner.Write(value);

        if (_capturing)
        {
            _buffer.Append(value);
        }
    }

    public override void Write(string? value)
    {
        _inner.Write(value);
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

    public void CreateHole(Type componentType, string? renderModeName = null, string? componentKey = null)
    {
        _segments.AddHole(componentType, renderModeName, componentKey);
    }

    public CacheComponentJson StopCapture()
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
