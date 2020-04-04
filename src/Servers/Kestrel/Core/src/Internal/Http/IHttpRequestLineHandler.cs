// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    public interface IHttpRequestLineHandler
    {
        void OnStartLine(
            HttpVersionAndMethod versionAndMethod,
            PathOffset pathOffset,
            Span<byte> startLine);
    }

    public struct HttpVersionAndMethod
    {
        private ulong _versionAndMethod;

        public HttpVersionAndMethod(HttpMethod method, int methodEnd)
        {
            _versionAndMethod = ((ulong)(uint)methodEnd << 32) | ((ulong)method << 8);
        }

        public HttpVersion Version
        {
            get => (HttpVersion)(sbyte)(byte)_versionAndMethod;
            set => _versionAndMethod = (_versionAndMethod & ~0xFFul) | (byte)value;
        }

        public HttpMethod Method => (HttpMethod)(byte)(_versionAndMethod >> 8);

        public int MethodEnd => (int)(uint)(_versionAndMethod >> 32);
    }

    public readonly struct PathOffset
    {
        private readonly int _path;

        public PathOffset(int end, bool isEncoded)
        {
            if (isEncoded)
            {
                end = -end;
            }

            _path = end;
        }

        public int End
        {
            get
            {
                var path = _path;
                if (path < 0)
                {
                    path = -path;
                }

                return path;
            }
        }

        public bool IsEncoded => _path < 0 ? true : false;
    }
}
