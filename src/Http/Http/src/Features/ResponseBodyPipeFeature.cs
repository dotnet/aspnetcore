// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Http.Features
{
    public class ResponseBodyPipeFeature : IResponseBodyPipeFeature
    {
        private StreamPipeWriter _internalPipeWriter;
        private PipeWriter _userSetPipeWriter;
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
                if (_userSetPipeWriter != null)
                {
                    return _userSetPipeWriter;
                }

                if (_internalPipeWriter == null ||
                    !object.ReferenceEquals(_internalPipeWriter.InnerStream, _context.Response.Body))
                {
                    _internalPipeWriter = new StreamPipeWriter(_context.Response.Body);
                    _context.Response.RegisterForDispose(_internalPipeWriter);
                }

                return _internalPipeWriter;
            }
            set
            {
                _userSetPipeWriter = value ?? throw new ArgumentNullException(nameof(value));
                // TODO set the response body Stream to an adapted pipe https://github.com/aspnet/AspNetCore/issues/3971
            }
        }
    }
}
