// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Routing.Tree;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.Routing;

internal sealed class RouteContext
{
    public RouteContext(string path)
    {
        Path = path.Contains('%') ? GetDecodedPath(path) : path;

        [SkipLocalsInit]
        static string GetDecodedPath(string path)
        {
            using var uriBuffer = path.Length < 128 ?
                new UriBuffer(stackalloc byte[path.Length]) :
                new UriBuffer(path.Length);

            var utf8Span = uriBuffer.Buffer;

            if (Encoding.UTF8.TryGetBytes(path.AsSpan(), utf8Span, out var written))
            {
                utf8Span = utf8Span[..written];
                var decodedLength = UrlDecoder.DecodeInPlace(utf8Span, isFormEncoding: false);
                utf8Span = utf8Span[..decodedLength];
                path = Encoding.UTF8.GetString(utf8Span);
                return path;
            }

            return path;
        }
    }

    public string Path { get; set; }

    public RouteValueDictionary RouteValues { get; set; } = new();

    public InboundRouteEntry? Entry { get; set; }

    [DynamicallyAccessedMembers(Component)]
    public Type? Handler => Entry?.Handler;

    public IReadOnlyDictionary<string, object?>? Parameters => RouteValues;

    private readonly ref struct UriBuffer
    {
        private readonly byte[]? _pooled;

        public Span<byte> Buffer { get; }

        public UriBuffer(int length)
        {
            _pooled = ArrayPool<byte>.Shared.Rent(length);
            Buffer = _pooled.AsSpan(0, length);
        }

        public UriBuffer(Span<byte> buffer) => Buffer = buffer;

        public void Dispose()
        {
            if (_pooled != null)
            {
                ArrayPool<byte>.Shared.Return(_pooled);
            }
        }
    }
}
