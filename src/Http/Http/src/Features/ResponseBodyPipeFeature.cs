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
                    !ReferenceEquals(_streamInstanceWhenWrapped, _context.Response.Body))
                {
                    _streamInstanceWhenWrapped = _context.Response.Body;
                    _internalPipeWriter = PipeWriter.Create(_context.Response.Body);

                    _context.Response.OnCompleted((self) => ((ResponseBodyPipeFeature)self).Complete(), this);
                }

                return _internalPipeWriter;
            }
            set
            {
                _userSetPipeWriter = value ?? throw new ArgumentNullException(nameof(value));
                // REVIEW: Should we do this (vvvv) or just get rid of the setter?
                // TODO set the response body Stream to an adapted pipe https://github.com/aspnet/AspNetCore/issues/3971
            }
        }

        private Task Complete()
        {
            if(_internalPipeWriter != null && ReferenceEquals(_context.Request.Body, _streamInstanceWhenWrapped))
            {
                _internalPipeWriter.Complete();
            }
            return Task.CompletedTask;
        }
    }
}
