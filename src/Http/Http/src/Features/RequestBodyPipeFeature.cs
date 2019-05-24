// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Http.Features
{
    public class RequestBodyPipeFeature : IRequestBodyPipeFeature
    {
        private StreamPipeReader _internalPipeReader;
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
                    !object.ReferenceEquals(_internalPipeReader.InnerStream, _context.Request.Body))
                {
                    _internalPipeReader = new StreamPipeReader(_context.Request.Body);
                    _context.Response.RegisterForDispose(_internalPipeReader);
                }

                return _internalPipeReader;
            }
            set
            {
                _userSetPipeReader = value ?? throw new ArgumentNullException(nameof(value));
                // TODO set the request body Stream to an adapted pipe https://github.com/aspnet/AspNetCore/issues/3971
            }
        }
    }
}
