// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    internal class HttpResponseStream : WriteOnlyPipeStream
    {
        private readonly HttpResponsePipeWriter _pipeWriter;
        private readonly IHttpBodyControlFeature _bodyControl;

        public HttpResponseStream(IHttpBodyControlFeature bodyControl, HttpResponsePipeWriter pipeWriter)
            : base(pipeWriter)
        {
            _bodyControl = bodyControl;
            _pipeWriter = pipeWriter;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!_bodyControl.AllowSynchronousIO)
            {
                throw new InvalidOperationException(CoreStrings.SynchronousWritesDisallowed);
            }

            base.Write(buffer, offset, count);
        }

        public override void Flush()
        {
            if (!_bodyControl.AllowSynchronousIO)
            {
                throw new InvalidOperationException(CoreStrings.SynchronousWritesDisallowed);
            }

            base.Flush();
        }
    }
}
