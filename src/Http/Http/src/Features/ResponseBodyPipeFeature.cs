// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Http.Features
{
    public class ResponseBodyPipeFeature : IResponseBodyPipeFeature
    {
        private PipeWriter _pipeWriter;
        private HttpContext _context;

        public ResponseBodyPipeFeature(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _context = context;
        }

        public PipeWriter ResponseBodyPipe
        {
            get
            {
                if (_pipeWriter == null ||
                    // If the Response.Body has been updated, recreate the pipeWriter
                    (_pipeWriter is StreamPipeWriter writer && !object.ReferenceEquals(writer.InnerStream, _context.Response.Body)))
                {
                    var streamPipeWriter = new StreamPipeWriter(_context.Response.Body);
                    _pipeWriter = streamPipeWriter;
                    _context.Response.RegisterForDispose(streamPipeWriter);

                }

                return _pipeWriter;
            }
            set
            {
                _pipeWriter = value ?? throw new ArgumentNullException(nameof(value));
            }
        }
    }
}
