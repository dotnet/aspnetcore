// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http.Features
{
    public class ResponseBodyPipeFeature : IResponseBodyPipeFeature
    {
        private PipeWriter _internalPipeWriter;
        private Stream _streamInstanceWhenWrapped;
        private HttpContext _context;

        public ResponseBodyPipeFeature(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            _context = context;
        }

        public PipeWriter Writer
        {
            get
            {
                if (_internalPipeWriter == null ||
                    !ReferenceEquals(_streamInstanceWhenWrapped, _context.Response.Body))
                {
                    _streamInstanceWhenWrapped = _context.Response.Body;
                    _internalPipeWriter = PipeWriter.Create(_context.Response.Body);

                    _context.Response.OnCompleted((self) =>
                    {
                        ((PipeWriter)self).Complete();
                        return Task.CompletedTask;
                    }, _internalPipeWriter);
                }

                return _internalPipeWriter;
            }
        }
    }
}
