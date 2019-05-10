// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http.Features
{
    public class RequestBodyPipeFeature : IRequestBodyPipeFeature
    {
        private PipeReader _internalPipeReader;
        private Stream _streamInstanceWhenWrapped;
        private PipeReader _userSetPipeReader;
        private HttpContext _context;

        public RequestBodyPipeFeature(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            _context = context;
        }

        public PipeReader Reader
        {
            get
            {
                if (_userSetPipeReader != null)
                {
                    return _userSetPipeReader;
                }

                if (_internalPipeReader == null ||
                    !ReferenceEquals(_streamInstanceWhenWrapped, _context.Request.Body))
                {
                    _streamInstanceWhenWrapped = _context.Request.Body;
                    _internalPipeReader = PipeReader.Create(_context.Request.Body);

                    _context.Response.OnCompleted((self) => ((RequestBodyPipeFeature)self).Complete(), this);
                }

                return _internalPipeReader;
            }
            set
            {
                _userSetPipeReader = value ?? throw new ArgumentNullException(nameof(value));
                // REVIEW: Should we do this (vvvv) or just get rid of the setter?
                // TODO set the request body Stream to an adapted pipe https://github.com/aspnet/AspNetCore/issues/3971
            }
        }

        private Task Complete()
        {
            if(_internalPipeReader != null && ReferenceEquals(_context.Request.Body, _streamInstanceWhenWrapped))
            {
                _internalPipeReader.Complete();
            }
            return Task.CompletedTask;
        }
    }
}
