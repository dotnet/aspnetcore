// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

/// <summary>
/// This API supports framework infrastructure and is not intended to be used
/// directly from application code.
/// </summary>
public interface IHttpRequestLineHandler
{
    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    void OnStartLine(
        HttpVersionAndMethod versionAndMethod,
        TargetOffsetPathLength targetPath,
        Span<byte> startLine);
}

/// <summary>
/// This API supports framework infrastructure and is not intended to be used
/// directly from application code.
/// </summary>
public struct HttpVersionAndMethod
{
    private ulong _versionAndMethod;

    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    public HttpVersionAndMethod(HttpMethod method, int methodEnd)
    {
        _versionAndMethod = ((ulong)(uint)methodEnd << 32) | ((ulong)method << 8);
    }

    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    public HttpVersion Version
    {
        get => (HttpVersion)(sbyte)(byte)_versionAndMethod;
        set => _versionAndMethod = (_versionAndMethod & ~0xFFul) | (byte)value;
    }

    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    public HttpMethod Method => (HttpMethod)(byte)(_versionAndMethod >> 8);

    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    public int MethodEnd => (int)(uint)(_versionAndMethod >> 32);
}

/// <summary>
/// This API supports framework infrastructure and is not intended to be used
/// directly from application code.
/// </summary>
public readonly struct TargetOffsetPathLength
{
    private readonly ulong _targetOffsetPathLength;

    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TargetOffsetPathLength(int offset, int length, bool isEncoded)
    {
        if (isEncoded)
        {
            length = -length;
        }

        _targetOffsetPathLength = ((ulong)offset << 32) | (uint)length;
    }

    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    public int Offset
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return (int)(_targetOffsetPathLength >> 32);
        }
    }

    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var length = (int)_targetOffsetPathLength;
            if (length < 0)
            {
                length = -length;
            }

            return length;
        }
    }

    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    public bool IsEncoded
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return (int)_targetOffsetPathLength < 0 ? true : false;
        }
    }
}
